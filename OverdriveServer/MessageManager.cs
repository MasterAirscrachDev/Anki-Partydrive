using static OverdriveServer.Definitions;
using static OverdriveServer.NetStructures;
using static OverdriveServer.Tracks;
namespace OverdriveServer {
    class MessageManager {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        public void ParseMessage(byte[] content, Car car){
            byte id = content[1];
            if(id == RECV_PING){//23 ping response
                Program.Log($"[23] Ping response: {Program.BytesToString(content)}");

            } else if(id == RECV_VERSION){ //25 version response
                //int8 size, int8 id, int16 version
                //read the version from the content
                short version = BitConverter.ToInt16(content, 2);
                Program.Log($"[25] Version response: {version} for {car.name}");
                car.SetCarSoftwareVersion(version);
            } else if(id == RECV_BATTERY_RESPONSE){ //27 battery response
                int battery = content[2];
                int maxBattery = 3800;
                Program.Log($"[27] Battery response: {battery} / {maxBattery}");
                Program.UtilLog($"27:{car.id}:{battery}");
                car.data.battery = battery;
            } else if(id == RECV_TRACK_LOCATION){ //39 where is car
                OnPosition(content, car); //moved to dedicated function bc its complex
            } else if(id == RECV_TRACK_TRANSITION){ //41 car moved between track pieces
                OnTransition(content, car); //moved to dedicated function bc its complex
            } else if(id == RECV_TRACK_INTERSECTION){ //42 track intersection
                //
            } else if(id == RECV_CAR_DELOCALIZED){ //43 Off track
                Program.UtilLog($"43:{car.id}");
                CarEventDelocalised?.Invoke(car.id);
            } else if(id == RECV_TRACK_CENTER_UPDATE){ //45 Track center updated
                //this reads the track center, however this gives us the same info as 39 and 41 making it unhelpful when trying to correct for errors
                Program.Log($"[45] Track center updated: {Program.BytesToString(content)}, Test as: {BitConverter.ToSingle(content, 2)}");
                Program.UtilLog($"45:{car.id}:{Program.BytesToString(content)}");
            } else if(id == RECV_CAR_SPEED_UPDATE){ //54 car speed changed
                Program.Log($"[54] {car.name} speed changed");
                Program.UtilLog($"54:{car.id}");
            } else if(id == RECV_CAR_CHARGING_STATUS){ //63 charging status changed
                bool charging = content[3] == 1;
                Program.UtilLog($"63:{car.id}:{charging}");
                Program.Log($"[63] {car.name} charging: {charging}");
                car.data.charging = charging;
            } else if(id == RECV_UNKNOWN_65){ //65 Unknown
                Program.UtilLog($"65:{car.id}");
                //Program.Log($"[65] {car.name} slipped");
            } else if(id == RECV_UNKNOWN_67){ //67 Unknown
                Program.UtilLog($"67:{car.id}");
                //Program.Log($"[67] {car.name} ???");
            } else if(id == RECV_TRACK_JUMP){ //75 car jumped
                //this provides a lot of data, unsure what any of it means / how to parse
                Program.UtilLog($"75:{car.id}");
                Program.Log($"[75] {car.name} jumped");
                CarEventJumpCall?.Invoke(car.id);
            } else if(id == RECV_CAR_COLLISION){ //77 Collision Detected
                Program.UtilLog($"77:{car.id}");
                Program.Log($"[77] {car.name} collision detected");
            } else if(id == RECV_UNKNOWN_78){ //78 Unknown
                Program.UtilLog($"78:{car.id}");
                //Program.Log($"[78] {car.name} ???");
            } else if(id == RECV_UNKNOWN_79){ //79 Unknown
                Program.UtilLog($"79:{car.id}");
                //Program.Log($"[79] {car.name} ???");
            } else if(id == RECV_TRACK_SPECIAL_TRIGGER){ //83 FnF specialBlock
                Program.UtilLog($"83:{car.id}");
                Program.Log($"[83] {car.name} hit special block");
            } else if(id == RECV_CAR_MESSAGE_CYCLE_OVERTIME){ //134 Car message cycle overtime
                Program.UtilLog($"134:{car.id}");
                //Program.Log($"[134] {car.name} message cycle overtime"); //commented until we know what this is
            }
            else{
                Program.Log($"???({id})[{Program.IntToByteString(id)}]:{Program.BytesToString(content)}");
            }
        }
        void OnPosition(byte[] content, Car car){
            int trackLocation = content[2], trackID = content[3], speed = BitConverter.ToInt16(content, 8);
            float offset = BitConverter.ToSingle(content, 4);
            bool clockwise = content[10] == 0x47; //this is a secret parsing flag, we can use it to decphier turn directions
            Program.socketMan.Notify(EVENT_CAR_LOCATION, 
                new LocationData {
                    carID = car.id,
                    trackLocation = trackLocation,
                    trackID = trackID,
                    offset = offset,
                    speed = speed,
                    clockwise = clockwise
                }
            );
            //Program.Log($"[39] {car.name} Track location: {trackLocation}, track ID: {trackID}, offset: {offset}, speed: {speed}, clockwise: {clockwise}");
            car.data.trackPosition = trackLocation; 
            car.data.trackID = trackID;
            car.data.laneOffset = offset;
            car.data.speed = speed;
            CarEventLocationCall?.Invoke(car.id, trackLocation, trackID, offset, speed, clockwise);

            car.lastPositionID = trackID; car.lastFlipped = clockwise; //set the last track piece to the current one

        }
        void OnTransition(byte[] content, Car car){
            try{
                if(content.Length < 18){ return; } //not enough data
                int trackPiece = Convert.ToInt32((sbyte)content[2]), oldTrackPiece = Convert.ToInt32((sbyte)content[3]); //0,0 because we are using sdk mode
                int uphillCounter = content[14], downhillCounter = content[15];
                int leftWheelDistance = content[16], rightWheelDistance = content[17];
                float offset = BitConverter.ToSingle(content, 4);

                // There is a shorter segment for the starting line track. (may fail on inside turns) 35, 34
                bool crossedStartingLine = (leftWheelDistance < 36) && (leftWheelDistance > 32) && (rightWheelDistance < 36) && (rightWheelDistance > 32);
                //Program.Log($"[41] {car.name} Track: {trackPiece} from {oldTrackPiece}, Y:(+{uphillCounter} -{downhillCounter}), X: {offset} LwheelDist: {leftWheelDistance}, RwheelDist: {rightWheelDistance}, Finish: {crossedStartingLine}");
                Program.socketMan.Notify(EVENT_CAR_TRANSITION, 
                    new TransitionData{
                        carID = car.id,
                        trackPiece = trackPiece,
                        oldTrackPiece = oldTrackPiece,
                        offset = offset,
                        uphillCounter = uphillCounter,
                        downhillCounter = downhillCounter,
                        leftWheelDistance = leftWheelDistance,
                        rightWheelDistance = rightWheelDistance,
                        crossedStartingLine = crossedStartingLine
                    }
                );
                CarEventTransitionCall?.Invoke(car.id, trackPiece, oldTrackPiece, offset, uphillCounter, downhillCounter, leftWheelDistance, rightWheelDistance, crossedStartingLine);
                car.data.laneOffset = offset;
                car.LaneCheck();
                
                SolveSegment(car, car.lastPositionID, car.lastFlipped, leftWheelDistance, rightWheelDistance, crossedStartingLine, offset, uphillCounter, downhillCounter); //solve the segment
                car.lastPositionID = 0; //set the last track piece to the current one
            }
            catch{
                Program.Log($"[41] Error parsing car track update: {Program.BytesToString(content)}");
                return;
            }
        }
        void SolveSegment(Car car, int ID, bool flipped, int left, int right, bool finish, float offset, int up, int down){
            TrackPiece segment = new TrackPiece(TrackPieceType.Unknown, 0, false); //create a new track piece
            if(ID != 0){ 
                TrackPieceType type = TrackManager.PieceFromID(ID);
                if(type != TrackPieceType.Unknown){ segment = new TrackPiece(type, ID, flipped); } //if we have a valid ID, set the segment to that
            }else{
                //difference can be upwards of 9 on turns
                // 39, 30 = inside turn
                // 61, 51 = outside turn
                // 57,56 = straight
                // 22, 22 = finish line
                // 34, 34 = prefinish line

                TrackPieceType type = (TrackManager.Abs(left - right) < 4) ? TrackPieceType.Straight : TrackPieceType.Turn;
                if(type == TrackPieceType.Straight && finish){ type = TrackPieceType.PreFinishLine; } //if we are straight and at the finish line, set it to prefinish line
                flipped = (type == TrackPieceType.Straight) ? false : (left > right);
                segment = new TrackPiece(type, 0, flipped); //fallback to straight or turn
            }
            Console.WriteLine($"Car: {car.id} L: {left}, R: {right}, Segment: {segment}");
            CarEventSegmentCall?.Invoke(car.id, segment, offset, up, down); //call the event
        }

#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        //subscribable event
        public delegate void CarEventLocation(string carID, int trackLocation, int trackID, float offset, int speed, bool clockwise);
        public event CarEventLocation? CarEventLocationCall;
        public delegate void CarEventTransition(string carID, int trackPieceIdx, int oldTrackPieceIdx, float offset, int uphillCounter, int downhillCounter, int leftWheelDistance, int rightWheelDistance, bool crossedStartingLine);
        public event CarEventTransition? CarEventTransitionCall;
        public delegate void CarEventSegment(string carID, TrackPiece trackPiece, float offset, int uphill, int downhill);
        public event CarEventSegment? CarEventSegmentCall;
        public delegate void CarEventFell(string carID);
        public event CarEventFell? CarEventDelocalised;
        public delegate void CarEventJump(string carID);
        public event CarEventJump? CarEventJumpCall;
    }
}