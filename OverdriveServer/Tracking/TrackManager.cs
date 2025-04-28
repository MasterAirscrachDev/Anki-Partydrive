using Newtonsoft.Json;
using static OverdriveServer.Tracks;
using static OverdriveServer.NetStructures;
using static OverdriveServer.NetStructures.UtilityMessages;

namespace OverdriveServer {
    class TrackManager {
        Segment[]? track;
        bool trackValidated = false;
        int carsAwaitingLineup = 0;
        Dictionary<string, TrackCarLocation> carLocations = new Dictionary<string, TrackCarLocation>();
        public void SetTrack(Segment[] track, bool validated){ 
            this.track = track; trackValidated = validated; 
            carLocations.Clear(); //clear the car locations, we need to revalidate them
        }
        public TrackManager(){
            carLocations = new Dictionary<string, TrackCarLocation>();
            Program.messageManager.CarEventSegmentCall += OnSegment; 
            Program.messageManager.CarEventDelocalised += OnCarDelocalised; 
        }
        public void AlertIfTrackIsValid(){ if(trackValidated){ Program.UtilLog($"{MSG_TR_SCAN_UPDATE}:{trackValidated}"); } }
        void OnSegment(string carID, Segment segment, float offset){
            if (!trackValidated){ return; }
            if (!carLocations.TryGetValue(carID, out TrackCarLocation carTracker)) {
                carTracker = new TrackCarLocation(track.Length); // Create a new location for this car
                carLocations[carID] = carTracker; // Add it to the dictionary
            }
            carTracker.horizontalPosition = offset;
            
            TrackingLogic(carTracker, carID, segment, offset); //check if we are in tracking mode
            
            if(carsAwaitingLineup > 0){ LineupLogic(carTracker, carID); } //check if we are in lineup mode
            
            Program.socketMan.Notify(EVENT_CAR_TRACKING_UPDATE, carTracker.GetCarLocationData(carID) ); //send the car location to the client
        }
        void TrackingLogic(TrackCarLocation carTracker, string carID, Segment segment, float offset){
            try{
                //match the cars track memory to see if we can find the correct index
                int memoryLength = carTracker.GetTrackMemoryLength();
                if(memoryLength > 1){ //we have some track memory, try to match it to the track
                    // Find all potential matches ========================================================================================
                    List<int> matchedIndexes = carTracker.GetBestIndexes(track); //get the best indexes for the track
                    if(matchedIndexes.Count == 1){ // If there is only 1 match =====================================================================
                        int trustedMatch = matchedIndexes[0]; //get the best match
                        int testLastIndex = (trustedMatch - 1 + track.Length) % track.Length; //get the last index of the match

                        if(carTracker.lastMatchedIndex == testLastIndex){ //if we have moved +1 index from the last match
                            carTracker.trackIndex = trustedMatch; //update the track index to the best match
                            carTracker.lostcount = 0; //we are no longer lost
                            //Console.WriteLine($"Car {carID} matched to index {carTracker.trackIndex}, memory length: {memoryLength}"); //debugging
                        }else{
                            //Console.WriteLine($"Car {carID} matched to index {trustedMatch}, but not in order, memory length: {memoryLength}"); //debugging
                        }
                        carTracker.trust = CarTrust.Trusted; //we are sure about the position now
                        carTracker.manyOptions = false; //we are sure about the position now
                        carTracker.lastMatchedIndex = trustedMatch; //update the last matched index
                        
                    }else if(matchedIndexes.Count > 1){ //Ambiguous position - multiple matches found ==============================================
                        bool includesCurrent = matchedIndexes.Any(x => x == carTracker.trackIndex); //check if the current index is included in the matches
                        if(includesCurrent){
                            carTracker.trust = CarTrust.Trusted;
                            //Console.WriteLine($"Car {carID} likely index {carTracker.trackIndex}, memory length: {memoryLength}"); //debugging
                        }else if(carTracker.manyOptions){ //if we have many options, we are unsure about the position
                            carTracker.trust = CarTrust.Unsure;
                            //carTracker.trackIndex = matchedIndexes[0]; //update the track index to the best match
                            //Console.WriteLine($"Car {carID} has multiple matches, but we are unsure about the position, memory length: {memoryLength}"); //debugging
                        }else{
                            //list our trackIndex and the matches
                            string matchList = string.Join(", ", matchedIndexes.Select(x => x.ToString()));
                            Program.Log($"Car {carID} has multiple matches: {matchList}, current index: {carTracker.trackIndex}");
                            carTracker.manyOptions = true; //we have many options, we are unsure about the position
                        }
                    }else{ //no matches found ======================================================================================================
                        //Console.WriteLine($"No matches found for {carID}, wasLost: {carTracker.lost}");
                        if(carTracker.lostcount > 2){
                            //Console.WriteLine($"Car {carID} is lost, attempting U-Turn, memory length: {memoryLength}"); //debugging
                            carTracker.lostcount = 0; //we are no longer lost
                            Car carE = Program.carSystem.GetCar(carID);
                            int speed = carE.data.speedMMPS; carE.SetCarSpeed(200,500);
                            carE.UTurn(); //request a U-turn
                            //Console.WriteLine($"Car {carID} has no matches, requesting U-turn");
                            carTracker.ClearTracks(CarTrust.Delocalized); //clear the track memory, we are lost
                            Task.Delay(1500).ContinueWith(t => { //in 2s return to normal speed
                                if(Program.carSystem.GetCar(carID) != null){
                                    Program.carSystem.GetCar(carID).SetCarSpeed(speed); //return to normal speed
                                    carTracker.ClearTracks(CarTrust.Unsure);
                                }
                            });
                        }else{
                            carTracker.ClearTracks(carTracker.lostcount > 0 ? CarTrust.Unsure : CarTrust.Trusted);
                            carTracker.lostcount++; //increment the lost count
                            //Console.WriteLine($"Car {carID} is lost ({carTracker.lostcount}), memory length: {memoryLength}"); //debugging
                        }
                    }
                }
                //end localisation check
                carTracker.OnSegment(segment, offset); //update the car's track position
            }catch(Exception e){
                Program.Log($"Error in Tracking: {e}"); //log the error
            }
        }
        void LineupLogic(TrackCarLocation carTracker, string carID){
            if(carTracker.trust == CarTrust.Trusted){ //if we are somewhat sure about the position
                if(carTracker.trackIndex == 0){ //if we are at the first segment
                    Car carE = Program.carSystem.GetCar(carID);
                    carE.SetCarSpeed(150, 1000); //slow down to 150
                    carE.TriggerStopOnTransition(); //stop the car on transition
                    Task.Delay(850).ContinueWith(t => { //in a bit mark the car as stopped
                        if(Program.carSystem.GetCar(carID) != null){
                            //Program.Log($"Lineup finished for {carID}");
                            carsAwaitingLineup--;
                            Program.UtilLog($"{MSG_LINEUP}:{carID}:{carsAwaitingLineup}");
                        }
                    });
                }//if we are in the last 2 segments then slow down
                else if(carTracker.trackIndex >= track.Length - 1){ //if we are in the last 2 segments then slow down
                    Program.carSystem.GetCar(carID).SetCarSpeed(330, 500); //slow down to 200
                }
            }
        }
        void OnCarDelocalised(string id){
            if(carLocations.ContainsKey(id)){ //we already have a location for this car
                carLocations[id].ClearTracks(CarTrust.Delocalized); //clear the track memory
                //Console.WriteLine($"Car {id} delocalised, clearing track memory");
            }
        }
        public void RequestLineup(){
            Car[] cars = Program.carSystem.GetCarsOnTrack();
            if(cars.Length == 0){ Program.Log("No cars to lineup"); return; }
            if(track == null || !trackValidated){ Program.Log("No track to lineup on"); return; }
            //set the lane offset for each car based on index
            if(cars.Length > 4){ Program.Log("Too many cars to lineup"); return; } //max 4 cars (for now)
            float[] lanes = { 72.25f, 21.25f, -21.25f, -72.25f };
            for(int i = 0; i < cars.Length; i++){
                float laneOffset = lanes[1]; //default lane offset
                if(cars.Length == 2){ laneOffset = (i == 0) ? lanes[1] : lanes[2];
                }else if(cars.Length > 2){ laneOffset = lanes[i]; }
                
                cars[i].SetCarSpeed(550, 1000, true, false); //set the speed to 550 for all cars
                cars[i].SetCarLane(laneOffset, 100); //set the lane offset for each car
            }
            carsAwaitingLineup = cars.Length;
            Program.Log($"Lineup started with {cars.Length} cars");
        }
        public void CancelLineup(){
            Car[] cars = Program.carSystem.GetCarsOnTrack();
            if(cars.Length == 0){  return; }
            for(int i = 0; i < cars.Length; i++){
                cars[i].SetCarSpeed(0, 1000, true, false); //set the speed to 0 for all cars
            }
            carsAwaitingLineup = 0;
            Program.Log($"Lineup cancelled");
        }
        public string TrackDataAsJson(){ return JsonConvert.SerializeObject(track); }
    }
}