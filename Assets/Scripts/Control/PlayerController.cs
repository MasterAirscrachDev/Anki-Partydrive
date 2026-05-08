using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;

public class PlayerController : MonoBehaviour
{
    PlayerInput iinput;
    CarController carController;
    PlayerInputMode currentInputMode = PlayerInputMode.Menu;
    float lastRumbleTime = 0f;
    public Vector2 moveInput;
    public bool selectInput;
    public bool altSelectInput;
    public bool backSelectInput;
    public bool startSelectInput;
    InputFrame inputs;
    
    // Control scheme detection
    public string currentControlScheme;
    
    // Cached action references
    InputAction racingAccelerate, racingSteer, racingItemA, racingItemB, racingBoost, racingSpecialAim;
    InputAction menuNav, menuSelect, menuBack, menuAltSelect, menuStart;
    [HideInInspector] public InputAction cameraMove, cameraShift, cameraExit;
    
    // Input debouncing
    bool lastSelectInput = false;
    bool lastAltSelectInput = false;
    bool lastBackSelectInput = false;
    bool lastStartSelectInput = false;
    // Start is called before the first frame update
    void Start()
    {
        inputs = InputFrame.Empty();
        iinput = GetComponent<PlayerInput>();
        carController = GetComponent<CarController>();
        int playerCount = FindObjectsByType<PlayerController>(FindObjectsSortMode.None).Length;
        carController.Setup(false, $"Player {playerCount}");
        
        // Cache racing actions
        var racingMap = iinput.actions.FindActionMap("Racing");
        racingAccelerate = racingMap.FindAction("Accelerate");
        racingSteer = racingMap.FindAction("Steer");
        racingItemA = racingMap.FindAction("ItemA");
        racingItemB = racingMap.FindAction("ItemB");
        racingBoost = racingMap.FindAction("Boost");
        racingSpecialAim = racingMap.FindAction("SpecialAim");
        // Cache menu actions
        var menuMap = iinput.actions.FindActionMap("Menu");
        menuNav = menuMap.FindAction("UINav");
        menuSelect = menuMap.FindAction("UISelect");
        menuBack = menuMap.FindAction("UIBack");
        menuAltSelect = menuMap.FindAction("AltSelect");
        menuStart = menuMap.FindAction("UIStart");
        // Cache camera actions
        var cameraMap = iinput.actions.FindActionMap("Camera");
        cameraMove = cameraMap.FindAction("Move");
        cameraShift = cameraMap.FindAction("Shift");
        cameraExit = cameraMap.FindAction("Exit");
        
        DetectAndSetControlScheme();
    }
    
    void DetectAndSetControlScheme()
    {
        if(iinput != null) {
            // Try to detect active devices and set appropriate control scheme
            var devices = iinput.devices;
            bool hasGamepad = false, hasMouseKeyboard = false;
            // Check what devices are connected to this player
            foreach(var device in devices) {
                if(device is UnityEngine.InputSystem.Gamepad)
                { hasGamepad = true; }
                else if(device is UnityEngine.InputSystem.Mouse || device is UnityEngine.InputSystem.Keyboard)
                { hasMouseKeyboard = true; }
            }
            
            // Set control scheme based on detected devices
            string targetScheme = null;
            if(hasGamepad) { targetScheme = "Gamepad"; }
            else if(hasMouseKeyboard) { targetScheme = "MouseAndKeyboard"; }
            // Fallback to current scheme if no devices detected
            else { targetScheme = iinput.currentControlScheme; }
            
            // Switch to the detected scheme if different
            if(targetScheme != null && targetScheme != iinput.currentControlScheme)
            { iinput.SwitchCurrentControlScheme(targetScheme, iinput.devices.ToArray()); }
            
            // Update our cached values
            currentControlScheme = iinput.currentControlScheme;
        }
    }
    public void SetInputMode(PlayerInputMode mode){
        currentInputMode = mode;
        
        switch(mode){
            case PlayerInputMode.Racing:
                iinput.currentActionMap = iinput.actions.FindActionMap("Racing");
                break;
            case PlayerInputMode.CameraControl:
                iinput.currentActionMap = iinput.actions.FindActionMap("Camera");
                break;
            default:
                iinput.currentActionMap = iinput.actions.FindActionMap("Menu");
                break;
        }
        
        Debug.Log($"Player {carController?.GetPlayerName()} set to {mode} mode using {currentControlScheme}");
    }
    public void TrySetGamepadColor(Color color){
        if(iinput.currentControlScheme == "Gamepad"){
            var gamepad = iinput.devices[0] as Gamepad;
            if(gamepad != null){
                var dsGamepad = gamepad as DualShockGamepad;
                if(dsGamepad != null){
                    dsGamepad.SetLightBarColor(color);
                }else{
                    DualSenseGamepadHID ds5Gamepad = gamepad as DualSenseGamepadHID;
                    if(ds5Gamepad != null){
                        ds5Gamepad.SetLightBarColor(color);
                    }
                }

            }
        }
    }
    
