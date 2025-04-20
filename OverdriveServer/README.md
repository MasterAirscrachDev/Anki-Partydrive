# Overdrive Server Documentation
For the Anki Overdrive Bluetooth API see [Overdrive BlootoothLE Protocol](https://github.com/MasterAirscrachDev/Anki-Partydrive/blob/main/OverdriveServer/Overdrive%20BLE.md#anki-overdrive-bluetooth-api)

# Alert
This version of the server is outdated and for reasons i cannot disclose will not be maintained publicly.  
This will be updated to the latest version when possible.  
Sorry for any inconvenience.  

Overdrive Server is a server that can connect to Anki Overdrive cars and control them. It is written in C# and uses the [32feet.NET](https://inthehand.com/components/32feet/) library to connect to the cars over Bluetooth LE.

Overdrive Server has a basic web interface that can be used to control the cars. 
`http://localhost:7117`

The server can be controlled using a simple WebSocket API. The WebSocket server is running on local port 7118.  
The WebSocket server is used to send and receive data.

[Full Data Structures (Maintained in source while in development)](https://github.com/MasterAirscrachDev/Anki-Partydrive/blob/main/Assets/Scripts/CarData/NetDefinitions.cs)

[Back To Root](https://github.com/MasterAirscrachDev/Anki-Partydrive?tab=readme-ov-file#anki-partydrive)