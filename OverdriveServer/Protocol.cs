using static OverdriveServer.NetStructures;
namespace OverdriveBLEProtocol 
{
    public enum MSG{
        // BletoothLE messages (10-19) =======================================================================
        SEND_BT_DISCONNECT = 13,
        // Core functionality messages (20-119) ==============================================================
        // Ping request / response
        SEND_PING = 22, RECV_PING = 23,
        // Messages for checking vehicle code version info
        SEND_REQUEST_CAR_VERSION = 24, RECV_CAR_VERSION = 25,
        // Battery state messages
        SEND_BATTERY_VOLTAGE = 26, RECV_BATTERY_VOLTAGE = 27,
        // Control commands =================================================================================
        SEND_SET_CAR_SPEED = 36,
        SEND_LANE_CHANGE = 37,
        SEND_CANCEL_LANE_CHANGE = 38, //Cancels a currently ongoing lane change and merges with the next lane found
        // Tracking messages ================================================================================
        RECV_TR_POSITION_UPDATE = 39, // Absolute position update from vehicle: position / location ID 
        RECV_TR_TRANSITION_UPDATE = 41,
        RECV_TR_INTERSECTION_POSITION_UPDATE = 42, //Triggered on crisscross
        RECV_TR_CAR_DELOCALIZED = 43,
        // Command from server to vehicle to correct its offset from road center
        // (after a successful localization).
        SEND_SET_OFFSET_FROM_ROAD_CENTER = 44,
        // Message from vehicle to server to correct its offset from road center
        // and confirm completed lane changes.
        RECV_OFFSET_FROM_ROAD_CENTER_UPDATE = 45,
        // Message from server to vehicle to do an open loop turn (180, or 90 deg turns)
        SEND_OPEN_LOOP_TURN = 50,
        // Message to set the default light pattern
        SEND_SET_LIGHTS_PATTERN = 51,
        // Message for a relative road offset from center adjustment
        SEND_SET_OFFSET_FROM_ROAD_CENTER_ADJUSTMENT = 52,
        RECV_SPEED_UPDATE = 54,
        SEND_DO_NOT_USE = 58, //leaving this in only to say never use it! YOU HAVE BEEN WARNED
        RECV_STATUS_UPDATE = 63,
        RECV_LANE_CHANGE_UPDATE = 65,
        // Message sent when vehicle enters delocalization auto-recovery
        RECV_TR_DELOC_AUTO_RECOVERY_ENTERED = 67,
        // Message sent when vehicle fails at auto-recovery and becomes delocalized
        RECV_TR_DELOC_AUTO_RECOVERY_SUCCESS = 68,
        //Command from server to vehicle to stop on transition bar
        SEND_TR_STOP_ON_TRANSITION = 74,
        // Jump Piece Boost info
        RECV_TR_JUMP_PIECE_BOOST = 75,
        // Collision detected message
        RECV_TR_COLLISION_DETECTED = 77,
        // Jump Piece Result (launch info, landing success/failure, etc)
        RECV_JUMP_PIECE_RESULT = 78,
        RECV_SUPERCODE = 79,
        RECV_TR_SPECIAL = 83,

