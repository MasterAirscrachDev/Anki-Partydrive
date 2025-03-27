using static OverdriveServer.Definitions;
using static OverdriveServer.NetStructures;
namespace OverdriveServer {
    class MessageManager {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        public void ParseMessage(byte[] content, Car car){
            byte id = content[1];
            if(id == RECV_PING){//23 ping response
                Program.Log($"[23] Ping response: {Program.BytesToString(content)}");

            } else if(id == RECV_VERSION){ //25 version response
                int version = content[2];
                Program.Log($"[25] Version response: {version}");
            } else if(id == RECV_BATTERY_RESPONSE){ //27 battery response
                int battery = content[2];
                int maxBattery = 3800;
                Program.Log($"[27] Battery response: {battery} / {maxBattery}");
                Program.UtilLog($"27:{car.id}:{battery}");
                car.data.battery = battery;
            } else if(id == RECV_TRACK_LOCATION){ //39 where is car
                int trackLocation = content[2];
                int trackID = content[3];
                float offset = BitConverter.ToSingle(content, 4);
                int speed = BitConverter.ToInt16(content, 8);
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
                Program.Log($"[39] {car.name} Track location: {trackLocation}, track ID: {trackID}, offset: {offset}, speed: {speed}, clockwise: {clockwise}");
                car.data.trackPosition = trackLocation; 
                car.data.trackID = trackID;
                car.data.laneOffset = offset;
                car.data.speed = speed;
                CarEventLocationCall?.Invoke(car.id, trackLocation, trackID, offset, speed, clockwise);
            } else if(id == RECV_TRACK_TRANSITION){ //41 car moved between track pieces
                try{
                    if(content.Length < 18){ return; } //not enough data
                    int trackPiece = Convert.ToInt32((sbyte)content[2]);
                    int oldTrackPiece = Convert.ToInt32((sbyte)content[3]);
                    float offset = BitConverter.ToSingle(content, 4);
                    int uphillCounter = content[14];
                    int downhillCounter = content[15];
                    int leftWheelDistance = content[16];
                    int rightWheelDistance = content[17];
                    // There is a shorter segment for the starting line track. (may fail on inside turns)
                    bool crossedStartingLine = false;
                    //greater than 25 and less than 37 (mm presumably)
                    if ((leftWheelDistance < 36) && (leftWheelDistance > 32) && (rightWheelDistance < 36) && (rightWheelDistance > 32)) {
                        crossedStartingLine = true;
                        Program.UtilLog($"-4:{car.id}:{DateTime.Now.ToBinary()}"); //UPGRADE TO WORK WITH MULTI FINISH LINES
                    }
                    Program.Log($"[41] {car.name} Track: {trackPiece} from {oldTrackPiece}, Y:(+{uphillCounter} -{downhillCounter}), X: {offset} LwheelDist: {leftWheelDistance}, RwheelDist: {rightWheelDistance}, Finish: {crossedStartingLine}");
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
                }
                catch{
                    Program.Log($"[41] Error parsing car track update: {Program.BytesToString(content)}");
                    return;
                }
                
            } else if(id == RECV_TRACK_INTERSECTION){ //42 track intersection
                //
            } else if(id == RECV_CAR_OFF_TRACK){ //43 ONOH FALL
                Program.UtilLog($"43:{car.id}");
                Program.Log($"[43] {car.name} fell off track");
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
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }
        //subscribable event
        public delegate void CarEventLocation(string carID, int trackLocation, int trackID, float offset, int speed, bool clockwise);
        public event CarEventLocation? CarEventLocationCall;
        public delegate void CarEventTransition(string carID, int trackPieceIdx, int oldTrackPieceIdx, float offset, int uphillCounter, int downhillCounter, int leftWheelDistance, int rightWheelDistance, bool crossedStartingLine);
        public event CarEventTransition? CarEventTransitionCall;
        public delegate void CarEventFell(string carID);
        public event CarEventFell? CarEventDelocalised;
        public delegate void CarEventJump(string carID);
        public event CarEventJump? CarEventJumpCall;
    }
}