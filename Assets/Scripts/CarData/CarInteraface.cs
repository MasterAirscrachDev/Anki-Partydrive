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
    NativeWebSocket.WebSocket ws;
    [SerializeField] TrackGenerator trackGenerator;
    CMS cms;
    CarEntityTracker carEntityTracker;
    [SerializeField] UIManager uiManager;
    bool trackValidated = false;
    public static CarInteraface io;
    public UCarData GetCarFromID(string id){
        for (int i = 0; i < cars.Length; i++)
        { if(cars[i].id == id){return cars[i];} }
        return null;
    }
    
    // Start is called before the first frame update
    void Start() {

        if(io == null){ io = this; } //singleton
        else{ Destroy(this); } //destroy if already exists

        ws = new NativeWebSocket.WebSocket("ws://localhost:7118/");

        ws.OnOpen += () => { 
            Debug.Log("WebSocket connection opened"); 
            FindObjectOfType<UIManager>().ServerConnected(); //show the server connected message
            ApiCallV2(SV_SCAN, ""); //Start scanning for cars
            GetCars();
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
        int fins = uiManager.GetFinishCounter();
        ApiCallV2(SV_TR_START_SCAN, fins);
    }
    
    public void CancelScan(){ ApiCallV2(SV_TR_CANCEL_SCAN, 0); } //idk on this one
    
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
                    carEntityTracker.SetPosition(tracking.carID, tracking.trackIndex, tracking.speed, tracking.offset, tracking.trustLevel);
                    break;
                case EVENT_CAR_DATA:
                    CarData[] carData = JsonConvert.DeserializeObject<CarData[]>(webhookData.Payload.ToString());
                    OnCarData(carData); //update the car data
                    break;
                case EVENT_TR_DATA:
                    Segment[] trackData = JsonConvert.DeserializeObject<Segment[]>(webhookData.Payload.ToString());
                    trackGenerator.Generate(trackData, trackValidated); //generate the track
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
                TTSCall($"Car {c[2]} has connected"); //this is a test, change to use the car name later
            }
        } else if(c[0] == MSG_TR_SCAN_UPDATE){
            bool valid = false;
            if(c[1] != "in-progress"){
                valid = c[1] == "True";
                uiManager.SetIsScanningTrack(false); //set the UI to not scanning
            }
            trackValidated = valid;
            ApiCallV2(SV_GET_TRACK, 0); //request the track data
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
    
    public void TTSCall(string text){ ApiCallV2(SV_TTS, text); }
    
    public void GetCars(){ GetCarInfo(); }
    public void ApiCallV2(string eventType, object data){
        WebhookData webhookData = new WebhookData {
            EventType = eventType,
            Payload = data
        };
        string jsonData = JsonConvert.SerializeObject(webhookData);
        ws.SendText(jsonData);
    }
    void OnCarData(CarData[] cars){
        UCarData[] uCars = new UCarData[cars.Length];
        for (int i = 0; i < cars.Length; i++)
        { uCars[i] = new UCarData(cars[i]); }
        this.cars = uCars;
        Debug.Log("Updated Cars");
        // Define color values in an array
        var colors = new (int, int, int)[] {
            (250, 0, 0), // Red
            (0, 250, 0), // Green
            (0, 0, 250), // Blue
            (250, 250, 0), // Yellow
            (250, 0, 250), // Magenta
            (0, 250, 250), // Cyan
            (250, 250, 250),  // White
            (0, 0, 0)  // Black (not visible on the track) 8 cars = onoh for now
        };
        for(int i = 0; i < cars.Length; i++){
            SetCarColours(uCars[i], colors[i].Item1, colors[i].Item2, colors[i].Item3);
        }
        FindObjectOfType<UIManager>().SetCarsCount(cars.Length);
        for (int i = 0; i < cms.controllers.Count; i++)
        { cms.controllers[i].CheckCarExists(); }
    }
    void GetCarInfo(){
        ApiCallV2(SV_GET_CARS, ""); //get the car data
        Debug.Log("Getting car info");
    }
    public int GetCar(string id){
        for (int i = 0; i < cars.Length; i++)
        { if(cars[i].id == id){return i;} }
        return -1;
    }
    public delegate void LineupCallback(string carID, int remainingCars);
    public event LineupCallback OnLineupEvent;
}
[System.Serializable]
public class UCarData{ //used for unity (bc it cant serialize properties)
    public string name;
    public string id;
    public float offset;
    public int speed;
    public bool charging;
    public bool onTrack;
    public int batteryStatus;
    public UCarData(CarData data){
        name = data.name;
        id = data.id;
        offset = data.offset;
        speed = data.speed;
        charging = data.charging;
        onTrack = data.onTrack;
        batteryStatus = data.batteryStatus;
    }
}