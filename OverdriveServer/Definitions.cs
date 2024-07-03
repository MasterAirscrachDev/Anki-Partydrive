namespace OverdriveServer{
    public static class Definitions{
        public const byte SEND_CAR_DISCONNECT = 0x0d; //13
        public const byte SEND_PING = 0x16; //22
        public const byte RECV_PING = 0x17; //23
        public const byte SEND_VERSION = 0x18; //24
        public const byte RECV_VERSION = 0x19; //25
        public const byte SEND_BATTERY_REQUEST = 0x1a; //26
        public const byte RECV_BATTERY_RESPONSE = 0x1b; //27
        public const byte SEND_LIGHTS_UPDATE = 0x1d; //29
        public const byte SEND_CAR_SPEED_UPDATE = 0x24; //36
        public const byte SEND_CAR_LANE_CHANGE = 0x25; //37
        public const byte SEND_CAR_CANCEL_LANE_CHANGE = 0x26; //38
        public const byte RECV_TRACK_LOCATION = 0x27; //39
        public const byte RECV_TRACK_TRANSITION = 0x29; //41
        public const byte RECV_TRACK_INTERSECTION = 0x2a; //42
        public const byte RECV_CAR_ERROR = 0x2a; //42
        public const byte RECV_CAR_OFF_TRACK = 0x2b; //43
        public const byte SEND_TRACK_CENTER_UPDATE = 0x2c; //44
        public const byte RECV_TRACK_CENTER_UPDATE = 0x2d; //45
        public const byte SEND_CAR_CHANGE_DIRECTION = 0x32; //50
        public const byte SEND_LIGHTS_PATTERN_UPDATE = 0x33; //51
        public const byte RECV_CAR_SPEED_UPDATE = 0x36; //54
        public const byte RECV_CAR_CHARGING_STATUS = 0x3f; //63
        public const byte SEND_CAR_CONFIGURATION = 0x45; //69
        public const byte RECV_CAR_COLLISION = 0x4d; //77
        public const byte RECV_TRACK_SPECIAL_TRIGGER = 0x53; //83
        public const byte RECV_CAR_MESSAGE_CYCLE_OVERTIME = 0x86; //134
        public const byte SEND_SDK_MODE = 0x90; //144
    }
}
