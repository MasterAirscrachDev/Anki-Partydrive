using UnityEngine;
using System.Collections.Generic;
using System.Collections;   
using System.Threading.Tasks;
using Newtonsoft.Json;
using System;
using static OverdriveServer.NetStructures;
using static OverdriveServer.NetStructures.UtilityMessages;
using System.Linq;

public class CarInteraface : MonoBehaviour
{
    public UCarData[] cars;
    public UCarAvailable[] availableCars;
    NativeWebSocket.WebSocket ws;
    bool trackValidated = false;
    public UCarData GetCarFromID(string id){
        for (int i = 0; i < cars.Length; i++)
        { if(cars[i].id == id){return cars[i];} }
        return null;
    }
    
    // Start is called before the first frame update
    void Start() {

        ws = new NativeWebSocket.WebSocket("ws://localhost:7118/");

        ws.OnOpen += () => { 
            Debug.Log("WebSocket connection opened"); 
            SR.ui.ServerConnected(); //show the server connected message
            GetCars();
            RefreshAvailableCars(); //Also get available cars
        };
        ws.OnError += (e) => { 
            Debug.Log($"WebSocket error: {e}"); 
            if(e.ToString() == "Unable to connect to the remote server" && !Application.isEditor){
                SR.ui.NoServerWarning(); //show the no server warning
            }
        }; //Unable to connect to the remote server (if server missing)
        ws.OnClose += (e) => { Debug.Log($"WebSocket connection closed: {e}"); };
        ws.OnMessage += (bytes) => { ProcessWebhookData(System.Text.Encoding.UTF8.GetString(bytes)); };

        ws.Connect();
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
        int fins = SR.ui.GetFinishCounter();
        SR.ui.SetScanningStatusText("Finding Finish...");
        
        // Clear all existing car entities when starting a new track scan
        SR.cet.ClearAllCars();
        
        ApiCallV2(SV_TR_START_SCAN, fins);
    }
    public void CancelTrackScan(){ ApiCallV2(SV_TR_CANCEL_SCAN, 0); } //called from ui
    public void ControlCar(UCarData car, int speed, int lane){
        if(car == null || car.charging){ return; }
        WebhookData data = new WebhookData{
            EventType = SV_CAR_MOVE,
            Payload = $"{car.id}:{speed}:{lane}"
        };
        string jsonData = JsonConvert.SerializeObject(data);
        ws.SendText(jsonData);
        SR.cet.SetSpeed(car.id, speed);
        SR.cet.SetOffset(car.id, lane);
    }
    
