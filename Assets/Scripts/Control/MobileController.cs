using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Controller component for mobile remote players.
/// Receives input from RemoteControlLink instead of Unity InputSystem.
/// Exposes moveInput (Vector2) driven by D-pad holds so CarSelector works
/// identically to PlayerController. CMS callbacks fire on confirm/altSelect/start/cancel.
/// EventSystem events fire in parallel for standard Unity UI panels.
/// Only P1 (isPlayerOne) controls UI.
/// </summary>
public class MobileController : MonoBehaviour
{
    public enum MobileInputMode { Racing, Menu }

    CarController carController;
    MobileInputMode currentInputMode = MobileInputMode.Menu;
    bool isPlayerOne;

    /// <summary>Current input mode — read by RemoteControlLink to push UI mode to browser.</summary>
    public MobileInputMode CurrentMode => currentInputMode;

    // Analog movement — driven by D-pad hold, read by CarSelector + any menu
    public Vector2 moveInput;
    int navX, navY;

    // One-shot pulses consumed in Update for EventSystem events
    bool confirmPulse;
    bool cancelPulse;
    bool altSelectPulse;

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Initialise this controller. Call immediately after AddComponent.</summary>
    public void Setup(bool isP1, string playerName)
    {
        isPlayerOne   = isP1;
        carController = GetComponent<CarController>();
        carController.Setup(false, playerName); // CarController.Setup already calls SR.cms.AddController
        Debug.Log($"[MobileController] Spawned '{playerName}' (P1={isP1})");
    }

    /// <summary>Switch between Racing and Menu input mode.</summary>
    public void SetInputMode(MobileInputMode mode)
    {
        currentInputMode = mode;
        if (mode == MobileInputMode.Menu)
            carController?.UpdateInputs(InputFrame.Empty(), 0);
        Debug.Log($"[MobileController] {carController?.GetPlayerName()} → {mode}");
    }

    /// <summary>Called by RemoteControlLink with each incoming racing input frame.</summary>
    public void ReceiveRacingInput(InputFrame frame)
    {
        if (currentInputMode == MobileInputMode.Racing)
            carController?.UpdateInputs(frame, 0);
    }

    /// <summary>
    /// Process a UI action string from the mobile remote.
    /// D-pad hold/release events update moveInput. Discrete actions (confirm,
    /// alt_select, start, cancel) fire CMS callbacks immediately so CarSelector
    /// and other non-EventSystem screens respond correctly. EventSystem move
    /// events are also fired on key-down so standard Unity UI panels navigate.
    /// Only acts when this controller is P1 and in Menu mode.
    /// </summary>
    public void ApplyUiAction(string action)
    {
        if (!isPlayerOne || currentInputMode != MobileInputMode.Menu) return;

        // ── D-pad hold tracking (drives moveInput for CarSelector) ───────────
        switch (action)
        {
            case "nav_up_down":    navY =  1; break;
            case "nav_up_up":      navY =  0; break;
            case "nav_down_down":  navY = -1; break;
            case "nav_down_up":    navY =  0; break;
            case "nav_left_down":  navX = -1; break;
            case "nav_left_up":    navX =  0; break;
            case "nav_right_down": navX =  1; break;
            case "nav_right_up":   navX =  0; break;

            // ── Discrete actions → CMS callbacks (CarSelector / gamemodes) ──
            case "confirm":
                confirmPulse = true;
                SR.cms?.OnSelectCallback(carController);
                break;
            case "alt_select":
                altSelectPulse = true;
                SR.cms?.OnAltSelectCallback(carController);
                break;
            case "start":
                SR.cms?.OnStartSelectCallback(carController);
                break;
            case "cancel":
                cancelPulse = true;
                SR.cms?.OnBackToMenuCallback();
                break;
        }

        moveInput = new Vector2(navX, navY);

        // ── EventSystem move events (standard Unity UI panels) ───────────────
        var es = EventSystem.current;
        if (es != null)
        {
            EnsureSomethingSelected(es);
            switch (action)
            {
                case "nav_up_down":    SendMoveEvent(es, MoveDirection.Up);    break;
                case "nav_down_down":  SendMoveEvent(es, MoveDirection.Down);  break;
                case "nav_left_down":  SendMoveEvent(es, MoveDirection.Left);  break;
                case "nav_right_down": SendMoveEvent(es, MoveDirection.Right); break;
            }
        }
    }

    // ── Unity Lifecycle ───────────────────────────────────────────────────────

    void Update()
    {
        // Keep moveInput current every frame (holds remain across frames)
        moveInput = new Vector2(navX, navY);

        if (!isPlayerOne || currentInputMode != MobileInputMode.Menu) return;
        if (!confirmPulse && !cancelPulse && !altSelectPulse) return;

        var es = EventSystem.current;
        if (es == null) { confirmPulse = cancelPulse = altSelectPulse = false; return; }

        EnsureSomethingSelected(es);

        if (confirmPulse)
        {
            ExecuteEvents.Execute(
                es.currentSelectedGameObject,
                new BaseEventData(es),
                ExecuteEvents.submitHandler);
            confirmPulse = false;
        }

        if (cancelPulse)
        {
            ExecuteEvents.Execute(
                es.currentSelectedGameObject,
                new BaseEventData(es),
                ExecuteEvents.cancelHandler);
            cancelPulse = false;
        }

        altSelectPulse = false;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static void EnsureSomethingSelected(EventSystem es)
    {
        if (es.currentSelectedGameObject != null) return;
        var first = UnityEngine.UI.Selectable.allSelectables.Count > 0
            ? UnityEngine.UI.Selectable.allSelectables[0]
            : null;
        if (first != null) es.SetSelectedGameObject(first.gameObject);
    }

    static void SendMoveEvent(EventSystem es, MoveDirection dir)
    {
        var axisData = new AxisEventData(es)
        {
            moveDir    = dir,
            moveVector = dir == MoveDirection.Up    ? Vector2.up    :
                         dir == MoveDirection.Down  ? Vector2.down  :
                         dir == MoveDirection.Left  ? Vector2.left  :
                                                      Vector2.right
        };
        ExecuteEvents.Execute(
            es.currentSelectedGameObject,
            axisData,
            ExecuteEvents.moveHandler);
    }
}
