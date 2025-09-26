using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    PlayerInput iinput;
    CarController carController;
    bool isMenuInputs = true;
    public Vector2 moveInput;
    public bool selectInput;
    public bool altSelectInput;
    public bool backSelectInput;
    public bool startSelectInput;
    
    // Control scheme detection
    public string currentControlScheme;
    
    // Input debouncing
    bool lastSelectInput = false;
    bool lastAltSelectInput = false;
    bool lastBackSelectInput = false;
    bool lastStartSelectInput = false;
    // Start is called before the first frame update
    void Start()
    {
        iinput = GetComponent<PlayerInput>();
        carController = GetComponent<CarController>();
        carController.Setup(false);
        int playerCount = FindObjectsOfType<PlayerController>().Length;
        carController.SetPlayerName($"Player {playerCount}");
        
        DetectAndSetControlScheme();
    }
    
    void DetectAndSetControlScheme()
    {
        if(iinput != null)
        {
            // Try to detect active devices and set appropriate control scheme
            var devices = iinput.devices;
            bool hasGamepad = false;
            bool hasMouseKeyboard = false;
            
            // Check what devices are connected to this player
            foreach(var device in devices)
            {
                if(device is UnityEngine.InputSystem.Gamepad)
                {
                    hasGamepad = true;
                }
                else if(device is UnityEngine.InputSystem.Mouse || device is UnityEngine.InputSystem.Keyboard)
                {
                    hasMouseKeyboard = true;
                }
            }
            
            // Set control scheme based on detected devices
            string targetScheme = null;
            if(hasGamepad)
            {
                targetScheme = "Gamepad";
            }
            else if(hasMouseKeyboard)
            {
                targetScheme = "MouseAndKeyboard";
            }
            else
            {
                // Fallback to current scheme if no devices detected
                targetScheme = iinput.currentControlScheme;
            }
            
            // Switch to the detected scheme if different
            if(targetScheme != null && targetScheme != iinput.currentControlScheme)
            {
                iinput.SwitchCurrentControlScheme(targetScheme, iinput.devices.ToArray());
            }
            
            // Update our cached values
            currentControlScheme = iinput.currentControlScheme;
            
            Debug.Log($"Player {carController?.GetPlayerName()} detected and set to control scheme: {currentControlScheme}");
        }
    }
    public void SetRacingMode(bool racing){
        isMenuInputs = !racing;
        
        if(racing){
            iinput.currentActionMap = iinput.actions.FindActionMap("Racing");
        }else{
            iinput.currentActionMap = iinput.actions.FindActionMap("Menu");
        }
        
        Debug.Log($"Player {carController?.GetPlayerName()} set to {(racing ? "Racing" : "Menu")} mode using {currentControlScheme}");
    }

    // Update is called once per frame
    void Update() {
        if(!isMenuInputs){
            carController.Iaccel = iinput.currentActionMap.actions[0].ReadValue<float>();
            carController.Isteer = iinput.currentActionMap.actions[1].ReadValue<float>();
            carController.IitemA = iinput.currentActionMap.actions[2].ReadValue<bool>();
            carController.IitemB = iinput.currentActionMap.actions[3].ReadValue<bool>();
            carController.Iboost = iinput.currentActionMap.actions[4].ReadValue<float>() > 0.5f;
            bool leftDrift = iinput.currentActionMap.actions[5].ReadValue<float>() > 0.5f;
            bool rightDrift = iinput.currentActionMap.actions[6].ReadValue<float>() > 0.5f;
            if(leftDrift && rightDrift || (!leftDrift && !rightDrift)){ carController.Idrift = 0; }
            else if(leftDrift){ carController.Idrift = -1; }
            else if(rightDrift){ carController.Idrift = 1; }
        }else{
            carController.Iaccel = 0;
            carController.Isteer = 0;
            carController.IitemA = false;
            carController.IitemB = false;
            carController.Iboost = false;
            carController.Idrift = 0;
            
            Vector2 Move = iinput.currentActionMap.actions[1].ReadValue<Vector2>();
            bool select = iinput.currentActionMap.actions[2].ReadValue<float>() > 0.5f;
            bool backSelect = iinput.currentActionMap.actions[4].ReadValue<float>() > 0.5f;
            bool altSelect = iinput.currentActionMap.actions[5].ReadValue<float>() > 0.5f;
            bool startSelect = iinput.currentActionMap.actions[6].ReadValue<float>() > 0.5f;
            
            moveInput = Move;
            selectInput = select;
            altSelectInput = altSelect;
            backSelectInput = backSelect;
            startSelectInput = startSelect;
            
            // Handle input debouncing and callbacks
            HandleMenuInputCallbacks();
        }
    }
    
    void HandleMenuInputCallbacks(){
        // Handle select input with debouncing
        if(selectInput && !lastSelectInput){
            // Rising edge detected - player pressed select
            if(CMS.cms != null){
                CMS.cms.OnSelectCallback(this);
            }
        }
        
        // Handle alt select input with debouncing
        if(altSelectInput && !lastAltSelectInput){
            // Rising edge detected - player pressed alt select
            if(CMS.cms != null){
                CMS.cms.OnAltSelectCallback(this);
            }
        }
        
        // Handle back button input with debouncing
        if(backSelectInput && !lastBackSelectInput){
            // Rising edge detected - player pressed back
            if(CMS.cms != null){
                CMS.cms.OnBackToMenuCallback();
            }
        }
        // Handle start button input with debouncing
        if(startSelectInput && !lastStartSelectInput){
            // Rising edge detected - player pressed start
            if(CMS.cms != null){
                CMS.cms.OnStartSelectCallback(this);
            }
        }
        
        // Update last input states
        lastSelectInput = selectInput;
        lastAltSelectInput = altSelectInput;
        lastBackSelectInput = backSelectInput;
        lastStartSelectInput = startSelectInput;
    }
}
