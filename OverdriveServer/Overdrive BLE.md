# Anki Overdrive Bluetooth Api (Unmaintained)
This is subject to change/improvement as I learn more about the Anki Overdrive API.  
All expansion on this is welcome.

### Usful Links
[Offical Anki SDK in C](https://github.com/anki/drive-sdk)

[AnkiNodeDrive SDK in Node.js](https://github.com/gravesjohnr/AnkiNodeDrive)

[Protocol (internal) in C#](https://github.com/MasterAirscrachDev/Anki-Partydrive/blob/main/OverdriveServer/Protcol.cs)

## Getting Started
The cars will advertise openly using Bluetooth LE.

useful notes:
- The cars will have unique IDs
- The cars name will end with "Drive" (doesnt apply to v4 firmware cars)

The cars are also supposed to send a model ID in the manufacturer data, but this does not work for me.

## Connecting
once you have found a car, connecting to it should just work.

The cars Gatt should have 1 service with the GUID `BE15BEEF-6186-407E-8381-0BD89C4D8DF4`

This service should have 2 characteristics:
- `BE15BEE0-6186-407E-8381-0BD89C4D8DF4` (read, notify)
- `BE15BEE1-6186-407E-8381-0BD89C4D8DF4` (write)

## Commands
The cars can be controlled by sending commands as `byte[]` to the write characteristic.

Commands have a variable byte length but follow the format
`length,commandID,data1,data2,...`

for the most complete list of commands see 
[Protocol.cs](https://github.com/MasterAirscrachDev/Anki-Partydrive/blob/main/OverdriveServer/Program.cs)

[Back To Root](https://github.com/MasterAirscrachDev/Anki-Partydrive?tab=readme-ov-file#anki-partydrive)