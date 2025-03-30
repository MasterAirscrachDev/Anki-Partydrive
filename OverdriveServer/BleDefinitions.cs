namespace OverdriveServer{
    public static class Definitions{
        //IMPLEMENTED ==========================================================
        public const byte SEND_CAR_DISCONNECT = 0x0d; //13
        public const byte RECV_PING = 0x17; //23
        public const byte SEND_VERSION_REQUEST = 0x18; //24
        public const byte RECV_VERSION = 0x19; //25
        public const byte SEND_BATTERY_REQUEST = 0x1a; //26
        public const byte RECV_BATTERY_RESPONSE = 0x1b; //27
        public const byte SEND_LIGHTS_UPDATE = 0x1d; //29
        public const byte SEND_CAR_SPEED_UPDATE = 0x24; //36
        public const byte SEND_CAR_LANE_CHANGE = 0x25; //37
        public const byte RECV_TRACK_LOCATION = 0x27; //39
        public const byte RECV_TRACK_TRANSITION = 0x29; //41
        public const byte RECV_TRACK_INTERSECTION = 0x2a; //42
        public const byte RECV_CAR_DELOCALIZED = 0x2b; //43
        public const byte SEND_TRACK_CENTER_UPDATE = 0x2c; //44
        public const byte RECV_TRACK_CENTER_UPDATE = 0x2d; //45
        public const byte SEND_CAR_UTURN = 0x32; //50
        public const byte SEND_LIGHTS_PATTERN_UPDATE = 0x33; //51
        public const byte RECV_CAR_SPEED_UPDATE = 0x36; //54
        public const byte RECV_CAR_CHARGING_STATUS = 0x3f; //63
        public const byte RECV_TRACK_JUMP = 0x4b; //75
        public const byte RECV_CAR_COLLISION = 0x4d; //77
        public const byte RECV_TRACK_SPECIAL_TRIGGER = 0x53; //83
        public const byte SEND_SDK_MODE = 0x90; //144

        //NOT IMPLEMENTED / UNKNOWN ===========================================
        //Not implemented, but known
        public const byte SEND_PING = 0x16; //22
        public const byte SEND_SHUTDOWN = 0x1c; //28
        public const byte SEND_CAR_CANCEL_LANE_CHANGE = 0x26; //38
        public const byte SEND_CAR_CONFIGURATION = 0x45; //69
        public const byte SEND_ROAD_NETWORK_META = 0x49; //73
        public const byte RECV_TRACK_TRANSITION_2 = 0x51; //81
        public const byte RECV_AWAITING_NEXT_INIT = 0x54; //84
        public const byte SEND_REBOOT = 0x55; //85
        public const byte RECV_CAR_MESSAGE_CYCLE_OVERTIME = 0x86; //134
        public const byte RECV_DEBUG_RAW = 0xD2; //210
        //Unknown
        public const byte RECV_UNKNOWN_65 = 0x41; //65 slip ? car on track ?
        public const byte RECV_UNKNOWN_67 = 0x43; //67
        public const byte RECV_UNKNOWN_68 = 0x44; //68
        public const byte RECV_UNKNOWN_78 = 0x4e; //78
        public const byte RECV_UNKNOWN_79 = 0x4f; //79
        
    }
}
///////////////////////////////////////////////////////////////////////////////
//                      *** Message ID description ***
// 
// MSG_B2V_* -- Message from basestation to vehicle
// MSG_V2B_* -- Message from vehicle to basestation
// 
// Messages are in range of [0,255]. Reserved message blocks for categories:
// 
//  0  -  9  -- MSG_*2*_BOOT_* -- Bootloader-related messages
// 10  - 19  -- MSG_*2*_BTLE_* -- BTLE-related messages
// 20  - 119 -- MSG_*2*_CORE_* -- Messages related to core functionality (used
//                                in standard product use)
// 120 - 229 -- MSG_*2*_DEV_*  -- Development/diagnostics/testing messages. Used
//                                throughout development, manufacturing,
//                                diagnostics, logging, etc.
// 230 - 255 -- MSG_*2*_TMP_*  -- Messages for temporary use (development, etc.)
//////////////////////////////////////////////////////////////////////////////