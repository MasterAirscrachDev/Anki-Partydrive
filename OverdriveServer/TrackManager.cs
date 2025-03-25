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
        }
        public TrackManager(){
            carLocations = new List<TrackCarLocation>();
            Program.messageManager.CarEventLocationCall += OnCarPosUpdate;
            Program.messageManager.CarEventTransitionCall += OnCarTransition;
            Program.messageManager.CarEventFellCall += OnCarFell;
        }
        public void AlertIfTrackIsValid(){ if(trackValidated){ Program.UtilLog($"-3:_:{trackValidated}"); } }
        void OnCarPosUpdate(string id, int trackLocation, int trackID, float offset, int speed, bool clockwise){
            if(!trackValidated){ return; }
            TrackCarLocation car = carLocations.Find(car => car.ID == id);
            if(car == null){ car = new TrackCarLocation(id); carLocations.Add(car); }
            car.SetLast(trackID, clockwise);
            car.horizontalPosition = offset;
            //does this car match the track?
            if(car.PieceMatches(track[car.trackIndex], 0)){ car.positionTrusted = true; return; }
            car.positionTrusted = false;
            //match the car to the track to find the correct index (using Unknown as a wildcard)
            if(car.AllUnknowns()){ return; } //we got no clue
            for(int i = 0; i < track.Length - car.GetLastTrackLength(); i++){
                bool match = true;
                int trackLength = car.GetLastTrackLength();
                for (int j = 0; j < trackLength; j++) { // Calculate the reversed index
                    int reversedIndex = trackLength - 1 - j;
                    if (!car.PieceMatches(track[i + j], reversedIndex)) { match = false; break; }
                }
                if(match){
                    if(i == car.trackIndex){ break; }
                    Console.WriteLine($"Car {id} index Corrected from {car.trackIndex} to {i}\nCurrentCarpieces:");
                    Console.WriteLine(car.GetLastTracks());
                    Console.WriteLine("");
                    car.trackIndex = i;
                    break;
                }
            }
        }
        void OnCarTransition(string id, int trackPiece, int oldTrackPiece, float offset, int uphillCounter, int downhillCounter, int leftWheelDistance, int rightWheelDistance, bool crossedStartingLine){
            if(!trackValidated){ return; }
            TrackCarLocation car = carLocations.Find(car => car.ID == id);
            if(car == null){ car = new TrackCarLocation(id); carLocations.Add(car); }
            car.horizontalPosition = offset;
            car.TrackCrossed(track.Length);
            //Console.WriteLine($"Car {id} is at index {car.trackIndex} {track[car.trackIndex]}");
            if(crossedStartingLine){
                if(track[car.trackIndex].type != TrackPieceType.FinishLine){ //something is off
                    int currentIndex = car.trackIndex, closestFinishLine = -1, closestDistance = 1000; //find the closest finish line to our current Index
                    for(int i = 0; i < track.Length; i++){
                        if(track[i].type == TrackPieceType.FinishLine){
                            int distance = Math.Abs(i - currentIndex);
                            if(distance < closestDistance){ closestDistance = distance; closestFinishLine = i; }
                        }
                    }
                    if(closestFinishLine != -1){
                        Console.WriteLine($"Car {id} crossed starting line, corrected from {currentIndex} to {closestFinishLine}, difference: {closestDistance}");
                        car.trackIndex = closestFinishLine;
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
        void OnCarFell(string id){
            TrackCarLocation car = carLocations.Find(car => car.ID == id);
            if(car != null){
                car.ClearTracks();
                car.horizontalPosition = 0;
                car.positionTrusted = false;
            }
        }
        public string TrackDataAsJson(){ return JsonConvert.SerializeObject(track); }   
    }
}
