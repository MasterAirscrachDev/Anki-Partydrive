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
        int finishesPassed = 0, totalFinishes = 0;
        bool tracking = false, done = false, checkingScan = false;
        int turnDiff = 0;
        bool suspendTurn = false;
        public async Task ScanTrack(Car car, int finishlines){
            trackPieces = new List<TrackPiece>();
            totalFinishes = finishlines;
            Program.messageManager.CarEvent += OnCarEvent;
            await car.SetCarSpeed(300, 500);
            while(!done){
                await Task.Delay(500);
            }
            TrackPiece[] pieces = trackPieces.ToArray();
            checkingScan = true;
            int maxRetries = 3, retries = 0;
            bool matched = false;
            while(retries < maxRetries && !matched){
                //run the track again faster to double check
                trackPieces.Clear();
                done = false;
                finishesPassed = -(totalFinishes - 1);
                Program.Log($"Skipping next {totalFinishes - 1} finishes");
                Program.messageManager.CarEvent += OnCarEvent;
                await car.SetCarSpeed(380, 500);
                while(!done){
                    await Task.Delay(500);
                }
                //compare the two track scans
                TrackPiece[] pieces2 = trackPieces.ToArray();
                trackPieces.Clear();

                if(pieces.Length != pieces2.Length){
                    Program.Log($"lengths do not match {pieces.Length} != {pieces2.Length}");
                    retries++;
                }
                else{ matched = true; }
            }
            await car.SetCarSpeed(0, 500);
            if(matched){
                Program.Log("Track scan successful");
            }
            else{
                Program.Log("Track scan failed");
            }
        }
        void OnCarEvent(string content){
            string[] data = content.Split(':');
            if(data[0] == "41"){ //Track transition
                tracking = true;
                int lWheel = int.Parse(data[7]);
                int rWheel = int.Parse(data[8]);
                turnDiff = lWheel - rWheel;
                if(suspendTurn){
                    trackPieces.Add(new TrackPiece(turnDiff < 0 ? TrackPieceType.CurveLeft : TrackPieceType.CurveRight, 0));
                    SendCurrentTrack();
                    suspendTurn = false;
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
                                //Finished
                                done = true; Program.messageManager.CarEvent -= OnCarEvent;
                            }
                            else{ trackPieces.Add(new TrackPiece(TrackPieceType.StartFinish, 0)); SendCurrentTrack(); }
                        }
                        else{
                            if(trackID == 36 || trackID == 39 || trackID == 40){
                                trackPieces.Add(new TrackPiece(TrackPieceType.Straight, 0)); SendCurrentTrack();
                            }
                            else if(trackID == 17 || trackID == 18 || trackID == 20 || trackID == 23){
                                suspendTurn = true;
                            }
                            else if(trackID == 57){
                                trackPieces.Add(new TrackPiece(TrackPieceType.Powerup, 0));
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
            string content = "-3";
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
            Powerup,
            StartFinish,
            PreFinishLine
        }
    }
}
