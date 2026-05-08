using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using NativeWebSocket;
using Newtonsoft.Json;
using UnityEngine.UI;
using UnityEngine.Networking;

/// <summary>
/// Manages the Partydrive Mobile Remote integration.
/// Inactive by default — call Activate() to launch the server and open the link.
/// Automatically maps mobile controllers to in-game CarControllers by player-number order.
/// </summary>
public class RemoteControlLink : MonoBehaviour
{
    [Header("Server")]
    [SerializeField] string executableName = "PartydriveMobileRemote.exe";
    [SerializeField] int    serverPort     = 3000;

    [Header("Timing")]
    [Tooltip("How often (seconds) player state is pushed to the remote server.")]
    [SerializeField] float stateSendInterval = 0.2f; // double the tick rate to the cars

    [SerializeField] RawImage QRdisplay;

    // ── Runtime ──────────────────────────────────────────────────────────────
    WebSocket ws;
    System.Diagnostics.Process    serverProcess;
    float nextStateSend;

    // controllerId -> MobileController (one spawned GameObject per connection)
    readonly Dictionary<string, MobileController> controllerMap = new Dictionary<string, MobileController>();

    // ── Unity Lifecycle ───────────────────────────────────────────────────────
    void Awake()
    {
        enabled = false; // Inactive until Activate() is called
    }

