namespace OverdriveServer{
    /// <summary>
    /// This is the Network Structures class, it contains all data structures used in the websocket.
    /// All data structures are serialized as JSON (or string for SYSTEM and UTILITY logs).
    /// The data structures are used to send and receive data from the server.
    /// </summary>
    [System.Serializable]
    public static class NetStructures{
        [System.Serializable]
        public class WebhookData { //This is the data structure used to send and receive data from the server
            public string EventType { get; set; } //Event type, EVENT_... is to cient, SV_... is to server
            public object Payload { get; set; }
        }
        [System.Serializable]
        public class CarData{
            public string name {get; set;} //Custom name
            public int model {get; set;} //ModelName
            public string id {get; set;} //Bluetooth ID
            public float offset {get; set;} //Horizontal Offset mm +/-
            public int speed {get; set;} //Speed in mm/s
            public bool charging {get; set;} //Is On Charger (should take priority over onTrack)
            public bool onTrack {get; set;} //Is the car on the track (doesnt seem very useful) (Addendum: goated)
            public int batteryStatus {get; set;} //0 = normal, 1 = charged, -1 = low battery
        }
        [System.Serializable]
        public class TransitionData { //This is sent, but i reccommend using SegmentData instead
            public string carID {get; set;} //Car ID (Bluetooth ID)
            public int trackPiece {get; set;} //Track Piece Idx (Will be 0)
            public int oldTrackPiece {get; set;} //Previous Track Piece Idx (Will be 0)
            public float offset {get; set;} //Offset in mm (may not be accurate)
            public int uphillCounter {get; set;} //Uphill counter
            public int downhillCounter {get; set;} //Downhill counter
            public int leftWheelDistance {get; set;} //Left Wheel Distance Traveled in mm
            public int rightWheelDistance {get; set;} //Right Wheel Distance Traveled in mm
            public bool crossedStartingLine {get; set;} //Crossed Starting Line (may not be accurate)
        }
        [System.Serializable]
        public class LocationData { //This is sent, but i reccommend using SegmentData instead
            public string carID {get; set;} //Car ID (Bluetooth ID)
            public int locationID {get; set;} //Location ID (If you know how this works then you should probably be making your own server)
            public int trackID {get; set;} //Track ID (resolves to a segmentType with conversion)
            public float offset {get; set;} //Offset in mm (may not be accurate)
            public int speed {get; set;} //Speed in mm/s
            public bool reversed {get; set;} //Is the track segment reversed (This doesn't mean the car is reversed)
        }
        [System.Serializable]
        public class CarLocationData { //Sent by OverdriveServer Tracking (Should be the most accurate)
            public string carID {get; set;} //Car ID (Bluetooth ID)
            public int trackIndex {get; set;} //Track Index (you should have a track saved at this point)
            public int speed {get; set;} //Speed in mm/s
            public float offset {get; set;} //Offset in mm
            public CarTrust trust {get; set;} //Car Trust (see CarTrust)
        }
        [System.Serializable]
        public enum CarTrust{
            Delocalized = -1, Unsure = 0, Trusted = 1 //Trust levels for the car location data
        }
        [System.Serializable]
        public enum SegmentType{
            Unknown, Straight, Turn, PreFinishLine, FinishLine, FnFSpecial, CrissCross, JumpRamp, JumpLanding
        }
        [System.Serializable]
        public class SegmentData{
            public SegmentType type {get; set;} //Segment Type
            public int internalID {get; set;} //Segment ID (0 = fallback, 10-63 = real piece, excluding drive mats)
            public bool reversed {get; set;} //Is the piece reversed (true = reversed, false = not reversed)
            public int up {get; set;} //Uphill counter (0-255)
            public int down {get; set;} //Downhill counter (0-255)
            public bool validated {get; set;} //Is the piece validated (true = validated, false = not validated)
        }
        [System.Serializable]
        public enum LightChannel{ RED = 0, TAIL, BLUE, GREEN, FRONTL, FRONTR, LIGHT_COUNT }
        [System.Serializable]
        public enum LightEffect{ STEADY = 0, FADE, THROB, FLASH, RANDOM, COUNT }

