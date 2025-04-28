using static OverdriveServer.NetStructures;

namespace OverdriveServer.Tracking
{
    class Location
    {
        public float CorrectOffsetFast(int segmentID, int locationID, float offset, bool reversed){
            if(segmentID == 10 || locationID < 0){ return offset; } //discard bad offsets (and crisscross bc its unreliable)
            int idx = -1;
            SegmentType type = Tracks.SegmentTypeFromID(segmentID);
            switch(type) {
                case SegmentType.Straight: case SegmentType.JumpLanding: idx = locationID / 3; break; // Straight segments has 3 locations per lane
                case SegmentType.Turn: // Binary search approach is faster than long if-else chains
                    if (locationID <= 19){
                        if(locationID <= 9){ if (locationID <= 3){idx = locationID <= 1 ? 0 : 1;} else{idx = locationID <= 5 ? 2 : locationID <= 7 ? 3 : 4;} } 
                        else{ if(locationID <= 13){idx = locationID <= 11 ? 5 : 6;} else{idx = locationID <= 15 ? 7 : locationID <= 17 ? 8 : 9;} }
                    } else{
                        if(locationID <= 28){ idx = locationID <= 22 ? 10 : locationID <= 25 ? 11 : 12; } 
                        else{ if (locationID <= 34){idx = locationID <= 31 ? 13 : 14;} else{idx = locationID <= 37 ? 15 : -1;} }
                    }
                    break;
                case SegmentType.FnFSpecial: // Similar binary search approach for FnFSpecial
                    if (locationID <= 20){
                        if(locationID <= 9){ if(locationID <= 3){idx = locationID <= 1 ? 0 : 1;} else{idx = locationID <= 5 ? 2 : locationID <= 7 ? 3 : 4;} } 
                        else{if(locationID <= 15){idx = locationID <= 11 ? 5 : locationID <= 13 ? 6 : 7;} else{idx = locationID <= 17 ? 8 : 9;} }
                    } else {
                        if (locationID <= 29) { idx = locationID <= 23 ? 10 : locationID <= 26 ? 11 : 12; } 
                        else{if(locationID <= 35){idx = locationID <= 32 ? 13 : 14; } else{idx = locationID <= 38 ? 15 : -1;} }
                    }
                    break;
                case SegmentType.PreFinishLine: case SegmentType.JumpRamp: idx = locationID >> 1; break; // 2 locations per lane
                case SegmentType.FinishLine: idx = locationID; break;
            }
            // Apply lane offset if valid index was found (with bounds check)
            if (idx >= 0 && idx < Tracks.Lanes.Length) { return reversed ? Tracks.Lanes[idx] : -Tracks.Lanes[idx]; }
            return offset;
        }
    }
}
