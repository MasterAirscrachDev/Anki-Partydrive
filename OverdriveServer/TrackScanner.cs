using static OverdriveServer.Tracks;
namespace OverdriveServer {
    class TrackScanner {
        List<TrackPiece> trackPieces = new List<TrackPiece>();
        List<TrackPiece>? validationTrack;
        bool successfulScan = false, finishedScan = false;
        bool isScanning = false, isValidation = false;
        int skipSegments = 0, finishesPassed = 0, scanAttempts = 0;

        int lastTrackID; bool lastFlipped;


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
                Program.messageManager.CarEventDelocalised += OnCarFell;
            }else{ 
                Program.messageManager.CarEventLocationCall -= OnTrackPosition;
                Program.messageManager.CarEventTransitionCall -= OnTrackTransition;
                Program.messageManager.CarEventJumpCall -= OnCarJumped;
                Program.messageManager.CarEventDelocalised -= OnCarFell;
            }
        }
        public async Task ScanTrack(Car car) {
            scanningCar = car;
            ResetScan();
            SetEventsSub(true);
            await car.SetCarSpeed(600, 500);
            await car.SetCarLane(0);
            while (!finishedScan){ await Task.Delay(500); }
        }
        void ResetScan(int resetAttempts = 0) { 
            trackPieces.Clear(); 
            validationTrack = null; 
            isScanning = false; 
            isValidation = false; 
            finishesPassed = 0;
            scanAttempts = resetAttempts;
            if(scanAttempts > 0){
                Program.Log($"Scan attempt {scanAttempts}");
            }
        }
        async Task SendFinishedTrack() {
            SetEventsSub(false);
            await scanningCar.SetCarSpeed(0, 500);
            string content = $"-3:{scanningCar.id}:{successfulScan}";
            //TrackPiece[] solvedPieces = HeightSolver();
            TrackPiece[] solvedPieces = trackPieces.ToArray();
            Program.UtilLog(content);
            Program.trackManager.SetTrack(successfulScan ? solvedPieces : null, successfulScan);
            finishedScan = true;
            //Program.Log($"Track scan finished, succsess: {successfulScan}, {solvedPieces.Length} pieces");
        }
        void OnCarJumped(string carID) {
            if(carID == scanningCar.id) {
                trackPieces.Add(new TrackPiece(TrackPieceType.JumpRamp, 58, false));
                trackPieces.Add(new TrackPiece(TrackPieceType.JumpLanding, 63, false));
                trackPieces.Add(new TrackPiece(TrackPieceType.Straight, 40, false));
                trackPieces[trackPieces.Count - 1].validated = isValidation;
                trackPieces[trackPieces.Count - 2].validated = isValidation;
                trackPieces[trackPieces.Count - 3].validated = isValidation;
                skipSegments = 4;
                ValidateMatchAndSendTrack();
            }
        }
        void OnCarFell(string carID) {
            if(carID == scanningCar.id) {
                SendFinishedTrack();
            }
        }
        void OnTrackPosition(string carID, int trackLocation, int trackID, float offset, int speed, bool clockwise) {
            if(carID == scanningCar.id){ 
                lastFlipped = clockwise;
                lastTrackID = trackID;
            }
        }
        void OnTrackTransition(string carID, int trackPieceIdx, int oldTrackPieceIdx, float offset, int uphillCounter, int downhillCounter, int leftWheelDistance, int rightWheelDistance, bool crossedStartingLine){
            if(!isScanning){ //true until we reach the first finish line
                int trackID = lastTrackID; lastTrackID = 0;
                if(crossedStartingLine){
                    scanningCar.SetCarSpeed(450, 500);
                    isScanning = true;
                    trackPieces.Add(new TrackPiece(TrackPieceType.PreFinishLine, 34, false));
                    trackPieces.Add(new TrackPiece(TrackPieceType.FinishLine, 33, false));
                    skipSegments = 1;
                    ValidateMatchAndSendTrack();
                }else if(trackID != 0 && TrackManager.PieceFromID(trackID) == TrackPieceType.FinishLine){ //should be able to initate a scan from the start correctly now
                    isScanning = true;
                    trackPieces.Add(new TrackPiece(TrackPieceType.PreFinishLine, 34, false));
                    trackPieces.Add(new TrackPiece(TrackPieceType.FinishLine, 33, false));
                    ValidateMatchAndSendTrack();
                }
            }else{
                int trackID = lastTrackID; lastTrackID = 0;
                if(skipSegments > 0){ 
                    Program.Log($"Skipping segment, left: {leftWheelDistance}, right: {rightWheelDistance}, skip: {skipSegments}");
                    skipSegments--; return; 
                }
                if(crossedStartingLine){ //currently only supports one finish piece
                    finishesPassed++;
                    if(finishesPassed >= finishCount){ //validation phase
                        if(!isValidation){ //if we are on the first scan
                            validationTrack = trackPieces.ToList();
                            if(ValidateTrackConnects(validationTrack)){ //ensure the track loops
                                isValidation = true;
                                trackPieces.Clear();
                                trackPieces.Add(new TrackPiece(TrackPieceType.PreFinishLine, 34, false));
                                trackPieces.Add(new TrackPiece(TrackPieceType.FinishLine, 33, false));
                                trackPieces[trackPieces.Count - 1].validated = true;
                                trackPieces[trackPieces.Count - 2].validated = true;
                                skipSegments = 1;
                                ValidateMatchAndSendTrack();
                            }else{ //validation failed reset and try again
                                scanAttempts++;
                                if(scanAttempts > 3){ finishedScan = true; return; }
                                trackPieces.Clear();
                                validationTrack = null;
                                isValidation = false;
                                finishesPassed = 0;
                                skipSegments = 1;
                                trackPieces.Add(new TrackPiece(TrackPieceType.PreFinishLine, 34, false));
                                trackPieces.Add(new TrackPiece(TrackPieceType.FinishLine, 33, false));
                                ValidateMatchAndSendTrack();
                            }
                        }else {  //compare validation track to trackPieces
                            successfulScan = MatchTracks(validationTrack, trackPieces);
                            //Program.Log($"Validation: {successfulScan}");
                            SendFinishedTrack();
                        }
                    }
                }else{
                    TrackPiece segment = TrackManager.GetTrackPieceWithFallback(trackID, lastFlipped, leftWheelDistance, rightWheelDistance);
                    if(
                        segment.type == TrackPieceType.Unknown || 
                        segment.type == TrackPieceType.PreFinishLine || 
                        segment.type == TrackPieceType.FinishLine ||
                        segment.type == TrackPieceType.JumpRamp ||
                        segment.type == TrackPieceType.JumpLanding
                    ){ return; }
                    trackPieces.Add(segment);
                    trackPieces[trackPieces.Count - 1].validated = isValidation;
                    trackPieces[trackPieces.Count - 1].SetUpDown(uphillCounter, downhillCounter);
                    ValidateMatchAndSendTrack();
                }
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
        
        void ValidateMatchAndSendTrack() {
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
            Program.UtilLog($"-3:{scanningCar.id}:false");
        }
        bool ValidateTrackConnects(List<TrackPiece> track){
            int X = 0, Y = -1, direction = 0;
            for(int i = 0; i < track.Count; i++){
                TrackPiece piece = track[i];
                if(piece.type == TrackPieceType.Turn){
                    (X,Y) = MoveBasedOnDirection(1, direction, X, Y);
                    direction = RotateDirection(piece.flipped, direction);
                }else if(piece.type == TrackPieceType.Straight || piece.type == TrackPieceType.FnFSpecial || piece.type == TrackPieceType.PreFinishLine || piece.type == TrackPieceType.CrissCross){
                    (X,Y) = MoveBasedOnDirection(1, direction, X, Y);
                }else if(piece.type == TrackPieceType.JumpRamp){
                    (X,Y) = MoveBasedOnDirection(2, direction, X, Y);
                }
                //Program.Log($"Validation: {i} {piece.type}, {X}, {Y}");
            }
            (X,Y) = MoveBasedOnDirection(1, direction, X, Y);
            if(X == 0 && Y == 0){ return true; } 
            Program.Log($"Validation failed, track does not connect, X: {X}, Y: {Y}, Expected: 0,0");
            return false;
        }
        bool MatchTracks(List<TrackPiece> trackPieces, List<TrackPiece> validation) {
            if(trackPieces.Count != validation.Count){ return false; }
            for(int i = 0; i < trackPieces.Count; i++){
                if(trackPieces[i] != validation[i]){ 
                    TrackPiece A = trackPieces[i];
                    TrackPiece B = validation[i];

                    //CrissCross is ID 10, and ID 0 is a fallback for unknown pieces
                    if(A.internalID == 0 && B.internalID == 10){ trackPieces[i] = validation[i]; }
                    else if(A.internalID == 10 && B.internalID == 0){ trackPieces[i] = validation[i]; }
                    else{
                        Program.Log($"Validation failed, track does not match, {trackPieces[i]} != {validation[i]}");
                        return false; 
                    }
                }
            }
            return true;
        }
        
        
    }
}
