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
            public override int GetHashCode() { return (int)type ^ flipped.GetHashCode(); }
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
            int maxTrackSegments = 6;
            public int trackIndex;
            public float trackPosition;
            public float horizontalPosition;
            public bool positionTrusted = false;
            List<TrackPieceLocalised> lastTracks = new List<TrackPieceLocalised>();
            public TrackCarLocation(string id, int maxTrackSegments){ ID = id; this.maxTrackSegments = maxTrackSegments; }
            public int GetLocalisedTrackLength(){ return lastTracks.Count; }
            public void AddTrack(TrackPieceLocalised piece){ 
                lastTracks.Add(piece);
                if(lastTracks.Count > 6){ lastTracks.RemoveAt(0); } //keep the last 6 (smallest possible number of pieces w)
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
        public class TrackPieceLocalised{
            public TrackPiece piece;
            public bool clockwise;
            public TrackPieceLocalised(TrackPiece piece, bool clockwise){
                this.piece = piece;
                this.clockwise = clockwise;
            }
        }
    }
}