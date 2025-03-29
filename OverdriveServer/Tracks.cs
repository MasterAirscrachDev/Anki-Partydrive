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
            int trackLength = 0, memoryLength = 0, lastTrackID = 0;
            bool lastFlipped = false;
            public int trackIndex = 1, voidSegments = 0;
            public float trackPosition;
            public float horizontalPosition;
            public bool positionTrusted = false;
            List<TrackPiece> lastTracks = new List<TrackPiece>();
            public TrackCarLocation(int trackLength){ 
                this.trackLength = trackLength; 
                memoryLength = Math.Max(6, trackLength - 2);
                if(memoryLength > 8){ memoryLength = 8; } //max 8 pieces in memory
            }
            public int GetLocalisedTrackLength(){ return lastTracks.Count; }
            public void OnSegment(TrackPiece segment, float offset){
                lastTracks.Add(segment);
                if(lastTracks.Count > memoryLength){ lastTracks.RemoveAt(0); } //keep the last 6 (smallest possible number of pieces w)
                trackIndex++;
                if(trackIndex >= trackLength){ trackIndex -= trackLength; } //loop around
                horizontalPosition = offset;
            }

            public bool PieceMatches(TrackPiece piece, int index, bool update = false) {
                if (index < 0 || index >= lastTracks.Count) { return false; } // Check for out-of-bounds index
                (bool match, TrackPiece? updateTo) = TrackManager.EvaluateMatch(piece, lastTracks[index]);
                if (match) { //if the piece matches, return true
                    if(updateTo != null && update) { lastTracks[index] = updateTo; } //update the piece if needed
                    return true;
                }
                return false; //otherwise, return false
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
            public void SafeClear() { lastTracks.Clear(); } //clear the track memory, but keep position Trusted
            public void ClearTracks(){ lastTracks.Clear(); positionTrusted = false; horizontalPosition = 0; voidSegments = 0; }
        }
    }
}