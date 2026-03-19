using System;
using System.Linq;
using UnityEngine;
using static OverdriveServer.NetStructures;

public class TrackPathSolver
{
    // Dynamic method to get track width based on current position
    static float GetTrackHalfWidth(TrackCoordinate coord)
    {
        TrackSpline spline = SR.track.GetTrackSpline(coord.idx);
        if (spline == null) return 0; // FailCase
        return spline.GetWidth(coord.progression);
    }
    
    const float CAR_SPACING = 45f;
    public enum AIState
    {
        Speed, //Drive as fast as possible
        Target, //Drive to the target
        Persuit, //Stay close behind the target
        Flee, //Stay as far from other cars as possible
        Defence, //drive in front of the target then slow down
        Side, //drive to the side of the target
    }
    public class PathingInputs
    {
        public int currentTargetSpeed;
        public float currentTargetOffset;
        public TrackCoordinate ourCoord;
        public TrackCoordinate[] carLocations;
        public TrackCoordinate[] hazardLocations; //track-mapped hazards (cones, trails)
        public TrackCoordinate currentTarget; //may be null
        public AIState state;
        public int depth;
    }

    // Candidate lane offsets — 4 natural abreast positions within the ±67.5 track envelope
    static readonly float[] CANDIDATE_LANES = { -67.5f, -22.5f, 22.5f, 67.5f };
    const float HAZARD_HIT_RADIUS = 42.5f; // half of ~85mm impact width
    const float SLOW_SPEED_THRESHOLD = 200; // cars below this are treated as nearly-stopped
    
