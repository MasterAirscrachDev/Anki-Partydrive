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
                    ""expectedControlType"": ""Analog"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": true
                },
                {
                    ""name"": ""ItemA"",
                    ""type"": ""Button"",
                    ""id"": ""cf152a41-7477-48a6-9e17-cc33fef97a6c"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""ItemB"",
                    ""type"": ""Button"",
                    ""id"": ""7aa53113-debb-442b-8bb7-abe7aa2cdbf8"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""Boost"",
                    ""type"": ""Value"",
                    ""id"": ""fd5142fa-ddd2-4e73-b4c0-281850e23de9"",
                    ""expectedControlType"": ""Analog"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": true
                },
                {
                    ""name"": ""DriftLeft"",
                    ""type"": ""Button"",
                    ""id"": ""b9ef3808-d67b-4ab5-b5df-263234c28a7f"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""DriftRight"",
                    ""type"": ""Button"",
                    ""id"": ""fd867876-1c32-4fff-8178-351845c55df2"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""UINav"",
                    ""type"": ""Value"",
                    ""id"": ""e413b2df-a88c-4420-add2-553d4c91b992"",
                    ""expectedControlType"": ""Vector2"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": true
                },
                {
                    ""name"": ""UISelect"",
                    ""type"": ""Button"",
                    ""id"": ""d7cde582-129f-48f5-bc97-063f5589afe7"",
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
                    ""groups"": ""Main"",
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
                    ""groups"": ""Main"",
                    ""action"": ""Accelerate"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""c8e1a3c7-4401-47c5-b9b4-65e96aa13509"",
                    ""path"": ""<Gamepad>/leftStick/x"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Main"",
                    ""action"": ""Steer"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""663e94c8-9b0d-4102-b014-c688e145928e"",
                    ""path"": ""<Gamepad>/rightStick/x"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Main"",
                    ""action"": ""Steer"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""1D Axis"",
                    ""id"": ""05fb45e5-84d9-49e9-9917-04df4080cf18"",
                    ""path"": ""1DAxis"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Steer"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""negative"",
                    ""id"": ""0512968f-e3cc-4bbd-8f12-2fcecfa64842"",
                    ""path"": ""<Keyboard>/a"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Main"",
                    ""action"": ""Steer"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""positive"",
                    ""id"": ""0c6d3d55-e3cf-4b15-863f-60c2fea04a27"",
                    ""path"": ""<Keyboard>/d"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Main"",
                    ""action"": ""Steer"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": """",
                    ""id"": ""8cf2c3e3-e8b4-4f70-a9f4-bff36c957ece"",
                    ""path"": ""<Gamepad>/buttonNorth"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Main"",
                    ""action"": ""ItemA"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""169cd1d6-f352-44e7-be2c-5e3c5135cb72"",
                    ""path"": ""<Gamepad>/buttonWest"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Main"",
                    ""action"": ""ItemB"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""f619e277-fa69-4438-8e7f-1baa645eb09a"",
                    ""path"": ""<Gamepad>/buttonEast"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Main"",
                    ""action"": ""Boost"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""c823be2f-6e3b-49ab-9b35-fab7b1a04d82"",
                    ""path"": ""<Keyboard>/shift"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Main"",
                    ""action"": ""Boost"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""5d5faaeb-0f0d-437f-9706-b9ad4d0fe2ae"",
                    ""path"": ""<Gamepad>/leftShoulder"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Main"",
                    ""action"": ""DriftLeft"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""d4675a66-a1f1-45d4-adb6-2db75e3e44fd"",
                    ""path"": ""<Keyboard>/q"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Main"",
                    ""action"": ""DriftLeft"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""76fc252c-80da-41c5-a22c-2f37458d079d"",
                    ""path"": ""<Gamepad>/rightShoulder"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Main"",
                    ""action"": ""DriftRight"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""a249c858-c1c1-47d4-a4e3-1651f29cbe36"",
                    ""path"": ""<Keyboard>/e"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Main"",
                    ""action"": ""DriftRight"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""e5a2af63-f4ba-4f40-bcae-44dde9b2e1d6"",
                    ""path"": ""<Gamepad>/leftStick"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Main"",
                    ""action"": ""UINav"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""2D Vector"",
                    ""id"": ""b2c91b0d-0915-4c81-bf14-70835a65a5ae"",
                    ""path"": ""2DVector"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""UINav"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""up"",
                    ""id"": ""08b435f1-ffbd-4d74-bf09-5c12242215ac"",
                    ""path"": ""<Keyboard>/w"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Main"",
                    ""action"": ""UINav"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""down"",
                    ""id"": ""782f83aa-cc24-4367-8c8a-e68f4ec82927"",
                    ""path"": ""<Keyboard>/s"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Main"",
                    ""action"": ""UINav"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""left"",
                    ""id"": ""ce076711-5cec-4517-a7c7-6d527a51d578"",
                    ""path"": ""<Keyboard>/a"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Main"",
                    ""action"": ""UINav"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""right"",
                    ""id"": ""bb439fa8-2068-4748-ad3c-e943a7a2296c"",
                    ""path"": ""<Keyboard>/d"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Main"",
                    ""action"": ""UINav"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": """",
                    ""id"": ""b91321a8-20c8-417e-98e3-84dedf0d831e"",
                    ""path"": ""<Keyboard>/space"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Main"",
                    ""action"": ""UISelect"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""dda49cac-5ac1-4f57-8984-ab50a89818f4"",
                    ""path"": ""<Gamepad>/buttonSouth"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Main"",
                    ""action"": ""UISelect"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        }
    ],
    ""controlSchemes"": [
        {
            ""name"": ""Main"",
            ""bindingGroup"": ""Main"",
            ""devices"": [
                {
                    ""devicePath"": ""<Keyboard>"",
                    ""isOptional"": true,
                    ""isOR"": false
                },
                {
                    ""devicePath"": ""<Gamepad>"",
                    ""isOptional"": true,
                    ""isOR"": false
                }
            ]
        }
    ]
}");
        // Racing
        m_Racing = asset.FindActionMap("Racing", throwIfNotFound: true);
        m_Racing_Accelerate = m_Racing.FindAction("Accelerate", throwIfNotFound: true);
        m_Racing_Steer = m_Racing.FindAction("Steer", throwIfNotFound: true);
        m_Racing_ItemA = m_Racing.FindAction("ItemA", throwIfNotFound: true);
        m_Racing_ItemB = m_Racing.FindAction("ItemB", throwIfNotFound: true);
        m_Racing_Boost = m_Racing.FindAction("Boost", throwIfNotFound: true);
        m_Racing_DriftLeft = m_Racing.FindAction("DriftLeft", throwIfNotFound: true);
        m_Racing_DriftRight = m_Racing.FindAction("DriftRight", throwIfNotFound: true);
        m_Racing_UINav = m_Racing.FindAction("UINav", throwIfNotFound: true);
        m_Racing_UISelect = m_Racing.FindAction("UISelect", throwIfNotFound: true);
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
    private readonly InputAction m_Racing_ItemA;
    private readonly InputAction m_Racing_ItemB;
    private readonly InputAction m_Racing_Boost;
    private readonly InputAction m_Racing_DriftLeft;
    private readonly InputAction m_Racing_DriftRight;
    private readonly InputAction m_Racing_UINav;
    private readonly InputAction m_Racing_UISelect;
    public struct RacingActions
    {
        private @IInput m_Wrapper;
        public RacingActions(@IInput wrapper) { m_Wrapper = wrapper; }
        public InputAction @Accelerate => m_Wrapper.m_Racing_Accelerate;
        public InputAction @Steer => m_Wrapper.m_Racing_Steer;
        public InputAction @ItemA => m_Wrapper.m_Racing_ItemA;
        public InputAction @ItemB => m_Wrapper.m_Racing_ItemB;
        public InputAction @Boost => m_Wrapper.m_Racing_Boost;
        public InputAction @DriftLeft => m_Wrapper.m_Racing_DriftLeft;
        public InputAction @DriftRight => m_Wrapper.m_Racing_DriftRight;
        public InputAction @UINav => m_Wrapper.m_Racing_UINav;
        public InputAction @UISelect => m_Wrapper.m_Racing_UISelect;
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
            @ItemA.started += instance.OnItemA;
            @ItemA.performed += instance.OnItemA;
            @ItemA.canceled += instance.OnItemA;
            @ItemB.started += instance.OnItemB;
            @ItemB.performed += instance.OnItemB;
            @ItemB.canceled += instance.OnItemB;
            @Boost.started += instance.OnBoost;
            @Boost.performed += instance.OnBoost;
            @Boost.canceled += instance.OnBoost;
            @DriftLeft.started += instance.OnDriftLeft;
            @DriftLeft.performed += instance.OnDriftLeft;
            @DriftLeft.canceled += instance.OnDriftLeft;
            @DriftRight.started += instance.OnDriftRight;
            @DriftRight.performed += instance.OnDriftRight;
            @DriftRight.canceled += instance.OnDriftRight;
            @UINav.started += instance.OnUINav;
            @UINav.performed += instance.OnUINav;
            @UINav.canceled += instance.OnUINav;
            @UISelect.started += instance.OnUISelect;
            @UISelect.performed += instance.OnUISelect;
            @UISelect.canceled += instance.OnUISelect;
        }

        private void UnregisterCallbacks(IRacingActions instance)
        {
            @Accelerate.started -= instance.OnAccelerate;
            @Accelerate.performed -= instance.OnAccelerate;
            @Accelerate.canceled -= instance.OnAccelerate;
            @Steer.started -= instance.OnSteer;
            @Steer.performed -= instance.OnSteer;
            @Steer.canceled -= instance.OnSteer;
            @ItemA.started -= instance.OnItemA;
            @ItemA.performed -= instance.OnItemA;
            @ItemA.canceled -= instance.OnItemA;
            @ItemB.started -= instance.OnItemB;
            @ItemB.performed -= instance.OnItemB;
            @ItemB.canceled -= instance.OnItemB;
            @Boost.started -= instance.OnBoost;
            @Boost.performed -= instance.OnBoost;
            @Boost.canceled -= instance.OnBoost;
            @DriftLeft.started -= instance.OnDriftLeft;
            @DriftLeft.performed -= instance.OnDriftLeft;
            @DriftLeft.canceled -= instance.OnDriftLeft;
            @DriftRight.started -= instance.OnDriftRight;
            @DriftRight.performed -= instance.OnDriftRight;
            @DriftRight.canceled -= instance.OnDriftRight;
            @UINav.started -= instance.OnUINav;
            @UINav.performed -= instance.OnUINav;
            @UINav.canceled -= instance.OnUINav;
            @UISelect.started -= instance.OnUISelect;
            @UISelect.performed -= instance.OnUISelect;
            @UISelect.canceled -= instance.OnUISelect;
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
    private int m_MainSchemeIndex = -1;
    public InputControlScheme MainScheme
    {
        get
        {
            if (m_MainSchemeIndex == -1) m_MainSchemeIndex = asset.FindControlSchemeIndex("Main");
            return asset.controlSchemes[m_MainSchemeIndex];
        }
    }
    public interface IRacingActions
    {
        void OnAccelerate(InputAction.CallbackContext context);
        void OnSteer(InputAction.CallbackContext context);
        void OnItemA(InputAction.CallbackContext context);
        void OnItemB(InputAction.CallbackContext context);
        void OnBoost(InputAction.CallbackContext context);
        void OnDriftLeft(InputAction.CallbackContext context);
        void OnDriftRight(InputAction.CallbackContext context);
        void OnUINav(InputAction.CallbackContext context);
        void OnUISelect(InputAction.CallbackContext context);
    }
}
