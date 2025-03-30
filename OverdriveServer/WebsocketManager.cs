using System.Text.Json;
using static OverdriveServer.NetStructures;
using Fleck;
//using Newtonsoft.Json;

namespace OverdriveServer
{
    public class WebsocketManager
    {
        WebSocketServer server;
        List<IWebSocketConnection> clients;
        public WebsocketManager()
        {
            server = new WebSocketServer("ws://0.0.0.0:7118");
            clients = new List<IWebSocketConnection>();
            server.Start(socket =>
            {
                socket.OnOpen = () =>
                {
                    clients.Add(socket);
                    Console.WriteLine("Client connected: " + socket.ConnectionInfo.ClientIpAddress);
                    Program.CheckCurrentTrack(); //check if the track is valid
                };

                socket.OnClose = () =>
                {
                    clients.Remove(socket);
                    Console.WriteLine("Client disconnected: " + socket.ConnectionInfo.ClientIpAddress);
                };

                socket.OnMessage = message =>
                {
                    MessageToServerCallback(message);
                    //Console.WriteLine("Received message: " + message);
                    // foreach (var client in clients)
                    // {
                    //     if (client != socket)
                    //     {
                    //         client.Send(message);
                    //     }
                    // }
                };
            });
            Console.WriteLine("WebSocket server started on ws://localhost:7118");
        }
        public bool HasClients()
        {
            return clients.Count > 0;
        }
        void SendMessageToClient(string message)
        {
            // Send a message to all connected clients
            foreach (var client in clients)
            {
                client.Send(message);
            }
        }
        public void Notify(string eventType, object data)
        {
            if(!HasClients()) { return; }
            // Create a JSON object to send with the original object as payload
            var WebhookData = new WebhookData
            {
                EventType = eventType,
                Payload = data
            };
            
            try {
                // Use serialization options to maintain object structure
                var options = new JsonSerializerOptions { 
                    WriteIndented = false,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                };
                
                // Serialize the object to JSON in one step
                string jsonString = JsonSerializer.Serialize(WebhookData, options);
                
                // Send the JSON string to all connected clients
                SendMessageToClient(jsonString);
            }
            catch (Exception ex) {
                Console.WriteLine($"Error serializing message for event {eventType}: {ex.Message}");
            }
        }
        void MessageToServerCallback(string message)
        {
            //Console.WriteLine("Message to server: " + message);
            // Handle the message from the client here
            try{
                var webhookData = JsonSerializer.Deserialize<WebhookData>(message);
                if(webhookData == null) { return; }
                if(webhookData.EventType == EVENT_CAR_MOVE){
                    //data will be id:speed:offset
                    //offset or speed may be - meaning null
                    string[] data = ((JsonElement)webhookData.Payload).GetString().Split(":");
                    if(data.Length != 3) { return; }
                    string id = data[0];
                    Car car = Program.carSystem.GetCar(id);
                    if(car == null) { return; }
                    if(data[1] != "-") { 
                        car.SetCarSpeed(int.Parse(data[1]));
                    }
                    if(data[2] != "-") { 
                        car.SetCarLane(float.Parse(data[2]));
                    }
                }else if(webhookData.EventType == EVENT_REFRESH_CONFIGS){
                    Program.carSystem.UpdateConfigs();
                }
            }
            catch (Exception ex) {
                Console.WriteLine($"Error deserializing message: {ex.Message}");
                return;
            }

        }
        [System.Serializable]
        public class WebhookData
        {
            public string EventType { get; set; }
            public object Payload { get; set; }
        }
    }
}