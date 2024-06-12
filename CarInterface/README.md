# Anki Overdrive Bluetooth Api
This is subject to change/improvement as I learn more about the Anki Overdrive API.
all expansion on this is welcome.

### Usful Links
[Offical Anki SDK in C](https://github.com/anki/drive-sdk)

[AnkiNodeDrive SDK in Node.js](https://github.com/gravesjohnr/AnkiNodeDrive)

[CarInterface (internal) in C#](https://github.com/MasterAirscrachDev/Anki-Partydrive/tree/main/CarInterface)

## Getting Started
The cars will advertise openly using Bluetooth LE.

useful notes:
- The cars will have unique IDs
- The cars name will end with "Drive"

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

### Command List
#### Set SDK Mode
- 4 bytes: 0x03
- ID 144: 0x90
- Data: 0x01 (SDK Mode) 0x00 (Normal Mode)
- Flags: 0x01 `ANKI_VEHICLE_SDK_OPTION_OVERRIDE_LOCALIZATION` unsure what this actually does

Example payload
```cs
byte[] payload = new byte[] { 0x03, 0x90, 0x01, 0x01 };
```
#### Set Speed
- 7 bytes: 0x06
- ID 36: 0x24
- Speed is a 2 byte signed integer, 0 is stopped, top speed may be 1200 (unsure
)
- Acceleration is a 2 byte signed integer, values untested (1000 works)

Example payload
```cs
byte[] payload = new byte[] { 0x06, 0x24, 0x00, 0x00, 0x00, 0x00, 0x00 };
```


## Messages
The cars will send messages to the read characteristic. messages follow the same format as commands.

### Message List
#### Ping Response (Untested)
- ID 23: 0x17

#### Version Response (Untested)
- ID 25: 0x19

#### Battery Level Response (Untested)
- ID 27: 0x1B

#### Localization Position Update
- ID 39: 0x27
- integer Location [2] (Unsure what this means)
- integer TrackID [3] (What piece of track the car is on)
- float Offset [4-7] (the cars horizontal position on the track)
- integer Speed [8-9] (the cars speed)

#### Track Transition Update (documented, unreliable)
- ID 41: 0x29
- integer NewTrackID [2] (the new track the car is on)
- integer PreviousTrackID [3] (the old track the car was on)
- float Offset [4-7] (the cars horizontal position on the track)
- bytes 8-13 (unsure what this is)
- integer Uphill [14] (returns a value indicating the car is acending)
- integer Downhill [15] (returns a value indicating the car is decending)
- integer Left Wheel Distance [16] (unsure what this is)
- integer Right Wheel Distance [17] (unsure what this is)

This comparison can detect the finish line, unsure how it works
```cs
if ((leftWheelDistance < 0x25) && (leftWheelDistance > 0x19) && (rightWheelDistance < 0x25) && (rightWheelDistance > 0x19)) {
    "We Are On A Finish Line"
}
```

#### Car Error (Untested)
- ID 42: 0x2A
- integer Error Code [2] (unsure what this is)

#### Car Off Track
- ID 43: 0x2B

#### Fast And Furious Special Track Section
- ID 83: 0x53