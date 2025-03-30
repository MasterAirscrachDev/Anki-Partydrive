using UnityEngine;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System;

public class CarInteraface : MonoBehaviour
{
    public bool connected = false;
    public UCarData[] cars;
    HttpClient client = new HttpClient();
    NativeWebSocket.WebSocket ws;
    public TimeTrialMode timeTrialMode;
    [SerializeField] TrackGenerator trackGenerator;
    CMS cms;
    CarEntityTracker carEntityTracker;
    [SerializeField] UIManager uiManager;
    string scanningCar;
    int DEBUG_SPEED = 400, DEBUG_LANE = 0;
    
    public UCarData GetCarFromID(string id){
        for (int i = 0; i < cars.Length; i++)
        { if(cars[i].id == id){return cars[i];} }
        return null;
    }
    
    // Start is called before the first frame update
    void Start() {
        client.BaseAddress = new Uri("http://localhost:7117/");
        ws = new NativeWebSocket.WebSocket("ws://localhost:7118/");

        ws.OnOpen += () => { Debug.Log("WebSocket connection opened"); };
        ws.OnError += (e) => { Debug.Log($"WebSocket error: {e}"); }; //Unable to connect to the remote server (if server missing)
        ws.OnClose += (e) => { Debug.Log($"WebSocket connection closed: {e}"); ReconnectToServer(); };
        ws.OnMessage += (bytes) => { ProcessWebhookData(System.Text.Encoding.UTF8.GetString(bytes)); };

        ws.Connect();

        cms = FindObjectOfType<CMS>();
        carEntityTracker = GetComponent<CarEntityTracker>();
        ReconnectToServer();
    }

    void Update(){
        #if !UNITY_WEBGL || UNITY_EDITOR
            ws.DispatchMessageQueue();
        #endif
    }

    void OnDestroy() { ws.Close(); }

    public void ScanTrack(){ MapTrack(); } //for UI button
    
    async Task MapTrack(){
        //get the first car that isnt on charge
        int index = 0;
        while(cars[index].charging){ index++; }
        int fins = FindObjectOfType<UIManager>().GetFinishCounter();
        scanningCar = cars[index].id;
        ApiCall($"scantrack/{cars[index].id}");
    }
    
    public void CancelScan(){ CancelTrackMap(); } //idk on this one
    
    async Task CancelTrackMap(){
        if(scanningCar == null){ return; }
        ApiCall($"canceltrackscan/{scanningCar}");
        scanningCar = null;
    }
    
    public void ControlCar(UCarData car, int speed, int lane){
        if(car.charging){ return; }
        //ApiCall($"controlcar/{car.id}:{speed}:{lane}");
        WebhookData data = new WebhookData{
            EventType = NetDefinitions.EVENT_CAR_MOVE,
            Payload = $"{car.id}:{speed}:{lane}"
        };

        string jsonData = JsonConvert.SerializeObject(data);
        ws.SendText(jsonData);
        carEntityTracker.SetSpeedAndLane(car.id, speed, lane);
    }
    
    public void SetCarColours(CarData car, float r, float g, float b){
        ApiCall($"setlights/{car.id}:{r}:{g}:{b}");
    }
    
    async Task<bool> SetupListener(){
        // Perform initial scan and car data fetch
        ApiCall("scan", false);
        GetCars();
        connected = true;
        return true;
    }
    
    void ProcessWebhookData(string jsonData) {
        try {
            // Parse the webhook data
            var webhookData = JsonConvert.DeserializeObject<WebhookData>(jsonData);
            switch (webhookData.EventType) {
                case NetDefinitions.EVENT_SYSTEM_LOG:
                    string[] ignore = new string[]{"[39]", "[77]", "[83]", "[41]", "[54]", "[45]"};
                    string message = webhookData.Payload.ToString();
                    //if message starts with any of the ignore strings, ignore it
                    foreach (string s in ignore){
                        if(message.StartsWith(s)){
                            return;
                        }
                    }
                    Debug.Log($"{message}");
                    break;

                case NetDefinitions.EVENT_UTILITY_LOG:
                    string[] c = webhookData.Payload.ToString().Split(':');
                    //Debug.Log($"C0 = {c[0]}");
                    if (c[0] == "-1" || c[0] == "-2"){
                        GetCarInfo();
                        if(c[0] == "-1"){
                            ApiCall($"tts/Car {c[2]} has connected");
                        }
                    } else if(c[0] == "-3"){
                        bool valid = false;
                        if(c[2] != "in-progress"){
                            valid = c[2] == "True";
                            uiManager.SetIsScanningTrack(false); //set the UI to not scanning
                        }
                        GetTrackAndGenerate(valid);
                    } else if(c[0] == "27"){
                        int battery = int.Parse(c[2]);
                        int index = GetCar(c[1]);
                        if(index != -1){
                            cars[index].battery = battery;
                        }
                    } else if(c[0] == "43"){ 
                        carEntityTracker.CarDelocalised(c[1]);

                    } else if(c[0] == "63"){ //charging status
                        int index = GetCar(c[1]);
                        if(index != -1){
                            cars[index].charging = c[2] == "True";
                        }
                    }
                    break;
                case NetDefinitions.EVENT_CAR_LOCATION:
                    try {
                        LocationData locationData = JsonConvert.DeserializeObject<LocationData>(webhookData.Payload.ToString());
                        int index = GetCar(locationData.carID);
                        if(index != -1){
                            cars[index].trackPosition = locationData.trackLocation;
                            cars[index].trackID = locationData.trackID;
                            cars[index].laneOffset = locationData.offset;
                            cars[index].speed = locationData.speed;
                        }
                    } catch (Exception e) {
                        Debug.LogError($"Error processing car position update: {e.Message}");
                    }
                    break;
                case NetDefinitions.EVENT_CAR_TRACKING_UPDATE:  
                    CarLocationData tracking = JsonConvert.DeserializeObject<CarLocationData>(webhookData.Payload.ToString());
                    carEntityTracker.SetPosition(tracking.carID, tracking.trackIndex, tracking.speed, tracking.offset, tracking.positionTrusted);
                    break;
            
            }
        } catch (Exception e) {
            Debug.LogError($"Error parsing webhook data: {e.Message}\n{e.StackTrace}\nData: {jsonData}");
        }
    }
    
