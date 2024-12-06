using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class CarInteraface : MonoBehaviour
{
    public bool connected = false;
    public CarData[] cars;
    HttpClient client = new HttpClient();
    public CarBalanceTesting balanceTesting;
    public TimeTrialMode timeTrialMode;
    CMS cms;
    CarEntityTracker carEntityTracker;
    string scanningCar;
    int DEBUG_SPEED = 0, DEBUG_LANE = 0;
    // Start is called before the first frame update
    void Start()
    {
        client.BaseAddress = new System.Uri("http://localhost:7117/");
        cms = FindObjectOfType<CMS>();
        carEntityTracker = GetComponent<CarEntityTracker>();
        ReconnectToServer();
    }
    public void ScanTrack(){
        MapTrack();
    }
    async Task MapTrack(){
        //get the first car that isnt on charge
        int index = 0;
        while(cars[index].charging){ index++; }
        int fins = FindObjectOfType<UIManager>().GetFinishCounter();
        scanningCar = cars[index].id;
        ApiCall($"scantrack/{cars[index].id}");
    }
    public void CancelScan(){
        CancelTrackMap();
    }
    async Task CancelTrackMap(){
        if(scanningCar == null){ return; }
        ApiCall($"canceltrackscan/{scanningCar}");
        scanningCar = null;
    }
    public void ControlCar(CarData car, int speed, int lane){
        if(car.charging){ return; }
        ApiCall($"controlcar/{car.id}:{speed}:{lane}");
    }
    public void SetCarColours(CarData car, float r, float g, float b){
        ApiCall($"setlights/{car.id}:{r}:{g}:{b}");
    }
    async Task<bool> SetupListener(){
        var response = await client.GetAsync("registerlogs");
        //if 200 then start ticking
        Debug.Log(response.Content.ReadAsStringAsync().Result);
        if(response.StatusCode == System.Net.HttpStatusCode.OK){
            TickServer();
        }
        else{
            return false;
        }
        ApiCall("scan", false);
        GetCars();
        connected = true;
        return true;
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
    async Task TickServer(){
        string[] ignore = new string[]{"[39]", "[77]", "[83]", "[41]", "[54]"};
        while(true){
            var response = await client.GetAsync("logs");
            if(response.StatusCode != System.Net.HttpStatusCode.OK){ ReconnectToServer(); return; }
            var responseString = await response.Content.ReadAsStringAsync();
            if(responseString != ""){ 
                string[] logs = responseString.Split('\n');
                for (int i = 0; i < logs.Length; i++)
                {
                    if(logs[i] == ""){ continue; }
                    for (int j = 0; j < ignore.Length; j++)
                    { if(logs[i].StartsWith(ignore[j])){ continue; } }
                    
                    Debug.Log(logs[i]);
                }
            }
            var utils = await client.GetAsync("utillogs");
            var utilsString = await utils.Content.ReadAsStringAsync();
            if(utilsString != ""){ 
                string[] logs = utilsString.Split('\n');
                for (int i = 0; i < logs.Length; i++)
                {
                    string[] c = logs[i].Split(':');
                    if (c[0] == "-1" || c[0] == "-2"){
                        GetCarInfo();
                        if(c[0] == "-1"){
                            ApiCall($"tts/Car {c[2]} has connected");
                        }
                    } else if(c[0] == "-3"){
                        GetTrackAndGenerate();
                    } else if(c[0] == "-4"){
                        try{
                            string id = c[1];
                            int speed = GetCar(id) == -1 ? 0 : cars[GetCar(id)].speed;
                            int index = int.Parse(c[2]);
                            if(index == -1){Debug.Log("car position currently unknown"); return; }
                            float horizontalOffset = float.Parse(c[3]);
                            bool trusted = c[4] != "True";
                            //Debug.Log($"Setting position for {id} at {index} with {horizontalOffset} and {trusted}");
                            carEntityTracker.SetPosition(id, index, speed, horizontalOffset, trusted);
                        }
                        catch(System.Exception e){
                            Debug.Log($"{logs[i]} caused an error: {e}");
                        }
                        
                    }
                    else if(c[0] == "-5"){
                        //Debug.Log("Fin was crossed");
                        if(balanceTesting != null){
                            balanceTesting.CrossedFinish();
                        }
                        if(timeTrialMode != null){
                            timeTrialMode.CarCrossedFinish(c);
                        }
                    }
                    else if(c[0] == "27"){
                        int battery = int.Parse(c[2]);
                        int index = GetCar(c[1]);
                        if(index != -1){
                            cars[index].battery = battery;
                        }
                    } else if(c[0] == "39"){
                        //Debug.Log(logs[i]);
                        int tracklocation = int.Parse(c[2]);
                        int trackID = int.Parse(c[3]);
                        float laneOffset = float.Parse(c[4]);
                        int speed = int.Parse(c[5]);
                        int index = GetCar(c[1]);
                        if(index != -1){
                            cars[index].trackPosition = tracklocation;
                            cars[index].trackID = trackID;
                            cars[index].laneOffset = laneOffset;
                            cars[index].speed = speed;
                        }
                    } else if (c[0] == "41"){
                        int trackIDIndex = int.Parse(c[2]); //Why are these blank? (need roadinfo)
                        int oldTrackIDIndex = int.Parse(c[3]); //Why are these blank?
                        float laneOffset = float.Parse(c[4]);
                        int uphill = int.Parse(c[5]);
                        int downhill = int.Parse(c[6]);
                        int leftWheelDistance = int.Parse(c[7]);
                        int rightWheelDistance = int.Parse(c[8]);
                        bool crossedFinish = c[9] == "true";
                    } else if(c[0] == "43"){ //fell off track
                        
                    } else if(c[0] == "63"){ //charging status
                        int index = GetCar(c[1]);
                        if(index != -1){
                            cars[index].charging = c[2] == "True";
                        }
                    }
                }
            }
            //if we exited play mode then we should stop the loop
            if(!Application.isPlaying){ return; }
        }
    }
    async Task GetTrackAndGenerate(){
        var track = await client.GetAsync("track");
        string trackString = await track.Content.ReadAsStringAsync();
        TrackPiece[] trackPieces = JsonConvert.DeserializeObject<TrackPiece[]>(trackString);
        FindObjectOfType<TrackGenerator>().Generate(trackPieces);
    }
    public void Call(string call){
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
    async Task GetCarInfo(){
        Debug.Log("Getting car info");
        var response = await client.GetAsync("cars");
        string responseString = await response.Content.ReadAsStringAsync();
        CarData[] cars = JsonConvert.DeserializeObject<CarData[]>(responseString);
        this.cars = cars;
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
        { cms.controllers[i].RefreshCarIndex(); }
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


    [System.Serializable]
    public class CarData{
        public string name;
        public string id;
        public int trackPosition;
        public int trackID;
        public float laneOffset;
        public int speed;
        public int battery;
        public bool charging;
    }
}