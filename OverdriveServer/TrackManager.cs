using Newtonsoft.Json;
using static OverdriveServer.Tracks;
using static OverdriveServer.NetStructures;

namespace OverdriveServer {
    class TrackManager {
        public int totalStarts = 1;
        TrackPiece[]? track;
        bool trackValidated = false;
        List<TrackCarLocation> carLocations;
        public void SetTrack(TrackPiece[] track, bool validated){ 
            this.track = track; trackValidated = validated; 
            carLocations.Clear(); //clear the car locations, we need to revalidate them
        }
        public TrackManager(){
            carLocations = new List<TrackCarLocation>();
            Program.messageManager.CarEventLocationCall += OnCarPosUpdate;
            Program.messageManager.CarEventTransitionCall += OnCarTransition;
            Program.messageManager.CarEventDelocalised += OnCarDelocalised;
        }
        public void AlertIfTrackIsValid(){ if(trackValidated){ Program.UtilLog($"-3:_:{trackValidated}"); } }
        void OnCarPosUpdate(string id, int trackLocation, int trackID, float offset, int speed, bool clockwise){
            if(!trackValidated){ return; }
            TrackCarLocation car = carLocations.Find(car => car.ID == id);
            if(car == null){ car = new TrackCarLocation(id, track.Length); carLocations.Add(car); }
            car.SetOnPosition(trackID, clockwise);
            car.horizontalPosition = offset;
            
            // car.SetLast(trackID, clockwise);
            // car.horizontalPosition = offset;
            // //does this car match the track?
            // if(car.PieceMatches(track[car.trackIndex], 0)){ car.positionTrusted = true; return; }
            // car.positionTrusted = false;
            // //match the car to the track to find the correct index (using Unknown as a wildcard)
            // if(car.AllUnknowns()){ return; } //we got no clue
            // for(int i = 0; i < track.Length - car.GetLastTrackLength(); i++){
            //     bool match = true;
            //     int trackLength = car.GetLastTrackLength();
            //     for (int j = 0; j < trackLength; j++) { // Calculate the reversed index
            //         int reversedIndex = trackLength - 1 - j;
            //         if (!car.PieceMatches(track[i + j], reversedIndex)) { match = false; break; }
            //     }
            //     if(match){
            //         if(i == car.trackIndex){ break; }
            //         Console.WriteLine($"Car {id} index Corrected from {car.trackIndex} to {i}\nCurrentCarpieces:");
            //         Console.WriteLine(car.GetLastTracks());
            //         Console.WriteLine("");
            //         car.trackIndex = i;
            //         break;
            //     }
            // }
        }
        void OnCarTransition(string id, int trackPiece, int oldTrackPiece, float offset, int uphillCounter, int downhillCounter, int leftWheelDistance, int rightWheelDistance, bool crossedStartingLine){
            if(!trackValidated){ return; }
            TrackCarLocation car = carLocations.Find(car => car.ID == id);
            if(car == null){ car = new TrackCarLocation(id, track.Length); carLocations.Add(car); }
            car.horizontalPosition = offset;
            Console.WriteLine($"idx {car.trackIndex} {track[car.trackIndex]}");
            if(crossedStartingLine && false){ //temp testing
                if(track[car.trackIndex].type != TrackPieceType.FinishLine){ //something is off
                    int currentIndex = car.trackIndex, closestFinishLine = -1, closestDistance = 1000; //find the closest finish line to our current Index
                    for(int i = 0; i < track.Length; i++){ //find the closest finishline both forward and backward
                        if(track[i].type == TrackPieceType.FinishLine){
                            int distance = Math.Abs(i - currentIndex);
                            if(distance < closestDistance){ closestDistance = distance; closestFinishLine = i; }
                        }
                    }
                    if(closestFinishLine != -1 && closestFinishLine != currentIndex){ //we found a finish line, and it's not the same as the current index
                        Console.WriteLine($"Car {id} crossed starting line, corrected from {currentIndex} to {closestFinishLine}, difference: {closestDistance}");
                        car.trackIndex = closestFinishLine - 1; //-1 because it gets incremented by the OnTransition function
                    }
                }
            }else{
                //match the cars track memory to see if we can find the correct index
                int memoryLength = car.GetLocalisedTrackLength();
                if(memoryLength > 2){ //we have some track memory, try to match it to the track
                    Console.WriteLine($"{car.MemoryString()}");
                    int matchCount = 0;
                    int matchedIndex = -1;
                    // Find all potential matches
                    for(int trackIndex = 0; trackIndex < track.Length; trackIndex++){
                        bool match = true;
                        string check = "Chk: ";
                        for (int memoryIndex = memoryLength - 1; memoryIndex >= 0; memoryIndex--) { 
                            TrackPiece piece = GetTrackPieceLooped(trackIndex + memoryIndex); // Get the track piece at the current index
                            check += $"[{piece.type} {piece.internalID} {piece.flipped}]";
                            if (!car.PieceMatches(piece, memoryIndex)) { match = false; break; }
                        }
                        if(match){
                            matchCount++;
                            matchedIndex = trackIndex + memoryLength; // Store the matched index
                            Console.WriteLine(check);
                        }
                        
                    }
                    // Report the number of potential matches
                    Console.WriteLine($"Car {id} has {matchCount} potential track position matches");
                    // Only update position if exactly one match is found and it's different from current
                    if(matchCount == 1 && matchedIndex != car.trackIndex){
                        Console.WriteLine($"Car {id} index corrected from {car.trackIndex} to {matchedIndex}");
                        car.trackIndex = matchedIndex;
                    }
                    else if(matchCount > 1){
                        Console.WriteLine($"Car {id} has ambiguous position - multiple matches found. Keeping current position.");
                    }else if(memoryLength > 4 && matchCount == 0){ //if we have a lot of memory and no matches, we might be on the wrong track
                        Program.carSystem.GetCar(id).UTurn(); //request a U-turn
                        Console.WriteLine($"Car {id} has no matches, requesting U-turn");
                        car.ClearTracks(); //clear the track memory, we are lost
                    }
                }
            }
            car.OnTransition(offset, leftWheelDistance, rightWheelDistance);


            int carspeed = 0;
            if(Program.carSystem.GetCar(id) != null){ carspeed = Program.carSystem.GetCar(id).data.speed; }
            Program.socketMan.Notify(EVENT_CAR_TRACKING_UPDATE, 
                new CarLocationData{
                    carID = id,
                    trackIndex = car.trackIndex,
                    speed = carspeed,
                    offset = car.horizontalPosition,
                    positionTrusted = car.positionTrusted,
                }
            );
        }
        void OnCarDelocalised(string id){
            TrackCarLocation car = carLocations.Find(car => car.ID == id);
            if(car != null){
                car.ClearTracks();
                Console.WriteLine($"Car {id} delocalised, clearing track memory");
            }
        }
        TrackPiece GetTrackPieceLooped(int index){
            if(track == null){ return null; }
            if(index < 0){ index = track.Length + index; } //loop backwards
            if(index >= track.Length){ index = index - track.Length; } //loop forwards
            return track[index];
        }
        public string TrackDataAsJson(){ return JsonConvert.SerializeObject(track); }
        public static TrackPiece GetTrackPieceWithFallback(int ID, bool flipped, int Lwheel, int Rwheel){
            if(ID != 0){ 
                TrackPieceType type = PieceFromID(ID); 
                if(type != TrackPieceType.Unknown){ return new TrackPiece(type, ID, flipped); }
            }else{
                TrackPieceType type = (Abs(Lwheel - Rwheel) < 4) ? TrackPieceType.Straight : TrackPieceType.Turn;
                flipped = (type == TrackPieceType.Straight) ? false  : (Lwheel > Rwheel);
                return new TrackPiece(type, 0, flipped); //fallback to straight or turn
            }
            return new TrackPiece(TrackPieceType.Unknown, 0, false); //fallback to unknown
        }
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

        static int Abs(int i) { return i < 0 ? -i : i; }
    }
}