    async Task ReconnectToServer(){
        connected = false;
        bool reconnnected = false;
        while(!reconnnected && Application.isPlaying){
            reconnnected = await SetupListener();
            await Task.Delay(3000);
        }
    }
    
    public void TTSCall(string text){
        ApiCall($"tts/{text}", false, true);
    }
    
    async Task GetTrackAndGenerate(bool validated){
        var track = await client.GetAsync("track");
        string trackString = await track.Content.ReadAsStringAsync();
        try{
            TrackPiece[] trackPieces = JsonConvert.DeserializeObject<TrackPiece[]>(trackString);
            trackGenerator.Generate(trackPieces, validated);
        }catch(System.Exception e){
            Debug.LogError($"Error parsing track data: {e.Message}");
            return;
        }
    }
    
    public void Call(string call){ //used by UI buttons
        ApiCall(call);
    }
    
    public void GetCars(){
        GetCarInfo();
    }
    
    async Task ApiCall(string call, bool printResult = true, bool safe = false){
        if(safe){call = call.Replace(" ", "%20"); }
        var response = await client.GetAsync(call);
        string responseString = await response.Content.ReadAsStringAsync();
        if(printResult){ Debug.Log(responseString); }
    }
    public void ApiCallV2(string eventType, object data){
        WebhookData webhookData = new WebhookData {
            EventType = eventType,
            Payload = data
        };
        string jsonData = JsonConvert.SerializeObject(webhookData);
        ws.SendText(jsonData);
    }
    
    async Task GetCarInfo(){
        Debug.Log("Getting car info");
        var response = await client.GetAsync("cars");
        string responseString = await response.Content.ReadAsStringAsync();
        CarData[] cars = JsonConvert.DeserializeObject<CarData[]>(responseString);
        UCarData[] uCars = new UCarData[cars.Length];
        for (int i = 0; i < cars.Length; i++)
        { uCars[i] = new UCarData(cars[i]); }
        this.cars = uCars;
        Debug.Log("Updated Cars");
        // Define color values in an array
        var colors = new (float, float, float)[] {
            (1, 0, 0), // Red
            (0, 1, 0), // Green
            (0, 0, 1), // Blue
            (1, 1, 0), // Yellow
            (1, 0, 1), // Magenta
            (0, 1, 1), // Cyan
            (1, 1, 1),  // White
            (0, 0, 0)  // Black (not visible on the track) 8 cars = onoh for now
        };
        for(int i = 0; i < cars.Length; i++){
            SetCarColours(cars[i], colors[i].Item1, colors[i].Item2, colors[i].Item3);
        }
        ApiCall("batteries");
        FindObjectOfType<UIManager>().SetCarsCount(cars.Length);
        for (int i = 0; i < cms.controllers.Count; i++)
        { cms.controllers[i].CheckCarExists(); }
    }
    
    public int GetCar(string id){
        for (int i = 0; i < cars.Length; i++)
        { if(cars[i].id == id){return i;} }
        return -1;
    }

    public void DEBUGSetCarsSpeed(int speed){
        DEBUG_SPEED = speed;
        DEBUGUpdate();
    }
    public void DEBUGSetCarsLane(int lane){
        DEBUG_LANE = lane;
        DEBUGUpdate();
    }
    void DEBUGUpdate(){
        for (int i = 0; i < cars.Length; i++)
        { ControlCar(cars[i], DEBUG_SPEED, DEBUG_LANE); }
    }

    // Classes to deserialize webhook data
    [System.Serializable]
    private class WebhookData {
        public string EventType { get; set; }
        public dynamic Payload { get; set; }
    }
}