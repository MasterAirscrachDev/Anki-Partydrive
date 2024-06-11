using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class CarInteraface : MonoBehaviour
{
    HttpClient client = new HttpClient();
    // Start is called before the first frame update
    void Start()
    {
        client.BaseAddress = new System.Uri("http://localhost:7117/");
        SetupListener();
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
            Debug.Log(responseString);
            await Task.Delay(100);
        }
    }
    async Task GetCarInfo(){
        var response = await client.GetAsync("cars");
        string responseString = await response.Content.ReadAsStringAsync();
        CarData[] cars = JsonUtility.FromJson<CarData[]>(responseString);
        //we get the car info
    }
    class CarData{
        public string name;
        public string id;
        int trackPosition;
        int trackID;
        int laneOffset;
        int speed;
    }
}
