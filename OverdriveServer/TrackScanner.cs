using static OverdriveServer.Tracks;
namespace OverdriveServer {
    class TrackScanner {
        List<TrackPiece> trackPieces;
        bool hasAddedPieceThisSegment = false, successfulScan = false, finishedScan = false;
        bool isScanning = false;
        int currentPieceIndex = 0;
        Car scanningCar;
        public async Task<bool> CancelScan(Car cancelThis) {
            if(scanningCar == cancelThis){
                await scanningCar.SetCarSpeed(0, 500);
                SetEventsSub(false);
                finishedScan = true;
                return true;
            }
            return false;
        }
        void SetEventsSub(bool sub) {
            if(sub){ 
                Program.messageManager.CarEventLocationCall += OnTrackPosition; 
                Program.messageManager.CarEventTransitionCall += OnTrackTransition;
                Program.messageManager.CarEventJumpCall += OnCarJumped;
            }else{ 
                Program.messageManager.CarEventLocationCall -= OnTrackPosition;
                Program.messageManager.CarEventTransitionCall -= OnTrackTransition;
                Program.messageManager.CarEventJumpCall -= OnCarJumped;
            }
        }
        public async Task ScanTrack(Car car) {
            scanningCar = car;
            trackPieces = new List<TrackPiece>();
            SetEventsSub(true);
            await car.SetCarSpeed(450, 500);
            await car.SetCarLane(0);
            while (!finishedScan){ await Task.Delay(500); }
        }
        void ResetScan() { trackPieces.Clear(); }
        async Task SendFinishedTrack() {
            SetEventsSub(false);
            await scanningCar.SetCarSpeed(0, 500);
            string content = $"-3:{scanningCar.id}:{successfulScan}";
            //TrackPiece[] solvedPieces = HeightSolver();
            TrackPiece[] solvedPieces = trackPieces.ToArray();
            Program.UtilLog(content);
            Program.trackManager.SetTrack(successfulScan ? solvedPieces : null, successfulScan);
            finishedScan = true;
        }
        void OnCarJumped(string carID) {
            if(carID == scanningCar.id) {
                OnTrackPosition(carID, 0, 58, 0, 0, false); //jump ramp
                hasAddedPieceThisSegment = true;
                OnTrackPosition(carID, 0, 63, 0, 0, false); //jump landing
            }
        }
        void OnTrackTransition(string carID, int trackPieceIdx, int oldTrackPieceIdx, float offset, int uphillCounter, int downhillCounter, int leftWheelDistance, int rightWheelDistance, bool crossedStartingLine){
            if(!isScanning){
                if(crossedStartingLine){ 
                    isScanning = true;
                    trackPieces.Add(new TrackPiece(TrackPieceType.PreFinishLine, 34, false));
                    trackPieces.Add(new TrackPiece(TrackPieceType.FinishLine, 33, false));
                    trackPieces[0].certaintyScore = 100;
                    trackPieces[1].certaintyScore = 100;
                    currentPieceIndex = 1;
                    hasAddedPieceThisSegment = false;
                }
            }else{
                if(!hasAddedPieceThisSegment){
                    if(crossedStartingLine){
                        successfulScan = true;
                        finishedScan = true;
                        SendFinishedTrack();
                    }else{
                        if(Abs(leftWheelDistance - rightWheelDistance) < 2){
                            trackPieces.Add(new TrackPiece(TrackPieceType.Straight, 36, false));
                        }else if(leftWheelDistance > rightWheelDistance){
                            trackPieces.Add(new TrackPiece(TrackPieceType.Turn, 17, false));
                        }else{
                            trackPieces.Add(new TrackPiece(TrackPieceType.Turn, 17, true));
                        }
                    }
                }
                //if(trackPieces.Count > 0){ trackPieces[trackPieces.Count - 1].SetUpDown(uphillCounter, downhillCounter);} //this would cause badness for loops
                hasAddedPieceThisSegment = false;
            }
        }
        void OnTrackPosition(string carID, int trackLocation, int trackID, float offset, int speed, bool clockwise) {
            if(!hasAddedPieceThisSegment && isScanning){
                hasAddedPieceThisSegment = true;
                TrackPieceType type = PeiceFromID(trackID);
                if(type == TrackPieceType.Unknown || type ==  TrackPieceType.PreFinishLine || type ==  TrackPieceType.FinishLine){ return; }
                TrackPiece piece = new TrackPiece(type, trackID, clockwise);
                trackPieces.Add(piece);
                currentPieceIndex++;
                SendCurrentTrack();
                //Console.WriteLine($"index: {currentPieceIndex}/{trackPieces.Count} is ({type}|{trackID}|[{oldX},{oldY}]), checking: {checkingScan}, retries: {retries}");
            }
        }
        int RotateDirection(bool clockwise, int direction) {
            if(clockwise){ direction++; }else{ direction--; }
            if(direction == 4){ direction = 0; }
            else if(direction == -1){ direction = 3; }
            return direction;
        }
        (int,int) MoveBasedOnDirection(int spaces, int direction, int X, int Y) {
            if(direction == 0){ Y += spaces; }
            else if(direction == 1){ X += spaces; }
            else if(direction == 2){ Y -= spaces; }
            else if(direction == 3){ X -= spaces; }
            return (X,Y);
        }
        // bool ValidateTrackPiece(TrackPiece piece) { //if this piece is the same as the piece at the same index in the trackPieces list, we are on the right track
        //     if(!trackPieces[currentPieceIndex].Equals(piece)) {
        //         retries++; //if we have retried too many times, we have failed the scan
        //         if(retries >= maxRetries) { finishedScan = true;  SendFinishedTrack(); }
        //         else{ checkingScan = false; ResetScan(); return false; }
        //     } else {
        //         if(currentPieceIndex == trackPieces.Count - 1){ successfulScan = true; finishedScan = true; SendFinishedTrack(); }
        //     }
        //     return true;
        // }
        
        void SendCurrentTrack() {
            Program.trackManager.SetTrack(trackPieces.ToArray(), false);
            Program.UtilLog($"-3:{scanningCar.id}");
        }
        public static TrackPieceType PeiceFromID(int id) {
            if(id == 17 || id == 18 || id == 20 || id == 23){ return TrackPieceType.Turn; }
            else if(id == 36 || id == 39 || id == 40){ return TrackPieceType.Straight; }
            else if(id == 57){ return TrackPieceType.FnFSpecial; }
            else if(id == 34){ return TrackPieceType.PreFinishLine; }
            else if(id == 33){ return TrackPieceType.FinishLine; }
            else if(id == 10){ return TrackPieceType.CrissCross; } 
            else if(id == 58){ return TrackPieceType.JumpRamp; }
            else if(id == 63){ return TrackPieceType.JumpLanding; }
            else{ return TrackPieceType.Unknown; }
        }
        int Abs(int i) { return i < 0 ? -i : i; }
    }
}
