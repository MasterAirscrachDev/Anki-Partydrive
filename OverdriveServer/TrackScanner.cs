using static OverdriveServer.Tracks;
namespace OverdriveServer
{
    class TrackScanner{
        List<TrackPiece> trackPieces;
        TrackPiece[] intialScan, confirmScan;
        int finishesPassed = 0, totalFinishes = 0;
        bool tracking = false, checkingScan = false, successfulScan = false, finishedScan = false;
        int retries = 0, maxRetries = 3;
        int scanSpeed = 530;
        Car scanningCar;
        public async Task<bool> CancelScan(Car cancelThis){
            if(scanningCar == cancelThis){
                SetEventsSub(false);
                await scanningCar.SetCarSpeed(0, 500);
                finishedScan = true;
                return true;
            }
            return false;
        }
        void SetEventsSub(bool sub){
            if(sub){ 
                Program.messageManager.CarEventLocationCall += OnTrackPosition; 
                Program.messageManager.CarEventTransitionCall += OnTrackTransition;
            }else{ 
                Program.messageManager.CarEventLocationCall -= OnTrackPosition;
                Program.messageManager.CarEventTransitionCall -= OnTrackTransition;
            }
        }
        public async Task ScanTrack(Car car, int finishlines){
            scanningCar = car;
            trackPieces = new List<TrackPiece>();
            totalFinishes = finishlines;
            SetEventsSub(true);
            await car.SetCarSpeed(480, 500);
            //await car.SetCarTrackCenter(0);
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
                scanningCar.SetCarSpeed(scanSpeed, 500);
                scanSpeed -= 10;
            }else{
                scanSpeed -= 10;
                confirmScan = trackPieces.ToArray();
                trackPieces.Clear();
                finishesPassed = 1;
                //compare the two track scans
                if(intialScan.Length != confirmScan.Length){
                    Program.Log($"\n\nlengths do not match {intialScan.Length} != {confirmScan.Length}");
                    //log the descrepency and its index
                    for(int i = 0; i < intialScan.Length; i++){
                        if(i >= confirmScan.Length){ Program.Log($"Index {i} does not exist in confirmScan"); }
                        else if(intialScan[i].type != confirmScan[i].type){ Program.Log($"Mismatch at {i} {intialScan[i].type} != {confirmScan[i].type}"); }
                    }
                    retries++;
                    if(retries <= maxRetries){ Program.Log("Retrying track scan"); }
                    else{ Program.Log("Track scan failed"); return true; }
                }
                else{
                    bool matched = true;
                    for(int i = 0; i < intialScan.Length; i++){
                        if(intialScan[i].type != confirmScan[i].type){
                            Program.Log($"Mismatch at {i} {intialScan[i].type} != {confirmScan[i].type}");
                            matched = false; break;
                        }
                    }
                    if(matched){ Program.Log("Track scan successful"); successfulScan = true; return true; }
                    Program.Log("Track scan comparison failed"); retries++; return !(retries <= maxRetries);
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
            //TrackPiece[] solvedPieces = HeightSolver();
            TrackPiece[] solvedPieces = intialScan;
            
            Program.UtilLog(content);
            if(successfulScan){
                Program.trackManager.SetTrack(solvedPieces);
            }
            finishedScan = true;
        }
        void OnTrackTransition(string carID, int trackPieceIdx, int oldTrackPieceIdx, float offset, int uphillCounter, int downhillCounter, int leftWheelDistance, int rightWheelDistance, bool crossedStartingLine){
            tracking = true;
            if(trackPieces.Count > 0){ trackPieces[trackPieces.Count - 1].SetUpDown(uphillCounter, downhillCounter);}
        }
        void OnTrackPosition(string carID, int trackLocation, int trackID, float offset, int speed, bool clockwise){
            if(tracking){
                tracking = false;
                if(trackID == 33){
                    finishesPassed++;
                    if(finishesPassed <= 1){
                        if(finishesPassed == 1){ trackPieces.Add(new TrackPiece(TrackPieceType.FinishLine, trackID, clockwise)); SendCurrentTrack();}
                    }else{
                        if(finishesPassed > totalFinishes){
                            bool stop = ScanLoopDone();
                            if(stop){
                                scanningCar.SetCarSpeed(0, 500);
                                SetEventsSub(false);
                                SendFinishedTrack(); return;
                            }else{ scanningCar.SetCarSpeed(scanSpeed, 500); trackPieces.Add(new TrackPiece(TrackPieceType.FinishLine, trackID, clockwise));  } //slow down a bit if we failed
                        }
                    }
                }else if(finishesPassed >= 1){
                    TrackPieceType pt = PeiceFromID(trackID);
                    trackPieces.Add(new TrackPiece(pt, trackID, clockwise));
                    if(pt != TrackPieceType.Unknown || pt != TrackPieceType.PreFinishLine){
                        SendCurrentTrack();
                    }
                }
            }
        }
        void SendCurrentTrack(){
            if(checkingScan){ return; }
            string content = $"-3:{scanningCar.id}";
            foreach(TrackPiece piece in trackPieces){ content += $":{(int)piece.type}"; }
            Program.UtilLog(content);
        }
        public static TrackPieceType PeiceFromID(int id){
            if(id == 17 || id == 18 || id == 20 || id == 23){ return TrackPieceType.Turn; }
            else if(id == 36 || id == 39 || id == 40){ return TrackPieceType.Straight; }
            else if(id == 57){ return TrackPieceType.FnFSpecial; }
            else if(id == 34){ return TrackPieceType.PreFinishLine; }
            else if(id == 33){ return TrackPieceType.FinishLine; }
            else{ return TrackPieceType.Unknown; }
        }
    }
}