        RECV_DEV_CYCLE_OVERTIME = 134,
        SEND_SDK_MODE = 144,
        //command to update car model (partydrive and newer)
        SEND_DEV_UPDATE_MODEL = 147,
        // Response from car after model update
        RECV_DEV_UPDATE_MODEL_RESPONSE = 148,
    }
    public enum OpenLoopTurnType{
        NONE, LEFT, RIGHT, U_TURN, JUMP_U_TURN
    }
    public enum OpenLoopTrigger{
        IMMEDIATE, WAIT_FOR_SEGMENT
    }
    public enum LightChannel{
        RED = 0x00, TAIL = 0x01, BLUE = 0x02, GREEN = 0x03, FRONTL = 0x04, FRONTR = 0x05, LIGHT_COUNT = 0x06
    }
    public enum LightEffect{
        STEADY = 0x00, FADE = 0x01, THROB = 0x02, FLASH = 0x03, RANDOM = 0x04, COUNT = 0x05
    }
    public enum ParseflagsMask{
        NUM_BITS = 0x0f, INVERTED_COLOR = 0x80, REVERSE_PARSING = 0x40
    }
    public class Structures{
        public struct SpeedUpdate{
            public short desiredSpeedMMPS;
            public short accelMMPS2;
            public short actualSpeedMMPS;
        }
        public struct OffsetUpdate{
            public float offsetFromCenterMM;
            public byte laneChangeID;
        }
        public struct CarStatus{
            public bool onTrack;
            public bool onCharger;
            public bool hasLowBattery;
            public bool hasChargedBattery;
        }
        public struct LaneChangeUpdate{
            public float currentOffset;
            public float targetOffset;
            public ushort horizontalSpeedMMPS;
            public short verticalSpeedMMPS;
            public byte laneChangeID;
        }
        public struct PositionUpdate{
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
        public struct TransitionUpdate{
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
        public struct IntersectionPositionUpdate{
            public int currRoadPieceIdx;
            public float offset;
            public byte intersectionCode; // 0: No intersection code (used for abnormal exit notification) 1: First entry connection 2: First exit connection 3: Second entry connection 4: Second exit connection
            public bool isExiting; // boolean flag for intersection update message
            public ushort mmSinceLastTransBar; // measured by encoders
            public ushort mmSinceLastIntersectionCode; // actual intersection code read, measured by encoders
        }
        public struct JumpPieceBoost{
            public short speedMMPS;
            public short batteryMillivolts;
        }
        
        public struct JumpPieceResult{
            public short launchSpeedMMPS;
            public bool jumpFailed;
        }
        
