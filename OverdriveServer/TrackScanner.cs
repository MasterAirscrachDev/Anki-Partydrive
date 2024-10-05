using static OverdriveServer.Tracks;
namespace OverdriveServer
{
    class TrackScanner{
        List<TrackPiece> trackPieces;
        bool tracking = false, checkingScan = false, successfulScan = false, finishedScan = false;
        int retries = 0, maxRetries = 3;
        int currentPieceIndex = 0;
        int scanSpeed = 530;
        int X = 0, Y = 0, direction = 0;

        Car scanningCar;
        public async Task<bool> CancelScan(Car cancelThis){
            if(scanningCar == cancelThis){
                SetEventsSub(false);
                await scanningCar.SetCarSpeed(0, 500);
                SetEventsSub(false);
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
        public async Task ScanTrack(Car car){
            scanningCar = car;
            trackPieces = new List<TrackPiece>();
            SetEventsSub(true);
            await car.SetCarSpeed(450, 500);
            //await car.SetCarTrackCenter(0);
            await car.SetCarLane(1);
            while (!finishedScan){
                await Task.Delay(500);
            }
        }
        void ResetScan(){
            trackPieces.Clear();
            X = 0; Y = 0; direction = 0;
            currentPieceIndex = 0;
        }
        async Task SendFinishedTrack(){
            SetEventsSub(false);
            await scanningCar.SetCarSpeed(0, 500);
            string content = $"-3:{scanningCar.id}:{successfulScan}";
            //TrackPiece[] solvedPieces = HeightSolver();
            TrackPiece[] solvedPieces = trackPieces.ToArray();
            Program.UtilLog(content);
            Program.trackManager.SetTrack(successfulScan ? solvedPieces : null, successfulScan);
            finishedScan = true;
        }
        void OnTrackTransition(string carID, int trackPieceIdx, int oldTrackPieceIdx, float offset, int uphillCounter, int downhillCounter, int leftWheelDistance, int rightWheelDistance, bool crossedStartingLine){
            tracking = true;
            if(trackPieces.Count > 0){ trackPieces[trackPieces.Count - 1].SetUpDown(uphillCounter, downhillCounter);}
        }
        void OnTrackPosition(string carID, int trackLocation, int trackID, float offset, int speed, bool clockwise){
            if(tracking){
                tracking = false;
                TrackPieceType type = PeiceFromID(trackID);
                TrackPiece piece = new TrackPiece(type, trackID, clockwise, X, Y);
                if(type == TrackPieceType.FinishLine && trackPieces.Count == 0){ 
                    //add a prefinish line piece if the finish line is the first piece
                    trackPieces.Add(new TrackPiece(TrackPieceType.PreFinishLine, 34, clockwise, X, Y));
                }
                int oldX = X, oldY = Y;
                if(checkingScan){
                    //if this piece is the same as the piece at the same index in the trackPieces list, we are on the right track
                    if(!trackPieces[currentPieceIndex].Equals(piece)){
                        //Console.WriteLine($"Mismatch at {currentPieceIndex}, expected: {trackPieces[currentPieceIndex]}, got {piece}");
                        retries++;
                        if(retries >= maxRetries){ //if we have retried too many times, we have failed the scan
                            finishedScan = true;
                            SendFinishedTrack();
                        }
                        else{
                            checkingScan = false;
                            ResetScan(); return;
                        }
                    }else{
                        trackPieces[currentPieceIndex].validated = true;
                        if(currentPieceIndex == trackPieces.Count - 1){
                            successfulScan = true;
                            finishedScan = true;
                            //Console.WriteLine("Scan successful");
                            // for(int i = 0; i < trackPieces.Count; i++){
                            //     Console.WriteLine($"{i}: ({trackPieces[i].type}|{trackPieces[i].internalID}|[{trackPieces[i].X},{trackPieces[i].Y}])");
                            // }
                            SendFinishedTrack();
                        }
                    }
                }else{
                    trackPieces.Add(piece);
                    if(type == TrackPieceType.Turn){
                        if(clockwise){ direction++; }else{ direction--; }
                        if(direction == 4){ direction = 0; }
                        else if(direction == -1){ direction = 3; }
                    }
                    
                    if(type != TrackPieceType.Unknown && type != TrackPieceType.PreFinishLine){
                        if(direction == 0){ Y++; }
                        else if(direction == 1){ X++; }
                        else if(direction == 2){ Y--; }
                        else if(direction == 3){ X--; }
                    }
                    else{
                        Program.UtilLog($"Unknown track piece: {trackID}");
                    }

                    if(trackPieces.Count > 4){
                        //if our current position is the same as the start position, set checkingScan to true
                        if(trackPieces[0].IsAt(X, Y)){
                            // Console.WriteLine($"reached assumed scan start at {piece}");
                            // //print the first 3 pieces
                            // for(int i = 0; i < 3; i++){
                            //     Console.WriteLine($"{i}: {trackPieces[i]}");
                            // }
                            checkingScan = true;
                            currentPieceIndex = -1;
                        }
                    }
                }
                SendCurrentTrack();
                //Console.WriteLine($"index: {currentPieceIndex}/{trackPieces.Count} is ({type}|{trackID}|[{oldX},{oldY}]), checking: {checkingScan}, retries: {retries}");
                currentPieceIndex++;
            }
        }
        void SendCurrentTrack(){
            Program.trackManager.SetTrack(trackPieces.ToArray(), false);
            Program.UtilLog($"-3:{scanningCar.id}");
        }
        public static TrackPieceType PeiceFromID(int id){
            if(id == 17 || id == 18 || id == 20 || id == 23){ return TrackPieceType.Turn; }
            else if(id == 36 || id == 39 || id == 40){ return TrackPieceType.Straight; }
            else if(id == 57){ return TrackPieceType.FnFSpecial; }
            else if(id == 34){ return TrackPieceType.PreFinishLine; }
            else if(id == 33){ return TrackPieceType.FinishLine; }
            else if(id == 10){ return TrackPieceType.CrissCross; }
            else{ return TrackPieceType.Unknown; }
        }
    }
}
