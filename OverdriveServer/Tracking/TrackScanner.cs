using AsyncAwaitBestPractices;
using static OverdriveServer.Tracks;
using static OverdriveServer.NetStructures.UtilityMessages;
using static OverdriveServer.NetStructures;
namespace OverdriveServer {
    class TrackScanner {
        List<CarScan> cars = new List<CarScan>();
        List<Segment[]> knownAreas = new List<Segment[]>();
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
            Program.trackManager.SetTrack(null, false); //clear the current track
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
                else{
                    mostSegments = 0; mostValidated = 0; //reset the most segments and validated
                }
            }
        }
        
        async void SendFinishedTrack(Segment[] segments, bool successfulScan) {
            SetEventsSub(false);
            Program.UtilLog($"{MSG_TR_SCAN_UPDATE}:{successfulScan}");
            Program.Log($"Track scan finished, successful: {successfulScan}, segments: {segments?.Length}");
            
            
            Program.trackManager.SetTrack(successfulScan ? segments : null, successfulScan, segments != null && successfulScan ? IsDriveMat(segments[0].type) : false);
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
        
        void ValidateAndSendTrack(Segment[] segments, Segment[]? validation) { //called by a car when it finds a track piece
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
            
            // Try to fill fallbacks with known areas if this is a complete track
            if(validation == null) {
                track = TryFillFallbacksWithKnownAreas(track);
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
        bool IsDriveMat(SegmentType type) {
            return type == SegmentType.Oval || 
            type == SegmentType.Bottleneck || 
            type == SegmentType.Crossroads || 
            type == SegmentType.F1 || 
            type == SegmentType.DoubleCross;
        }
        
        void AnalyzeFallbacksAndExtractKnownAreas(Segment[] segments) {
            if(segments == null || segments.Length == 0) return;
            
            int fallbackCount = 0;
            var fallbackTypes = new Dictionary<SegmentType, int>();
            
            // Analyze fallbacks
            foreach(var segment in segments) {
                if(segment.internalID == 0) {
                    fallbackCount++;
                    fallbackTypes[segment.type] = fallbackTypes.GetValueOrDefault(segment.type, 0) + 1;
                }
            }
            
            float fallbackPercentage = (float)fallbackCount / segments.Length * 100f;
            Program.Log($"Track Analysis: {fallbackPercentage:F1}% fallbacks ({fallbackCount}/{segments.Length}) lower is better", true);
            
            if(fallbackTypes.Count > 0) {
                var fallbackBreakdown = string.Join(", ", fallbackTypes.Select(kv => $"{kv.Key}: {kv.Value}"));
                Program.Log($"Fallback types: {fallbackBreakdown}", true);
                // Extract known areas (sequences of 4+ non-fallback segments)
                ExtractKnownAreas(segments);
            }
        }
        
        void ExtractKnownAreas(Segment[] segments) {
            var currentSequence = new List<Segment>();
            
            for(int i = 0; i < segments.Length; i++) {
                if(segments[i].internalID != 0) {
                    // Non-fallback segment
                    currentSequence.Add(segments[i]);
                } else {
                    // Fallback segment - end current sequence
                    if(currentSequence.Count >= 4) {
                        AddKnownArea(currentSequence.ToArray());
                        Program.Log($"Extracted known area: {currentSequence.Count} segments starting with {currentSequence[0].type} (ID: {currentSequence[0].internalID})");
                    }
                    currentSequence.Clear();
                }
            }
            
            // Check final sequence
            if(currentSequence.Count >= 4) {
                AddKnownArea(currentSequence.ToArray());
                Program.Log($"Extracted known area: {currentSequence.Count} segments starting with {currentSequence[0].type} (ID: {currentSequence[0].internalID})");
            }
        }
        
        void AddKnownArea(Segment[] area) {
            // Check if we already have this sequence
            foreach(var existing in knownAreas) {
                if(SequencesMatch(existing, area)) {
                    return; // Already have this sequence
                }
            }
            
            knownAreas.Add(area);
            Program.Log($"Added new known area with {area.Length} segments to database (total: {knownAreas.Count})");
        }
        
        bool SequencesMatch(Segment[] a, Segment[] b) {
            if(a.Length != b.Length) return false;
            
            for(int i = 0; i < a.Length; i++) { //only need to compare internalID and flipped (type is not needed when comparing non-fallback segments)
                if(a[i].internalID != b[i].internalID || a[i].flipped != b[i].flipped) {
                    return false;
                }
            }
            return true;
        }
        
        Segment[] TryFillFallbacksWithKnownAreas(Segment[] track) {
            if(knownAreas.Count == 0) return track;
            
            var modifiedTrack = track.ToArray(); // Create a copy
            bool madeChanges = false;
            
            // Find sequences of fallback segments
            for(int i = 0; i < track.Length; i++) {
                if(track[i].internalID == 0) {
                    // Found a fallback, try to find a sequence
                    int fallbackStart = i;
                    int fallbackEnd = i;
                    
                    // Extend the fallback sequence
                    while(fallbackEnd < track.Length && track[fallbackEnd].internalID == 0) {
                        fallbackEnd++;
                    }
                    
                    int fallbackLength = fallbackEnd - fallbackStart;
                    if(fallbackLength >= 4) { // Only try to fill sequences of 4+ fallbacks
                        var matchingArea = FindMatchingKnownArea(track, fallbackStart, fallbackLength);
                        if(matchingArea != null) {
                            // Replace fallbacks with known area
                            for(int j = 0; j < matchingArea.Length && (fallbackStart + j) < modifiedTrack.Length; j++) {
                                modifiedTrack[fallbackStart + j] = matchingArea[j];
                                madeChanges = true;
                            }
                            Program.Log($"Filled {Math.Min(matchingArea.Length, fallbackLength)} fallback segments at position {fallbackStart} with known area");
                        }
                    }
                    
                    i = fallbackEnd - 1; // Skip past this sequence
                }
            }
            
            if(madeChanges) {
                Program.Log($"Successfully filled fallbacks using known areas database");
            }else{
                Program.Log($"No fallbacks could be filled with known areas");
            }
            
            return modifiedTrack;
        }
        
        Segment[]? FindMatchingKnownArea(Segment[] track, int fallbackStart, int fallbackLength) {
            var candidates = new List<Segment[]>();
            
            // Check each known area to see if it could fit
            foreach(var knownArea in knownAreas) {
                if(knownArea.Length == fallbackLength) {
                    // Check if this known area could match the fallback pattern
                    bool couldMatch = true;
                    for(int i = 0; i < fallbackLength; i++) {
                        if(i + fallbackStart < track.Length) {
                            var fallback = track[fallbackStart + i];
                            var known = knownArea[i];
                            
                            // Check if the known segment type matches the fallback type
                            if(fallback.type != known.type) {
                                couldMatch = false;
                                break;
                            }
                        }
                    }
                    
                    if(couldMatch) {
                        candidates.Add(knownArea);
                    }
                }
            }
            
            // Only return if there's exactly one match
            if(candidates.Count == 1) {
                return candidates[0];
            } else if(candidates.Count > 1) {
                Program.Log($"Multiple known areas match fallback sequence at position {fallbackStart}, skipping to avoid ambiguity");
            }
            
            return null;
        }
        static Segment[] GenerateDriveMatSegments(SegmentType matType){

            if(matType == SegmentType.Oval){
                return [
                    new Segment(SegmentType.Oval, 70, false),
                    new Segment(SegmentType.Oval, 71, false),
                    new Segment(SegmentType.Oval, 72, false),
                    new Segment(SegmentType.Oval, 73, false),
                    new Segment(SegmentType.Oval, 74, false),
                    new Segment(SegmentType.Oval, 75, false),
                    new Segment(SegmentType.Oval, 76, false),
                    new Segment(SegmentType.Oval, 77, false),
                ];
            }else if(matType == SegmentType.Bottleneck){
                return [
                    new Segment(SegmentType.Bottleneck, 81, true),
                    new Segment(SegmentType.Bottleneck, 80, true),
                    new Segment(SegmentType.Bottleneck, 79, true),
                    new Segment(SegmentType.Bottleneck, 78, true),
                    new Segment(SegmentType.Bottleneck, 91, true),
                    new Segment(SegmentType.Bottleneck, 90, true),
                    new Segment(SegmentType.Bottleneck, 89, true),
                    new Segment(SegmentType.Bottleneck, 88, true),
                    new Segment(SegmentType.Bottleneck, 87, true),
                    new Segment(SegmentType.Bottleneck, 86, true),
                    new Segment(SegmentType.Bottleneck, 85, true),
                    new Segment(SegmentType.Bottleneck, 84, true),
                    new Segment(SegmentType.Bottleneck, 83, true),
                    new Segment(SegmentType.Bottleneck, 82, true),
                ];
            }else if(matType == SegmentType.Crossroads){
                return [
                    new Segment(SegmentType.Crossroads, 100, false),
                    new Segment(SegmentType.Crossroads, 101, false),
                    new Segment(SegmentType.Crossroads, 102, false),
                    new Segment(SegmentType.Crossroads, 103, false),
                    new Segment(SegmentType.Crossroads, 104, false),
                    new Segment(SegmentType.Crossroads, 105, false),
                    new Segment(SegmentType.Crossroads, 92, false),
                    new Segment(SegmentType.Crossroads, 93, false),
                    new Segment(SegmentType.Crossroads, 94, false),
                    new Segment(SegmentType.Crossroads, 95, false),
                    new Segment(SegmentType.Crossroads, 96, false),
                    new Segment(SegmentType.Crossroads, 97, false),
                    new Segment(SegmentType.Crossroads, 98, false),
                    new Segment(SegmentType.Crossroads, 99, false),
                ];
            }
            return Array.Empty<Segment>();
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
                float[] lanes = { 67.5f, 22.5f, -22.5f, -67.5f }; // Second and first linable lanes (lane 8 and 3)
                index += 1; //increment the index to start at 1
                //wrap the index if it is greater than 4
                while(index > 3){ index -= 4; } //ensure the index is between 0 and 4
                car.SetCarLane(lanes[index]); //set the car to the lane based on the index
            }
            public Car car;
            TrackScanner scanner;
            List<Segment> segments = new List<Segment>();
            List<Segment>? validationTrack;
            bool validation = false, isScanning = false, isJumping = false, isStopped = false;
            int finishCount;
            int finishesPassed = 0, scanAttempts = 0, skipSegments = 0;
            SegmentType possibleDriveMat = SegmentType.Unknown;

            internal void OnTrackSegment(Segment segment){
                if(isStopped){ return; } //stop processing if we've already finished
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
                    }else if(segment.type == SegmentType.Oval || segment.type == SegmentType.Bottleneck || segment.type == SegmentType.Crossroads){
                        if(possibleDriveMat == SegmentType.Unknown){ possibleDriveMat = segment.type; }
                        else if(possibleDriveMat != segment.type){ possibleDriveMat = SegmentType.Unknown; } //if we see a different piece, reset the possible piece
                        else{ //we are on a drive mat
                            Segment[] matSegments = GenerateDriveMatSegments(possibleDriveMat);
                            if(matSegments.Length > 0){
                                segments.AddRange(matSegments);
                                isStopped = true; //stop processing further segments
                                scanner.SendFinishedTrack(segments.ToArray(), true);
                            }
                        }
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
                                isStopped = true; //stop processing further segments
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
                        if(new SegmentType[] { SegmentType.Unknown, SegmentType.PreFinishLine, SegmentType.FinishLine, SegmentType.JumpRamp }.Contains(segment.type)){ return; }
                        if(isJumping){ //if we just jumped, continue the scan
                            if(segment.type != SegmentType.JumpLanding){
                                segments.Add(new Segment(SegmentType.JumpLanding, 63, false)); //there no guarantee that its 63
                                segments[segments.Count - 1].validated = validation;
                            }
                            isJumping = false;
                        }
                        segments.Add(segment);
                        segments[segments.Count - 1].validated = validation;
                        scanner.ValidateAndSendTrack(segments.ToArray(), validationTrack?.ToArray());
                    }
                }
            }
            internal void OnCarJumped() {
                if(isScanning){
                    segments.Add(new Segment(SegmentType.JumpRamp, 58, false)); //there no guarantee that its 58
                    segments[segments.Count - 1].validated = validation;
                    isJumping = true;
                    skipSegments = 2; //skip the detecting the jump ramp and landing (maybe)
                    scanner.ValidateAndSendTrack(segments.ToArray(), validationTrack?.ToArray());
                }
            }
            internal void OnCarDelocalized() {
                //if the car fell, stop the car
                scanner.RemoveCar(car.id);
            }
        }
    }
}
