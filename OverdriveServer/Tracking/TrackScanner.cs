using AsyncAwaitBestPractices;
using static OverdriveServer.Tracks;
using static OverdriveServer.NetStructures.UtilityMessages;
using static OverdriveServer.NetStructures;
namespace OverdriveServer {
    class TrackScanner {
        List<CarScan> cars = new List<CarScan>();
        bool eventsSubbed = false;
        int mostSegments = 0, mostValidated = 0;
        public void ScanTrack(int finishLines) {
            SetEventsSub(true);
            mostSegments = 0; mostValidated = 0;
            Car[] useForScan = Program.carSystem.GetCarsOnTrack(true);

            if(useForScan.Length == 0){ Program.Log("No cars available for scanning"); return; }
            cars.Clear();
            for(int i = 0; i < useForScan.Length; i++){
                cars.Add(new CarScan(useForScan[i], this, finishLines, i));
            }
        }
        public void CancelScan() {
            SetEventsSub(false);
            for(int i = 0; i < cars.Count; i++){
                cars[i].car.SetCarSpeed(0, 500);
            }
            cars.Clear();
        }
        void SetEventsSub(bool sub) {
            if(sub && !eventsSubbed){ 
                Program.messageManager.CarEventSegmentCall += OnTrackSegment;
                Program.messageManager.CarEventJumpCall += OnCarJumped;
                Program.messageManager.CarEventDelocalised += OnCarFell;
                eventsSubbed = true;
            }else if(!sub && eventsSubbed){
                Program.messageManager.CarEventSegmentCall -= OnTrackSegment;
                Program.messageManager.CarEventJumpCall -= OnCarJumped;
                Program.messageManager.CarEventDelocalised -= OnCarFell;
                eventsSubbed = false;
            }
        }
        void RemoveCar(string carID) {
            CarScan? car = cars.Find(c => c.car.id == carID);
            if(car != null){
                Program.Log($"Removing car {car.car.name} from track scan");
                cars.Remove(car);
                if(cars.Count == 0){ SendFinishedTrack(null, false); } //if no cars left, fail scan
            }
        }
        
        void SendFinishedTrack(Segment[] segments, bool successfulScan) {
            SetEventsSub(false);
            Program.UtilLog($"{MSG_TR_SCAN_UPDATE}:{successfulScan}");
            Program.Log($"Track scan finished, successful: {successfulScan}, segments: {segments?.Length}");
            Program.trackManager.SetTrack(successfulScan ? segments : null, successfulScan);
            if(successfulScan){  
                LineupDelayed(); //wait for the cars to stop before requesting lineup
            }
            else{
                for(int i = 0; i < cars.Count; i++){
                    cars[i].car.SetCarSpeed(0, 500); //stop the car
                }
            }
            cars.Clear(); //clear the cars
        }
        async Task LineupDelayed(){
            await Task.Delay(6000); //wait for the cars localize
            Program.trackManager.RequestLineup(); //request lineup
        }
        void OnCarJumped(string carID) { cars.Find(c => c.car.id == carID)?.OnCarJumped(); }
        void OnCarFell(string carID) { cars.Find(c => c.car.id == carID)?.OnCarDelocalized(); }
        void OnTrackSegment(string carID, Segment segment, float _){ cars.Find(c => c.car.id == carID)?.OnTrackSegment(segment); }
        
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
        
