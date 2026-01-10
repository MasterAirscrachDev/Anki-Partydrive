using System.Text.Json;
using static OverdriveServer.NetStructures;
using Fleck;
using AsyncAwaitBestPractices;
using System.Reflection;

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
        public WebsocketManager() {
            server = new WebSocketServer("ws://0.0.0.0:7118");
            clients = new List<IWebSocketConnection>();
            //suppress fleck logging
            FleckLog.Level = LogLevel.Warn;
        
            server.Start(socket =>
            {
                socket.OnOpen = () => {
                    clients.Add(socket);
                    Console.WriteLine($"Client connected: {socket.ConnectionInfo.ClientIpAddress}, {clients.Count} clients connected");
                    Program.CheckCurrentTrack(); //check if the track is valid
                };

                socket.OnClose = () => {
                    clients.Remove(socket);
                    Console.WriteLine($"Client disconnected: {socket.ConnectionInfo.ClientIpAddress}, {clients.Count} clients connected");
                    OnClientLost(); 
                };
                socket.OnError = (exception) => {
                    Console.WriteLine($"WebSocket error: {exception.Message}");
                    socket.Close();
                    clients.Remove(socket);
                    OnClientLost();
                };
                socket.OnMessage = message => { MessageToServerCallback(message); };
            });
            //reset fleck logging
            FleckLog.Level = LogLevel.Info;
            Console.WriteLine("WebSocket: ws://localhost:7118");
        }
        public void Cleanup(){
            foreach (var client in clients)
            { client.Close(); } //close all clients
            server.Dispose(); //dispose of the server
            clients.Clear(); //clear the list of clients
        }

        void OnClientLost(){
            if(Program.requireClient && !HasClients()){
                //quit the program if the client disconnects
                SendLogAndQuit().SafeFireAndForget(); //send the log and quit 
            }
        }
        async Task SendLogAndQuit(){
            if(!Program.clientClosedGracefully){
                int seconds = 10;
                for(int i = seconds; i > 0; i--){
                    Console.WriteLine($"Client disconnected unexpectedly || sending crash log in {i} seconds");
                    await Task.Delay(1000); //wait for 1 second
                }
                //path is "C:\Users\MasterAirscrach\AppData\LocalLow\ReplayStudios\Anki Partydrive\Player.log"
                string localLowPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "..", "LocalLow");
                string path = Path.Combine(localLowPath, "ReplayStudios", "ReplayStudios Partydrive", "Player.log");
                if (File.Exists(path))
                {
                    
                }
                else {
                    Console.WriteLine("Log file not found, quitting without sending log");
                    await Task.Delay(2000); //wait for 1 second
                }
            }else{
                Console.WriteLine("Client disconnected gracefully, quitting without sending log");
            }
            
            Environment.Exit(0); //exit the program
        }

        public bool HasClients()
        { return clients.Count > 0; }
        void SendMessageToClient(string message) {
            // Send a message to all connected clients
            foreach (var client in clients)
            { client.Send(message); }
        }
        public void Notify(string eventType, object data) {
            if(!HasClients()) { return; }

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
            
            try {
                // Serialize the object to JSON in one step
                string jsonString = JsonSerializer.Serialize(WebhookData, jsOptions);
                
                // Send the JSON string to all connected clients
                SendMessageToClient(jsonString);
            }
            catch (Exception ex) {
                Console.WriteLine($"Error serializing message for event {eventType}: {ex.Message}");
            }
        }
        void MessageToServerCallback(string message) {
            // Handle the message from the client here
            try{
                var webhookData = JsonSerializer.Deserialize<WebhookData>(message);
                if(webhookData == null) { return; }
                //Debugging ==============================================================
                //Console.WriteLine($"Message from client: {webhookData.EventType}");
                // =======================================================================
                if(webhookData.EventType == SV_CAR_MOVE){
                    
                    //data will be id:speed:offset
                    //offset or speed may be - meaning null
                    string[] data = ((JsonElement)webhookData.Payload).GetString().Split(":");
                    if(data.Length != 3) { return; }
                    string id = data[0];
                    Car car = Program.carSystem.GetCar(id);
                    //Program.Log($"Received move command for car {id}: Speed={data[1]}, Offset={data[2]}", true);
                    if(car == null) { return; }
                    if(data[1] != "-") { car.SetCarSpeed(int.Parse(data[1]), 5000, false); }
                    if(data[2] != "-") { car.SetCarLane(float.Parse(data[2])); }
                }else if(webhookData.EventType == SV_REFRESH_CONFIGS){
                    Program.carSystem.UpdateConfigs();
                } else if(webhookData.EventType == SV_LINEUP){
                    Program.trackManager.RequestLineup();
                } else if(webhookData.EventType == SV_LINEUP_CANCEL){
                    Program.trackManager.CancelLineup();
                } else if(webhookData.EventType == SV_CAR_S_LIGHTS){ //simple lights
                    string[] data = ((JsonElement)webhookData.Payload).GetString().Split(":");
                    if(data.Length != 4) { return; }
                    string id = data[0];
                    Car car = Program.carSystem.GetCar(id);
                    if(car == null) { return; }
                    int R = int.Parse(data[1]);
                    int G = int.Parse(data[2]);
                    int B = int.Parse(data[3]);
                    car.SetEngineRGB(R, G, B);
                } else if(webhookData.EventType == SV_CAR_C_LIGHTS){
                    //deserialize the payload as new{carID = car.id, lights = lights}
                    var data = JsonSerializer.Deserialize<LightData[]>(((JsonElement)webhookData.Payload).GetProperty("lights").ToString());
                    string carid = ((JsonElement)webhookData.Payload).GetProperty("carID").GetString();
                    Car car = Program.carSystem.GetCar(carid);
                    if(car == null) { Program.Log($"Car {carid} not found"); return; }
                    if(data == null) { Program.Log($"Car {carid} lights data is null"); return; }
                    // Console.WriteLine($"Car {carid} lights data: {data.Length} lights");
                    // foreach(LightData light in data){
                    //     Console.WriteLine($"Light = Channel:{light.channel} Effect:{light.effect} Start:{light.startStrength} End:{light.endStrength} Cycles:{light.cyclesPer10Seconds}");
                    // }
                    car.SetLights(data);
                } else if(webhookData.EventType == SV_GET_TRACK){ //send the track data to the client
                    Notify(EVENT_TR_DATA, Program.trackManager.TrackDataAsJson());
                } else if(webhookData.EventType == SV_GET_CARS){ //send the car data to the client
                    Notify(EVENT_CAR_DATA, Program.carSystem.CarDataAsJson());
                } else if(webhookData.EventType == SV_GET_AVAILABLE_CARS){ //send the available car data to the client
                    Notify(EVENT_AVAILABLE_CARS, Program.carSystem.AvailableCarDataAsJson());
                } else if(webhookData.EventType == SV_CONNECT_CAR){ //connect to a specific available car
                    string carId = ((JsonElement)webhookData.Payload).GetString();
                    if(carId != null){
                        Program.carSystem.ConnectToAvailableCar(carId);
                    }
                } else if(webhookData.EventType == SV_DISCONNECT_CAR){ //disconnect from a specific connected car
                    string carId = ((JsonElement)webhookData.Payload).GetString();
                    if(carId != null){
                        Program.carSystem.DisconnectCar(carId);
                    }
                } else if(webhookData.EventType == SV_TR_START_SCAN){
                    int finishLines = int.Parse(webhookData.Payload.ToString());
                    Program.trackScanner.ScanTrack(finishLines);
                } else if(webhookData.EventType == SV_TR_CANCEL_SCAN){
                    Program.trackScanner.CancelScan();
                } else if(webhookData.EventType == SV_CLIENT_CLOSED){
                    Program.clientClosedGracefully = true; //set the client closed flag to true
                } else if(webhookData.EventType == SV_CAR_UPDATE_MODEL){
                    string data = ((JsonElement)webhookData.Payload).GetString();
                    string[] parts = data.Split(":");
                    if(parts.Length != 2) { return; }
                    string carId = parts[0];
                    if(!uint.TryParse(parts[1], out uint newModel)) { 
                        Program.Log($"Invalid model number: {parts[1]}"); 
                        return; 
                    }
                    Car car = Program.carSystem.GetCar(carId);
                    if(car == null) {
                        Program.Log($"Car with ID {carId} not found");
                        return; 
                    }
                    car.UpdateCarModel(newModel).SafeFireAndForget();
                    Program.Log($"Model update command initiated for car {carId} -> model {newModel}");
                }
            }
            catch (Exception ex) {
                Console.WriteLine($"Error deserializing message: {ex.Message}");
                return;
            }
        }
    }
}