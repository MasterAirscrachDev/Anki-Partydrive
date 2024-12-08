using static OverdriveServer.Tracks;
namespace OverdriveServer {
    class TrackScanner {
        List<TrackPiece> trackPieces;
        bool tracking = false, checkingScan = false, successfulScan = false, finishedScan = false;
        int retries = 0, maxRetries = 3;
        int currentPieceIndex = 0;
        int X = 0, Y = 0, direction = 0;
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
        void ResetScan() { trackPieces.Clear(); X = 0; Y = 0; direction = 0; currentPieceIndex = 0; }
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
        void OnTrackTransition(string carID, int trackPieceIdx, int oldTrackPieceIdx, float offset, int uphillCounter, int downhillCounter, int leftWheelDistance, int rightWheelDistance, bool crossedStartingLine){
            tracking = true; if(trackPieces.Count > 0){ trackPieces[trackPieces.Count - 1].SetUpDown(uphillCounter, downhillCounter);}
        }
        void OnTrackPosition(string carID, int trackLocation, int trackID, float offset, int speed, bool clockwise) {
            if(tracking) {
                tracking = false; bool AutoIncrementIndex = true;
                TrackPieceType type = PeiceFromID(trackID);
                TrackPiece piece = new TrackPiece(type, trackID, clockwise, X, Y);
                if(type == TrackPieceType.FinishLine && trackPieces.Count == 0) { //add a prefinish line piece if the finish line is the first piece
                    trackPieces.Add(new TrackPiece(TrackPieceType.PreFinishLine, 34, clockwise, X, Y));
                }
                if(checkingScan) {
                    bool valid = ValidateTrackPiece(piece); if(!valid){ return; }
                } else {
                    trackPieces.Add(piece);
                    if(type == TrackPieceType.Turn){ RotateDirection(clockwise); } //rotate the direction if we are on a turn
                    if(type != TrackPieceType.PreFinishLine) {
                        if(type != TrackPieceType.Unknown){ MoveBasedOnDirection(); } //move the car forward if we are not on a prefinish line
                        else{ Program.Log($"Unknown track piece: {trackID}, internal id:{(int)type}"); }
                    } else { AutoIncrementIndex = false; }

                    if(!checkingScan && trackPieces.Count > 4 && trackPieces[0].IsAt(X, Y)){ //if our current position is the same as the start position, set checkingScan to true
                        checkingScan = true; currentPieceIndex = -1;
                    }
                }
                SendCurrentTrack();
                //Console.WriteLine($"index: {currentPieceIndex}/{trackPieces.Count} is ({type}|{trackID}|[{oldX},{oldY}]), checking: {checkingScan}, retries: {retries}");
                currentPieceIndex+= AutoIncrementIndex ? 1 : 0;
            }
        }
        void RotateDirection(bool clockwise){
            if(clockwise){ direction++; }else{ direction--; }
            if(direction == 4){ direction = 0; }
            else if(direction == -1){ direction = 3; }
        }
        void MoveBasedOnDirection(int spaces = 1){
            if(direction == 0){ Y += spaces; }
            else if(direction == 1){ X += spaces; }
            else if(direction == 2){ Y -= spaces; }
            else if(direction == 3){ X -= spaces; }
        }
        bool ValidateTrackPiece(TrackPiece piece) { //if this piece is the same as the piece at the same index in the trackPieces list, we are on the right track
            if(!trackPieces[currentPieceIndex].Equals(piece)) {
                retries++; //if we have retried too many times, we have failed the scan
                if(retries >= maxRetries) { finishedScan = true;  SendFinishedTrack(); }
                else{ checkingScan = false; ResetScan(); return false; }
            } else {
                trackPieces[currentPieceIndex].validated = true;
                if(currentPieceIndex == trackPieces.Count - 1){ successfulScan = true; finishedScan = true; SendFinishedTrack(); }
            }
            return true;
        }
        void OnCarJumped(string carID) {
            if(carID == scanningCar.id) {
                OnTrackPosition(carID, 0, 58, 0, 0, false); //jump ramp
                tracking = true;
                OnTrackPosition(carID, 0, 63, 0, 0, false); //jump landing
            }
        }
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
    }
}