        void ValidateAndSendTrack(Segment[] segments, Segment[] validation) { //called by a car when it finds a track piece
            Segment[] track;
            if(validation != null){ //if we are validating a track
                if(segments.Length > validation.Length){ //if the track is longer than the validation track, we need to copy the validation track
                    Program.Log($"Validation failed, track is longer than validation track, {segments.Length} > {validation.Length}");
                    return;
                }
                track = validation;
                //copy any segments onto the validation track
                for(int i = 0; i < segments.Length; i++){
                    track[i] = segments[i];
                }
            }else{ //if we are not validating a track
                track = segments;
            }
            
            int validated = 0; bool send = false;
            for(int i = 0; i < track.Length; i++){ if(track[i].validated){ validated++; } }

            if(validated > mostValidated){ 
                mostValidated = validated; mostSegments = 0; send = true;
            }else if(track.Length > mostSegments && mostValidated == 0){
                mostSegments = track.Length; send = true;
            }
            if(send){
                Program.trackManager.SetTrack(track, false);
                Program.UtilLog($"{MSG_TR_SCAN_UPDATE}:in-progress");
            }
        }
        bool ValidateTrackConnects(List<Segment> track){ //called when the car switches to validation mode (if the track is connects)
            int X = 0, Y = -1, direction = 0;
            for(int i = 0; i < track.Count; i++){
                Segment piece = track[i];
                if(piece.type == SegmentType.Turn){
                    (X,Y) = MoveBasedOnDirection(1, direction, X, Y);
                    direction = RotateDirection(piece.flipped, direction);
                }else if(piece.type == SegmentType.Straight || piece.type == SegmentType.FnFSpecial || piece.type == SegmentType.PreFinishLine || piece.type == SegmentType.CrissCross){
                    (X,Y) = MoveBasedOnDirection(1, direction, X, Y);
                }else if(piece.type == SegmentType.JumpRamp){
                    (X,Y) = MoveBasedOnDirection(2, direction, X, Y);
                }
                //Program.Log($"Validation: {i} {piece.type}, {X}, {Y}");
            }
            (X,Y) = MoveBasedOnDirection(1, direction, X, Y);
            if(X == 0 && Y == 0){ return true; } 
            Program.Log($"Validation failed, track does not connect, X: {X}, Y: {Y}, Expected: 0,0");
            return false;
        }
        bool MatchTracks(List<Segment> trackPieces, List<Segment> validation) {
            if(trackPieces.Count != validation.Count){ return false; }
            for(int i = 0; i < trackPieces.Count; i++){
                (bool match, Segment? updateTo) = EvaluateMatch(trackPieces[i], validation[i]);
                if(match){ //if the piece matches, return true
                    if(updateTo != null) { trackPieces[i] = updateTo; validation[i] = updateTo; } //update the piece if needed
                }else{
                    Program.Log($"Validation failed, track does not match, {trackPieces[i]} != {validation[i]}");
                    return false; 
                }
            }
            return true;
        }
        
        internal class CarScan{
            public CarScan(Car car, TrackScanner scanner, int finishCount, int scanIdx){
                this.car = car;
                this.scanner = scanner;
                this.finishCount = finishCount;
                StartDriving(scanIdx).Wait(); //start the car moving
            }
            async Task StartDriving(int index){
                await Task.Delay(500 * index); //wait for the car to start moving
                car.SetCarSpeed(600, 500, true, false); //start the car moving
                float[] lanes = { 72.25f, 21.25f, -21.25f, -72.25f };
                index += 1; //increment the index to start at 1
                //wrap the index if it is greater than 4
                while(index > 3){ index -= 4; } //ensure the index is between 0 and 4
                car.SetCarLane(lanes[index]); //set the car to the lane based on the index
            }
            public Car car;
            TrackScanner scanner;
            List<Segment> segments = new List<Segment>();
            List<Segment>? validationTrack;
            bool validation = false, isScanning = false, isJumping = false;
            int finishCount;
            int finishesPassed = 0, scanAttempts = 0, skipSegments = 0;