    public void SetCarColours(UCarData car, int r, int g, int b){ // 0-14
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
                    return;
                    string[] ignore = new string[]{"[39]", "[77]", "[83]", "[41]", "[54]", "[45]", "[13]"};
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
                    SR.cet.SetPosition(tracking.carID, tracking.trackIndex, tracking.speedMMPS, tracking.offsetMM, tracking.trust);
                    break;
                case EVENT_CAR_DATA:
                    CarData[] carData = JsonConvert.DeserializeObject<CarData[]>(webhookData.Payload.ToString());
                    OnCarData(carData); //update the car data
                    break;
                case EVENT_AVAILABLE_CARS:
                    AvailableCarData[] availableCarData = JsonConvert.DeserializeObject<AvailableCarData[]>(webhookData.Payload.ToString());
                    OnAvailableCarData(availableCarData); //update the available car data
                    break;
                case EVENT_TR_DATA:
                    Segment[] trackData = JsonConvert.DeserializeObject<Segment[]>(webhookData.Payload.ToString());
                    SR.track.Generate(trackData, trackValidated); //generate the track
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
        } else if(c[0] == MSG_TR_SCAN_UPDATE){
            bool valid = false;
            if(c[1] != "in-progress"){
                valid = c[1] == "True";
                SR.ui.SetIsScanningTrack(false); //set the UI to not scanning
            }
            trackValidated = valid;
            ApiCallV2(SV_GET_TRACK, 0); //request the track data
        } else if(c[0] == MSG_CAR_DELOCALIZED){ 
            SR.cet.CarDelocalised(c[1]);
        } else if(c[0] == MSG_CAR_STATUS_UPDATE){ // status
            GetCarInfo();
        } else if(c[0] == MSG_CAR_SPEED_UPDATE){
            string carID = c[1];
            int speed = int.Parse(c[2]);
            int trueSpeed = int.Parse(c[3]);
            int index = GetCarIndex(carID);
            if(index != -1){
                cars[index].speed = speed;
                SR.cet.SetSpeed(cars[index].id, speed);
            }
        } else if(c[0] == MSG_CAR_POWERUP){ //powerup
            string carID = c[1];
            int index = GetCarIndex(carID);
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
    public void GetCars(){ GetCarInfo(); }
    public void RefreshAvailableCars(){ 
        ApiCallV2(SV_GET_AVAILABLE_CARS, 0); //get the available car data
    }
    
    // Connect to a car by ID
    public void ConnectCar(string carID){
        ApiCallV2(SV_CONNECT_CAR, carID);
        Debug.Log($"Sending connection request for car: {carID}");
    }
    
    // Disconnect from a car by ID  
    public void DisconnectCar(string carID){
        ApiCallV2(SV_DISCONNECT_CAR, carID);
        Debug.Log($"Sending disconnection request for car: {carID}");
    }
    
    public void ApiCallV2(string eventType, object data){
        WebhookData webhookData = new WebhookData {
            EventType = eventType,
            Payload = data
        };
        string jsonData = JsonConvert.SerializeObject(webhookData);
        ws.SendText(jsonData);
    }
    void OnCarData(CarData[] cars){

        UCarData[] currentCars = this.cars;

        UCarData[] uCars = new UCarData[cars.Length];
        for (int i = 0; i < cars.Length; i++)
        { uCars[i] = new UCarData(cars[i]); }
        this.cars = uCars;
        LightData[] colors = new LightData[3];
        //Partylights
        colors[0] = new LightData{ channel = LightChannel.RED, effect = LightEffect.THROB, startStrength = 14, endStrength = 0, cyclesPer10Seconds = 10 };
        colors[1] = new LightData{ channel = LightChannel.GREEN, effect = LightEffect.THROB, startStrength = 14, endStrength = 0, cyclesPer10Seconds = 8 };
        colors[2] = new LightData{ channel = LightChannel.BLUE, effect = LightEffect.THROB, startStrength = 14, endStrength = 0, cyclesPer10Seconds = 6 };
        for(int i = 0; i < cars.Length; i++){ 
            //if the car was not in the previous list, send the lights
            if(currentCars == null || currentCars.FirstOrDefault(x => x.id == cars[i].id) == null){
                //if the car is not in the current list, send the lights
                StartCoroutine(SendLightsDelayed(uCars[i], colors, i * 0.05f));
            }
        } //send the lights with a delay
        SR.ui.SetCarsCount(cars.Length + availableCars.Length);
        for (int i = 0; i < SR.cms.controllers.Count; i++)
        { SR.cms.controllers[i].CheckCarExists(); }
    }
    void OnAvailableCarData(AvailableCarData[] availableCarData){
        UCarAvailable[] uAvailableCars = new UCarAvailable[availableCarData.Length];
        for (int i = 0; i < availableCarData.Length; i++)
        { uAvailableCars[i] = new UCarAvailable(availableCarData[i]); }
        this.availableCars = uAvailableCars;
        SR.ui.SetCarsCount(cars.Length + availableCars.Length);
        //Debug.Log($"Updated available cars: {availableCarData.Length} cars found");
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
    
    // Get all cars for selection (both connected and available)
    public List<CarSelectionData> GetAllCarsForSelection(){
        List<CarSelectionData> allCars = new List<CarSelectionData>();
        
        // Add connected cars
        if(cars != null){
            foreach(UCarData car in cars){
                allCars.Add(new CarSelectionData{
                    id = car.id,
                    model = (uint)car.modelName,
                    name = car.name,
                    isConnected = true
                });
            }
        }
        
        // Add available cars (not already connected)
        if(availableCars != null){
            foreach(UCarAvailable car in availableCars){
                // Check if this car is not already in connected cars
                bool alreadyConnected = false;
                if(cars != null){
                    foreach(UCarData connectedCar in cars){
                        if(connectedCar.id == car.id){
                            alreadyConnected = true;
                            break;
                        }
                    }
                }
                
                if(!alreadyConnected){
                    allCars.Add(new CarSelectionData{
                        id = car.id,
                        model = car.model,
                        name = $"Available Car ({(ModelName)car.model})",
                        isConnected = false
                    });
                }
            }
        }
        
        return allCars;
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
[System.Serializable]
public class UCarAvailable{ //used for unity (bc it cant serialize properties)
    public string id;
    public uint model;
    public float secondsSinceLastSeen;
    public UCarAvailable(AvailableCarData data){
        id = data.id;
        model = data.model;
        //convert lastSeen to seconds
        secondsSinceLastSeen = (float)(DateTime.UtcNow - data.lastSeen.ToUniversalTime()).TotalSeconds;
    }
}

[System.Serializable]
public class CarSelectionData{ //used for car selection in CarSelector
    public string id;
    public uint model;
    public string name;
    public bool isConnected;
}