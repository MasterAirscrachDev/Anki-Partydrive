using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class CarInteraface : MonoBehaviour
{
    [SerializeField] CarData[] cars;
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
            ApiCall($"/controlcar/{cars[0].id}:100");
        }
        if(Input.GetKeyDown(KeyCode.Alpha2)){
            ApiCall($"/controlcar/{cars[0].id}:200");
        }
        if(Input.GetKeyDown(KeyCode.Alpha3)){
            ApiCall($"/controlcar/{cars[0].id}:300");
        }
        if(Input.GetKeyDown(KeyCode.Alpha4)){
            ApiCall($"/controlcar/{cars[0].id}:400");
        }
        if(Input.GetKeyDown(KeyCode.Alpha5)){
            ApiCall($"/controlcar/{cars[0].id}:500");
        }
        if(Input.GetKeyDown(KeyCode.Alpha6)){
            ApiCall($"/controlcar/{cars[0].id}:600");
        }
        if(Input.GetKeyDown(KeyCode.Alpha7)){
            ApiCall($"/controlcar/{cars[0].id}:700");
        }
        if(Input.GetKeyDown(KeyCode.Alpha8)){
            ApiCall($"/controlcar/{cars[0].id}:800");
        }
        if(Input.GetKeyDown(KeyCode.Alpha9)){
            ApiCall($"/controlcar/{cars[0].id}:900");
        }
        if(Input.GetKeyDown(KeyCode.Alpha0)){
            ApiCall($"/controlcar/{cars[0].id}:0");
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
                    if(!logs[i].StartsWith("[37]")){
                        continue;
                    }
                    Debug.Log(logs[i]);
                }  
            }
            //Debug.Log(responseString);
            await Task.Delay(100);
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
