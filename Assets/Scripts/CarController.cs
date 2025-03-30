using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.InputSystem;

public class CarController : MonoBehaviour
{
    [SerializeField] int speed, lane;
    [SerializeField] string carID;
    public float energy = 75;
    float maxEnergy = 100;
    int oldSpeed, oldLane;
    bool locked = true;
    Color playerColor = Color.red;
    CarInteraface carInterface;
    CMS cms;
    PlayerInput iinput;
    PlayerCardSystem pcs;
    public CarsManagement carsManagement;
    // Start is called before the first frame update
    void Start()
    {
        iinput = GetComponent<PlayerInput>();
        carInterface = FindObjectOfType<CarInteraface>();
        cms = FindObjectOfType<CMS>();
        cms.AddController(this);
        ControlTicker();
        FindObjectOfType<PlayerCardmanager>().UpdateCardCount();
        iinput.currentActionMap.actions[2].performed += ctx => OnDrift(ctx.ReadValueAsButton());
        iinput.currentActionMap.actions[3].performed += ctx => OnUseItem(ctx.ReadValueAsButton());
        iinput.currentActionMap.actions[4].performed += ctx => OnActivateItem(ctx.ReadValueAsButton());
        iinput.currentActionMap.actions[5].performed += ctx => OnBoost(ctx.ReadValueAsButton());
        int uiLayer = FindObjectOfType<UIManager>().GetUILayer();
        if(uiLayer == 2){
            carsManagement = FindObjectOfType<CarsManagement>();
        }
    }
    public void SetCard(PlayerCardSystem pcs){
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
    }
    void OnDrift(bool pressed){ //Button B
        //Debug.Log("Drift");

    }
    void OnUseItem(bool pressed){ //Button Y
        //Debug.Log("Use Item");
    }
    void OnActivateItem(bool pressed){ //Button X
        //Debug.Log("Activate Item");
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
        carInterface.ControlCar(carInterface.GetCarFromID(carID), 0, 0);
    }
    async Task ControlTicker(){
        while(true){
            if(!Application.isPlaying){ return; }
            await Task.Delay(500); //approx 60 ticks per second
            bool boosting = iinput.currentActionMap.actions[5].IsPressed();
            if(!locked && (speed != oldSpeed || lane != oldLane)){
                oldSpeed = speed;
                oldLane = lane;
                int useSpeed = speed;
                if(boosting && energy > 0){
                    useSpeed += 200;
                    energy -= 3f;
                }
                carInterface.ControlCar(carInterface.GetCarFromID(carID), useSpeed, lane);
            }
            if(energy < maxEnergy && !boosting && !locked){
                energy += 0.5f;
                if(energy > maxEnergy){ energy = maxEnergy; }
            }
        }
    }
    void FixedUpdate(){
        float accel = iinput.currentActionMap.actions[0].ReadValue<float>();
        int targetSpeed = (int)Mathf.Lerp(0, 800, accel);
        speed = (int)Mathf.Lerp(speed, targetSpeed, (accel == 0) ? 0.05f : 0.009f);
        if(accel == 0 && speed < 150){ speed = 0; }
        else if(accel > 0 && speed < 150){ speed = 150; }

        Vector2 move = iinput.currentActionMap.actions[1].ReadValue<Vector2>();
        //lane = (int)move.x * 10;
        lane += Mathf.RoundToInt(move.x * 2.5f);
        if(lane < -50){ lane = -50; }
        if(lane > 50){ lane = 50; }
        pcs.SetEnergy((int)energy, (int)maxEnergy);
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
