using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

public class CarController : MonoBehaviour
{
    [SerializeField] int speed;
    [SerializeField] float lane;
    [SerializeField] string carID;
    public float energy = 750;
    public float maxEnergy = 1000;
    int oldSpeed;
    float oldLane;
    bool isSetup = false;
    [SerializeField] bool locked = true;
    public bool isAI = false;
    Color playerColor = Color.white;
    CarInteraface carInterface;
    public PlayerCardSystem pcs;
    public CarsManagement carsManagement; //used when in the car selection screen
    List<SpeedModifer> speedModifiers = new List<SpeedModifer>();
    //INPUT VALUES======
    public float Iaccel;
    public float Isteer;
    public bool Iboost;
    public int Idrift;
    public bool IitemA;
    public bool IitemB;
    //===================
    //BASE MODIFIERS======
    const int maxTargetSpeed = 800;
    const int minTargetSpeed = 50;
    const int baseBoostSpeed = 50;
    const float baseEnergyGain = 0.01f;
    const float baseSteering = 0.7f;
    public float statSpeedMod = 1f;
    public float statSteerMod = 1f;
    public float statBoostMod = 1f;
    public float statEnergyMod = 1f;
    public float statDamageMod = 1f;
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
        carInterface = CarInteraface.io;
        FindObjectOfType<CMS>().AddController(this);
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

    public void AddSpeedModifier(int mod, bool isPercentage, float time, string ID = null){
        if(mod == 0){ return; }
        if(ID != null){
            for(int i = 0; i < speedModifiers.Count; i++){
                if(speedModifiers[i].ID == ID){
                    speedModifiers[i] = new SpeedModifer(mod, isPercentage, time, ID);
                    break;
                }
            }
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
        }
    }
    void FixedUpdate(){
        if(Iaccel > 0 && Iboost && energy > 1){
            energy -= 6f;
            if(energy < 0){ energy = 0; }
            AddSpeedModifier(Mathf.RoundToInt(baseBoostSpeed * statBoostMod), false, 0.1f, "Boost");
        } else if(!Iboost && energy < maxEnergy){
            energy += baseEnergyGain * statEnergyMod;
            if(energy > maxEnergy){ energy = maxEnergy; }
        }

        int targetSpeed = (int)Mathf.Lerp(minTargetSpeed, maxTargetSpeed * statSpeedMod, Iaccel);
        speed = (int)Mathf.Lerp(speed, targetSpeed, (Iaccel == 0) ? 0.05f : 0.009f);

        int speedModifier = 0;
        List<int> speedPercentModifierList = null;
        for(int i = 0; i < speedModifiers.Count; i++){
            speedModifiers[i].time -= Time.fixedDeltaTime;
            if(speedModifiers[i].isPercentage){
                if(speedPercentModifierList == null){ speedPercentModifierList = new List<int>(); }
                speedPercentModifierList.Add(speedModifiers[i].modifier);
            }else{
                speedModifier += speedModifiers[i].modifier;
            }
            if(speedModifiers[i].time <= 0){
                speedModifiers.RemoveAt(i);
                i--;
            }
        }
        speed += speedModifier;

        if(speedPercentModifierList != null){
            float percent = 1f;
            for(int i = 0; i < speedPercentModifierList.Count; i++){
                percent += speedPercentModifierList[i] / 100f;
            }
            percent /= speedPercentModifierList.Count;
            speed = Mathf.RoundToInt(speed * percent);
        }

        if(Iaccel == 0 && speed < 150){ speed = 0; } //cut speed to 0 if no input and slow speed
        else if(Iaccel > 0 && speed < 150){ speed = 150; } //snap to 150 if accelerating

        lane += Isteer * baseSteering * statSteerMod;
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
            //Debug.LogError($"Car {carID} has disconnected!");
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
    public (int, float) GetMetrics(){ return (speed, lane); }
    public string GetCarID(){ return carID; }
    public void SetLocked(bool state){ locked = state; }
    public string GetID(){ return carID; }
    public Color GetPlayerColor(){ return playerColor; }
    public void SetPosition(int position){ pcs.SetPosition(position); }
    public void SetTimeTrialTime(float time){ pcs.SetTimeTrialTime(time); }
}
class SpeedModifer{
    public int modifier;
    public bool isPercentage;
    public float time;
    public string ID;
    public SpeedModifer(int mod, bool isPercentage, float time, string ID){
        this.modifier = mod;
        this.isPercentage = isPercentage;
        this.time = time;
        this.ID = ID;
    }
}