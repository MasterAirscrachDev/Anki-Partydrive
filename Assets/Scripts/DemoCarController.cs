using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

public class DemoCarController : MonoBehaviour
{
    [SerializeField] int speed, lane;
    int oldSpeed, oldLane;
    CarInteraface carInterface;

    IInput iinput;
    
    // Start is called before the first frame update
    void Start()
    {
        iinput = new IInput();
        iinput.Racing.Enable();
        carInterface = GetComponent<CarInteraface>();
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
                if(iinput.Racing.Boost.IsPressed()){
                    useSpeed += 200;
                }
                carInterface.ControlCar(carInterface.cars[0], useSpeed, lane);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    void FixedUpdate(){
        float accel = iinput.Racing.Accelerate.ReadValue<float>();
        int targetSpeed = (int)Mathf.Lerp(0, 800, accel);
        speed = (int)Mathf.Lerp(speed, targetSpeed, (accel == 0) ? 0.05f : 0.007f);
        if(accel == 0 && speed < 150){ speed = 0; }
        else if(accel > 0 && speed < 150){ speed = 150; }

        Vector2 move = iinput.Racing.Steer.ReadValue<Vector2>();
        lane = (int)move.x * 10;
        // if(move.x != 0){
        //     lane += (int)(move.x * 5);
        // }
        // lane = Mathf.Clamp(lane, -70, 70);
    }
}
