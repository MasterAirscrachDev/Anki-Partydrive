namespace OverdriveServer {
    public class Tracks{
        [System.Serializable]
        public class TrackPiece{
            public readonly TrackPieceType type;
            public readonly int internalID;
            public readonly bool flipped;
            public int up, down, elevation;
            public TrackPiece(TrackPieceType type, int id, bool flipped){
                this.type = type;
                this.flipped = flipped;
                internalID = id;
                up = 0; down = 0;
            }
            public bool validated = false;
            public void SetUpDown(int up, int down){ this.up = up; this.down = down; }
            public override bool Equals(object? obj) { // Check for null and compare run-time types.
                if (obj == null || !GetType().Equals(obj.GetType())) { return false; }
                else {
                    TrackPiece p = (TrackPiece)obj;
                    return (type == p.type) && (flipped == p.flipped);
                }
            }
            public static bool operator ==(TrackPiece? a, TrackPiece? b) {
                if (ReferenceEquals(a, b)) { return true; }
                if (a is null || b is null) { return false; }
                return a.Equals(b);
            }
            public static bool operator !=(TrackPiece? a, TrackPiece? b) { return !(a == b); }
            //public override int GetHashCode() { return (type, internalID, flipped).GetHashCode(); }
            public override string ToString() { return $"({type}|id:{internalID}|flipped:{flipped})"; }
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
            public TrackCarLocation(string id){ ID = id; }
            public bool PieceMatches(TrackPiece piece, int index){
                if(lastTracks.Count <= index){ return false; }
                if(lastTracks[index].id == -1){ return true; } //if unknown, we dont care
                if(lastTracks[index].type == piece.type && lastTracks[index].flipped == piece.flipped && lastTracks[index].id == piece.internalID){ return true; }
                return false;
            }
            public bool AllUnknowns() {
                foreach(TypeIDPair pair in lastTracks){ if(pair.id != -1){ return false; } }
                return true;
            }
            public int GetLastTrackLength(){ return lastTracks.Count; }
            public void TrackCrossed(int trackLength) {
                lastTracks.Add(new TypeIDPair());
                if(lastTracks.Count > 6){ lastTracks.RemoveAt(0); }
                trackIndex++;
                if(trackIndex >= trackLength){ trackIndex = 0; }
            }
            public void SetLast(int id, bool flipped) {
                if(lastTracks.Count == 0){ return;}
                TrackPieceType type = TrackScanner.PieceFromID(id);
                lastTracks[lastTracks.Count - 1] = new TypeIDPair(type, id, flipped);
            }
            public string GetLastTracks() {
                string str = "";
                foreach(TypeIDPair pair in lastTracks){ str += $"{pair.type}({pair.id})\n";  }
                return str;
            }
            public void ClearTracks(){ lastTracks.Clear(); }
        }
        class TypeIDPair{
            public TrackPieceType type;
            public int id;
            public bool flipped;
            public TypeIDPair(){
                id = -1;
                type = TrackPieceType.Unknown;
            }
            public TypeIDPair(TrackPieceType type, int id, bool flipped){
                this.type = type;
                this.id = id;
                this.flipped = flipped;
            }
        }
    }
}