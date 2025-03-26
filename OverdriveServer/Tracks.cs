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
            int trackLength = 0, memoryLength = 0, lastTrackID = 0;
            bool lastFlipped = false;
            public int trackIndex;
            public float trackPosition;
            public float horizontalPosition;
            public bool positionTrusted = false;
            List<TrackPiece> lastTracks = new List<TrackPiece>();
            public TrackCarLocation(string id, int trackLength){ 
                ID = id; 
                this.trackLength = trackLength; 
                memoryLength = Math.Max(6, trackLength - 2);
                if(memoryLength > 8){ memoryLength = 8; } //max 8 pieces in memory
            }
            public int GetLocalisedTrackLength(){ return lastTracks.Count; }
            public void SetOnPosition(int ID, bool flipped){ //set the last track piece to the current one
                lastTrackID = ID; lastFlipped = flipped;
            }
            public void OnTransition(float offset, int left, int right) {
                int lID = lastTrackID; bool lFlipped = lastFlipped;
                lastTrackID = 0; lastFlipped = false; //reset the last track piece
                horizontalPosition = offset;
                TrackPiece segment = TrackManager.GetTrackPieceWithFallback(lID, lFlipped, left, right);

                lastTracks.Add(segment);
                if(lastTracks.Count > memoryLength){ lastTracks.RemoveAt(0); } //keep the last 6 (smallest possible number of pieces w)
                trackIndex++;
                if(trackIndex >= trackLength){ trackIndex -= trackLength; } //loop around
            }

            public bool PieceMatches(TrackPiece piece, int index) {
                if (index < 0 || index >= lastTracks.Count) { return false; } // Check for out-of-bounds index
                TrackPiece lastPiece = lastTracks[index];
                if(lastPiece.internalID == 0) { //fallbacks are treated as wildcards
                    if(lastPiece.type == piece.type) { return true; } //if the type is the same, its probably the same piece
                    else{
                        if(lastPiece.type == TrackPieceType.Straight && piece.type == TrackPieceType.CrissCross) { return true; } //crisscross is a straight piece
                    }
                } 
                return (piece.type == lastPiece.type) && (piece.flipped == lastPiece.flipped);
            }
            public TrackPiece GetTrackAt(int index) {
                if (index < 0 || index >= lastTracks.Count) { return null; } // Check for out-of-bounds index
                return lastTracks[index];
            }
            public string MemoryString() {
                string content = $"Mem: ";
                for (int i = lastTracks.Count - 1; i >= 0; i--) {
                    content += $"[{lastTracks[i].type} {lastTracks[i].internalID} {lastTracks[i].flipped}]";
                }
                return content;
            }
            public void ClearTracks(){ lastTracks.Clear(); positionTrusted = false; horizontalPosition = 0; }
        }
    }
}