        [System.Serializable]
        public class LightData{
            public LightChannel channel {get; set;} //Light channel (see LightChannel)
            public LightEffect effect {get; set;} //Light effect (see LightEffect)
            public int startStrength {get; set;} //Start strength (0-255)
            public int endStrength {get; set;} //End strength (0-255)
            public int cyclesPer10Seconds {get; set;} //Cycles per 10 seconds (0-255)
        }
        
        // Event types for webhooks
        public const string EVENT_SYSTEM_LOG = "system_log", //System log string
        EVENT_UTILITY_LOG = "utility_log", //Utility log string
        EVENT_CAR_LOCATION = "car_location", //Car location data (see LocationData)
        EVENT_CAR_TRANSITION = "car_transition", //Car transition data (see TransitionData)
        EVENT_CAR_SEGMENT = "car_segment", //Car segment data (see NetSegment)
        EVENT_CAR_DELOCALIZED = "car_delocalized", //Car delocalized (currently not used, see MSG_CAR_DELOCALIZED for the time being)
        EVENT_CAR_TRACKING_UPDATE = "car_tracking_update", //Car tracking update (see CarLocationData)
        EVENT_TR_DATA = "track_data", //Track data (an array of SegmentData)
        EVENT_CAR_DATA = "car_data"; //Car data (an array of CarData)

        public const string SV_CAR_MOVE = "car_move_update", //Car move update [id:speed:offset] (speed and offset may be - meaning keep existing value)
        SV_REFRESH_CONFIGS = "refresh_configs", //Refresh configs (used to reload the car configs, name, speedbalance ect)
        SV_LINEUP = "lineup", // Request lineup (used to lineup the cars on the track)
        SV_CAR_S_LIGHTS = "car_s_lights", //Simple lights [id:Red:Green:Blue] (colours are 0-255)
        SV_CAR_C_LIGHTS = "car_c_lights", //Complex lights, a string id and array of LightData (see LightData) Min 1, Max 3
        SV_GET_TRACK = "get_track", //Get track (should return EVENT_TRACK_DATA with the track data)
        SV_TR_START_SCAN = "start_track_scan", //Start track scan (used to start a track scan)
        SV_TR_CANCEL_SCAN = "stop_track_scan", //Stop track scan (used to stop a track scan)
        SV_SCAN = "scan", //Scan for cars (used to start a scan for cars)
        SV_GET_CARS = "request_cars", //Request cars (should return EVENT_CAR_DATA with the car data)
        SV_CAR_FLASH = "car_flash", //DONT USE THIS UNLESS YOU KNOW WHAT YOU ARE DOING || Flash car [id:path] (used to flash a car, path should be the ota file)
        SV_TTS = "tts", //Text to speech [message] (used to send a message to the TTS engine)
        SV_CLIENT_CLOSED = "client_closed"; //Client closed (used to indicate a client has closed the connection intentionally)
        public static class UtilityMessages { //these are ids for the utility messages (parse them as strings)
            public const string MSG_CAR_CONNECTED = "cc", //:carID:name (used to indicate a car has connected)
            MSG_CAR_DISCONNECTED = "cd", //:carID (used to indicate a car has disconnected)
            MSG_CAR_DELOCALIZED = "deloc", //:carID (used to indicate a car is not on the track)
            MSG_CAR_SPEED_UPDATE = "sud", //:carID:speed:trueSpeed (Something internal has changed the speed)
            MSG_CAR_POWERUP = "pup", //:carID (a car has driven on a FnF powerup)
            MSG_TR_SCAN_UPDATE = "skup", //:carID:trackValidated (true/false/in-progress)
            MSG_CAR_STATUS_UPDATE = "csu", //:carID (a cars status has changed, call /cars)
            MSG_LINEUP = "lu", //:carID:remainingCars (if 0 then lineup is complete)
            MSG_CAR_FLASH_PROGRESS = "cfp"; //:carID:currBytes:totalBytes (both ints, used to indicate the progress of a car flash)
        }
        public enum ModelName{
            //Drive
            Kourai = 1, Boson = 2, Rho = 3, Katal = 4, Hadion = 5, Spektrix = 6, Corax = 7,
            //Overdrive
            Groundshock = 8, Skull = 9, Thermo = 10, Nuke = 11, Guardian = 12, Bigbang = 14, NukePhantom = 20,
            //Supertrucks
            Freewheel = 15, x52 = 16, x52Ice = 17,
            //FnF Cars
            Mammoth = 18, Dynamo = 19,
            Unknown = 0
        }
    }
    
}