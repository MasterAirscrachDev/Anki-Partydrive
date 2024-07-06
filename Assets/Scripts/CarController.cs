using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.InputSystem;

public class CarController : MonoBehaviour
{
    [SerializeField] int speed, lane;
    [SerializeField] string carID;
    [SerializeField] int carIndex = -1;
    public float energy = 75;
    float maxEnergy = 100;
    int oldSpeed, oldLane,
    internalControlIndex;
    bool locked = true;
    CarInteraface carInterface;
    CMS cms;
    PlayerInput iinput;
    // Start is called before the first frame update
    void Start()
    {
        iinput = GetComponent<PlayerInput>();
        carInterface = FindObjectOfType<CarInteraface>();
        cms = FindObjectOfType<CMS>();
        cms.AddController(this);
        ControlTicker();
        FindObjectOfType<PlayerCardmanager>().UpdateCardCount();
    }
    public void SetControlIndex(int index){
        internalControlIndex = index;
    }
    async Task ControlTicker(){
        while(true){
            if(!Application.isPlaying){ return; }
            await Task.Delay(500); //approx 60 ticks per second
            if(!locked && (speed != oldSpeed || lane != oldLane)){
                oldSpeed = speed;
                oldLane = lane;
                int useSpeed = speed;
                if(iinput.currentActionMap.actions[2].IsPressed() && energy > 0){
                    useSpeed += 200;
                    energy -= 0.5f;
                }
                carInterface.ControlCar(carInterface.cars[carIndex], useSpeed, lane);
            }
            if(energy < maxEnergy){
                energy += 0.3f;
                if(energy > maxEnergy){ energy = maxEnergy; }
            }
        }
    }
    void FixedUpdate(){
        float accel = iinput.currentActionMap.actions[0].ReadValue<float>();
        int targetSpeed = (int)Mathf.Lerp(0, 800, accel);
        speed = (int)Mathf.Lerp(speed, targetSpeed, (accel == 0) ? 0.05f : 0.007f);
        if(accel == 0 && speed < 150){ speed = 0; }
        else if(accel > 0 && speed < 150){ speed = 150; }

        Vector2 move = iinput.currentActionMap.actions[1].ReadValue<Vector2>();
        //lane = (int)move.x * 10;
        lane = Mathf.RoundToInt(move.x * 5f);
    }
    public void GetCarIndex(){
        carIndex = carInterface.GetCar(carID);
        if(carIndex == -1){
            Debug.LogError($"Car {carID} has disconnected!");
        }
    }
    public void SetLocked(bool state){
        locked = state;
    }
    public bool isId(string id){
        return carID == id;
    }
}