    public void SetControllerRumble(float lowFrequency, float highFrequency){
        if(iinput != null && iinput.currentControlScheme == "Gamepad"){
            var gamepad = iinput.devices[0] as Gamepad;
            if(gamepad != null){
                //if dualsense or dualshock, set lightbar to red when rumbling
                var dsGamepad = gamepad as DualShockGamepad;
                if(dsGamepad != null){
                    dsGamepad.SetMotorSpeeds(lowFrequency, highFrequency);
                }else{ //treat as generic gamepad if not dualshock, since rumble is supported on most controllers
                    gamepad.SetMotorSpeeds(lowFrequency, highFrequency);
                }
            }
        }
    }

    // Update is called once per frame
    void Update() {
        if(currentInputMode == PlayerInputMode.Racing){
            inputs.accel = racingAccelerate.ReadValue<float>();
            inputs.steer = racingSteer.ReadValue<float>();
            inputs.itemA = racingItemA.ReadValue<float>() > 0.5f;
            inputs.itemB = racingItemB.ReadValue<float>() > 0.5f;
            inputs.boost = racingBoost.ReadValue<float>() > 0.5f;
            inputs.specialAim = racingSpecialAim.ReadValue<float>();
            carController.UpdateInputs(inputs, 0);
        }else if(currentInputMode == PlayerInputMode.Menu){
            carController.UpdateInputs(InputFrame.Empty(), 0); // Send empty inputs to disable car control in menu
            
            Vector2 Move = menuNav.ReadValue<Vector2>();
            bool select = menuSelect.ReadValue<float>() > 0.5f;
            bool backSelect = menuBack.ReadValue<float>() > 0.5f;
            bool altSelect = menuAltSelect.ReadValue<float>() > 0.5f;
            bool startSelect = menuStart.ReadValue<float>() > 0.5f;
            
            moveInput = Move;
            selectInput = select;
            altSelectInput = altSelect;
            backSelectInput = backSelect;
            startSelectInput = startSelect;
            
            // Handle input debouncing and callbacks
            HandleMenuInputCallbacks();
        }
        
        // Camera control mode - inputs are read by TrackCamera directly
        
        // Check for low energy and rumble controller if needed
        if(currentInputMode == PlayerInputMode.Racing && carController != null){
            float energyPercent = carController.GetEnergyPercent();
            if(energyPercent < 0.1f){
                // Rumble every 0.5 seconds when energy is low
                if(Time.time - lastRumbleTime > 0.5f){
                    SetControllerRumble(0.3f, 0.3f);
                    lastRumbleTime = Time.time;
                }
            } else {
                // Stop rumble when energy is above 10%
                SetControllerRumble(0f, 0f);
            }
        }
        else
        {
            // Stop rumble in menu mode
            SetControllerRumble(0f, 0f);
        }
    }
    
    void HandleMenuInputCallbacks(){
        // Handle select input with debouncing
        if(selectInput && !lastSelectInput){
            if(SR.cms != null){ SR.cms.OnSelectCallback(carController); }
        }
        
        // Handle alt select input with debouncing
        if(altSelectInput && !lastAltSelectInput){
            if(SR.cms != null){ SR.cms.OnAltSelectCallback(carController); }
        }
        
        // Handle back button input with debouncing
        if(backSelectInput && !lastBackSelectInput){
            if(SR.cms != null){ SR.cms.OnBackToMenuCallback(); }
        }

        // Handle start button input with debouncing
        if(startSelectInput && !lastStartSelectInput){
            if(SR.cms != null){ SR.cms.OnStartSelectCallback(carController); }
        }
        
        // Update last input states
        lastSelectInput = selectInput;
        lastAltSelectInput = altSelectInput;
        lastBackSelectInput = backSelectInput;
        lastStartSelectInput = startSelectInput;
    }
    public enum PlayerInputMode{
        Menu, Racing, CameraControl
    }
}
