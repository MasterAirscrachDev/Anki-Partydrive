# Anki-Partydrive
An attempt to interface with Anki Overdrive from c# using the unity engine

This project uses InTheHand.BluetoothLE to connect to the Anki Overdrive cars. 

The project is creating a c# server that can manage and control the cars, accessable via http
And a Unity game client that will show a 3D representation of the cars and the track. as well as control the game flow (using controllers for local multiplayer)

[Language Agnostic Overdrive API](https://github.com/MasterAirscrachDev/Anki-Partydrive/tree/main/CarInterface)

# What this is
This is a project to create a full http server to interface with the cars

And a unity game to control the cars and show the track with gameplay resembling mario kart

# What this is not
This is not a remake of the Anki Overdrive app, I am not making a campaign or upgradeable weapons.

# Where we're at:
## Server
- [x] Connect to the cars
- [x] Read the cars' state
- [x] Control the cars speed
- [x] Basic lane control
- [ ] Acurate lane tracking
- [ ] Track Scanning
- [ ] Race Mode support
## Game
- [x] Cool map
- [x] Procedural track generation (show the irl track in 3D in game)
- [x] Talk to the server
- [x] Control the cars
- [x] Identify the cars by lights
- [ ] AI
- [ ] Game modes (Time Trial, Cell Deduction, Party Mode)
- [ ] Powerups