    public static (int targetSpeed, float targetOffset, bool shouldBoost, string debugString) GetBestPath(PathingInputs inputs)
    {
        // --- Build future-track lookahead ---
        SegmentType[] futureTrack = new SegmentType[inputs.depth + 1];
        for (int i = 0; i < futureTrack.Length; i++)
        { futureTrack[i] = SR.track.GetSegmentType(inputs.ourCoord.idx + i); }

        string log = "";
        for (int i = 0; i < futureTrack.Length; i++)
        { log += futureTrack[i] + " "; }
        log += "\n";

        bool upcomingTurn = futureTrack[0] == SegmentType.Turn || futureTrack[1] == SegmentType.Turn;
        float trackHalfWidth = GetTrackHalfWidth(inputs.ourCoord);

        // --- Determine target speed before lane selection (state gives base speed) ---
        int targetSpeed = 1000;
        bool shouldBoost = false;
        bool stoppedCarAhead = false; // track if any car ahead is slow/stopped

        // =====================================================================
        // LANE SCORING — evaluate each candidate and pick the best
        // =====================================================================
        float[] laneScores = new float[CANDIDATE_LANES.Length];
        for (int l = 0; l < CANDIDATE_LANES.Length; l++) { laneScores[l] = 0f; }

        // --- Car penalties (speed-aware) ---
        foreach (var carLoc in inputs.carLocations)
        {
            float YDistance = inputs.ourCoord.DistanceY(carLoc);
            bool isAhead = carLoc.IsAhead(inputs.ourCoord);

            // Extend detection range for slow/stopped cars ahead
            bool carIsSlow = carLoc.speed < SLOW_SPEED_THRESHOLD;
            float detectionRange = isAhead && carIsSlow ? 3.0f * carLoc.BACK_DISTANCE : 1.5f * carLoc.BACK_DISTANCE;

            if (YDistance > detectionRange) continue; // too far to matter

            // Flag if any slow car is ahead and in our current lane
            if (isAhead && carIsSlow && carLoc.IntersectsOffset(inputs.ourCoord))
            {
                stoppedCarAhead = true;
                SR.track.DrawLineBetweenTrackCoordinates(inputs.ourCoord, carLoc, Color.yellow, 0.01f);
                log += $"Slow/stopped car ahead (spd {carLoc.speed}) at offset {carLoc.offset:F0}\n";
            }

            // Proximity multiplier: closer = worse, slow cars ahead = much worse
            float proximityWeight = Mathf.Lerp(2f, 0.5f, YDistance / detectionRange);
            if (isAhead && carIsSlow) proximityWeight *= 2f; // double penalty for stopped cars ahead

            // Penalise each candidate lane that would collide with this car
            for (int l = 0; l < CANDIDATE_LANES.Length; l++)
            {
                float laneDist = Mathf.Abs(CANDIDATE_LANES[l] - carLoc.offset);
                if (laneDist < CAR_SPACING)
                {
                    // Within collision range — heavy penalty, scaled by proximity
                    laneScores[l] -= (CAR_SPACING - laneDist) * proximityWeight;
                    if (isAhead) { SR.track.DrawLineBetweenTrackCoordinates(inputs.ourCoord, carLoc, Color.red, 0.01f); }
                }
            }
        }
        
        // --- Hazard penalties ---
        if (inputs.hazardLocations != null)
        {
            foreach (var haz in inputs.hazardLocations)
            {
                float YDistance = inputs.ourCoord.DistanceY(haz);
                if (YDistance > 2.0f) continue; // hazards only matter when close-ish

                float proximityWeight = Mathf.Lerp(3f, 0.5f, YDistance / 2.0f); // hazards are very bad up close

                for (int l = 0; l < CANDIDATE_LANES.Length; l++)
                {
                    float laneDist = Mathf.Abs(CANDIDATE_LANES[l] - haz.offset);
                    if (laneDist < HAZARD_HIT_RADIUS)
                    {
                        laneScores[l] -= (HAZARD_HIT_RADIUS - laneDist) * proximityWeight;
                    }
                }
                log += $"Hazard at seg {haz.idx} offset {haz.offset:F0}\n";
            }
        }

        // --- Wall penalties (lanes outside track bounds get hard penalty) ---
        for (int l = 0; l < CANDIDATE_LANES.Length; l++)
        {
            if (CANDIDATE_LANES[l] < -trackHalfWidth || CANDIDATE_LANES[l] > trackHalfWidth)
            {
                laneScores[l] -= 500f; // effectively blocked
            }
            else
            {
                // Soft penalty near the edges
                float edgeDist = trackHalfWidth - Mathf.Abs(CANDIDATE_LANES[l]);
                if (edgeDist < 10f) laneScores[l] -= (10f - edgeDist) * 2f;
            }
        }

        // --- Steering cost (prefer staying near current offset to avoid oscillation) ---
        for (int l = 0; l < CANDIDATE_LANES.Length; l++)
        {
            float steerDist = Mathf.Abs(CANDIDATE_LANES[l] - inputs.currentTargetOffset);
            laneScores[l] -= steerDist * 0.1f; // small preference
        }

        // --- Racing line bonus for upcoming turns ---
        if (futureTrack.Take(3).Any(s => s == SegmentType.Turn))
        {
            int turnIndex = 0;
            for (int i = 0; i < 3 && i < futureTrack.Length; i++)
            {
                if (futureTrack[i] == SegmentType.Turn) { turnIndex = i; break; }
            }
            bool turnReversed = SR.track.GetSegmentReversed(turnIndex + inputs.ourCoord.idx);
            float idealOffset = turnReversed ? trackHalfWidth * 0.6f : -trackHalfWidth * 0.6f;
            for (int l = 0; l < CANDIDATE_LANES.Length; l++)
            {
                float distFromIdeal = Mathf.Abs(CANDIDATE_LANES[l] - idealOffset);
                laneScores[l] += Mathf.Max(0, 30f - distFromIdeal * 0.4f); // bonus for being near the apex
            }
        }
        else
        {
            // Straight — slight centre bonus
            for (int l = 0; l < CANDIDATE_LANES.Length; l++)
            { laneScores[l] += Mathf.Max(0, 15f - Mathf.Abs(CANDIDATE_LANES[l]) * 0.15f); }
        }

        // --- State-specific scoring adjustments ---
        switch (inputs.state)
        {
            case AIState.Persuit:
                if (inputs.currentTarget != null)
                {
                    // Bonus for lanes that let us overtake the target
                    float targetOff = inputs.currentTarget.offset;
                    for (int l = 0; l < CANDIDATE_LANES.Length; l++)
                    {
                        float distFromTarget = Mathf.Abs(CANDIDATE_LANES[l] - targetOff);
                        // Reward lanes one car-width away from target (good overtaking position)
                        if (distFromTarget > CAR_SPACING * 0.8f && distFromTarget < CAR_SPACING * 2f)
                            laneScores[l] += 20f;
                    }
                }
                break;

            case AIState.Defence:
                if (inputs.currentTarget != null && !inputs.currentTarget.IsAhead(inputs.ourCoord))
                {
                    // Reward lanes that block the pursuer
                    float behindOffset = inputs.currentTarget.offset;
                    for (int l = 0; l < CANDIDATE_LANES.Length; l++)
                    {
                        float distFromBehind = Mathf.Abs(CANDIDATE_LANES[l] - behindOffset);
                        laneScores[l] += Mathf.Max(0, 40f - distFromBehind * 0.6f);
                    }
                }
                break;

            case AIState.Flee:
                // Reward lanes far from all nearby cars
                foreach (var carLoc in inputs.carLocations)
                {
                    if (inputs.ourCoord.DistanceY(carLoc) > 2f) continue;
                    for (int l = 0; l < CANDIDATE_LANES.Length; l++)
                    {
                        float dist = Mathf.Abs(CANDIDATE_LANES[l] - carLoc.offset);
                        laneScores[l] += dist * 0.3f; // reward distance from threats
                    }
                }
                break;
        }

        // --- Pick the best lane ---
        int bestIdx = 0;
        for (int l = 1; l < CANDIDATE_LANES.Length; l++)
        {
            if (laneScores[l] > laneScores[bestIdx]) bestIdx = l;
        }
        float targetLane = CANDIDATE_LANES[bestIdx];
        log += $"Lane scores: ";
        for (int l = 0; l < CANDIDATE_LANES.Length; l++)
            log += $"[{CANDIDATE_LANES[l]:F1}]={laneScores[l]:F1} ";
        log += $" -> {targetLane:F1}\n";

        // =====================================================================
        // SPEED SELECTION — state-driven, with hazard/stopped-car awareness
        // =====================================================================
        switch (inputs.state)
        {
            case AIState.Speed:
                targetSpeed = 1000;
                if (!upcomingTurn && !stoppedCarAhead) shouldBoost = true;
                break;

            case AIState.Target:
                targetSpeed = upcomingTurn ? (futureTrack[0] == SegmentType.Turn ? 650 : 850) : 900;
                if (!upcomingTurn && !stoppedCarAhead) shouldBoost = true;
                break;

            case AIState.Persuit:
                if (inputs.currentTarget != null)
                {
                    float distToTarget = inputs.currentTarget.DistanceY(inputs.ourCoord);
                    targetSpeed = distToTarget < 2.5f ? 1000 : 950;
                    if (distToTarget < 2.5f && !upcomingTurn) shouldBoost = true;
                }
                else
                {
                    targetSpeed = 1000;
                    if (!upcomingTurn) shouldBoost = true;
                }
                break;

            case AIState.Flee:
                targetSpeed = upcomingTurn ? 500 : 700;
                break;

            case AIState.Defence:
                targetSpeed = upcomingTurn ? 500 : 650;
                break;

            default:
                targetSpeed = 800;
                break;
        }

        // --- Emergency speed adjustments ---
        // Stopped car directly ahead in our chosen lane
        if (stoppedCarAhead)
        {
            // Check if our chosen lane still collides with the stopped car
            foreach (var carLoc in inputs.carLocations)
            {
                if (carLoc.speed >= SLOW_SPEED_THRESHOLD) continue;
                if (!carLoc.IsAhead(inputs.ourCoord)) continue;
                float laneDist = Mathf.Abs(targetLane - carLoc.offset);
                float yDist = inputs.ourCoord.DistanceY(carLoc);
                if (laneDist < CAR_SPACING && yDist < 1.0f)
                {
                    targetSpeed = Mathf.Min(targetSpeed, 400);
                    shouldBoost = false;
                    log += "Emergency brake for stopped car!\n";
                    break;
                }
            }
        }

        // Turn speed caps
        if (upcomingTurn && inputs.state != AIState.Defence)
        {
            if (targetSpeed > 750) targetSpeed = 750;
            if (futureTrack[0] == SegmentType.Turn && targetSpeed > 600) targetSpeed = 600;
            shouldBoost = false;
        }

        // Clamp lane to valid bounds
        targetLane = Mathf.Clamp(targetLane, -trackHalfWidth, trackHalfWidth);
        return (targetSpeed, targetLane, shouldBoost, log);
    }
    
