using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OverdriveServer
{
    class TrackScanner
    {
        List<TrackPiece> trackPieces;
        TrackPiece[] intialScan, confirmScan;
        int finishesPassed = 0, totalFinishes = 0;
        bool tracking = false, checkingScan = false;
        int retries = 0, maxRetries = 3;
        bool awaitTurn = false, awaitPowerup = false;
        Car scanningCar;
        public async Task ScanTrack(Car car, int finishlines){
            scanningCar = car;
            trackPieces = new List<TrackPiece>();
            totalFinishes = finishlines;
            Program.messageManager.CarEvent += OnCarEvent;
            await car.SetCarSpeed(300, 500);
        }
        bool ScanLoopDone(){
            if(!checkingScan){
                checkingScan = true;
                intialScan = trackPieces.ToArray();
                trackPieces.Clear();
                finishesPassed = 1;
                scanningCar.SetCarSpeed(400, 500);
            }else{
                confirmScan = trackPieces.ToArray();
                trackPieces.Clear();
                //compare the two track scans
                if(intialScan.Length != confirmScan.Length){
                    Program.Log($"lengths do not match {intialScan.Length} != {confirmScan.Length}");
                    retries++;
                    if(retries < maxRetries){
                        Program.Log("Retrying track scan");
                    }
                    else{
                        Program.Log("Track scan failed");
                        return true;
                    }
                }
                else{
                    bool matched = true;
                    for(int i = 0; i < intialScan.Length; i++){
                        if(intialScan[i].type != confirmScan[i].type){
                            matched = false;
                            Program.Log($"Mismatch at {i} {intialScan[i].type} != {confirmScan[i].type}");
                            break;
                        }
                    }
                    if(matched){
                        Program.Log("Track scan successful");
                        return true;
                    }
                    else{
                        Program.Log("Track scan comparison failed");
                        retries++;
                        return !(retries < maxRetries);
                    }
                }
            }
            return false;
        }
        void OnCarEvent(string content){
            string[] data = content.Split(':');
            if(data[0] == "41"){ //Track transition
                tracking = true;
                int lWheel = int.Parse(data[7]);
                int rWheel = int.Parse(data[8]);
                if(awaitTurn){
                    trackPieces.Add(new TrackPiece((lWheel - rWheel) < 0 ? TrackPieceType.CurveLeft : TrackPieceType.CurveRight, 0));
                    SendCurrentTrack();
                    awaitTurn = false;
                }
            }
            else if(data[0] == "39"){ //Track location
                if(tracking){
                    tracking = false;
                    int trackID = int.Parse(data[3]);
                    if(finishesPassed <= 0){
                        if(trackID == 33){
                            finishesPassed++;
                            if(finishesPassed == 1){
                                trackPieces.Add(new TrackPiece(TrackPieceType.StartFinish, 0));
                            }
                        }
                    }
                    else{
                        if(trackID == 33){
                            finishesPassed++;
                            if(finishesPassed > totalFinishes){
                                bool stop = ScanLoopDone();
                                if(stop){
                                    scanningCar.SetCarSpeed(0, 0);
                                    Program.messageManager.CarEvent -= OnCarEvent;
                                    return;
                                }
                            }
                            trackPieces.Add(new TrackPiece(TrackPieceType.StartFinish, 0)); SendCurrentTrack();
                        }
                        else{
                            if(trackID == 36 || trackID == 39 || trackID == 40){
                                trackPieces.Add(new TrackPiece(TrackPieceType.Straight, 0)); SendCurrentTrack();
                            }
                            else if(trackID == 17 || trackID == 18 || trackID == 20 || trackID == 23){
                                awaitTurn = true;
                            }
                            else if(trackID == 57){
                                trackPieces.Add(new TrackPiece(TrackPieceType.PowerupL, 0));
                                SendCurrentTrack();
                            }
                            else if(trackID == 34){
                                trackPieces.Add(new TrackPiece(TrackPieceType.PreFinishLine, 0));
                                //SendCurrentTrack();
                            }
                        }
                    }
                }
            }
        }
        void SendCurrentTrack(){
            if(checkingScan){ return; }
            string content = $"-3:{scanningCar.id}";
            foreach(TrackPiece piece in trackPieces){
                content += $":{(int)piece.type}:{piece.height}";
            }
            Program.UtilLog(content);
        }
        class TrackPiece{
            public TrackPieceType type;
            public int height;
            public TrackPiece(TrackPieceType type, int height){
                this.type = type;
                this.height = height;
            }
        }
        enum TrackPieceType{
            Straight,
            CurveLeft,
            CurveRight,
            PowerupL,
            StartFinish,
            PreFinishLine,
            PowerupR
        }
    }
}
