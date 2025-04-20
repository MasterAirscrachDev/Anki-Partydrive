using Newtonsoft.Json;
using static OverdriveServer.Tracks;
using static OverdriveServer.NetStructures;

namespace OverdriveServer {
    class TrackManager {
        public int totalStarts = 1;
        TrackPiece[]? track;
        bool trackValidated = false;
        int carsAwaitingLineup = 0;
        Dictionary<string, TrackCarLocation> carLocations = new Dictionary<string, TrackCarLocation>();
        public void SetTrack(TrackPiece[] track, bool validated){ 
            this.track = track; trackValidated = validated; 
            carLocations.Clear(); //clear the car locations, we need to revalidate them
        }
        public TrackManager(){
            carLocations = new Dictionary<string, TrackCarLocation>();
            Program.messageManager.CarEventLocationCall += OnCarPosUpdate;
            Program.messageManager.CarEventSegmentCall += OnSegment;
            //Program.messageManager.CarEventTransitionCall += OnCarTransition;
            Program.messageManager.CarEventDelocalised += OnCarDelocalised;
        }
        public void AlertIfTrackIsValid(){ if(trackValidated){ Program.UtilLog($"-3:_:{trackValidated}"); } }
        void OnCarPosUpdate(string id, int trackLocation, int trackID, float offset, int speed, bool clockwise){
            if(!trackValidated){ return; }
            if(carLocations.ContainsKey(id)){ //we already have a location for this car
                carLocations[id].horizontalPosition = offset;
            }else{
                carLocations.Add(id, new TrackCarLocation(track.Length)); //add a new location for this car
                carLocations[id].horizontalPosition = offset;
            }
        }
        void OnSegment(string carID, TrackPiece segment, float offset, int up, int down){
            if(!trackValidated){ return; }
            TrackCarLocation car;
            if(carLocations.ContainsKey(carID)){ //we already have a location for this car
                car = carLocations[carID];
            }else{
                car = new TrackCarLocation(track.Length); //add a new location for this car
                carLocations.Add(carID, car);
            }
            car.horizontalPosition = offset;
            //Console.WriteLine($"idx {car.trackIndex} {track[car.trackIndex]}");
            //match the cars track memory to see if we can find the correct index
            int memoryLength = car.GetLocalisedTrackLength();
            if(memoryLength > 2){ //we have some track memory, try to match it to the track
                //Console.WriteLine($"{car.MemoryString()}");
                int matchCount = 0;
                int matchedIndex = -1;
                int bestTrackIndex = -1, bestMemoryCount = 0;
                // Find all potential matches
                for(int trackIndex = 0; trackIndex < track.Length; trackIndex++){
                    bool match = true;
                    
                    string check = "Chk: ";
                    int memCount = 0;
                    for (int memoryIndex = memoryLength - 1; memoryIndex >= 0; memoryIndex--) { 
                        TrackPiece piece = GetTrackPieceLooped(trackIndex + memoryIndex); // Get the track piece at the current index
                        check += $"[{piece.type} {piece.internalID} {piece.flipped}]";
                        if (!car.PieceMatches(piece, memoryIndex)) { match = false; break; }
                        memCount++;
                    }
                    if(match){
                        matchCount++;
                        matchedIndex = trackIndex + memoryLength; // Store the matched index
                        if(memCount > bestMemoryCount){ //if this match is better than the previous one, store it
                            bestMemoryCount = memCount; // Store the best memory count
                            bestTrackIndex = trackIndex; // Store the best track index
                        }
                    }
                    //Console.WriteLine(check);
                    
                }
                // Report the number of potential matches
                //Console.WriteLine($"Car {carID} has {matchCount} potential track position matches");
                if(matchedIndex > track.Length - 1){ //if the matched index is greater than the track length, we need to loop it
                    matchedIndex = matchedIndex - track.Length; //loop the index
                }
                // Only update position if exactly one match is found and it's different from current
                if(matchCount == 1 && (matchedIndex != car.trackIndex || !car.positionTrusted)){
                    //Console.WriteLine($"Car {id} index corrected from {car.trackIndex} to {matchedIndex}");
                    car.trackIndex = matchedIndex;
                    car.positionTrusted = true; //we are sure about the position now
                    car.voidSegments = 0; //reset the void segments counter
                }
                else if(matchCount > 1){
                    //Console.WriteLine($"Car {carID} has ambiguous position - multiple matches found. Keeping current position.");
                    car.trackIndex = matchedIndex; //update the track index to the best match
                    car.positionTrusted = false; //we are not sure about the position now
                    car.voidSegments = 0; //reset the void segments counter
                }else if(matchCount == 0){ //if we have a lot of memory and no matches, we might be on the wrong track (update this to require a few warnings before we do this)
                    if(car.voidSegments > 4){
                        Car carE = Program.carSystem.GetCar(carID);
                        int speed = carE.data.speed;
                        carE.SetCarSpeed(150, 2000); //slow down to 150
                        Program.carSystem.GetCar(carID).UTurn(); //request a U-turn
                        //Console.WriteLine($"Car {carID} has no matches, requesting U-turn");
                        car.ClearTracks(); //clear the track memory, we are lost
                        //in 2s return to normal speed
                        Task.Delay(2000).ContinueWith(t => {
                            if(Program.carSystem.GetCar(carID) != null){
                                Program.carSystem.GetCar(carID).SetCarSpeed(speed); //return to normal speed
                                car.ClearTracks(); //clear the track memory
                            }
                        });
                    }else{
                        car.voidSegments++; //increment the void segments counter
                        car.SafeClear(); //clear the track memory, but keep position Trusted
                    }
                }
                Console.WriteLine("");
            }
            //end localisation check
            car.OnSegment(segment, offset); //update the car's track position
            if(carsAwaitingLineup > 0){ //if we are waiting for lineup, check if we are at the start line
                if(car.positionTrusted){
                    if(car.trackIndex == 0){ //if we are at the start line, stop the car
                        //Program.carSystem.GetCar(carID).SetCarSpeed(150, 1000); //stop the car
                        Task.Delay(850).ContinueWith(t => {
                            if(Program.carSystem.GetCar(carID) != null){
                                Program.carSystem.GetCar(carID).SetCarSpeed(0, 3000); //stop the car
                                Program.Log($"Lineup finished for {carID}");
                                carsAwaitingLineup--;
                            }
                        });
                    }//if we are in the last 2 segments then slow down
                    else if(car.trackIndex >= track.Length - 1){ //if we are in the last 2 segments then slow down
                        Program.carSystem.GetCar(carID).SetCarSpeed(330, 500); //slow down to 200
                        Program.Log($"Lineup slowing down for {carID}");
                    }
                }
            }

            int carspeed = 0;
            if(Program.carSystem.GetCar(carID) != null){ carspeed = Program.carSystem.GetCar(carID).data.speed; }
            Program.socketMan.Notify(EVENT_CAR_TRACKING_UPDATE, 
                new CarLocationData{
                    carID = carID,
                    trackIndex = car.trackIndex,
                    speed = carspeed,
                    offset = car.horizontalPosition,
                    trustLevel = car.positionTrusted == true ? 2 : 1 //trust level (0 = not trusted, 1 = likely accurate, 2 = certain)
                }
            );
        }
        void OnCarDelocalised(string id){
            if(carLocations.ContainsKey(id)){ //we already have a location for this car
                carLocations[id].ClearTracks(); //clear the track memory
                Console.WriteLine($"Car {id} delocalised, clearing track memory");
            }
        }

