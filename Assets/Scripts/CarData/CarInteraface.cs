using UnityEngine;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System;
using static OverdriveServer.NetStructures;
using static OverdriveServer.NetStructures.UtilityMessages;

public class CarInteraface : MonoBehaviour
{
    public UCarData[] cars;
    HttpClient client = new HttpClient();
    NativeWebSocket.WebSocket ws;
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

        ws.OnOpen += () => { 
            Debug.Log("WebSocket connection opened"); 
            FindObjectOfType<UIManager>().ServerConnected(); //show the server connected message
        };
        ws.OnError += (e) => { 
            Debug.Log($"WebSocket error: {e}"); 
            if(e.ToString() == "Unable to connect to the remote server" && !Application.isEditor){
                //
                FindObjectOfType<UIManager>().NoServerWarning(); //show the no server warning
            }
        }; //Unable to connect to the remote server (if server missing)
        ws.OnClose += (e) => { Debug.Log($"WebSocket connection closed: {e}"); };
        ws.OnMessage += (bytes) => { ProcessWebhookData(System.Text.Encoding.UTF8.GetString(bytes)); };

        ws.Connect();

        cms = FindObjectOfType<CMS>();
        carEntityTracker = GetComponent<CarEntityTracker>();
        ApiCall("scan", false);
        GetCars();
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
        WebhookData data = new WebhookData{
            EventType = SV_CAR_MOVE,
            Payload = $"{car.id}:{speed}:{lane}"
        };
        string jsonData = JsonConvert.SerializeObject(data);
        ws.SendText(jsonData);
        carEntityTracker.SetSpeed(car.id, speed);
        carEntityTracker.SetOffset(car.id, lane);
    }
    
    public void SetCarColours(UCarData car, int r, int g, int b){
        ApiCallV2(SV_CAR_S_LIGHTS, $"{car.id}:{r}:{g}:{b}");
    }
    
    void ProcessWebhookData(string jsonData) {
        try {
            // Parse the webhook data
            var webhookData = JsonConvert.DeserializeObject<WebhookData>(jsonData);
            switch (webhookData.EventType) {
                case EVENT_SYSTEM_LOG:
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

                case EVENT_UTILITY_LOG:
                    UtilLog(webhookData.Payload.ToString());
                    break;
                case EVENT_CAR_LOCATION:
                    try {
                        LocationData locationData = JsonConvert.DeserializeObject<LocationData>(webhookData.Payload.ToString());
                        int index = GetCar(locationData.carID);
                        if(index != -1){
                            cars[index].offset = locationData.offset;
                            cars[index].speed = locationData.speed;
                        }
                    } catch (Exception e) {
                        Debug.LogError($"Error processing car position update: {e.Message}");
                    }
                    break;
                case EVENT_CAR_TRACKING_UPDATE:  
                    CarLocationData tracking = JsonConvert.DeserializeObject<CarLocationData>(webhookData.Payload.ToString());
                    carEntityTracker.SetPosition(tracking.carID, tracking.trackIndex, tracking.speed, tracking.offset, tracking.positionTrusted);
                    break;
            
            }
        } catch (Exception e) {
            Debug.LogError($"Error parsing webhook data: {e.Message}\n{e.StackTrace}\nData: {jsonData}");
        }
    }
    void UtilLog(string message){
        string[] c = message.Split(':');
        if (c[0] == MSG_CAR_CONNECTED){
            GetCarInfo();
            if(c[0] == "-1"){
                ApiCall($"tts/Car {c[2]} has connected");
            }
        } else if(c[0] == MSG_TR_SCAN_UPDATE){
            bool valid = false;
            if(c[2] != "in-progress"){
                valid = c[2] == "True";
                uiManager.SetIsScanningTrack(false); //set the UI to not scanning
            }
            GetTrackAndGenerate(valid);
        } else if(c[0] == MSG_CAR_DELOCALIZED){ 
            carEntityTracker.CarDelocalised(c[1]);
        } else if(c[0] == MSG_CAR_STATUS_UPDATE){ // status
            GetCarInfo();
        } else if(c[0] == MSG_CAR_SPEED_UPDATE){
            string carID = c[1];
            int speed = int.Parse(c[2]);
            int trueSpeed = int.Parse(c[3]);
            int index = GetCar(carID);
            if(index != -1){
                cars[index].speed = speed;
                carEntityTracker.SetSpeed(cars[index].id, speed);
            }
        } else if(c[0] == MSG_CAR_POWERUP){ //powerup
            string carID = c[1];
            int index = GetCar(carID);
            if(index != -1){
                
            }
        } else if(c[0] == MSG_CAR_DISCONNECTED){ //disconnected
            GetCarInfo();
        } else if(c[0] == MSG_LINEUP){
            string carID = c[1];
            int remainingCars = int.Parse(c[2]);
            OnLineupEvent?.Invoke(carID, remainingCars);
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
        var colors = new (int, int, int)[] {
            (255, 0, 0), // Red
            (0, 255, 0), // Green
            (0, 0, 255), // Blue
            (255, 255, 0), // Yellow
            (255, 0, 255), // Magenta
            (0, 255, 255), // Cyan
            (255, 255, 255),  // White
            (0, 0, 0)  // Black (not visible on the track) 8 cars = onoh for now
        };
        for(int i = 0; i < cars.Length; i++){
            SetCarColours(uCars[i], colors[i].Item1, colors[i].Item2, colors[i].Item3);
        }
        //ApiCall("batteries");
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

    public delegate void LineupCallback(string carID, int remainingCars);
    public event LineupCallback OnLineupEvent;
}
[System.Serializable]
public class UCarData{
    public string name;
    public string id;
    public float offset;
    public int speed;
    public int battery;
    public bool charging;
    public bool onTrack;
    public int batteryStatus;
    public UCarData(CarData data){
        name = data.name;
        id = data.id;
        offset = data.offset;
        speed = data.speed;
        battery = data.battery;
        charging = data.charging;
        onTrack = data.onTrack;
        batteryStatus = data.batteryStatus;
    }
}