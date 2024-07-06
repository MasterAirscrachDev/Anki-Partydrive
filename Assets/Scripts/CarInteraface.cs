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
    // Start is called before the first frame update
    void Start()
    {
        client.BaseAddress = new System.Uri("http://localhost:7117/");
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
        ApiCall($"scantrack/{cars[index].id}:{fins}");
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
    async Task TickServer(){
        while(true){
            var response = await client.GetAsync("logs");
            if(response.StatusCode != System.Net.HttpStatusCode.OK){ ReconnectToServer(); return; }
            var responseString = await response.Content.ReadAsStringAsync();
            if(responseString != ""){ 
                string[] logs = responseString.Split('\n');
                for (int i = 0; i < logs.Length; i++)
                {
                    if(logs[i] == ""){ continue; }
                    if(logs[i].StartsWith("[39]") || logs[i].StartsWith("[77]")  || logs[i].StartsWith("[83]") || logs[i].StartsWith("[41]")){ continue; }
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
                        TrackFromData(c, 2);
                    }
                    else if(c[0] == "-4"){
                        bool success = c[2] == "True";
                        if(!success){ Debug.Log($"Failed to scan track"); continue; }
                        TrackFromData(c, 3, true);
                    } 
                    else if(c[0] == "-5"){
                        Debug.Log("Fin was crossed");
                        string carID = c[1];
                        if(balanceTesting != null){
                            balanceTesting.CrossedFinish();
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
    void TrackFromData(string[] data, int offset, bool getHeight = false){
        List<Segment> segments = new List<Segment>();
        while(offset < data.Length){
            TrackType type = TrackType.Unknown; bool flipped = false;
            int up = 0, down = 0;
            if(data[offset] == "0"){ type = TrackType.Straight; }
            if(data[offset] == "1"){ type = TrackType.CurveLeft; }
            if(data[offset] == "2"){ type = TrackType.CurveRight; }
            if(data[offset] == "3"){ type = TrackType.Poweup; }
            if(data[offset] == "4"){ type = TrackType.Finish; }
            if(data[offset] == "6"){ type = TrackType.Poweup; flipped = true; }
            offset++;
            segments.Add(new Segment(type, 0, false, flipped));
            if(getHeight){
                up = int.Parse(data[offset]);
                offset++;
                down = int.Parse(data[offset]);
                offset++;
                segments[segments.Count - 1].SetHeight(up, down);
            }
        }
        FindObjectOfType<TrackGenerator>().Generate(segments.ToArray());
        
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
        CarController[] controllers = FindObjectsOfType<CarController>();
        for (int i = 0; i < controllers.Length; i++)
        { controllers[i].GetCarIndex(); }
    }
    public int GetCar(string id){
        for (int i = 0; i < cars.Length; i++)
        { if(cars[i].id == id){return i;} }
        return -1;
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