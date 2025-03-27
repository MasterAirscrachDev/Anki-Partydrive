using Newtonsoft.Json;
using static OverdriveServer.Tracks;
using static OverdriveServer.NetStructures;

namespace OverdriveServer {
    class TrackManager {
        public int totalStarts = 1;
        TrackPiece[]? track;
        bool trackValidated = false;
        int carsAwaitingLineup = 0;
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
        }
        void OnCarTransition(string id, int trackPiece, int oldTrackPiece, float offset, int uphillCounter, int downhillCounter, int leftWheelDistance, int rightWheelDistance, bool crossedStartingLine){
            if(!trackValidated){ return; }
            TrackCarLocation car = carLocations.Find(car => car.ID == id);
            if(car == null){ car = new TrackCarLocation(id, track.Length); carLocations.Add(car); }
            car.horizontalPosition = offset;
            //Console.WriteLine($"idx {car.trackIndex} {track[car.trackIndex]}");
            //match the cars track memory to see if we can find the correct index
            int memoryLength = car.GetLocalisedTrackLength();
            if(memoryLength > 2){ //we have some track memory, try to match it to the track
                //Console.WriteLine($"{car.MemoryString()}");
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
                        //Console.WriteLine(check);
                    }
                    
                }
                // Report the number of potential matches
                //Console.WriteLine($"Car {id} has {matchCount} potential track position matches");
                // Only update position if exactly one match is found and it's different from current
                if(matchCount == 1 && matchedIndex != car.trackIndex){
                    //Console.WriteLine($"Car {id} index corrected from {car.trackIndex} to {matchedIndex}");
                    car.trackIndex = matchedIndex;
                    car.positionTrusted = true; //we are sure about the position now
                    car.voidSegments = 0; //reset the void segments counter
                }
                else if(matchCount > 1){
                    //Console.WriteLine($"Car {id} has ambiguous position - multiple matches found. Keeping current position.");
                    car.voidSegments = 0; //reset the void segments counter
                }else if(memoryLength > 2 && matchCount == 0){ //if we have a lot of memory and no matches, we might be on the wrong track (update this to require a few warnings before we do this)
                    if(car.voidSegments > 4){
                        Car carE = Program.carSystem.GetCar(id);
                        int speed = carE.data.speed;
                        carE.SetCarSpeed(150, 2000); //slow down to 150
                        Program.carSystem.GetCar(id).UTurn(); //request a U-turn
                        //Console.WriteLine($"Car {id} has no matches, requesting U-turn");
                        car.ClearTracks(); //clear the track memory, we are lost
                        //in 2s return to normal speed
                        Task.Delay(2000).ContinueWith(t => {
                            if(Program.carSystem.GetCar(id) != null){
                                Program.carSystem.GetCar(id).SetCarSpeed(speed); //return to normal speed
                                car.ClearTracks(); //clear the track memory
                            }
                        });
                    }else{
                        car.voidSegments++; //increment the void segments counter
                    }
                }
            }
            //end localisation check
            car.OnTransition(offset, leftWheelDistance, rightWheelDistance);
            if(carsAwaitingLineup > 0){ //if we are waiting for lineup, check if we are at the start line
                if(car.positionTrusted){
                    if(car.trackIndex == 1){ //if we are at the start line, stop the car
                        Program.carSystem.GetCar(id).SetCarSpeed(0, 3000); //stop the car
                        Program.Log($"Lineup finished for {id}");
                        carsAwaitingLineup--;
                    }//if we are in the last 2 segments then slow down
                    else if(car.trackIndex >= track.Length - 1){ //if we are in the last 2 segments then slow down
                        Program.carSystem.GetCar(id).SetCarSpeed(150, 500); //slow down to 200
                        Program.Log($"Lineup slowing down for {id}");
                    }
                }
            }

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

        public void RequestLineup(){
            Car[] cars = Program.carSystem.GetCarsOffCharge();
            if(cars.Length == 0){ Program.Log("No cars to lineup"); return; }
            if(track == null || !trackValidated){ Program.Log("No track to lineup on"); return; }
            //set the lane offset for each car based on index
            if(cars.Length > 4){ Program.Log("Too many cars to lineup"); return; } //max 4 cars (for now)
            //68,23,-23,-68 lanes 1-4
            for(int i = 0; i < cars.Length; i++){
                float laneOffset = 0;
                if(i == 0){ laneOffset = 68; } //lane 1
                else if(i == 1){ laneOffset = 23; } //lane 2
                else if(i == 2){ laneOffset = -23; } //lane 3
                else if(i == 3){ laneOffset = -68; } //lane 4
                cars[i].SetCarSpeed(550, 1000); //set the speed to 550 for all cars
                cars[i].SetCarLane(laneOffset, 100, 1000); //set the lane offset for each car
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
