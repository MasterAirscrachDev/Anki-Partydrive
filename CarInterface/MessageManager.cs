using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using static OverdriveServer.Definitions;

namespace OverdriveServer
{
    class MessageManager
    {
        public void ParseMessage(byte[] content, Car car){
            byte id = content[1];
            string util = "";
            if(id == RECV_PING){//23 ping response
                Program.Log($"[23] Ping response: {Program.BytesToString(content)}");
            } else if(id == RECV_VERSION){ //25 version response
                int version = content[2];
                Program.Log($"[25] Version response: {version}");
            } else if(id == RECV_BATTERY_RESPONSE){ //27 battery response
                int battery = content[2];
                int maxBattery = 3800;
                Program.Log($"[27] Battery response: {battery} / {maxBattery}");
                util = Program.UtilLog($"27:{car.id}:{battery}");
                car.data.battery = battery;
            } else if(id == RECV_TRACK_LOCATION){ //39 where is car
                int trackLocation = content[2];
                int trackID = content[3];
                float offset = BitConverter.ToSingle(content, 4);
                int speed = BitConverter.ToInt16(content, 8);
                //tf does location mean
                util = Program.UtilLog($"39:{car.id}:{trackLocation}:{trackID}:{offset}:{speed}");
                Program.Log($"[39] {car.name} Track location: {trackLocation}, track ID: {trackID}, offset: {offset}, speed: {speed}");
                //IDs
                //36 ??? 39 FnF Straight 40 Straight
                //17 18 20 23 FnF Curve / Curve
                //34 PreFinishLine
                //33 Start/Finish
                //57 FnF Powerup

                car.data.trackPosition = trackLocation;
                car.data.trackID = trackID;
                car.data.laneOffset = offset;
                car.data.speed = speed;
            } else if(id == RECV_TRACK_TRANSITION){ //41 car moved between track pieces
                try{
                    if(content.Length < 18){ return; } //not enough data
                    //Console.WriteLine(BytesToString(content));
                    int trackPiece = Convert.ToInt32((sbyte)content[2]);
                    int oldTrackPiece = Convert.ToInt32((sbyte)content[3]);
                    float offset = BitConverter.ToSingle(content, 4);
                    int uphillCounter = content[14];
                    int downhillCounter = content[15];
                    int leftWheelDistance = content[16];
                    int rightWheelDistance = content[17];

                    // There is a shorter segment for the starting line track.
                    string crossedStartingLine = "";
                    if ((leftWheelDistance < 0x25) && (leftWheelDistance > 0x19) && (rightWheelDistance < 0x25) && (rightWheelDistance > 0x19)) {
                        crossedStartingLine = " (Crossed Starting Line)";
                    }
                    util = Program.UtilLog($"41:{car.id}:{trackPiece}:{oldTrackPiece}:{offset}:{uphillCounter}:{downhillCounter}:{leftWheelDistance}:{rightWheelDistance}:{!string.IsNullOrEmpty(crossedStartingLine)}");
                    Program.Log($"[41] {car.name} Track: {trackPiece} from {oldTrackPiece}, up:{uphillCounter}down:{downhillCounter}, offest: {offset} LwheelDist: {leftWheelDistance}, RwheelDist: {rightWheelDistance} {crossedStartingLine}");
                }
                catch{
                    Program.Log($"[41] Error parsing car track update: {Program.BytesToString(content)}");
                    return;
                }
                
            } else if(id == RECV_CAR_ERROR){ //42 car error
                int error = content[2];
                Program.Log($"[42] {car.name} error: {error}");
            } else if(id == RECV_CAR_OFF_TRACK){ //43 ONOH FALL
                util = Program.UtilLog($"43:{car.id}");
                Program.Log($"[43] {car.name} fell off track");
            } else if(id == RECV_TRACK_CENTER_UPDATE){ //45 Track center updated
                Program.Log($"[45] Track center updated: {Program.BytesToString(content)}");
                util = Program.UtilLog($"45:{car.id}:{Program.BytesToString(content)}");
            } else if(id == RECV_CAR_SPEED_UPDATE){ //54 car speed changed
                Program.Log($"[54] {car.name} speed changed");
                util = Program.UtilLog($"54:{car.id}");
            } else if(id == RECV_CAR_CHARGING_STATUS){ //63 charging status changed
                bool charging = content[3] == 1;
                util = Program.UtilLog($"63:{car.id}:{charging}");
                Program.Log($"[63] {car.name} charging: {charging}");
                car.data.charging = charging;
            } else if(id == RECV_CAR_COLLISION){ //77 Collision Detected
                Program.UtilLog($"77:{car.id}");
                Program.Log($"[77] {car.name} collision detected");
            }
            else if(id == RECV_TRACK_SPECIAL_TRIGGER){ //83 FnF specialBlock
                Program.UtilLog($"83:{car.id}");
                Program.Log($"[83] {car.name} hit special block");
            }
            else{
                Program.Log($"Unknown message {id} [{Program.IntToByteString(id)}]: {Program.BytesToString(content)}");
                //45
                //65
                //134 CarMsgCycleOvertime
            }
            CarEvent?.Invoke(util);
        }
        //subscribable event
        public delegate void CarEventHandler(string message);
        public event CarEventHandler CarEvent;
    }
}