        public struct SupercodeDetected{
            public uint supercodeSeq;
            public int currRoadPieceIdx;
        }
        public struct CollisionDetected{
            public bool wasSideOnCollision;
            public bool wasFrontBackCollision;
        }
        public struct UpdateModelResponse{
            public byte status;      // Flash operation status (0 = success, non-zero = error)
            public uint modelID;     // Current model ID after operation
        }
    }
    public static class Formatter
    {
        public static byte[] SEND_BLEDisconnect()
        {
            byte[] message = new byte[2];
            message[0] = (byte)1;
            message[1] = (byte)MSG.SEND_BT_DISCONNECT;
            return message;
        }
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
            if (v4_compat) { message[7] = (byte)1; } //drive without localization
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
        public static byte[] SEND_OpenLoopTurn(OpenLoopTurnType type, OpenLoopTrigger condition)
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
        public static byte[] SEND_EnableSDKMode(){
            byte[] message = new byte[4];
            message[0] = (byte)3;
            message[1] = (byte)MSG.SEND_SDK_MODE;
            message[2] = 1; // Enable SDK mode
            message[3] = 1; // Localize Override (Depricated, unused)
            return message;
        }
        public static byte[] SEND_DEV_UpdateModel(uint newModel)
        {
            byte[] message = new byte[6];
            message[0] = (byte)5;  // Size (6 bytes total - 1 for header = 5)
            message[1] = (byte)MSG.SEND_DEV_UPDATE_MODEL;
            message[2] = (byte)(newModel & 0xFF);
            message[3] = (byte)((newModel >> 8) & 0xFF);
            message[4] = (byte)((newModel >> 16) & 0xFF);
            message[5] = (byte)((newModel >> 24) & 0xFF);
            return message;
        }
    }
    public static class Parser{
        public static Structures.SpeedUpdate RECV_SpeedUpdate(byte[] message){
            Structures.SpeedUpdate speedUpdate = new Structures.SpeedUpdate();
            speedUpdate.desiredSpeedMMPS = (short)(message[2] | (message[3] << 8));
            speedUpdate.accelMMPS2 = (short)(message[4] | (message[5] << 8));
            speedUpdate.actualSpeedMMPS = (short)(message[6] | (message[7] << 8));
            return speedUpdate;
        }
        public static Structures.OffsetUpdate RECV_OffsetUpdate(byte[] message){
            Structures.OffsetUpdate offsetUpdate = new Structures.OffsetUpdate();
            offsetUpdate.offsetFromCenterMM = BitConverter.ToSingle(message, 2);
            offsetUpdate.laneChangeID = message[6];
            return offsetUpdate;
        }
        public static ushort RECV_BatteryVoltageResponse(byte[] message){
            ushort millivolts = (ushort)(message[2] | (message[3] << 8));
            return millivolts;
        }
        public static Structures.CarStatus RECV_CarStatusUpdate(byte[] message){
            Structures.CarStatus carStatus = new Structures.CarStatus();
            carStatus.onTrack = (message[2] & 0x01) != 0;
            carStatus.onCharger = (message[3] & 0x01) != 0;
            carStatus.hasLowBattery = (message[4] & 0x01) != 0;
            carStatus.hasChargedBattery = (message[5] & 0x01) != 0;
            return carStatus;
        }
        public static Structures.LaneChangeUpdate RECV_LaneChangeUpdate(byte[] message){
            Structures.LaneChangeUpdate laneChangeUpdate = new Structures.LaneChangeUpdate();
            laneChangeUpdate.currentOffset = BitConverter.ToSingle(message, 2);
            laneChangeUpdate.targetOffset = BitConverter.ToSingle(message, 6);
            laneChangeUpdate.horizontalSpeedMMPS = (ushort)(message[10] | (message[11] << 8));
            laneChangeUpdate.verticalSpeedMMPS = (short)(message[12] | (message[13] << 8));
            laneChangeUpdate.laneChangeID = message[14];
            return laneChangeUpdate;
        }
        public static Structures.PositionUpdate RECV_PositionUpdate(byte[] message){
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
        public static Structures.TransitionUpdate RECV_TransitionUpdate(byte[] message){
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
        public static Structures.IntersectionPositionUpdate RECV_IntersectionPositionUpdate(byte[] message){
            Structures.IntersectionPositionUpdate intersectionPositionUpdate = new Structures.IntersectionPositionUpdate();
            intersectionPositionUpdate.currRoadPieceIdx = message[2];
            intersectionPositionUpdate.offset = BitConverter.ToSingle(message, 3);
            intersectionPositionUpdate.intersectionCode = message[7];
            intersectionPositionUpdate.isExiting = message[8] != 0;
            intersectionPositionUpdate.mmSinceLastTransBar = (ushort)(message[9] | (message[10] << 8));
            intersectionPositionUpdate.mmSinceLastIntersectionCode = (ushort)(message[11] | (message[12] << 8));
            return intersectionPositionUpdate;
        }
        public static uint RECV_VehicleVersion(byte[] message){
            uint version = (uint)(message[2] | (message[3] << 8) | (message[4] << 16) | (message[5] << 24));
            return version;
        }
        public static Structures.JumpPieceBoost RECV_JumpPieceBoost(byte[] message){
            Structures.JumpPieceBoost jumpPieceBoost = new Structures.JumpPieceBoost();
            jumpPieceBoost.speedMMPS = (short)(message[2] | (message[3] << 8));
            jumpPieceBoost.batteryMillivolts = (short)(message[4] | (message[5] << 8));
            return jumpPieceBoost;
        }
        
        public static Structures.JumpPieceResult RECV_JumpPieceResult(byte[] message){
            Structures.JumpPieceResult jumpPieceResult = new Structures.JumpPieceResult();
            jumpPieceResult.launchSpeedMMPS = (short)(message[2] | (message[3] << 8));
            jumpPieceResult.jumpFailed = message[4] != 0;
            return jumpPieceResult;
        }
        
        public static Structures.SupercodeDetected RECV_Supercode(byte[] message){
            Structures.SupercodeDetected supercodeDetected = new Structures.SupercodeDetected();
            supercodeDetected.supercodeSeq = (uint)(message[2] | (message[3] << 8) | (message[4] << 16) | (message[5] << 24));
            supercodeDetected.currRoadPieceIdx = message[6];
            return supercodeDetected;
        }
        public static Structures.CollisionDetected RECV_CollisionDetected(byte[] message){
            Structures.CollisionDetected collisionDetected = new Structures.CollisionDetected();
            collisionDetected.wasSideOnCollision = message[2] != 0;
            collisionDetected.wasFrontBackCollision = message[3] != 0;
            return collisionDetected;
        }
        public static Structures.UpdateModelResponse RECV_UpdateModelResponse(byte[] message){
            Structures.UpdateModelResponse response = new Structures.UpdateModelResponse();
            response.status = message[2];
            response.modelID = (uint)(message[3] | (message[4] << 8) | (message[5] << 16) | (message[6] << 24));
            return response;
        }
    }
}