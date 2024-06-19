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
- Speed is a 2 byte signed integer, 0 is stopped, top speed may be 1200 (unsure)
- Acceleration is a 2 byte signed integer, values untested (1000 works)
- Respect Road Piece Speed Limit is a 1 byte boolean, presumably to ignore the speed limit of the track (or enforce it)

#### Set Car Track Center
- 6 bytes: 0x05
- ID 44: 0x2C
- Offset is a 4 byte float, the offset from the center of the track


#### Set Car Lane
- 12 bytes: 0x0B
- ID 37: 0x25
- Horizontal Speed is a 2 byte signed integer, 0 is stopped, top speed may be 1200 (unsure)
- Horizontal Acceleration is a 2 byte signed integer, values untested (1000 works)
- Horizontal Position is a 4 byte float, the offset from the center of the track as set by the track center command

#### Request Battery Level
- 2 bytes: 0x01
- ID 26: 0x1A

Example payload
```cs
byte[] payload = new byte[] { 0x01, 0x1A };
```

#### Set Car Lights Pattern (Unsure of the exact bytes)
- 18 bytes: 0x12
- ID 51: 0x33

## Messages
The cars will send messages to the read characteristic. messages follow the same format as commands.

### Message List
#### Ping Response (Untested)
- ID 23: 0x17

#### Version Response (Untested)
- ID 25: 0x19
- integer Version [2]

#### Battery Level Response (Untested)
- ID 27: 0x1B
- integer Battery Level [2] (unsure if this a value of total or a milliamperage)

#### Localization Position Update
- ID 39: 0x27
- integer Location [2] (Unsure what this means)
- integer TrackID [3] (What piece of track the car is on)
- float Offset [4-7] (the cars horizontal position on the track)
- integer Speed [8-9] (the cars speed)

#### Track Transition Update
- ID 41: 0x29
- integer NewTrackIndex [2] (the new track the car is on)
- integer PreviousTrackIndex [3] (the old track the car was on)
- float Offset [4-7] (the cars horizontal position on the track)
- bytes 8-13 (unsure what this is)
- integer Uphill [14] (returns a value indicating the car is acending)
- integer Downhill [15] (returns a value indicating the car is decending)
- integer Left Wheel Distance [16] (unsure what this is)
- integer Right Wheel Distance [17] (unsure what this is)

The New and Previous track indexes will be black unless RoadNetworkInfo is set

```cs

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

#### Track Center Update (Untested)
- ID 45: 0x2D

#### Car Speed Update (Untested)
- ID 54: 0x36

#### Car Charging Status Changed (maybe incomplete)
- ID 63: 0x3F
- integer Charging [3] (returns a value indicating if the car is charging)

#### Car Collision (Works but often triggers without any collision)
- ID 77: 0x4D

#### Fast And Furious Special Track Section
- ID 83: 0x53

#### Car Message Cycle Overtime (No Clue what this is)
- ID 134: 0x86



## All known ids
```cs
    SEND_CAR_DISCONNECT = 0x0d; //13
    SEND_PING = 0x16; //22
    RECV_PING = 0x17; //23
    SEND_VERSION = 0x18; //24
    RECV_VERSION = 0x19; //25
    SEND_BATTERY_REQUEST = 0x1a; //26
    RECV_BATTERY_RESPONSE = 0x1b; //27
    SEND_LIGHTS_UPDATE = 0x1d; //29
    SEND_CAR_SPEED_UPDATE = 0x24; //36
    SEND_CAR_LANE_CHANGE = 0x25; //37
    SEND_CAR_CANCEL_LANE_CHANGE = 0x26; //38
    RECV_TRACK_LOCATION = 0x27; //39
    RECV_TRACK_TRANSITION = 0x29; //41
    RECV_TRACK_INTERSECTION = 0x2a; //42
    RECV_CAR_ERROR = 0x2a; //42
    RECV_CAR_OFF_TRACK = 0x2b; //43
    SEND_TRACK_CENTER_UPDATE = 0x2c; //44
    RECV_TRACK_CENTER_UPDATE = 0x2d; //45
    SEND_CAR_CHANGE_DIRECTION = 0x32; //50
    SEND_LIGHTS_PATTERN_UPDATE = 0x33; //51
    RECV_CAR_SPEED_UPDATE = 0x36; //54
    RECV_CAR_CHARGING_STATUS = 0x3f; //63
    SEND_CAR_CONFIGURATION = 0x45; //69
    RECV_CAR_COLLISION = 0x4d; //77
    RECV_TRACK_SPECIAL_TRIGGER = 0x53; //83
    RECV_CAR_MESSAGE_CYCLE_OVERTIME = 0x86; //134
    SEND_SDK_MODE = 0x90; //144
```