    void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        ws?.DispatchMessageQueue();
#endif
        if (ws == null || ws.State != WebSocketState.Open) return;
        if (Time.time < nextStateSend) return;
        nextStateSend = Time.time + stateSendInterval;
        SendAllPlayerStates();
    }

    void OnDestroy()  => Deactivate();
    void OnApplicationQuit() => Deactivate();

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Activate the mobile remote link — launches the server and connects.</summary>
    public void Activate()
    {
        if (enabled) return;
        enabled = true;
        StartCoroutine(LaunchAndConnect());
    }

    /// <summary>Deactivate — closes the WebSocket and stops the server process.</summary>
    public void Deactivate()
    {
        enabled = false;
        StopAllCoroutines();
        CloseWebSocket();
        StopServerProcess();
        foreach (var mc in controllerMap.Values)
            if (mc != null) Destroy(mc.gameObject);
        controllerMap.Clear();
    }

    // ── Server Process ────────────────────────────────────────────────────────
    IEnumerator LaunchAndConnect()
    {
        LaunchServerProcess();
        yield return new WaitForSeconds(2f); // Give the Node server time to start
        ConnectWebSocket();
    }

    void LaunchServerProcess()
    {
        //Debug.Log("[RemoteControlLink] Attempting to launch mobile remote server...");
        string exePath = FindExecutable();
        if (exePath == null)
        {
            Debug.LogWarning("[RemoteControlLink] Executable not found. Attempting to connect to a running server.");
            ConnectWebSocket();
            return;
        }

        try
        {
            serverProcess = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName         = exePath,
                    UseShellExecute  = false,
                    CreateNoWindow   = true,
                    WorkingDirectory = Path.GetDirectoryName(exePath),
                }
            };
            serverProcess.Start();
            //serverRunning = true;
            //Debug.Log($"[RemoteControlLink] Launched {exePath}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[RemoteControlLink] Failed to launch server: {e.Message}");
        }
    }

    string FindExecutable()
    {
        // 1. Next to the game executable (deployed build)
        string appDir = Path.GetDirectoryName(Application.dataPath);
        string path1  = Path.Combine(appDir, executableName);
        if (File.Exists(path1)) return path1;
        // 2. BUILDS folder sibling (common dev layout)
        string path2 = Path.Combine(appDir, "BUILDS", executableName);
        //Debug.Log($"[RemoteControlLink] Looking for server executable at: {path2}");
        path2 = Path.GetFullPath(path2);
        if (File.Exists(path2)) return path2;
        return null;
    }

    void StopServerProcess()
    {
        if (serverProcess != null)
        {
            try { if (!serverProcess.HasExited) serverProcess.Kill(); }
            catch { /* process may have already exited */ }
            serverProcess.Dispose();
            serverProcess = null;
        }
    }

    // ── WebSocket ─────────────────────────────────────────────────────────────
    async void ConnectWebSocket()
    {
        ws = new WebSocket($"ws://localhost:{serverPort}/game");

        ws.OnOpen += () =>
        {
            Debug.Log("[RemoteControlLink] Connected to mobile remote server.");
            StartCoroutine(FetchAndDisplayQR());
        };
        ws.OnMessage += (bytes) =>
        {
            string json = System.Text.Encoding.UTF8.GetString(bytes);
            HandleMessage(json);
        };
        ws.OnError += (e) =>
        { Debug.LogWarning($"[RemoteControlLink] WebSocket error: {e}"); };
        ws.OnClose += (e) =>
        {
            Debug.Log("[RemoteControlLink] Disconnected. Reconnecting...");
            if (this && enabled) StartCoroutine(ReconnectAfterDelay());
        };
        await ws.Connect();
    }

    async void CloseWebSocket()
    {
        if (ws != null) { await ws.Close(); ws = null; }
    }

    IEnumerator ReconnectAfterDelay()
    {
        yield return new WaitForSeconds(3f);
        if (enabled) ConnectWebSocket();
    }
    IEnumerator FetchAndDisplayQR()
    {
        string url = $"http://localhost:{serverPort}/qr.png";
        using var req = UnityWebRequestTexture.GetTexture(url);
        yield return req.SendWebRequest();
        if (req.result == UnityWebRequest.Result.Success)
        {
            if (QRdisplay != null)
                QRdisplay.texture = DownloadHandlerTexture.GetContent(req);
                QRdisplay.gameObject.SetActive(true);
        }
        else
        { Debug.LogWarning($"[RemoteControlLink] Failed to fetch QR code: {req.error}"); }
    }

    // ── Incoming messages from the Node server ────────────────────────────────
    void HandleMessage(string json) {
        try {
            var msg = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            if (msg == null) return;

            string type = msg.TryGetValue("type", out var t) ? t?.ToString() : null;

            switch (type)
            {
                case "controller_connected":
                {
                    string ctrlId  = msg["controllerId"]?.ToString();
                    int playerNum  = Convert.ToInt32(msg["playerNumber"]);
                    SpawnMobileController(ctrlId, playerNum);
                    break;
                }

                case "controller_disconnected":
                {
                    string ctrlId = msg["controllerId"]?.ToString();
                    if (ctrlId != null && controllerMap.TryGetValue(ctrlId, out var disconnected))
                    {
                        if (disconnected != null)
                        {
                            var car = disconnected.GetComponent<CarController>();
                            if (car != null) SR.cms?.controllers.Remove(car);
                            Destroy(disconnected.gameObject);
                        }
                        controllerMap.Remove(ctrlId);
                    }
                    break;
                }

                case "input":
                {
                    string ctrlId = msg.TryGetValue("controllerId", out var c) ? c?.ToString() : null;
                    if (ctrlId != null && controllerMap.TryGetValue(ctrlId, out var mc) && mc != null)
                    {
                        float throttle = msg.TryGetValue("throttle", out var th) ? Convert.ToSingle(th) : 0f;
                        float steering = msg.TryGetValue("steering", out var st) ? Convert.ToSingle(st) : 0f;
                        bool  boost    = msg.TryGetValue("boost",    out var bo) && Convert.ToBoolean(bo);
                        bool  ability  = msg.TryGetValue("ability",  out var ab) && Convert.ToBoolean(ab);

                        throttle = Mathf.Clamp01(throttle);
                        steering = Mathf.Clamp(steering, -1f, 1f);

                        mc.ReceiveRacingInput(new InputFrame(throttle, steering, boost, ability, false, 0f));
                    }
                    break;
                }

                case "ui_input":
                {
                    string ctrlId = msg.TryGetValue("controllerId", out var c2) ? c2?.ToString() : null;
                    string action = msg.TryGetValue("action",       out var ac) ? ac?.ToString() : null;
                    if (ctrlId != null && action != null && controllerMap.TryGetValue(ctrlId, out var uiMc) && uiMc != null)
                        uiMc.ApplyUiAction(action);
                    break;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[RemoteControlLink] Message parse error: {e.Message}");
        }
    }

    // ── Player spawning ───────────────────────────────────────────────────────
    void SpawnMobileController(string controllerId, int playerNumber)
    {
        var go = new GameObject($"MobilePlayer_{playerNumber}");
        go.AddComponent<CarController>();
        var mc = go.AddComponent<MobileController>();
        mc.Setup(playerNumber == 1, $"Mobile P{playerNumber}");
        controllerMap[controllerId] = mc;
        Debug.Log($"[RemoteControlLink] Spawned MobileController for {controllerId} as Player {playerNumber}");
    }

    // ── Outgoing: push player state to each controller ────────────────────────
    void SendAllPlayerStates()
    {
        if (ws == null || ws.State != WebSocketState.Open) return;

        foreach (var kv in controllerMap)
        {
            string           controllerId = kv.Key;
            MobileController mc           = kv.Value;
            if (mc == null) continue;
            CarController    car          = mc.GetComponent<CarController>();
            if (car == null) continue;

            int    model    = (int)car.GetCarModel();
            int    energy   = Mathf.RoundToInt(car.GetEnergyPercent() * 100f);
            int    position = car.GetPosition();
            string ability  = AbilityToIconName(car.GetCurrentAbility());
            string uiMode   = mc.CurrentMode == MobileController.MobileInputMode.Menu ? "menu" : "race";

            var payload = new
            {
                type         = "player_state",
                controllerId,
                position,
                energy,
                carModel     = model,
                abilityIcon  = ability,
                uiMode,
            };

            ws.SendText(JsonConvert.SerializeObject(payload));
        }
    }

    // ── Ability → icon name mapping ───────────────────────────────────────────
    static string AbilityToIconName(Ability ability)
    {
        switch (ability)
        {
            case Ability.Missle1:        return "Missle1";
            case Ability.Missle2:        return "Missle2";
            case Ability.Missle3:        return "Missle3";
            case Ability.MissleSeeking1: return "MissleSeeking1";
            case Ability.MissleSeeking2: return "MissleSeeking2";
            case Ability.MissleSeeking3: return "MissleSeeking3";
            case Ability.EMP:            return "EMP";
            case Ability.Recharger:      return "Recharger";
            case Ability.TrailDamage:    return "TrailDamage";
            case Ability.TrailSlow:      return "TrailSlow";
            case Ability.Overdrive:      return "Overdrive";
            case Ability.CrasherBoost:   return "CrasherBoost";
            case Ability.OrbitalLazer:   return "OrbitalLazer";
            case Ability.Grappler:       return "Grappler";
            case Ability.LightningPower: return "LightningPower";
            case Ability.TrafficCone:    return "TrafficCone";
            case Ability.BurstShield:    return "BurstShield";
            case Ability.IceBlast:       return "IceBlast";
            default:                     return "None";
        }
    }
}
