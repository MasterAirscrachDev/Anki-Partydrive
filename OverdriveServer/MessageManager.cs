using static OverdriveServer.NetStructures;
using static OverdriveServer.NetStructures.UtilityMessages;
using static OverdriveServer.Tracks;
namespace OverdriveServer {
    class MessageManager {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        public void ParseMessage(byte[] content, Car car){
            byte id = content[1];
            if(id == (int)MSG.RECV_PING){//23 ping response
                Program.Log($"[23] Ping response: {Program.BytesToString(content)}");
            } else if(id == (int)MSG.RECV_CAR_VERSION){ //25 version response
                //int8 size, int8 id, int16 version
                //read the version from the content
                short version = BitConverter.ToInt16(content, 2);
                Program.Log($"[25] Version response: {version} for {car.name}");
                car.SetCarSoftwareVersion(version); 
            } else if(id == (int)MSG.RECV_BATTERY_VOLTAGE){ //27 battery response
                //int battery = content[2];
                //Program.Log($"[27] Battery response: {battery} / {maxBattery}");
                //Program.UtilLog($"27:{car.id}:{battery}");
            } else if(id == (int)MSG.RECV_TR_POSITION_UPDATE){ //39 where is car
                OnPosition(Parser.RECV_PositionUpdate(content), car); //moved to dedicated function bc its complex
            } else if(id == (int)MSG.RECV_TR_TRANSITION_UPDATE){ //41 car moved between track pieces
                OnTransition(Parser.RECV_TransitionUpdate(content), car); //moved to dedicated function bc its complex
            } else if(id == (int)MSG.RECV_TR_INTERSECTION_POSITION_UPDATE){ //42 track intersection
                //
            } else if(id == (int)MSG.RECV_TR_CAR_DELOCALIZED){ //43 Off track
                Program.UtilLog($"{MSG_CAR_DELOCALIZED}:{car.id}");
                car.data.onTrack = false; //set the car to off track
                CarEventDelocalised?.Invoke(car.id);
            } else if(id == (int)MSG.RECV_OFFSET_FROM_ROAD_CENTER_UPDATE){ //45 Track center updated
                float offset = BitConverter.ToSingle(content, 2); //get the offset from the content
                car.data.offsetMM = offset; //set the offset
            } else if(id == (int)MSG.RECV_SPEED_UPDATE){ //54 car speed changed
                Structures.SpeedUpdate speed = Parser.RECV_SpeedUpdate(content);
                Program.UtilLog($"{MSG_CAR_SPEED_UPDATE}:{car.id}:{speed.desiredSpeedMMPS}:{speed.actualSpeedMMPS}");
                car.data.speedMMPS = speed.desiredSpeedMMPS; //set the speed
            } else if(id == (int)MSG.RECV_STATUS_UPDATE){ //63 car status changed
                Structures.CarStatus state = Parser.RECV_CarStatusUpdate(content);
                Program.UtilLog($"{MSG_CAR_STATUS_UPDATE}:{car.id}");
                //Program.Log($"[63] {car.name} chargedBattery:{state.hasChargedBattery} lowBattery:{state.hasLowBattery} charger:{state.onCharger} onTrack:{state.onTrack}");
                car.data.charging = state.onCharger;
                car.GoIfNotGoing(state.onTrack); //if the car is on the charger, go to the charger
                car.data.onTrack = state.onCharger? false : state.onTrack; //if the car is on the charger, it is not on track
                car.data.batteryStatus = state.hasChargedBattery ? 1 : (state.hasLowBattery ? -1 : 0); //0 = normal, 1 = charged, -1 = low battery
            } else if(id == (int)MSG.RECV_LANE_CHANGE_UPDATE){ //65 car lane change status update
                //Program.UtilLog($"65:{car.id}");
            } else if(id == (int)MSG.RECV_TR_FOUND_TRACK){ //68 car auto recovery success
                car.data.onTrack = true; //set the car to on track
            }else if(id == (int)MSG.RECV_TR_JUMP_PIECE_BOOST){ //75 car jumped
                Program.UtilLog($"{MSG_CAR_JUMPED}:{car.id}");
                CarEventJumpCall?.Invoke(car.id);
            } else if(id == (int)MSG.RECV_TR_COLLISION_DETECTED){ //77 Collision Detected
                //Program.UtilLog($"77:{car.id}");
            } else if(id == (int)MSG.RECV_JUMP_PIECE_RESULT){ //78 Jump Piece Result
                Structures.JumpPieceResult result = Parser.RECV_JumpPieceResult(content); //get the result from the content
                Program.UtilLog($"{MSG_CAR_LANDED}:{car.id}:{!result.jumpFailed}"); //log the result
            } else if(id == (int)MSG.RECV_TR_SPECIAL){ //83 FnF specialBlock
                Program.UtilLog($"{MSG_CAR_POWERUP}:{car.id}");
            }
            else{
                Program.Log($"???({id})[{Program.IntToByteString(id)}]:{Program.BytesToString(content)}");
            }
        }
        void OnPosition(Structures.PositionUpdate p, Car car){
            try{
                bool reversed = (p.parsingFlags & (int)ParseflagsMask.REVERSE_PARSING) != 0; //check if the car is reversed
                int bits = p.parsingFlags & (int)ParseflagsMask.NUM_BITS; //get the parsing flags
                Program.socketMan.Notify(EVENT_CAR_LOCATION, 
                    new LocationData {
                        carID = car.id,
                        locationID = p.locationID,
                        trackID = p.segmentID,
                        offsetMM = p.offset,
                        speedMMPS = p.speedMMPS,
                        reversed = reversed
                    }
                );
                car.data.speedMMPS = p.speedMMPS; //set the speed

                car.UpdateValues(p.locationID, p.segmentID, p.offset, bits, reversed); //check the lane

                CarEventLocationCall?.Invoke(car.id, p.locationID, p.segmentID, p.offset, p.speedMMPS, reversed);
            }catch (Exception e){
                Console.WriteLine($"Position error: {e}"); //catch any errors
            }
        }
        void OnTransition(Structures.TransitionUpdate u, Car car){
            // There is a shorter segment for the starting line track. (may fail on inside turns) 35, 34
            bool crossedStartingLine = (u.leftWheelDistanceCM < 36) && (u.leftWheelDistanceCM > 32) && (u.rightWheelDistanceCM < 36) && (u.rightWheelDistanceCM > 32);
            Program.socketMan.Notify(EVENT_CAR_TRANSITION, 
                new TransitionData{
                    carID = car.id,
                    trackPiece = u.currentSegmentIdx,
                    oldTrackPiece = u.previousSegmentIdx,
                    offsetMM = u.offset,
                    uphillCounter = u.uphillCounter,
                    downhillCounter = u.downhillCounter,
                    leftWheelDistance = u.leftWheelDistanceCM,
                    rightWheelDistance = u.rightWheelDistanceCM,
                    crossedStartingLine = crossedStartingLine
                }
            );
            CarEventTransitionCall?.Invoke(car.id, u.currentSegmentIdx, u.previousSegmentIdx, u.offset, u.uphillCounter, u.downhillCounter, u.leftWheelDistanceCM, u.rightWheelDistanceCM, crossedStartingLine); //call the event
            car.data.offsetMM = u.offset;
            
            SolveSegment(car, u.leftWheelDistanceCM, u.rightWheelDistanceCM, crossedStartingLine, u.offset, u.uphillCounter, u.downhillCounter); //solve the segment
            car.lastPositionID = 0; //set the last track piece to the current one
        }
        void SolveSegment(Car car, int left, int right, bool finish, float offset, int up, int down){
            Segment segment = new Segment(SegmentType.Unknown, 0, false); //create a new track piece
            int ID = car.lastPositionID; //get the last track piece ID
            bool reversed = car.lastReversed; //get the last track piece 
            if(ID != 0){ 
                SegmentType type = SegmentTypeFromID(ID);
                if(type != SegmentType.Unknown){ segment = new Segment(type, ID, reversed); } //if we have a valid ID, set the segment to that
            }else{
                SegmentType type = (Abs(left - right) < 4) ? SegmentType.Straight : SegmentType.Turn;
                if(type == SegmentType.Straight && finish){ type = SegmentType.PreFinishLine; } //if we are straight and at the finish line, set it to prefinish line
                reversed = (type == SegmentType.Straight) ? false : (left > right);
                segment = new Segment(type, 0, reversed); //fallback to straight or turn
            }
            segment.SetUpDown(up, down); //set the up and down values
            CarEventSegmentCall?.Invoke(car.id, segment, offset); //call the event
            Program.socketMan.Notify(EVENT_CAR_SEGMENT,  
                new SegmentData{
                    type = segment.type,
                    internalID = segment.internalID,
                    reversed = segment.flipped,
                    up = segment.up,
                    down = segment.down,
                    validated = segment.validated
                }
            );
        }
        int Abs(int i) { return i < 0 ? -i : i; }

#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        //subscribable event
        public delegate void CarEventLocation(string carID, int trackLocation, int trackID, float offset, int speed, bool clockwise);
        public event CarEventLocation? CarEventLocationCall;
        public delegate void CarEventTransition(string carID, int trackPieceIdx, int oldTrackPieceIdx, float offset, int uphillCounter, int downhillCounter, int leftWheelDistance, int rightWheelDistance, bool crossedStartingLine);
        public event CarEventTransition? CarEventTransitionCall;
        public delegate void CarEventSegment(string carID, Segment trackPiece, float offset);
        public event CarEventSegment? CarEventSegmentCall;
        public delegate void CarEventFell(string carID);
        public event CarEventFell? CarEventDelocalised;
        public delegate void CarEventJump(string carID);
        public event CarEventJump? CarEventJumpCall;
    }
}