//------------------------------------------------------------------------------
// <auto-generated>
//     This code was auto-generated by com.unity.inputsystem:InputActionCodeGenerator
//     version 1.6.3
//     from Assets/Controls/IInput.inputactions
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public partial class @IInput: IInputActionCollection2, IDisposable
{
    public InputActionAsset asset { get; }
    public @IInput()
    {
        asset = InputActionAsset.FromJson(@"{
    ""name"": ""IInput"",
    ""maps"": [
        {
            ""name"": ""Racing"",
            ""id"": ""df5ca952-3316-4316-bf5a-709816177216"",
            ""actions"": [
                {
                    ""name"": ""Accelerate"",
                    ""type"": ""Value"",
                    ""id"": ""0b27c834-6316-414b-bf7f-eb1581f3f209"",
                    ""expectedControlType"": ""Analog"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": true
                },
                {
                    ""name"": ""Steer"",
                    ""type"": ""Value"",
                    ""id"": ""72fa2af1-f93d-49cf-af06-70351c892a03"",
                    ""expectedControlType"": ""Vector2"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": true
                },
                {
                    ""name"": ""Drift"",
                    ""type"": ""Button"",
                    ""id"": ""378cb8ad-6f3d-420a-babf-78c0244b2465"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""UseItem"",
                    ""type"": ""Button"",
                    ""id"": ""cf152a41-7477-48a6-9e17-cc33fef97a6c"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""ActivateItem"",
                    ""type"": ""Button"",
                    ""id"": ""7aa53113-debb-442b-8bb7-abe7aa2cdbf8"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""Boost"",
                    ""type"": ""Button"",
                    ""id"": ""fd5142fa-ddd2-4e73-b4c0-281850e23de9"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""ResetCenter"",
                    ""type"": ""Button"",
                    ""id"": ""f050b106-33ff-4830-a43e-70fe2b749787"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""f541128d-a779-4bac-ad6c-f315b8d74998"",
                    ""path"": ""<Gamepad>/rightTrigger"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Accelerate"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""bda6ecf3-27aa-485e-b4e6-c548469e3ffb"",
                    ""path"": ""<Keyboard>/w"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Accelerate"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""c8e1a3c7-4401-47c5-b9b4-65e96aa13509"",
                    ""path"": ""<Gamepad>/leftStick"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Steer"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""663e94c8-9b0d-4102-b014-c688e145928e"",
                    ""path"": ""<Gamepad>/rightStick"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Steer"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""2D Vector"",
                    ""id"": ""079f5c22-3fde-4d2c-a34e-d9ee640a9046"",
                    ""path"": ""2DVector(mode=2)"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Steer"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""up"",
                    ""id"": ""650da01b-8e36-431e-af0a-ecb64579d9dc"",
                    ""path"": """",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Steer"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""down"",
                    ""id"": ""a046ff19-5031-422a-96dc-c079891322d7"",
                    ""path"": """",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Steer"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""left"",
                    ""id"": ""4a741a6c-6af2-45f1-83b8-c8ae6f5d3c0a"",
                    ""path"": ""<Keyboard>/a"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Steer"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""right"",
                    ""id"": ""80948332-057f-4803-a269-ac87886ce5ab"",
                    ""path"": ""<Keyboard>/d"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Steer"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": """",
                    ""id"": ""6d8912d9-2cc7-40cd-ba4a-91d21771e6a2"",
                    ""path"": ""<Gamepad>/buttonEast"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Drift"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""8cf2c3e3-e8b4-4f70-a9f4-bff36c957ece"",
                    ""path"": ""<Gamepad>/buttonNorth"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""UseItem"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""169cd1d6-f352-44e7-be2c-5e3c5135cb72"",
                    ""path"": ""<Gamepad>/buttonWest"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""ActivateItem"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""f619e277-fa69-4438-8e7f-1baa645eb09a"",
                    ""path"": ""<Gamepad>/buttonSouth"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Boost"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""ef73f246-5916-4284-aea9-c12749d1610d"",
                    ""path"": ""<Gamepad>/leftStickPress"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""ResetCenter"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        }
    ],
    ""controlSchemes"": []
}");
        // Racing
        m_Racing = asset.FindActionMap("Racing", throwIfNotFound: true);
        m_Racing_Accelerate = m_Racing.FindAction("Accelerate", throwIfNotFound: true);
        m_Racing_Steer = m_Racing.FindAction("Steer", throwIfNotFound: true);
        m_Racing_Drift = m_Racing.FindAction("Drift", throwIfNotFound: true);
        m_Racing_UseItem = m_Racing.FindAction("UseItem", throwIfNotFound: true);
        m_Racing_ActivateItem = m_Racing.FindAction("ActivateItem", throwIfNotFound: true);
        m_Racing_Boost = m_Racing.FindAction("Boost", throwIfNotFound: true);
        m_Racing_ResetCenter = m_Racing.FindAction("ResetCenter", throwIfNotFound: true);
    }

    public void Dispose()
    {
        UnityEngine.Object.Destroy(asset);
    }

    public InputBinding? bindingMask
    {
        get => asset.bindingMask;
        set => asset.bindingMask = value;
    }

    public ReadOnlyArray<InputDevice>? devices
    {
        get => asset.devices;
        set => asset.devices = value;
    }

    public ReadOnlyArray<InputControlScheme> controlSchemes => asset.controlSchemes;

    public bool Contains(InputAction action)
    {
        return asset.Contains(action);
    }

    public IEnumerator<InputAction> GetEnumerator()
    {
        return asset.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Enable()
    {
        asset.Enable();
    }

    public void Disable()
    {
        asset.Disable();
    }

    public IEnumerable<InputBinding> bindings => asset.bindings;

    public InputAction FindAction(string actionNameOrId, bool throwIfNotFound = false)
    {
        return asset.FindAction(actionNameOrId, throwIfNotFound);
    }

    public int FindBinding(InputBinding bindingMask, out InputAction action)
    {
        return asset.FindBinding(bindingMask, out action);
    }

    // Racing
    private readonly InputActionMap m_Racing;
    private List<IRacingActions> m_RacingActionsCallbackInterfaces = new List<IRacingActions>();
    private readonly InputAction m_Racing_Accelerate;
    private readonly InputAction m_Racing_Steer;
    private readonly InputAction m_Racing_Drift;
    private readonly InputAction m_Racing_UseItem;
    private readonly InputAction m_Racing_ActivateItem;
    private readonly InputAction m_Racing_Boost;
    private readonly InputAction m_Racing_ResetCenter;
    public struct RacingActions
    {
        private @IInput m_Wrapper;
        public RacingActions(@IInput wrapper) { m_Wrapper = wrapper; }
        public InputAction @Accelerate => m_Wrapper.m_Racing_Accelerate;
        public InputAction @Steer => m_Wrapper.m_Racing_Steer;
        public InputAction @Drift => m_Wrapper.m_Racing_Drift;
        public InputAction @UseItem => m_Wrapper.m_Racing_UseItem;
        public InputAction @ActivateItem => m_Wrapper.m_Racing_ActivateItem;
        public InputAction @Boost => m_Wrapper.m_Racing_Boost;
        public InputAction @ResetCenter => m_Wrapper.m_Racing_ResetCenter;
        public InputActionMap Get() { return m_Wrapper.m_Racing; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(RacingActions set) { return set.Get(); }
        public void AddCallbacks(IRacingActions instance)
        {
            if (instance == null || m_Wrapper.m_RacingActionsCallbackInterfaces.Contains(instance)) return;
            m_Wrapper.m_RacingActionsCallbackInterfaces.Add(instance);
            @Accelerate.started += instance.OnAccelerate;
            @Accelerate.performed += instance.OnAccelerate;
            @Accelerate.canceled += instance.OnAccelerate;
            @Steer.started += instance.OnSteer;
            @Steer.performed += instance.OnSteer;
            @Steer.canceled += instance.OnSteer;
            @Drift.started += instance.OnDrift;
            @Drift.performed += instance.OnDrift;
            @Drift.canceled += instance.OnDrift;
            @UseItem.started += instance.OnUseItem;
            @UseItem.performed += instance.OnUseItem;
            @UseItem.canceled += instance.OnUseItem;
            @ActivateItem.started += instance.OnActivateItem;
            @ActivateItem.performed += instance.OnActivateItem;
            @ActivateItem.canceled += instance.OnActivateItem;
            @Boost.started += instance.OnBoost;
            @Boost.performed += instance.OnBoost;
            @Boost.canceled += instance.OnBoost;
            @ResetCenter.started += instance.OnResetCenter;
            @ResetCenter.performed += instance.OnResetCenter;
            @ResetCenter.canceled += instance.OnResetCenter;
        }

        private void UnregisterCallbacks(IRacingActions instance)
        {
            @Accelerate.started -= instance.OnAccelerate;
            @Accelerate.performed -= instance.OnAccelerate;
            @Accelerate.canceled -= instance.OnAccelerate;
            @Steer.started -= instance.OnSteer;
            @Steer.performed -= instance.OnSteer;
            @Steer.canceled -= instance.OnSteer;
            @Drift.started -= instance.OnDrift;
            @Drift.performed -= instance.OnDrift;
            @Drift.canceled -= instance.OnDrift;
            @UseItem.started -= instance.OnUseItem;
            @UseItem.performed -= instance.OnUseItem;
            @UseItem.canceled -= instance.OnUseItem;
            @ActivateItem.started -= instance.OnActivateItem;
            @ActivateItem.performed -= instance.OnActivateItem;
            @ActivateItem.canceled -= instance.OnActivateItem;
            @Boost.started -= instance.OnBoost;
            @Boost.performed -= instance.OnBoost;
            @Boost.canceled -= instance.OnBoost;
            @ResetCenter.started -= instance.OnResetCenter;
            @ResetCenter.performed -= instance.OnResetCenter;
            @ResetCenter.canceled -= instance.OnResetCenter;
        }

        public void RemoveCallbacks(IRacingActions instance)
        {
            if (m_Wrapper.m_RacingActionsCallbackInterfaces.Remove(instance))
                UnregisterCallbacks(instance);
        }

        public void SetCallbacks(IRacingActions instance)
        {
            foreach (var item in m_Wrapper.m_RacingActionsCallbackInterfaces)
                UnregisterCallbacks(item);
            m_Wrapper.m_RacingActionsCallbackInterfaces.Clear();
            AddCallbacks(instance);
        }
    }
    public RacingActions @Racing => new RacingActions(this);
    public interface IRacingActions
    {
        void OnAccelerate(InputAction.CallbackContext context);
        void OnSteer(InputAction.CallbackContext context);
        void OnDrift(InputAction.CallbackContext context);
        void OnUseItem(InputAction.CallbackContext context);
        void OnActivateItem(InputAction.CallbackContext context);
        void OnBoost(InputAction.CallbackContext context);
        void OnResetCenter(InputAction.CallbackContext context);
    }
}
