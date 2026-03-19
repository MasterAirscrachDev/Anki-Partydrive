# Anki-Partydrive
<img src= "https://github.com/MasterAirscrachDev/Anki-Partydrive/blob/main/Assets/Textures/Logo.png?raw=true" alt="Logo" width="200" height="200">  

Its Anki Overdrive, but open source.

The project is creating an open source server to use Anki Overdrive cars with a custom game client. The server is written in C# and uses the InTheHand.BluetoothLE library to connect to the cars. The game client is written in Unity and uses the server to control the cars.

[Overdrive Server (This Project, outdated source)](https://github.com/MasterAirscrachDev/Anki-Partydrive/tree/main/OverdriveServer#overdrive-server-documentation)  
[Overdrive BluetoothLE Protocol](https://github.com/MasterAirscrachDev/Anki-Partydrive/blob/main/OverdriveServer/Overdrive%20BLE.md#anki-overdrive-bluetooth-api)  
[Overdrive Car Documentation](https://github.com/MasterAirscrachDev/Anki-Partydrive/blob/main/OverdriveServer/Overdrive%20Cars.md#overdrive-car-hardware)  

# What this is
Anki Partydrive is actually two projects:
- **Overdrive Server**: A c# application that connects to the cars and exposes a simple API Websocket server to communicate with external clients. The server is responsible for connecting to the cars, reading their state, and controlling them.

- **Replay Studios Partydrive**: A Unity game client that connects to the server and shows a 3D representation of the cars and the track. The game client is responsible for directing the cars and showing the track.

# What this is not
This is not a remake of the Anki Overdrive app, I am not making a campaign or upgradable weapons.

# Support the project
Airscrach here, i've been working with Anki Overdrive for over 3 years now reverse engineering the cars and app. I love Anki Overdrive and I want to see it continue to exist in a better state than it has been for a while now.  
I am doing this project in my free time and I am not getting paid for it, so if you want to support the project then please consider donating to me on [Ko-fi](https://ko-fi.com/masterairscrachdev) Every little bit helps and it allows me to keep working on the project and improving it.

# Where I'm at:
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
- [X] Powerups (Similar to Sonic & Sega All-Stars Racing)
- [x] AI for cars (Will be improved over time)
### Gamemodes:
- [x] Laps Mode (First to complete X laps wins)
- [x] Time Trial (Standard race, drive a good line and use boost to get the best time)
- [X] Party Mode (Item boxes, its on the track and its there for your advantage)
- [X] Hyperdrive (Double speed, no items, first to complete X laps wins, good luck staying on the track)
- [ ] Cell Deduction (Your battery is dying, keep it topped up by driving over the cells on the track)
- [ ] Juggernaut (Only the juggernaut can score points, but everyone can attack the juggernaut. first to 5 points wins)

# Credits:
The server is using 
Windows: UWP Bluetooth / InTheHand.BluetoothLE 
Linux: BlueZ D-Bus API 
to connect to the Anki Overdrive cars  
and Fleck for the Websocket server.  

The game is using Unity for the game engine  
and NativeWebSocket for the Websocket connection.

Logo by toastito


# Disclaimer:  
This project is not affiliated with Anki, or new Anki (Digital Dream Labs / DDL) in any way.  
Use of the Anki Overdrive name is for internet searchability and is not intended to imply any affiliation with Anki or DDL.

# Server information
For reasons I cannot disclose, the server (Overdrive Server) that Replay Studios Partydrive connects to not maintined here.  
versions of OverdriveServer that are avalible both in the releases and with Partydrive builds are from a closed source branch of the server that is not being updated publicly.  
When I am able to update the server publicly again, The closed source will be merged back into this repository and the server will be updated here as well.  
[C# Bindings](https://github.com/MasterAirscrachDev/Anki-Partydrive/blob/main/Assets/Scripts/CarData/NetDefinitions.cs) for the server will be available here, so full functionality of the server is still available for use with the game client and for anyone who wants to make their own client.  
If you have any questions about the Anki Overdrive BLE protocol that are not covered by the Server source [here](https://github.com/MasterAirscrachDev/Anki-Partydrive/tree/main/OverdriveServer) then please reach via email or discord and I will do my best to answer them.