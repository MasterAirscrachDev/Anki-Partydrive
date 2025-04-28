namespace OverdriveServer
{
    public enum MSG
    {
        SEND_PING = 22, RECV_PING = 23,
        SEND_REQUEST_CAR_VERSION = 24, RECV_CAR_VERSION = 25,
        SEND_BATTERY_VOLTAGE = 26, RECV_BATTERY_VOLTAGE = 27,
        SEND_SET_CAR_SPEED = 36,
        SEND_LANE_CHANGE = 37,
        SEND_CANCEL_LANE_CHANGE = 38,
        RECV_TR_POSITION_UPDATE = 39,
        RECV_TR_TRANSITION_UPDATE = 41,
        RECV_TR_INTERSECTION_POSITION_UPDATE = 42,
        RECV_TR_CAR_DELOCALIZED = 43,
        SEND_SET_OFFSET_FROM_ROAD_CENTER = 44,
        RECV_OFFSET_FROM_ROAD_CENTER_UPDATE = 45,
        SEND_OPEN_LOOP_TURN = 50,
        SEND_SET_LIGHTS_PATTERN = 51,
        SEND_SET_OFFSET_FROM_ROAD_CENTER_ADJUSTMENT = 52,
        RECV_SPEED_UPDATE = 54,
        RECV_STATUS_UPDATE = 63,
        RECV_LANE_CHANGE_UPDATE = 65,
        RECV_TR_FOUND_TRACK = 68,
        SEND_TR_STOP_ON_TRANSITION = 74,
        RECV_TR_JUMP_PIECE_BOOST = 75,
        RECV_TR_COLLISION_DETECTED = 77,
        RECV_JUMP_PIECE_RESULT = 78,
        RECV_TR_SPECIAL = 83,
        SEND_SDK_MODE = 144,
    }
    public enum OpenLoopTurnType
    {
        NONE, LEFT, RIGHT, U_TURN, JUMP_U_TURN
    }
    public enum OpenLoopTriggerCondition
    {
        IMMEDIATE, WAIT_FOR_TRANSISSION
    }
    public enum LightChannel
    {
        RED = 0x00, TAIL = 0x01, BLUE = 0x02, GREEN = 0x03, FRONTL = 0x04, FRONTR = 0x05, LIGHT_COUNT = 0x06
    }
    public enum LightEffect
    {
        STEADY = 0x00, FADE = 0x01, THROB = 0x02, FLASH = 0x03, RANDOM = 0x04, COUNT = 0x05
    }
    public enum ParseflagsMask
    {
        NUM_BITS = 0x0f, INVERTED_COLOR = 0x80, REVERSE_PARSING = 0x40
    }
    public class Structures
    {
        public struct SpeedUpdate
        {
            public short desiredSpeedMMPS;
            public short accelMMPS2;
            public short actualSpeedMMPS;
        }
        public struct OffsetUpdate
        {
            public float offsetFromCenterMM;
            public byte laneChangeID;
        }
        public struct CarStatus
        {
            public bool onTrack;
            public bool onCharger;
            public bool hasLowBattery;
            public bool hasChargedBattery;
        }
        public struct LaneChangeUpdate
        {
            public float currentOffset;
            public float targetOffset;
            public ushort horizontalSpeedMMPS;
            public short verticalSpeedMMPS;
            public byte laneChangeID;
        }
        public struct PositionUpdate
        {
            public int locationID;
            public int segmentID;
            public float offset;
            public short speedMMPS;
            public byte parsingFlags;
            public byte lastRecvdLaneChangeCmdID;
            public byte lastExecutedLaneChangeCmdID;
            public ushort lastDesiredLaneChangeSpeedMMPS;
            public short lastDesiredSpeedMMPS;
        }
        public struct TransitionUpdate
        {
            public int currentSegmentIdx;
            public int previousSegmentIdx;
            public float offset;
            public byte lastRecvdLaneChangeCmdID;
            public byte lastExecutedLaneChangeCmdID;
            public ushort lastDesiredLaneChangeSpeedMMPS;
            public byte avgFollowLineDrift;
            public byte hadLaneChangeActivity;
            public byte uphillCounter;
            public byte downhillCounter;
            public byte leftWheelDistanceCM;
            public byte rightWheelDistanceCM;
        }

        public struct JumpPieceBoost
        {
            public short speedMMPS;
            public short batteryMillivolts;
        }

        public struct JumpPieceResult
        {
            public short launchSpeedMMPS;
            public bool jumpFailed;
        }
        public struct CollisionDetected
        {
            public bool wasSideOnCollision;
            public bool wasFrontBackCollision;
        }
    }
    public static class Formatter
    {
        public static byte[] SEND_SetCarSpeed(short speedMMPS, short accelMMPS2, bool respectRoadPieceSpeedLimit, bool v4_compat)
        {
            byte[] message = new byte[v4_compat ? 8 : 7];
            message[0] = (byte)(v4_compat ? 7 : 6);
            message[1] = (byte)MSG.SEND_SET_CAR_SPEED;
            message[2] = (byte)(speedMMPS & 0xFF);
            message[3] = (byte)((speedMMPS >> 8) & 0xFF);
            message[4] = (byte)(accelMMPS2 & 0xFF);
            message[5] = (byte)((accelMMPS2 >> 8) & 0xFF);
            message[6] = respectRoadPieceSpeedLimit ? (byte)1 : (byte)0;
            if (v4_compat) { message[7] = (byte)1; } //dive without localization
            return message;
        }
        public static byte[] SEND_LaneChange(ushort horizontalVelMMPS, ushort horizontalAccMMPS2, float offsetFromCenterMM, byte hopIntent, byte laneChangeID)
        {
            byte[] message = new byte[12];
            message[0] = (byte)11;
            message[1] = (byte)MSG.SEND_LANE_CHANGE;
            message[2] = (byte)(horizontalVelMMPS & 0xFF);
            message[3] = (byte)((horizontalVelMMPS >> 8) & 0xFF);
            message[4] = (byte)(horizontalAccMMPS2 & 0xFF);
            message[5] = (byte)((horizontalAccMMPS2 >> 8) & 0xFF);
            BitConverter.GetBytes(offsetFromCenterMM).CopyTo(message, 6);
            message[10] = hopIntent;
            message[11] = laneChangeID;
            return message;
        }
        public static byte[] SEND_CancelLaneChange()
        {
            byte[] message = new byte[2];
            message[0] = (byte)1;
            message[1] = (byte)MSG.SEND_CANCEL_LANE_CHANGE;
            return message;
        }
        public static byte[] SEND_OpenLoopTurn(OpenLoopTurnType type, OpenLoopTriggerCondition condition)
        {
            byte[] message = new byte[4];
            message[0] = (byte)3;
            message[1] = (byte)MSG.SEND_OPEN_LOOP_TURN;
            message[2] = (byte)type;
            message[3] = (byte)condition;
            return message;
        }
        public static byte[] SEND_SetOffsetFromRoadCenter(float offsetFromCenterMM, bool isAdjustment = false)
        {
            byte[] message = new byte[6];
            message[0] = (byte)5;
            int code = isAdjustment ? (int)MSG.SEND_SET_OFFSET_FROM_ROAD_CENTER_ADJUSTMENT : (int)MSG.SEND_SET_OFFSET_FROM_ROAD_CENTER;
            message[1] = (byte)code;
            BitConverter.GetBytes(offsetFromCenterMM).CopyTo(message, 2);
            return message;
        }
        public static byte[] SEND_PingRequest()
        {
            byte[] message = new byte[2];
            message[0] = (byte)1;
            message[1] = (byte)MSG.SEND_PING;
            return message;
        }
        public static byte[] SEND_BatteryVoltageRequest()
        {
            byte[] message = new byte[2];
            message[0] = (byte)1;
            message[1] = (byte)MSG.SEND_BATTERY_VOLTAGE;
            return message;
        }
        public static byte[] SEND_StopOnTranstion()
        {
            byte[] message = new byte[2];
            message[0] = (byte)1;
            message[1] = (byte)MSG.SEND_TR_STOP_ON_TRANSITION;
            return message;
        }
    }
    public static class Parser
    {
        public static Structures.SpeedUpdate RECV_SpeedUpdate(byte[] message)
        {
            Structures.SpeedUpdate speedUpdate = new Structures.SpeedUpdate();
            speedUpdate.desiredSpeedMMPS = (short)(message[2] | (message[3] << 8));
            speedUpdate.accelMMPS2 = (short)(message[4] | (message[5] << 8));
            speedUpdate.actualSpeedMMPS = (short)(message[6] | (message[7] << 8));
            return speedUpdate;
        }
        public static Structures.OffsetUpdate RECV_OffsetUpdate(byte[] message)
        {
            Structures.OffsetUpdate offsetUpdate = new Structures.OffsetUpdate();
            offsetUpdate.offsetFromCenterMM = BitConverter.ToSingle(message, 2);
            offsetUpdate.laneChangeID = message[6];
            return offsetUpdate;
        }
        public static ushort RECV_BatteryVoltageResponse(byte[] message)
        {
            ushort millivolts = (ushort)(message[2] | (message[3] << 8));
            return millivolts;
        }
        public static Structures.CarStatus RECV_CarStatusUpdate(byte[] message)
        {
            Structures.CarStatus carStatus = new Structures.CarStatus();
            carStatus.onTrack = (message[2] & 0x01) != 0;
            carStatus.onCharger = (message[3] & 0x01) != 0;
            carStatus.hasLowBattery = (message[4] & 0x01) != 0;
            carStatus.hasChargedBattery = (message[5] & 0x01) != 0;
            return carStatus;
        }
        public static Structures.LaneChangeUpdate RECV_LaneChangeUpdate(byte[] message)
        {
            Structures.LaneChangeUpdate laneChangeUpdate = new Structures.LaneChangeUpdate();
            laneChangeUpdate.currentOffset = BitConverter.ToSingle(message, 2);
            laneChangeUpdate.targetOffset = BitConverter.ToSingle(message, 6);
            laneChangeUpdate.horizontalSpeedMMPS = (ushort)(message[10] | (message[11] << 8));
            laneChangeUpdate.verticalSpeedMMPS = (short)(message[12] | (message[13] << 8));
            laneChangeUpdate.laneChangeID = message[14];
            return laneChangeUpdate;
        }
        public static Structures.PositionUpdate RECV_PositionUpdate(byte[] message)
        {
            Structures.PositionUpdate positionUpdate = new Structures.PositionUpdate();
            positionUpdate.locationID = message[2];
            positionUpdate.segmentID = message[3];
            positionUpdate.offset = BitConverter.ToSingle(message, 4);
            positionUpdate.speedMMPS = (short)(message[8] | (message[9] << 8));
            positionUpdate.parsingFlags = message[10];
            positionUpdate.lastRecvdLaneChangeCmdID = message[11];
            positionUpdate.lastExecutedLaneChangeCmdID = message[12];
            positionUpdate.lastDesiredLaneChangeSpeedMMPS = (ushort)(message[13] | (message[14] << 8));
            positionUpdate.lastDesiredSpeedMMPS = (short)(message[15] | (message[16] << 8));
            return positionUpdate;
        }
        public static Structures.TransitionUpdate RECV_TransitionUpdate(byte[] message)
        {
            Structures.TransitionUpdate locationUpdate = new Structures.TransitionUpdate();
            locationUpdate.currentSegmentIdx = message[2];
            locationUpdate.previousSegmentIdx = message[3];
            locationUpdate.offset = BitConverter.ToSingle(message, 4);
            locationUpdate.lastRecvdLaneChangeCmdID = message[8];
            locationUpdate.lastExecutedLaneChangeCmdID = message[9];
            locationUpdate.lastDesiredLaneChangeSpeedMMPS = (ushort)(message[10] | (message[11] << 8));
            locationUpdate.avgFollowLineDrift = message[12];
            locationUpdate.hadLaneChangeActivity = message[13];
            locationUpdate.uphillCounter = message[14];
            locationUpdate.downhillCounter = message[15];
            locationUpdate.leftWheelDistanceCM = message[16];
            locationUpdate.rightWheelDistanceCM = message[17];
            return locationUpdate;
        }
        public static Structures.JumpPieceBoost RECV_JumpPieceBoost(byte[] message)
        {
            Structures.JumpPieceBoost jumpPieceBoost = new Structures.JumpPieceBoost();
            jumpPieceBoost.speedMMPS = (short)(message[2] | (message[3] << 8));
            jumpPieceBoost.batteryMillivolts = (short)(message[4] | (message[5] << 8));
            return jumpPieceBoost;
        }

        public static Structures.JumpPieceResult RECV_JumpPieceResult(byte[] message)
        {
            Structures.JumpPieceResult jumpPieceResult = new Structures.JumpPieceResult();
            jumpPieceResult.launchSpeedMMPS = (short)(message[2] | (message[3] << 8));
            jumpPieceResult.jumpFailed = message[4] != 0;
            return jumpPieceResult;
        }

        public static Structures.CollisionDetected RECV_CollisionDetected(byte[] message)
        {
            Structures.CollisionDetected collisionDetected = new Structures.CollisionDetected();
            collisionDetected.wasSideOnCollision = message[2] != 0;
            collisionDetected.wasFrontBackCollision = message[3] != 0;
            return collisionDetected;
        }
    }
}