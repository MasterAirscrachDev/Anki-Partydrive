using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.InputSystem;

public class CarController : MonoBehaviour
{
    [SerializeField] int speed;
    [SerializeField] float lane;
    [SerializeField] string carID;
    public float energy = 75;
    float maxEnergy = 100;
    int oldSpeed;
    float oldLane;
    bool isSetup = false;
    [SerializeField] bool locked = true;
    Color playerColor = Color.white;
    CarInteraface carInterface;
    CMS cms;
    PlayerCardSystem pcs;
    public CarsManagement carsManagement;

    //INPUT VALUES======
    public float Iaccel;
    public float Isteer;
    public bool Iboost;
    public int Idrift;
    public bool IitemA;
    public bool IitemB;
    //===================
    bool wasBoostLastFrame = false;
    void Start(){
        Setup();
    }
    // Start is called before the first frame update
    void Setup()
    {
        if(isSetup){ return; }
        isSetup = true;
        carInterface = FindObjectOfType<CarInteraface>();
        cms = FindObjectOfType<CMS>();
        cms.AddController(this);
        ControlTicker();
        FindObjectOfType<PlayerCardmanager>().UpdateCardCount();
        int uiLayer = FindObjectOfType<UIManager>().GetUILayer();
        if(uiLayer == 2){
            carsManagement = FindObjectOfType<CarsManagement>();
        }
            
    }
    public void SetColour(Color c){
        playerColor = c;
        if(pcs == null){
            Setup();
        }
    }
    public void SetCard(PlayerCardSystem pcs){
        //Debug.Log("SetCard");
        this.pcs = pcs;
        //pcs.SetCharacterName(carInterface.cars[carIndex].characterName);
        UCarData carData = carInterface.GetCarFromID(carID);
        string text = "Sitting Out";
        if(carData != null){
            text = carData.name;
        }
        pcs.SetCarName(text);
        //pcs.SetPosition(internalControlIndex + 1);
        pcs.SetEnergy((int)energy, (int)maxEnergy);
        pcs.SetColor(playerColor);
    }
    void OnBoost(bool pressed){ //Button A
        if(!pressed){ return; }
        Debug.Log("Boost");
        if(carsManagement != null){
            carsManagement.NextCar(this);
        }
    }
    public void StopCar(){
        speed = 0;
        lane = 0;
        UCarData carData = carInterface.GetCarFromID(carID);
        if(carData == null){ return; }
        carInterface.ControlCar(carData, 0, 0);
    }
    async Task ControlTicker(){
        while(true){
            if(!Application.isPlaying){ return; }
            await Task.Delay(500); //approx 2 ticks per second
            if(!locked && (speed != oldSpeed || lane != oldLane)){
                oldLane = lane;
                oldSpeed = speed;
                carInterface.ControlCar(carInterface.GetCarFromID(carID), speed, Mathf.RoundToInt(lane));
            }
            if(energy < maxEnergy && !Iboost && !locked){
                energy += 0.5f;
                if(energy > maxEnergy){ energy = maxEnergy; }
            }
        }
    }
    void FixedUpdate(){ //change this to support AI cars
        int targetSpeed = (int)Mathf.Lerp(50, 800, Iaccel);
        speed = (int)Mathf.Lerp(speed, targetSpeed, (Iaccel == 0) ? 0.05f : 0.009f);
        if(Iaccel == 0 && speed < 150){ speed = 0; } //cut speed to 0 if no input and slow speed
        else if(Iaccel > 0 && speed < 150){ speed = 150; } //snap to 150 if accelerating
        if(Iaccel > 0 && Iboost && energy > 1){
            energy -= 6f;
            if(energy < 0){ energy = 0; }
            speed += 50;
        }

        lane += Isteer * 0.5f;
        lane = Mathf.Clamp(lane, -64, 64);
        pcs.SetEnergy((int)energy, (int)maxEnergy);
    }
    void Update(){
        if(Iboost && !wasBoostLastFrame){
            if(carsManagement != null){
                carsManagement.NextCar(this);
            }
        }
        wasBoostLastFrame = Iboost;
    }
    public void CheckCarExists(){
        int idx = carInterface.GetCar(carID);
        if(idx == -1){
            Debug.LogError($"Car {carID} has disconnected!");
            carID = "";
            pcs.SetCarName("Disconnected");
        }
    }
    public void SetID(UCarData data){
        if(data == null){
            carID = "";
            pcs.SetCarName("Sitting Out");
            return;
        }
        carID = data.id;
        CheckCarExists();
        pcs.SetCarName(data.name);
    }
    public void SetLocked(bool state){
        locked = state;
    }
    public string GetID(){
        return carID;
    }
    public Color GetPlayerColor(){
        return playerColor;
    }
    public void SetPosition(int position){
        pcs.SetPosition(position);
    }
    public void SetTimeTrialTime(float time){
        pcs.SetTimeTrialTime(time);
    }
}