        public void RequestLineup(){
            Car[] cars = Program.carSystem.GetCarsOnTrack();
            if(cars.Length == 0){ Program.Log("No cars to lineup"); return; }
            if(track == null || !trackValidated){ Program.Log("No track to lineup on"); return; }
            //set the lane offset for each car based on index
            if(cars.Length > 4){ Program.Log("Too many cars to lineup"); return; } //max 4 cars (for now)
            float outerMostLane = 70; //outer most lane offset
            //lanes  go 1, 2, 3, 4 (left to right) flipping from positive to negative in the middle
            for(int i = 0; i < cars.Length; i++){
                float laneOffset = 0;
                if(cars.Length == 2){
                    //car 0 should be in the middle of the left half, car 1 should be in the middle of the right half
                    laneOffset = (i == 0) ? (outerMostLane / 2) : (-outerMostLane / 2); //set the lane offset for each car
                }else if(cars.Length == 3){
                    if(i == 0){ laneOffset = (outerMostLane / 2) + 15; } //car 0 should be in the middle of the left half
                    else if(i == 1){ laneOffset = 0; } //car 1 should be in the middle of the track
                    else{ laneOffset = -(outerMostLane / 2) - 15 ; } //car 2 should be in the middle of the right half
                }else if(cars.Length == 4){
                    
                }
                cars[i].SetCarSpeed(550, 1000); //set the speed to 550 for all cars
                cars[i].SetCarLane(laneOffset, 10); //set the lane offset for each car
            }
            carsAwaitingLineup = cars.Length;
            Program.Log($"Lineup started with {cars.Length} cars");
        }

