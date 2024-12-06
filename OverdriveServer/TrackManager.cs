using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using static OverdriveServer.Tracks;

namespace OverdriveServer
{
    class TrackManager
    {
        public int totalStarts = 1;
        TrackPiece[]? track;
        bool trackValidated = false;
        List<TrackCarLocation> carLocations;
        public void SetTrack(TrackPiece[] track, bool validated){ this.track = track; trackValidated = validated; }
        public TrackManager(){
            carLocations = new List<TrackCarLocation>();
            Program.messageManager.CarEventLocationCall += OnCarPosUpdate;
            Program.messageManager.CarEventTransitionCall += OnCarTransition;
            Program.messageManager.CarEventFellCall += OnCarFell;
            PositionTicker();
        }
        public void AlertIfTrackIsValid(){
            if(trackValidated){
                Program.UtilLog($"-3:_:{trackValidated}");
            }
        }
        async Task PositionTicker(){
            await Task.Delay(15000); //odds are we havent connected to cars and scanned a track in 15 seconds
            while(true){
                if(track == null){ await Task.Delay(4900); continue; }
                await Task.Delay(100);
                foreach(TrackCarLocation location in carLocations){
                    //Console.WriteLine($"Car {location.ID} is on track piece {location.trackIndex} at {location.horizontalPosition}");
                }
            }
        }
        void OnCarPosUpdate(string id, int trackLocation, int trackID, float offset, int speed, bool clockwise){
            if(!trackValidated){ return; }
            TrackCarLocation car = carLocations.Find(car => car.ID == id);
            if(car == null){
                car = new TrackCarLocation(id); carLocations.Add(car);
            }
            car.SetLast(trackID, clockwise);
            car.horizontalPosition = offset;
            //does this car match the track?
            if(car.PieceMatches(track[car.trackIndex], 0)){
                car.positionTrusted = true; return;
            }
            car.positionTrusted = false;
            //match the car to the track to find the correct index (using Unknown as a wildcard)
            if(car.AllUnknowns()){ return; } //we got no clue
            for(int i = 0; i < track.Length - car.GetLastTrackLength(); i++){
                bool match = true;
                int trackLength = car.GetLastTrackLength();
                for (int j = 0; j < trackLength; j++) {
                    // Calculate the reversed index
                    int reversedIndex = trackLength - 1 - j;
                    if (!car.PieceMatches(track[i + j], reversedIndex)) {
                        match = false;
                        break;
                    }
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
            if(car == null){
                car = new TrackCarLocation(id); carLocations.Add(car);
            }
            car.horizontalPosition = offset;
            car.TrackCrossed(track.Length);
            Console.WriteLine($"Car {id} is at index {car.trackIndex} {track[car.trackIndex]}");
            if(crossedStartingLine){
                if(track[car.trackIndex].type != TrackPieceType.FinishLine){ //something is off
                    int currentIndex = car.trackIndex;
                    //find the closest finish line to our current Index
                    int closestFinishLine = -1;
                    int closestDistance = 1000;
                    for(int i = 0; i < track.Length; i++){
                        if(track[i].type == TrackPieceType.FinishLine){
                            int distance = Math.Abs(i - currentIndex);
                            if(distance < closestDistance){
                                closestDistance = distance;
                                closestFinishLine = i;
                            }
                        }
                    }
                    if(closestFinishLine != -1){
                        Console.WriteLine($"Car {id} crossed starting line, corrected from {currentIndex} to {closestFinishLine}, difference: {closestDistance}");
                        car.trackIndex = closestFinishLine;
                    }
                }
            }
            Program.UtilLog($"-4:{id}:{car.trackIndex}:{car.horizontalPosition}:{car.positionTrusted}");
        }
        void OnCarFell(string id){
            TrackCarLocation car = carLocations.Find(car => car.ID == id);
            if(car != null){
                car.ClearTracks();
                car.horizontalPosition = 0;
                car.positionTrusted = false;
            }
        }

        public string TrackDataAsJson(){
            return JsonConvert.SerializeObject(track);
        }   
    }
    public class Tracks{
        [System.Serializable]
        public class TrackPiece{
            public readonly TrackPieceType type;
            public readonly int internalID;
            public readonly bool flipped;
            public readonly int X, Y;
            public int up, down, elevation;
            public bool validated = false;
            public TrackPiece(TrackPieceType type, int id, bool flipped, int X, int Y){
                this.type = type;
                this.flipped = flipped;
                internalID = id;
                up = 0; down = 0;
                this.X = X; this.Y = Y;
            }
            public void SetUpDown(int up, int down){
                this.up = up; this.down = down;
            }
            public bool IsAt(int x, int y){
                return X == x && Y == y;
            }
            public override bool Equals(object obj) {
                // Check for null and compare run-time types.
                if (obj == null || !GetType().Equals(obj.GetType())) {
                    return false;
                } else {
                    TrackPiece p = (TrackPiece)obj;
                    //return (type == p.type) && (internalID == p.internalID) && (flipped == p.flipped) && (X == p.X) && (Y == p.Y);
                    return (type == p.type) && (internalID == p.internalID) && (flipped == p.flipped);
                }
            }
            public override string ToString()
            {
                return $"({type}|{internalID}|[{X},{Y}])";
            }
        }
        [System.Serializable]
        public enum TrackPieceType{
            Unknown, Straight, Turn, PreFinishLine, FinishLine, FnFSpecial, CrissCross, JumpRamp, JumpLanding
        }
        public class TrackCarLocation{
            public string ID;
            public int trackIndex;
            public float trackPosition;
            public float horizontalPosition;
            public bool positionTrusted = false;
            List<TypeIDPair> lastTracks = new List<TypeIDPair>();
            public bool PieceMatches(TrackPiece piece, int index){
                if(lastTracks.Count <= index){ return false; }
                if(lastTracks[index].id == -1){ return true; } //if unknown, we dont care
                if(lastTracks[index].type == piece.type && lastTracks[index].flipped == piece.flipped && lastTracks[index].id == piece.internalID){ return true; }
                return false;
            }
            public bool AllUnknowns(){
                foreach(TypeIDPair pair in lastTracks){
                    if(pair.id != -1){ return false; }
                }
                return true;
            }
            public TrackCarLocation(string id){
                ID = id;
            }
            public int GetLastTrackLength(){
                return lastTracks.Count;
            }
            public void TrackCrossed(int trackLength){
                lastTracks.Add(new TypeIDPair());
                if(lastTracks.Count > 6){ lastTracks.RemoveAt(0); }
                trackIndex++;
                if(trackIndex >= trackLength){ trackIndex = 0; }
            }
            public void SetLast(int id, bool flipped){
                if(lastTracks.Count == 0){ return;}
                TrackPieceType type = TrackScanner.PeiceFromID(id);
                lastTracks[lastTracks.Count - 1] = new TypeIDPair(type, id, flipped);
            }
            public string GetLastTracks(){
                string str = "";
                foreach(TypeIDPair pair in lastTracks){
                    str += $"{pair.type}({pair.id})\n";
                }
                return str;
            }
            public void ClearTracks(){ lastTracks.Clear(); }
        }
        class TypeIDPair{
            public TrackPieceType type;
            public int id;
            public bool flipped;
            public TypeIDPair(TrackPieceType type, int id, bool flipped){
                this.type = type;
                this.id = id;
                this.flipped = flipped;
            }
            public TypeIDPair(){
                this.id = -1;
                this.type = TrackPieceType.Unknown;
            }
        }
    }
}
