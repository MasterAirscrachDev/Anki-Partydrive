using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    PlayerInput iinput;
    CarController carController;
    // Start is called before the first frame update
    void Start()
    {
        iinput = GetComponent<PlayerInput>();
        carController = GetComponent<CarController>();
        carController.Setup(false); //setup the car controller
        //get the total number of PlayerControllers in the scene
        int playerCount = FindObjectsOfType<PlayerController>().Length;
        carController.SetPlayerName($"Player {playerCount}");

    }

    // Update is called once per frame
    void Update()
    {
        carController.Iaccel = iinput.currentActionMap.actions[0].ReadValue<float>();
        carController.Isteer = iinput.currentActionMap.actions[1].ReadValue<float>();
        carController.IitemA = iinput.currentActionMap.actions[2].ReadValue<bool>();
        carController.IitemB = iinput.currentActionMap.actions[3].ReadValue<bool>();
        carController.Iboost = iinput.currentActionMap.actions[4].ReadValue<float>() > 0.5f;
        bool leftDrift = iinput.currentActionMap.actions[5].ReadValue<bool>();
        bool rightDrift = iinput.currentActionMap.actions[6].ReadValue<bool>();
        if(leftDrift && rightDrift || (!leftDrift && !rightDrift)){ carController.Idrift = 0; }
        else if(leftDrift){ carController.Idrift = -1; }
        else if(rightDrift){ carController.Idrift = 1; }
    }
}