        TrackPiece GetTrackPieceLooped(int index){
            if(track == null){ return null; }
            if(index < 0){ index = track.Length + index; } //loop backwards
            if(index >= track.Length){ index = index - track.Length; } //loop forwards
            return track[index];
        }
        public string TrackDataAsJson(){ return JsonConvert.SerializeObject(track); }
        public static TrackPieceType PieceFromID(int id) {
            if(id == 17 || id == 18 || id == 20 || id == 23 || id == 24 || id == 27){ return TrackPieceType.Turn; }
            else if(id == 36 || id == 39 || id == 40 || id == 48 || id == 51){ return TrackPieceType.Straight; }
            else if(id == 57 || id == 53 || id == 54){ return TrackPieceType.FnFSpecial; }
            else if(id == 34){ return TrackPieceType.PreFinishLine; }
            else if(id == 33){ return TrackPieceType.FinishLine; }
            else if(id == 10){ return TrackPieceType.CrissCross; } 
            else if(id == 58 || id == 43){ return TrackPieceType.JumpRamp; }
            else if(id == 63 || id == 46){ return TrackPieceType.JumpLanding; }
            else{ return TrackPieceType.Unknown; }
        }
        public static (bool, TrackPiece?) EvaluateMatch(TrackPiece A, TrackPiece B){
            if(A == null || B == null){ return (false, null); } //if either piece is null, we can't match them
            else if(A.internalID != 0 && B.internalID != 0){ //if both pieces are not fallbacks
                return ((A.type == B.type) && (A.flipped == B.flipped), null); //match if the type and flipped state are the same
            }else if(A.internalID == 0 && B.internalID == 0){ //if both pieces are fallbacks
                return ((A.type == B.type) && (A.flipped == B.flipped), null); //match if the type and flipped state are the same
            }
            else{ //A or B is a fallback, check if we can match them
                for(int i = 0; i < 2; i++){ 
                    TrackPiece C = A; //copy A to C
                    TrackPiece D = B; //copy B to D
                    if(i == 1){ C = B; D = A; } //swap C and D
                    if(C.internalID == 0 && D.internalID != 0){ //if C is a fallback and D is not
                        if(C.type == D.type){ 
                            if(C.validated){ D.validated = true; } //if C is validated, set D to validated
                            return (true, D); 
                        } //if the type is the same, its probably the same piece (return the one with the ID)
                        else{
                            if(C.type == TrackPieceType.Straight && D.type == TrackPieceType.CrissCross) { 
                                if (C.validated){ D.validated = true; } //if C is validated, set D to validated
                                return (true, D);
                            } //crisscross is a straight piece
                            else if(C.type == TrackPieceType.Straight && D.type == TrackPieceType.FnFSpecial) { 
                                if (C.validated){ D.validated = true; } //if C is validated, set D to validated
                                return (true, D); 
                            } //FnF is a straight piece
                        }
                    }
                }
            }
            return (false, null); //no match found (should not happen)
        }
        public static int Abs(int i) { return i < 0 ? -i : i; }
    }
}
