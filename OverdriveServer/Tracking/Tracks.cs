using static OverdriveServer.NetStructures;
namespace OverdriveServer {
    public class Tracks{
        public static readonly float[] Lanes = {72.25f, 63.75f, 55.25f, 46.75f, 38.25f, 29.75f, 21.25f, 12.75f, 4.25f, -4.25f, -12.75f, -21.25f, -29.75f, -38.25f, -46.75f, -55.25f, -63.75f, -72.25f};
        [System.Serializable]
        public class Segment{
            public readonly SegmentType type;
            public readonly int internalID;
            public readonly bool flipped;
            public int up, down;
            public Segment(SegmentType type, int id, bool flipped){
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
                    Segment p = (Segment)obj;
                    return (type == p.type) && (flipped == p.flipped);
                }
            }
            public override int GetHashCode() { return (int)type ^ flipped.GetHashCode(); }
            public static bool operator ==(Segment? a, Segment? b) {
                if (ReferenceEquals(a, b)) { return true; }
                if (a is null || b is null) { return false; }
                return a.Equals(b);
            }
            public static bool operator != (Segment? a, Segment? b) { return !(a == b); }
            public override string ToString() { return $"({type}|id:{internalID}|flipped:{flipped})"; }
        }
        
        public class TrackCarLocation{
            int trackLength, memoryLength = 0;
            public int trackIndex = 1;
            public float horizontalPosition;
            public CarTrust trust = CarTrust.Unsure; //default trust level
            //State values
            public int lostcount = 0; //if the car was lost (not on track)
            public bool manyOptions = false; //if the car has many options (multiple pieces in memory)
            public int lastMatchedIndex = -1; //last matched index

            List<Segment> lastTracks = new List<Segment>();
            public TrackCarLocation(int trackLength){ 
                this.trackLength = trackLength; 
                memoryLength = Math.Max(6, trackLength - 2);
                if(memoryLength > 8){ memoryLength = 8; } //max 8 pieces in memory
            }
            public int GetTrackMemoryLength(){ return lastTracks.Count; }
            public void OnSegment(Segment segment, float offset){
                lastTracks.Insert(0, segment); //insert the new piece at the front of the list
                if(lastTracks.Count > memoryLength){ lastTracks.RemoveAt(lastTracks.Count - 1); } //keep the last 6 (smallest possible number of pieces w)
                trackIndex++;
                if(trackIndex >= trackLength){ trackIndex -= trackLength; } //loop around
                horizontalPosition = offset;
                //Console.WriteLine($"Index: {trackIndex} MemLength: {lastTracks.Count}, Trust: [{trust}], Offset: {horizontalPosition}"); //debugging
            }
            string MemoryString() {
                string content = $"Mem: ";
                foreach (Segment piece in lastTracks) {
                    content += $"[{piece.type} {piece.internalID} {piece.flipped}]";
                }
                return content;
            }
            public void ClearTracks(CarTrust trust){ lastTracks.Clear(); this.trust = trust; } //clear the track memory and trust level
        
            public List<int> GetBestIndexes(Segment[] track){
                List<int> matchedIndexes = new List<int>();
                int bestMemoryCount = 0, memoryLength = lastTracks.Count; // Get the length of the track memory
                for(int trackIndex = 0; trackIndex < trackLength; trackIndex++){
                    bool match = true;
                    int memCount = 0;

                    for (int memoryIndex = 0; memoryIndex < memoryLength; memoryIndex++) { 
                        int idx = (trackIndex - memoryIndex + trackLength) % trackLength; // Simpler mod operation
                        if (!FastEvaluateMatch(track[idx], lastTracks[memoryIndex])) { match = false; break; }
                        memCount++;
                    }
                    if(match){
                        int idx = (trackIndex + 1 + trackLength) % trackLength;
                        if(memCount > bestMemoryCount){ //if this match is better than the previous one store it at the front
                            bestMemoryCount = memCount; // Store the best memory count
                            matchedIndexes.Insert(0, idx); // Store the matched index at the front
                            if(memCount == memoryLength){ break; } //if we have matched all the pieces, break
                        }else{ matchedIndexes.Add(idx); } // Store the matched index
                    }
                }
                return matchedIndexes; // Return the list of matched indexes
            }
            public CarLocationData GetCarLocationData(string carID){
                int speed = Program.carSystem.GetCar(carID).data.speedMMPS;
                return new CarLocationData{
                    carID = carID,
                    trackIndex = this.trackIndex,
                    speedMMPS = speed,
                    offsetMM = this.horizontalPosition,
                    trust = this.trust
                };
            }
        }
        public static (bool, Segment?) EvaluateMatch(Segment A, Segment B) {
            if(A == null || B == null) return (false, null);
            
            bool isMatch = FastEvaluateMatch(A, B);
            
            if(isMatch && A.internalID != 0 && B.internalID == 0) {
                if(B.validated) A.validated = true;
                return (true, A);
            }
            else if(isMatch && A.internalID == 0 && B.internalID != 0) {
                if(A.validated) B.validated = true;
                return (true, B);
            }
            return (isMatch, null);
        }
        public static bool FastEvaluateMatch(Segment A, Segment B){
            // Null check
            if(A == null || B == null) return false;
            // Direct type and flip comparison for non-fallbacks
            if(A.internalID != 0 && B.internalID != 0 || A.internalID == 0 && B.internalID == 0) {
                return (A.type == B.type) && (A.flipped == B.flipped);
            }
            // Handle fallback cases - identify which is the fallback segment
            Segment fallbackSegment = A.internalID == 0 ? A : B;
            Segment trustedSegment = A.internalID == 0 ? B : A;
            // Only proceed if we have a fallback and non-fallback
            if(fallbackSegment.internalID != 0 || trustedSegment.internalID == 0){ return false; } // Invalid case, return false
            // Check for type match
            if(fallbackSegment.type == trustedSegment.type) { return true; }
            // Special case matches
            return fallbackSegment.type == SegmentType.Straight && (trustedSegment.type == SegmentType.CrissCross || trustedSegment.type == SegmentType.FnFSpecial);
        }
        public static SegmentType SegmentTypeFromID(int id) {
            if(id == 17 || id == 18 || id == 20 || id == 23 || id == 24 || id == 27){ return SegmentType.Turn; }
            else if(id == 36 || id == 39 || id == 40 || id == 48 || id == 51){ return SegmentType.Straight; }
            else if(id == 57 || id == 53 || id == 54){ return SegmentType.FnFSpecial; }
            else if(id == 34){ return SegmentType.PreFinishLine; }
            else if(id == 33){ return SegmentType.FinishLine; }
            else if(id == 10){ return SegmentType.CrissCross; } 
            else if(id == 58 || id == 43){ return SegmentType.JumpRamp; }
            else if(id == 63 || id == 46){ return SegmentType.JumpLanding; }
            else{ return SegmentType.Unknown; }
        }
    }
}