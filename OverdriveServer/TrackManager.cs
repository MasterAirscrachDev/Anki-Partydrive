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
        List<TrackCarLocation> carLocations;
        public void SetTrack(TrackPiece[] track){ this.track = track; }
        public TrackManager(){
            carLocations = new List<TrackCarLocation>();
            Program.messageManager.CarEventLocationCall += OnCarPosUpdate;
            Program.messageManager.CarEventTransitionCall += OnCarTransition;
            PositionTicker();
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
            if(track == null){ return; }
            TrackCarLocation car = carLocations.Find(car => car.ID == id);
            if(car == null){
                car = new TrackCarLocation(id); carLocations.Add(car);
            }
            car.SetLast(trackID, clockwise);
            car.horizontalPosition = offset;
            //match the car to the track to find the correct index (using Unknown as a wildcard)
            if(car.AllUnknowns()){ return; }
            for(int i = 0; i < track.Length - car.GetLastTrackLength(); i++){
                bool match = true;
                for(int j = 0; j < car.GetLastTrackLength(); j++){
                    if(!car.PieceMatches(track[i + j], j)){ match = false; break; }
                }
                if(match){
                    car.trackIndex = i;
                    break;
                }
            }
        }
        void OnCarTransition(string id, int trackPiece, int oldTrackPiece, float offset, int uphillCounter, int downhillCounter, int leftWheelDistance, int rightWheelDistance, bool crossedStartingLine){
            if(track == null){ return; }
            TrackCarLocation car = carLocations.Find(car => car.ID == id);
            if(car == null){
                car = new TrackCarLocation(id); carLocations.Add(car);
            }
            car.horizontalPosition = offset;
            car.TrackCrossed(track.Length);
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
                    return (type == p.type) && (internalID == p.internalID) && (flipped == p.flipped) && (X == p.X) && (Y == p.Y);
                }
            }
        }
        [System.Serializable]
        public enum TrackPieceType{
            Unknown, Straight, Turn, PreFinishLine, FinishLine, FnFSpecial, CrissCross, Jump
        }
        public class TrackCarLocation{
            public string ID;
            public int trackIndex;
            public float trackPosition;
            public float horizontalPosition;
            List<TypeIDPair> lastTracks = new List<TypeIDPair>();
            public bool PieceMatches(TrackPiece piece, int index){
                if(lastTracks[index].id == -1){ return true; }
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
