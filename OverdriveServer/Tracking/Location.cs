using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static OverdriveServer.NetStructures;

namespace OverdriveServer.Tracking
{
    class Location
    {
        public Location(){
        }

        public float CorrectOffsetFast(int segmentID, int locationID, float offset, bool reversed){
            if(segmentID == 10 || locationID < 0){ return offset; } //discard bad offsets (and crisscross bc its unreliable)
            int idx = -1;
            SegmentType type = Tracks.SegmentTypeFromID(segmentID);
            switch(type) {
                case SegmentType.Unknown:
                    return offset; // Unknown segment, return original offset (currently Drive mats)
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
                case SegmentType.Bottleneck:
                    if(locationID == 82 || locationID == 84 || locationID == 89 || locationID == 91){ //normal straights
                        if(locationID == 82){ idx = locationID / 2; } // 2 length
                        else if(locationID == 91){ idx = locationID / 3; } // 3 length
                        else{ idx = locationID; } // 1 length
                    } 

                    break;
            }
            // Apply lane offset if valid index was found (with bounds check)
            // Add offset to recenter the 0-16 lookup range to the center of our expanded lane array
            int laneOffset = 6; // Offset to center the original 0-16 range in our 28-lane array
            if (idx >= 0 && idx <= 15) { // Original expected range 0-16
                int adjustedIdx = idx + laneOffset;
                if (adjustedIdx >= 0 && adjustedIdx < Tracks.Lanes.Length) {
                    return reversed ? Tracks.Lanes[adjustedIdx] : -Tracks.Lanes[adjustedIdx];
                }
            }
            return offset;
        }
    }
}
