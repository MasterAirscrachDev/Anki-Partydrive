using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class CarInteraface : MonoBehaviour
{
    public CarData[] cars;
    [SerializeField] List<TrackSegment> trackSegments = new List<TrackSegment>();
    HttpClient client = new HttpClient();
    bool trackScanning;
    public int FinishBlocks = 1;
    int passedFinishBlocks = 0;
    // Start is called before the first frame update
    void Start()
    {
        client.BaseAddress = new System.Uri("http://localhost:7117/");
        SetupListener();
    }
    public void ScanTrack(){
        trackScanning = true;
        passedFinishBlocks = 0;
        trackSegments.Clear();
        MapTrack();
    }
    public void TestCars(int speed = 300){
        int lane = -68;
        for (int i = 0; i < cars.Length; i++)
        {
            ControlCar(cars[i].id, speed, lane);
            lane += 44;
        }
    }
    async Task MapTrack(){
        //await ApiCall("clearcardata");
        ControlCar(cars[0].id, 450, 0);
    }
    public void ControlCar(string id, int speed, int lane){
        ApiCall($"controlcar/{id}:{speed}:{lane}");
    }
    async Task SetupListener(){
        var response = await client.GetAsync("registerlogs");
        //if 200 then start ticking
        Debug.Log(response.Content.ReadAsStringAsync().Result);
        if(response.StatusCode == System.Net.HttpStatusCode.OK){
            TickServer();
        }
    }
    async Task TickServer(){
        while(true){
            var response = await client.GetAsync("logs");
            var responseString = await response.Content.ReadAsStringAsync();
            if(responseString != ""){ 
                string[] logs = responseString.Split('\n');
                for (int i = 0; i < logs.Length; i++)
                {
                    if(logs[i] == ""){ continue; }
                    if(logs[i].StartsWith("[41]")){ continue; }
                    //if(logs[i] == "" || logs[i].StartsWith("[39]")){ continue; }
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
                    } else if(c[0] == "27"){
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

                        if(trackScanning && cars[0].trackID != 0 && cars[0].trackID != 34){
                            trackSegments.Add(new TrackSegment(cars[0].trackPosition, cars[0].trackID, leftWheelDistance - rightWheelDistance));
                            GenerateTrack();
                        }
                    } else if(c[0] == "43"){
                        if(trackScanning){ trackScanning = false; }
                    }
                }
            }
            //if we exited play mode then we should stop the loop
            if(!Application.isPlaying){ return; }
        }
    }
    void GenerateTrack(){
        TrackType[] tracks = new TrackType[trackSegments.Count];
        for (int j = 0; j < trackSegments.Count; j++)
        { tracks[j] = trackSegments[j].trackType; }
        FindObjectOfType<FakeTrackGenerator>().TestGen(tracks);
    }
    public void Call(string call){
        ApiCall(call);
    }
    public void GetCars(){
        GetCarInfo();
    }
    async Task ApiCall(string call){
        var response = await client.GetAsync(call);
        string responseString = await response.Content.ReadAsStringAsync();
        Debug.Log(responseString);
    }
    async Task GetCarInfo(){
        Debug.Log("Getting car info");
        var response = await client.GetAsync("cars");
        string responseString = await response.Content.ReadAsStringAsync();
        CarData[] cars = JsonConvert.DeserializeObject<CarData[]>(responseString);
        this.cars = cars;
        Debug.Log("Updated Cars");
        //we get the car info
        // foreach(CarData car in cars){
        //     Debug.Log($"Car: {car.name} ID: {car.id}");
        // }
    }
    int GetCar(string id){
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
    }
    [System.Serializable]
    public class TrackSegment{
        public int position;
        public int trackID;
        public  TrackType trackType;
        public TrackSegment(int position, int trackID, int wheelDiff = 0){
            this.position = position;
            this.trackID = trackID;
            if(trackID == 36 || trackID == 39 || trackID == 40){
                trackType = TrackType.Straight;
            } else if(trackID == 33){
                trackType = TrackType.Finish;
            } else if(trackID == 57){
                trackType = TrackType.Poweup;
            } else if(trackID == 17 || trackID == 18 || trackID == 20 || trackID == 23){
                if(wheelDiff < 0){
                    trackType = TrackType.CurveLeft;
                }else{
                    trackType = TrackType.CurveRight;
                }
            }
            else {
                trackType = TrackType.Unknown;
            }
        }
    }
    //Indexes of TrackIDs
    //0
    //1
    //2
    //3
    //4
    //5
    //6
    //7
    //8
    //9 = 33 (Finish Line)
    //10
    //11
    //12 = 20 (Curve Right)
    //13
    //14
    //15
    //16
    //17
    //18 = 23 (Curve Left)
    //19 = 17 (Straight)
}
