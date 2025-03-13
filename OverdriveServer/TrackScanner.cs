using static OverdriveServer.Tracks;
namespace OverdriveServer {
    class TrackScanner {
        List<TrackPiece> trackPieces = new List<TrackPiece>();
        List<TrackPiece>? validationTrack;
        bool hasAddedPieceThisSegment = false, successfulScan = false, finishedScan = false;
        bool isScanning = false, isValidation = false;
        int skipSegments = 0, finishesPassed = 0;


        int finishCount = 1; //temp hardcoded

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
            ResetScan();
            SetEventsSub(true);
            await car.SetCarSpeed(600, 500);
            await car.SetCarLane(0);
            while (!finishedScan){ await Task.Delay(500); }
        }
        void ResetScan() { trackPieces.Clear(); validationTrack = null; isScanning = false; isValidation = false; hasAddedPieceThisSegment = false; }
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
            if(!isScanning){ //true until we reach the first finish line
                if(crossedStartingLine){ 
                    scanningCar.SetCarSpeed(450, 500);
                    isScanning = true;
                    trackPieces.Add(new TrackPiece(TrackPieceType.PreFinishLine, 34, false));
                    trackPieces.Add(new TrackPiece(TrackPieceType.FinishLine, 33, false));
                    skipSegments = 2; //i dont like this
                    hasAddedPieceThisSegment = false;
                }
            }else{
                if(!hasAddedPieceThisSegment){
                    if(skipSegments > 0){ skipSegments--; return; }
                    if(crossedStartingLine){ //currently only supports one finish piece
                        finishesPassed++;
                        if(finishesPassed >= finishCount){
                            if(!isValidation){
                                validationTrack = trackPieces.ToList();
                                isValidation = true;
                                trackPieces.Clear();
                                trackPieces.Add(new TrackPiece(TrackPieceType.PreFinishLine, 34, false));
                                trackPieces.Add(new TrackPiece(TrackPieceType.FinishLine, 33, false));
                                trackPieces[trackPieces.Count - 1].validated = true;
                                trackPieces[trackPieces.Count - 2].validated = true;
                                skipSegments = 2;
                                hasAddedPieceThisSegment = false;
                                return;
                            }else {
                                //compare validation track to trackPieces
                                bool valid = true;
                                for(int i = 0; i < validationTrack.Count; i++){
                                    if(validationTrack[i] != trackPieces[i]){   
                                        valid = false;
                                        break;
                                    }
                                }
                                successfulScan = valid;
                                finishedScan = true;
                                SendFinishedTrack();
                            }
                        }
                    }else{
                        if(Abs(leftWheelDistance - rightWheelDistance) < 2){
                            trackPieces.Add(new TrackPiece(TrackPieceType.Straight, 36, false));
                        }else if(leftWheelDistance > rightWheelDistance){
                            trackPieces.Add(new TrackPiece(TrackPieceType.Turn, 17, true));
                        }else{
                            trackPieces.Add(new TrackPiece(TrackPieceType.Turn, 17, false));
                        }
                        trackPieces[trackPieces.Count - 1].validated = isValidation;
                        Program.Log($"TEMP: used fallback for transition, {trackPieces[trackPieces.Count - 1].type}");
                    }
                }
                //if(trackPieces.Count > 0){ trackPieces[trackPieces.Count - 1].SetUpDown(uphillCounter, downhillCounter);} //this would cause badness for loops
                hasAddedPieceThisSegment = false;
            }
        }
        void OnTrackPosition(string carID, int trackLocation, int trackID, float offset, int speed, bool clockwise) {
            if(!hasAddedPieceThisSegment && isScanning){
                if(skipSegments > 0){ skipSegments--; return; }
                hasAddedPieceThisSegment = true;
                TrackPieceType type = PieceFromID(trackID);
                if(type == TrackPieceType.Unknown || type ==  TrackPieceType.PreFinishLine || type ==  TrackPieceType.FinishLine){ hasAddedPieceThisSegment = false; return; }
                trackPieces.Add(new TrackPiece(type, trackID, clockwise));
                trackPieces[trackPieces.Count - 1].validated = isValidation;
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
        
        void SendCurrentTrack() {
            List<TrackPiece> currentTrack;

            if(validationTrack != null){
                currentTrack = validationTrack.ToList();
                for(int i = 0; i < trackPieces.Count; i++){
                    currentTrack[i] = trackPieces[i];
                }
            }else{
                currentTrack = trackPieces.ToList();
            }

            Program.trackManager.SetTrack(currentTrack.ToArray(), false);
            Program.UtilLog($"-3:{scanningCar.id}");
        }
        public static TrackPieceType PieceFromID(int id) {
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
