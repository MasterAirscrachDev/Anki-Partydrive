using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class CarInteraface : MonoBehaviour
{
    public CarData[] cars;
    [SerializeField] int[] track;
    List<int> trackIDs = new List<int>();
    HttpClient client = new HttpClient();
    // Start is called before the first frame update
    void Start()
    {
        client.BaseAddress = new System.Uri("http://localhost:7117/");
        SetupListener();
    }
    void Update(){
        
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
                    if(logs[i] == "" || logs[i].StartsWith("[39]")){ continue; }
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
                    if (c[0] == "-1"){
                        GetCarInfo();
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
                    }
                }
            }
            //if we exited play mode then we should stop the loop
            if(!Application.isPlaying){ return; }
        }
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
}
