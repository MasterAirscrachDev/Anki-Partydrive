using System.Text.Json;
using static OverdriveServer.NetStructures;
using Fleck;
using AsyncAwaitBestPractices;

namespace OverdriveServer
{
    public class WebsocketManager
    {
        WebSocketServer server;
        List<IWebSocketConnection> clients;
        static readonly JsonSerializerOptions jsOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
        public WebsocketManager()
        {
            server = new WebSocketServer("ws://0.0.0.0:7118");
            clients = new List<IWebSocketConnection>();
            server.Start(socket =>
            {
                socket.OnOpen = () => {
                    clients.Add(socket);
                    Console.WriteLine($"Client connected: {socket.ConnectionInfo.ClientIpAddress}, {clients.Count} clients connected");
                    //Console.WriteLine("Client connected: " + socket.ConnectionInfo.ClientIpAddress);
                    Program.CheckCurrentTrack(); //check if the track is valid
                };

                socket.OnClose = () => {
                    clients.Remove(socket);
                    Console.WriteLine($"Client disconnected: {socket.ConnectionInfo.ClientIpAddress}, {clients.Count} clients connected");
                    OnClientLost(); //check if the client is lost
                };
                socket.OnMessage = message => { MessageToServerCallback(message); };
            });
            Console.WriteLine("WebSocket server started on ws://localhost:7118");
        }
        public void Cleanup()
        {
            foreach (var client in clients)
            { client.Close(); } //close all clients
            server.Dispose(); //dispose of the server
            clients.Clear(); //clear the list of clients
        }

        void OnClientLost()
        {
            if (Program.requireClient)
            {
                //quit the program if the client disconnects
                Environment.Exit(0);
            }
        }

        public bool HasClients()
        { return clients.Count > 0; }
        void SendMessageToClient(string message)
        {
            // Send a message to all connected clients
            foreach (var client in clients)
            { client.Send(message); }
        }
        public void Notify(string eventType, object data)
        {
            if (!HasClients()) { return; }

            // Debugging ==================================================
            // if(eventType == EVENT_SYSTEM_LOG || eventType == EVENT_UTILITY_LOG){ 
            //     Console.WriteLine($"Sending message to client: {eventType} - {data}");
            // }else{
            //     Console.WriteLine($"Sending message to client: {eventType}");
            // }
            //=============================================================
            // Create a JSON object to send with the original object as payload
            var WebhookData = new WebhookData
            {
                EventType = eventType,
                Payload = data
            };

            try
            {
                // Serialize the object to JSON in one step
                string jsonString = JsonSerializer.Serialize(WebhookData, jsOptions);

                // Send the JSON string to all connected clients
                SendMessageToClient(jsonString);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error serializing message for event {eventType}: {ex.Message}");
            }
        }
        void MessageToServerCallback(string message)
        {
            // Handle the message from the client here
            try
            {
                var webhookData = JsonSerializer.Deserialize<WebhookData>(message);
                if (webhookData == null) { return; }
                //Debugging ==============================================================
                //Console.WriteLine($"Message from client: {webhookData.EventType}");
                // =======================================================================
                if (webhookData.EventType == SV_CAR_MOVE)
                {
                    //data will be id:speed:offset
                    //offset or speed may be - meaning null
                    string[] data = ((JsonElement)webhookData.Payload).GetString().Split(":");
                    if (data.Length != 3) { return; }
                    string id = data[0];
                    Car car = Program.carSystem.GetCar(id);
                    if (car == null) { return; }
                    if (data[1] != "-") { car.SetCarSpeed(int.Parse(data[1]), 1000, false); }
                    if (data[2] != "-") { car.SetCarLane(float.Parse(data[2])); }
                }
                else if (webhookData.EventType == SV_REFRESH_CONFIGS)
                {
                    Program.carSystem.UpdateConfigs();
                }
                else if (webhookData.EventType == SV_LINEUP)
                {
                    Program.trackManager.RequestLineup();
                }
                else if (webhookData.EventType == SV_LINEUP_CANCEL)
                {
                    Program.trackManager.CancelLineup();
                }
                else if (webhookData.EventType == SV_CAR_S_LIGHTS)
                { //simple lights
                    string[] data = ((JsonElement)webhookData.Payload).GetString().Split(":");
                    if (data.Length != 4) { return; }
                    string id = data[0];
                    Car car = Program.carSystem.GetCar(id);
                    if (car == null) { return; }
                    int R = int.Parse(data[1]);
                    int G = int.Parse(data[2]);
                    int B = int.Parse(data[3]);
                    //lights are 0-255
                    if (R > 255) { R = 255; } else if (R < 0) { R = 0; }
                    if (G > 255) { G = 255; } else if (G < 0) { G = 0; }
                    if (B > 255) { B = 255; } else if (B < 0) { B = 0; }
                    car.SetEngineRGB(R, G, B);
                }
                else if (webhookData.EventType == SV_CAR_C_LIGHTS)
                {
                    //deserialize the payload as new{carID = car.id, lights = lights}
                    var data = JsonSerializer.Deserialize<LightData[]>(((JsonElement)webhookData.Payload).GetProperty("lights").ToString());
                    string carid = ((JsonElement)webhookData.Payload).GetProperty("carID").GetString();
                    Car car = Program.carSystem.GetCar(carid);
                    if (car == null) { Program.Log($"Car {carid} not found"); return; }
                    if (data == null) { Program.Log($"Car {carid} lights data is null"); return; }
                    // Console.WriteLine($"Car {carid} lights data: {data.Length} lights");
                    // foreach(LightData light in data){
                    //     Console.WriteLine($"Light = Channel:{light.channel} Effect:{light.effect} Start:{light.startStrength} End:{light.endStrength} Cycles:{light.cyclesPer10Seconds}");
                    // }
                    car.SetLights(data);
                }
                else if (webhookData.EventType == SV_TTS)
                {
                    Program.TTS(webhookData.Payload.ToString());
                }
                else if (webhookData.EventType == SV_GET_TRACK)
                { //send the track data to the client
                    Notify(EVENT_TR_DATA, Program.trackManager.TrackDataAsJson());
                }
                else if (webhookData.EventType == SV_GET_CARS)
                { //send the car data to the client
                    Notify(EVENT_CAR_DATA, Program.carSystem.CarDataAsJson());
                }
                else if (webhookData.EventType == SV_TR_START_SCAN)
                {
                    int finishLines = int.Parse(webhookData.Payload.ToString());
                    Program.trackScanner.ScanTrack(finishLines);
                }
                else if (webhookData.EventType == SV_TR_CANCEL_SCAN)
                {
                    Program.trackScanner.CancelScan();
                }
                else if (webhookData.EventType == SV_SCAN)
                {
                    Program.bluetoothInterface.ScanForCars();
                }
                else if (webhookData.EventType == SV_CLIENT_CLOSED)
                {
                    Program.clientClosedGracefully = true; //set the client closed flag to true
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deserializing message: {ex.Message}");
                return;
            }

        }
    }
}