            internal void OnTrackSegment(Segment segment){
                if(!isScanning){ //true until we reach the first finish line
                    if(segment.type == SegmentType.PreFinishLine && segment.internalID == 34){
                        car.SetCarSpeed(450, 500);
                        isScanning = true;
                        segments.Add(segment);
                        segments.Add(new Segment(SegmentType.FinishLine, 33, false));
                        skipSegments = 1;
                        scanner.ValidateAndSendTrack(segments.ToArray(), null);
                    }else if(segment.internalID == 33 && segment.type == SegmentType.FinishLine){ //should be able to initate a scan from the start correctly now
                        isScanning = true;
                        segments.Add(new Segment(SegmentType.PreFinishLine, 34, false));
                        segments.Add(segment);
                        scanner.ValidateAndSendTrack(segments.ToArray(), null);
                    }
                }else{
                    if(skipSegments > 0){ 
                        //Program.Log($"Skipping segment, skip: {skipSegments}");
                        skipSegments--; return; 
                    }
                    if(segment.type == SegmentType.PreFinishLine){
                        finishesPassed++;
                        if(finishesPassed >= finishCount){ //validation phase
                            if(!validation){ //if we are on the first scan
                                validationTrack = segments.ToList();
                                if(scanner.ValidateTrackConnects(validationTrack)){ //ensure the track loops
                                    validation = true;
                                    segments.Clear();
                                    segments.Add(segment);
                                    segments.Add(new Segment(SegmentType.FinishLine, 33, false));
                                    segments[segments.Count - 1].validated = true;
                                    segments[segments.Count - 2].validated = true;
                                    skipSegments = 1;
                                    scanner.ValidateAndSendTrack(segments.ToArray(), validationTrack?.ToArray());
                                    finishesPassed = 0; //reset the finishes passed
                                }else{ //validation failed reset and try again
                                    scanAttempts++;
                                    if(scanAttempts > 3){ scanner.RemoveCar(car.id); return; }
                                    segments.Clear();
                                    validationTrack = null;
                                    validation = false;
                                    finishesPassed = 0 ;
                                    skipSegments = 1;
                                    segments.Add(segment);
                                    segments.Add(new Segment(SegmentType.FinishLine, 33, false));
                                    scanner.ValidateAndSendTrack(segments.ToArray(), null);
                                }
                            }else { //compare validation track to trackPieces
                                bool successfulScan = scanner.MatchTracks(validationTrack, segments);
                                scanner.SendFinishedTrack(segments.ToArray(), successfulScan);
                            }
                        }else{
                            segments.Add(segment);
                            segments.Add(new Segment(SegmentType.FinishLine, 33, false));
                            segments[segments.Count - 1].validated = validation;
                            segments[segments.Count - 2].validated = validation;
                            skipSegments = 1;

                            scanner.ValidateAndSendTrack(segments.ToArray(), validationTrack?.ToArray());
                        }
                    }else{
                        if(new SegmentType[] { SegmentType.Unknown, SegmentType.PreFinishLine, SegmentType.FinishLine, SegmentType.JumpRamp, SegmentType.JumpLanding }.Contains(segment.type)){ return; }
                        if(isJumping && segment.type == SegmentType.Straight){ //if we just jumped, continue the scan
                            isJumping = false;
                            segments[segments.Count - 1] = segment;
                        }else{
                            segments.Add(segment);
                        }
                        segments[segments.Count - 1].validated = validation;
                        scanner.ValidateAndSendTrack(segments.ToArray(), validationTrack?.ToArray());
                    }
                }
            }
            internal void OnCarJumped() {
                segments.Add(new Segment(SegmentType.JumpRamp, 58, false));
                segments.Add(new Segment(SegmentType.JumpLanding, 63, false));
                segments.Add(new Segment(SegmentType.Straight, 40, false));
                segments[segments.Count - 1].validated = validation;
                segments[segments.Count - 2].validated = validation;
                segments[segments.Count - 3].validated = validation;
                isJumping = true;
                scanner.ValidateAndSendTrack(segments.ToArray(), validationTrack?.ToArray());
            }
            internal void OnCarDelocalized() {
                //if the car fell, stop the car
                scanner.RemoveCar(car.id);
            }
        }
    }
}
