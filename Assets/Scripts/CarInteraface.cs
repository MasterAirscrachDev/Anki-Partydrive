using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class CarInteraface : MonoBehaviour
{
    [SerializeField] CarData[] cars;
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
        if(Input.GetKeyDown(KeyCode.Space)){
            Debug.Log("Scanning");
            ApiCall("scan");
        }
        if(Input.GetKeyDown(KeyCode.C)){
            GetCarInfo();
        }
        if(Input.GetKeyDown(KeyCode.Alpha1)){
            ApiCall($"controlcar/{cars[0].id}:100:68");
        }
        if(Input.GetKeyDown(KeyCode.Alpha2)){
            ApiCall($"controlcar/{cars[0].id}:200:23");
        }
        if(Input.GetKeyDown(KeyCode.Alpha3)){
            ApiCall($"controlcar/{cars[0].id}:300:-23");
        }
        if(Input.GetKeyDown(KeyCode.Alpha4)){
            ApiCall($"controlcar/{cars[0].id}:400:-68");
        }
        if(Input.GetKeyDown(KeyCode.Alpha5)){
            ApiCall($"controlcar/{cars[0].id}:500");
        }
        if(Input.GetKeyDown(KeyCode.Alpha6)){
            ApiCall($"controlcar/{cars[0].id}:600");
        }
        if(Input.GetKeyDown(KeyCode.Alpha7)){
            ApiCall($"controlcar/{cars[0].id}:700");
        }
        if(Input.GetKeyDown(KeyCode.Alpha8)){
            ApiCall($"controlcar/{cars[0].id}:800");
        }
        if(Input.GetKeyDown(KeyCode.Alpha9)){
            ApiCall($"controlcar/{cars[0].id}:900");
        }
        if(Input.GetKeyDown(KeyCode.Alpha0)){
            ApiCall($"controlcar/{cars[0].id}:0");
        }


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
            if(!Application.isPlaying){
                return;
            }
        }
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
    class CarData{
        public string name;
        public string id;
        public int trackPosition;
        public int trackID;
        public float laneOffset;
        public int speed;
    }
}
