namespace OverdriveServer{
    [System.Serializable]
    public static class NetStructures{
        [System.Serializable]
        public class CarData{
            public string name {get; set;}
            public string id {get; set;}
            public float offset {get; set;}
            public int speed {get; set;}
            public int battery {get; set;}
            public bool charging {get; set;}
            public bool onTrack {get; set;}
            public int batteryStatus {get; set;}
            public CarData(string name, string id){
                this.name = name;
                this.id = id;
            }
        }
        [System.Serializable]
        public class TransitionData {
            public string carID {get; set;}
            public int trackPiece {get; set;}
            public int oldTrackPiece {get; set;}           
            public float offset {get; set;}
            public int uphillCounter {get; set;}
            public int downhillCounter {get; set;}
            public int leftWheelDistance {get; set;}
            public int rightWheelDistance {get; set;}
            public bool crossedStartingLine {get; set;}
        }
        [System.Serializable]
        public class LocationData {
            public string carID {get; set;}
            public int trackLocation {get; set;}
            public int trackID {get; set;}
            public float offset {get; set;}
            public int speed {get; set;}
            public bool clockwise {get; set;}
        }
        [System.Serializable]
        public class CarLocationData {
            public string carID {get; set;}
            public int trackIndex {get; set;}
            public int speed {get; set;}
            public float offset {get; set;}
            public bool positionTrusted {get; set;}
        }
        // Define event types for webhooks
        public const string EVENT_SYSTEM_LOG = "system_log";
        public const string EVENT_UTILITY_LOG = "utility_log";
        public const string EVENT_CAR_LOCATION = "car_location";
        public const string EVENT_CAR_TRANSITION = "car_transition";
        public const string EVENT_CAR_DELOCALIZED = "car_delocalized";
        public const string EVENT_CAR_TRACKING_UPDATE = "car_tracking_update";
        public const string SV_CAR_MOVE = "car_move_update";
        public const string SV_REFRESH_CONFIGS = "refresh_configs";
        public const string SV_LINEUP = "lineup";

        public static class UtilityMessages {
            public const string MSG_CAR_CONNECTED = "cc"; //:carID:name
            public const string MSG_CAR_DISCONNECTED = "cd";//:carID
            public const string MSG_CAR_DELOCALIZED = "deloc";//:carID
            public const string MSG_CAR_SPEED_UPDATE = "sud";//:carID:speed:trueSpeed
            public const string MSG_CAR_POWERUP = "pup"; //:carID
            public const string MSG_TR_SCAN_UPDATE = "skup"; //:carID:trackValidated
            public const string MSG_CAR_STATUS_UPDATE = "csu"; //:carID
        }
    }
}