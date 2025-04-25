using UnityEngine;
using System.Collections.Generic;
using System.Collections;   
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
    
    public void CancelScan(){ ApiCallV2(SV_TR_CANCEL_SCAN, 0); } //called from ui
    
    public void ControlCar(UCarData car, int speed, int lane){
        if(car == null || car.charging){ return; }
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
    public void SetCarColoursComplex(UCarData car, LightData[] lights){
        ApiCallV2(SV_CAR_C_LIGHTS, new{carID = car.id, lights = lights});
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
                        int index = GetCarIndex(locationData.carID);
                        if(index != -1){
                            cars[index].offset = locationData.offsetMM;
                            cars[index].speed = locationData.speedMMPS;
                        }
                    } catch (Exception e) {
                        Debug.LogError($"Error processing car position update: {e.Message}");
                    }
                    break;
                case EVENT_CAR_TRACKING_UPDATE:  
                    CarLocationData tracking = JsonConvert.DeserializeObject<CarLocationData>(webhookData.Payload.ToString());
                    carEntityTracker.SetPosition(tracking.carID, tracking.trackIndex, tracking.speedMMPS, tracking.offsetMM, tracking.trust);
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
            int index = GetCarIndex(carID);
            if(index != -1){
                cars[index].speed = speed;
                carEntityTracker.SetSpeed(cars[index].id, speed);
            }
        } else if(c[0] == MSG_CAR_POWERUP){ //powerup
            string carID = c[1];
            int index = GetCarIndex(carID);
            if(index != -1){
                
            }
        } else if(c[0] == MSG_CAR_DISCONNECTED){ //disconnected
            GetCarInfo();
            ApiCallV2(SV_SCAN, 0); //Start scanning for cars
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
        LightData[] colors = new LightData[3];
        //white lights
        //colors[0] = new LightData{ channel = LightChannel.RED, effect = LightEffect.STEADY, startStrength = 100, endStrength = 0, cyclesPer10Seconds = 0 };
        //colors[1] = new LightData{ channel = LightChannel.GREEN, effect = LightEffect.STEADY, startStrength = 100, endStrength = 0, cyclesPer10Seconds = 0 };
        //colors[2] = new LightData{ channel = LightChannel.BLUE, effect = LightEffect.STEADY, startStrength = 100, endStrength = 0, cyclesPer10Seconds = 0 };

        //Partylights
        colors[0] = new LightData{ channel = LightChannel.RED, effect = LightEffect.THROB, startStrength = 20, endStrength = 0, cyclesPer10Seconds = 6 };
        colors[1] = new LightData{ channel = LightChannel.GREEN, effect = LightEffect.THROB, startStrength = 20, endStrength = 0, cyclesPer10Seconds = 5 };
        colors[2] = new LightData{ channel = LightChannel.BLUE, effect = LightEffect.THROB, startStrength = 20, endStrength = 0, cyclesPer10Seconds = 4 };
        for(int i = 0; i < cars.Length; i++){ StartCoroutine(SendLightsDelayed(uCars[i], colors, i * 0.05f));  } //send the lights with a delay
        FindObjectOfType<UIManager>().SetCarsCount(cars.Length);
        for (int i = 0; i < cms.controllers.Count; i++)
        { cms.controllers[i].CheckCarExists(); }
    }
    IEnumerator SendLightsDelayed(UCarData car, LightData[] lights, float delay){
        yield return new WaitForSeconds(delay);
        SetCarColoursComplex(car, lights);
    }
    void GetCarInfo(){
        ApiCallV2(SV_GET_CARS, 0); //get the car data
    }
    public int GetCarIndex(string id){
        if(cars == null){ return -1; } //if cars is null, return -1 
        for (int i = 0; i < cars.Length; i++)
        { if(cars[i].id == id){return i;} }
        return -1;
    }
    public delegate void LineupCallback(string carID, int remainingCars);
    public event LineupCallback OnLineupEvent;

    void OnApplicationQuit() {
        ApiCallV2(SV_CLIENT_CLOSED, 0); //send the client closed message
        ws.Close();
    }
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
    public ModelName modelName; //used for the car model
    public UCarData(CarData data){
        name = data.name;
        id = data.id;
        offset = data.offsetMM;
        speed = data.speedMMPS;
        charging = data.charging;
        onTrack = data.onTrack;
        batteryStatus = data.batteryStatus;
        modelName = (ModelName)data.model; //used for the car model
    }
}