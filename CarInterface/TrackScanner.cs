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
        bool tracking = false, checkingScan = false, successfulScan = false, firstTick = true, finishedScan = false;
        int retries = 0, maxRetries = 3, height = 0, nextHeightForced = 0;
        bool awaitTurn = false, awaitPowerup = false;

        Car scanningCar;
        public async Task ScanTrack(Car car, int finishlines){
            scanningCar = car;
            trackPieces = new List<TrackPiece>();
            totalFinishes = finishlines;
            Program.messageManager.CarEvent += OnCarEvent;
            await car.SetCarSpeed(450, 500);
            await car.SetCarTrackCenter(0);
            await car.SetCarLane(24);
            while (!finishedScan){
                await Task.Delay(500);
            }
        }
        bool ScanLoopDone(){
            if(!checkingScan){
                checkingScan = true;
                intialScan = trackPieces.ToArray();
                trackPieces.Clear();
                finishesPassed = 1;
                //scanningCar.SetCarSpeed(500, 500);
            }else{
                confirmScan = trackPieces.ToArray();
                trackPieces.Clear();
                finishesPassed = 1;
                //compare the two track scans
                if(intialScan.Length != confirmScan.Length){
                    Program.Log($"lengths do not match {intialScan.Length} != {confirmScan.Length}");
                    for(int i = 0; i < intialScan.Length; i++){
                        TrackPieceType hopeful = TrackPieceType.Unknown;
                        TrackPieceType compare = TrackPieceType.Unknown;
                        if(i < confirmScan.Length){ compare = confirmScan[i].type; }
                        if(i < intialScan.Length){ hopeful = intialScan[i].type; }
                        Console.WriteLine($"Track {i} hopeful: {hopeful} compare: {compare}");
                        //Program.Log($"Track {i} hopeful: {hopeful} compare: {compare}");
                    }
                    retries++;
                    if(retries <= maxRetries){
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
                            Program.Log($"Mismatch at {i} {intialScan[i].type} != {confirmScan[i].type}");
                            matched = false; break;
                        }
                    }
                    if(matched){
                        Program.Log("Track scan successful");
                        successfulScan = true; return true;
                    }
                    else{
                        Program.Log("Track scan comparison failed");
                        retries++; return !(retries <= maxRetries);
                    }
                }
            }
            return false;
        }
        async Task SendFinishedTrack(){
            while(scanningCar.data.speed > 0){
                await Task.Delay(500);
                await scanningCar.SetCarSpeed(0, 500);
            }
            string content = $"-4:{scanningCar.id}:{successfulScan}";
            foreach(TrackPiece piece in intialScan){
                content += $":{(int)piece.type}:{piece.height}";
            }
            Program.UtilLog(content);
            finishedScan = true;
        }
        void OnCarEvent(string content){
            string[] data = content.Split(':');
            if(data[0] == "41"){ //Track transition
                tracking = true;
                int incline = int.Parse(data[5]);
                int decline = int.Parse(data[6]);
                int lWheel = int.Parse(data[7]);
                int rWheel = int.Parse(data[8]);
                if(awaitTurn){
                    trackPieces.Add(new TrackPiece((lWheel - rWheel) < 0 ? TrackPieceType.CurveLeft : TrackPieceType.CurveRight, 0));
                    SendCurrentTrack();
                    awaitTurn = false;
                }
                if(firstTick){ firstTick = false; return; }
                int change = incline - decline;
                // 2, 1, 0, -1, -2
                int heightChange = change > 200 ? 2 : change < -200 ? -2 : change > 0 ? 1 : change < 0 ? -1 : 0;
                
                if(trackPieces.Count > 0){
                    TrackPieceType lastType = trackPieces[trackPieces.Count - 1].type;
                    TrackPieceType[] validFor2 = {TrackPieceType.Straight, TrackPieceType.PowerupL, TrackPieceType.PowerupR, TrackPieceType.CurveLeft, TrackPieceType.CurveRight};
                    if(validFor2.Contains(lastType) && Math.Abs(heightChange) == 2){
                        height += heightChange;
                    }
                    else if((lastType == TrackPieceType.CurveLeft || lastType == TrackPieceType.CurveRight) && Math.Abs(heightChange) == 1){
                        height += heightChange;
                        if(nextHeightForced != 0){ nextHeightForced = 0; }
                        else if(nextHeightForced == 0){ nextHeightForced = heightChange; }
                    }
                    else if((lastType == TrackPieceType.CurveLeft || lastType == TrackPieceType.CurveRight) && heightChange == 0 && nextHeightForced != 0){
                        height += nextHeightForced;
                        nextHeightForced = 0;
                    }
                    trackPieces[trackPieces.Count - 1].height = height;
                }
            }
            else if(data[0] == "39"){ //Track location
                if(tracking){
                    tracking = false;
                    int trackID = int.Parse(data[3]);
                    if(finishesPassed <= 0){
                        height = 0;
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
                                    scanningCar.SetCarSpeed(0, 500);
                                    Program.messageManager.CarEvent -= OnCarEvent;
                                    SendFinishedTrack();
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
            else if(data[0] == "45"){
                //scanningCar.SetCarLane(24);
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
            PowerupR,
            Unknown
        }
    }
}
