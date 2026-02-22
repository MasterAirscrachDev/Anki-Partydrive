# Anki-Partydrive
<img src= "https://github.com/MasterAirscrachDev/Anki-Partydrive/blob/main/Assets/Textures/Logo.png?raw=true" alt="Logo" width="200" height="200">  

Its Anki Overdrive, but open source.

The project is creating an open source server to use Anki Overdrive cars with a custom game client. The server is written in C# and uses the InTheHand.BluetoothLE library to connect to the cars. The game client is written in Unity and uses the server to control the cars.

[Overdrive Server (This Project)](https://github.com/MasterAirscrachDev/Anki-Partydrive/tree/main/OverdriveServer#overdrive-server-documentation)  
[Overdrive BluetoothLE Protocol](https://github.com/MasterAirscrachDev/Anki-Partydrive/blob/main/OverdriveServer/Overdrive%20BLE.md#anki-overdrive-bluetooth-api)  
[Overdrive Car Documentation](https://github.com/MasterAirscrachDev/Anki-Partydrive/blob/main/OverdriveServer/Overdrive%20Cars.md#overdrive-car-hardware)  

# What this is
Anki Partydrive is actually two projects:
- Overdrive Server: A c# server that connects to the cars and exposes a simple API over HTTP to control them. And a Websocket server to communicate with a game client.

- Replay Studios PartyDrive: A Unity game client that connects to the server and shows a 3D representation of the cars and the track. The game client is responsible for controlling the cars and showing the track.

# What this is not
This is not a remake of the Anki Overdrive app, I am not making a campaign or upgradable weapons.

# Where we're at:
## Server
- [x] Connect to the cars
- [x] Read the cars' state
- [x] Control the cars speed
- [x] Track Scanning (Now V2 with fallback enhancement)
- [x] Support for all overdrive tracks
- [x] Car Tracking (Also with V2 fallback enhancement)
- [x] Websocket connection (Much faster data transfer, and allows for more data to be sent)
- [x] Simple graphical web interface
- [x] Full Light control
- [ ] Race Mode (Full API) support (probably useless)
## Game
- [x] Procedural track generation (show the real world track in a 3D game environment)
- [x] Communication with the server (using NativeWebSocket for Unity)
- [x] Control the cars
- [ ] Track Effects (3D enhancements of the game world)
- [x] Modular Game Modes (allowing for easy addition of new game modes)
- [ ] Powerups (Similar to Sonic & Sega All-Stars Racing)
- [x] AI for cars (Will be improved over time)
### Gamemodes:
- [x] Laps Mode (First to complete X laps wins)
- [x] Time Trial (Standard race, drive a good line and use boost to get the best time)
- [ ] Party Mode (Item boxes, boosters, repair station, its on the track and its there for your advantage)
- [ ] Cell Deduction (Your battery is dying, keep it topped up by driving over the cells on the track)
- [ ] Juggernaut (Only the juggernaut can score points, but everyone can attack the juggernaut. first to 5 points wins)

# Credits:
The server is using 
Windows: UWP Bluetooth / InTheHand.BluetoothLE 
Linux: InTheHand.BluetoothLE (doesnt really work)
Mac: InTheHand.BluetoothLE (doesnt really work)
to connect to the Anki Overdrive cars  
and Fleck for the Websocket server.  

The game is using Unity for the game engine  
and NativeWebSocket for the Websocket connection.

Logo by toastito


# Disclaimer:  
This project is not affiliated with Anki, or new Anki (Digital Dream Labs / DDL) in any way.  
Use of the Anki Overdrive name is for internet searchability and is not intended to imply any affiliation with Anki or DDL.

# Server information
For reasons i cannot disclose, the server is temporarily not being updated publicly.
I am not happy about this change however it is necessary for best functionality of the project.  
All the bindings for the server will be updated here for, so full functionality of the server is still available.  
Information listed about the server here is accurate as of the latest build.  
The server will be updated here as soon as possible.