    //this function is badness
    public static float GetProgress(SegmentType segment, int id, bool reversed, TrackCoordinate car, float deltaTime){
        //pre start = 340mm
        //start = 220mm
        //straight = 560mm
        //turn inside = 280mm
        //turn outside = 640mm
        int distanceMM = 560; //default distance for straight track

        TrackGenerator trackGenerator = SR.track;

        SegmentLength sl = trackGenerator.GetCachedSegmentLengths(id);
        if(sl != null)
        {
            if (sl.isStraight)
            {
                distanceMM = (int)sl.leftSideLength;
            }
            else
            {
                float trackHalfWidth = GetTrackHalfWidth(car);
                float offset = car.offset / trackHalfWidth; //scale offset to -1 to 1 based on actual track width
                if(reversed){ offset = -offset;  } //reverse the offset if the segment is reversed
                distanceMM = (int)Mathf.Lerp(sl.leftSideLength, sl.rightSideLength, offset); //scale distance to 280 to 640
            }
        }
        else
        {
           if(segment == SegmentType.Turn){
                //offset is scaled based on current track width
                float trackHalfWidth = GetTrackHalfWidth(car);
                float offset = car.offset / trackHalfWidth; //scale offset to -1 to 1 based on actual track width
                if(reversed){ offset = -offset;  } //reverse the offset if the segment is reversed
                distanceMM = (int)Mathf.Lerp(280, 640, offset); //scale distance to 280 to 640
            } 
        }
        //distanceMM = Mathf.RoundToInt(distanceMM * 0.95f); //tolerance
        //speed is in mm/s
        return (((float)car.speed / distanceMM) * 1.01f) * deltaTime;
    }
}
