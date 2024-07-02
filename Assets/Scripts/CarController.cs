using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.InputSystem;

public class CarController : MonoBehaviour
{
    [SerializeField] int speed, lane;
    [SerializeField] string carID;
    [SerializeField] int index;
    int oldSpeed, oldLane;
    CarInteraface carInterface;
    PlayerInput iinput;
    // Start is called before the first frame update
    void Start()
    {
        iinput = GetComponent<PlayerInput>();
        index = iinput.playerIndex;
        carInterface = FindObjectOfType<CarInteraface>();
        ControlTicker();
    }

    async Task ControlTicker(){
        while(true){
            if(!Application.isPlaying){ return; }
            await Task.Delay(500); //approx 60 ticks per second
            if(speed != oldSpeed || lane != oldLane){
                oldSpeed = speed;
                oldLane = lane;
                int useSpeed = speed;
                if(iinput.currentActionMap.actions[2].IsPressed()){
                    useSpeed += 200;
                }
                carInterface.ControlCar(carInterface.cars[iinput.playerIndex], useSpeed, lane);
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
        index = carInterface.GetCar(carID);
    }
}
