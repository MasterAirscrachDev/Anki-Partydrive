using System.Linq;
using UnityEngine;
using static OverdriveServer.NetStructures;

public class TrackPathSolver
{
    const float OVERDRIVE_OUTER_LANE = 72.25f; // Outer offset for overdrive
    const float CAR_SPACING = 45f;
    public enum AIState
    {
        Speed, //Drive as fast as possible
        Target, //Drive to the target
        Persuit, //Stay close behind the target
        Defence, //Stay as far from other cars as possible
        Block, //drive in front of the target then slow down
    }
    public class PathingInputs
    {
        public int currentTargetSpeed;
        public float currentTargetOffset;
        public TrackCoordinate ourCoord;
        public TrackCoordinate[] carLocations;
        public TrackCoordinate currentTarget; //may be null
        public AIState state;
        public int depth;
    }

    public static (int targetSpeed, float targetOffset, bool shouldBoost, string debugString) GetBestPath(PathingInputs inputs)
    {
        SegmentType[] futureTrack = new SegmentType[inputs.depth + 1]; //depth + 1 because we want to include the current segment
        for (int i = 0; i < futureTrack.Length; i++)
        { futureTrack[i] = TrackGenerator.track.GetSegmentType(inputs.ourCoord.idx + i); }

        string log = "";
        for (int i = 0; i < futureTrack.Length; i++)
        { log += futureTrack[i] + " "; }
        // Values to use in AI logic
        bool blockedAhead = false;
        bool blockedLeft = false;
        bool blockedRight = false;
        bool carClose = false;

        int targetSpeed = inputs.currentTargetSpeed;
        float targetLane = inputs.currentTargetOffset;
        bool shouldBoost = false;


        foreach (var carLoc in inputs.carLocations)
        {
            float YDistance = inputs.ourCoord.DistanceY(carLoc); //calculate the Y distance between us and the car
            float XDistance = inputs.ourCoord.DistanceX(carLoc); //calculate the X distance between us and the car
            bool carIsInOurLane = carLoc.IntersectsOffset(inputs.ourCoord); //check if the car other car occupies any part of our X space
            bool weAreAheadOf = carLoc.IsAhead(inputs.ourCoord); //check if we are ahead of the car 

            // Check if the car is ahead of us and in our lane
            if (YDistance < (1.5f * carLoc.BACK_DISTANCE))
            { //dont worry about cars that are far away
                if (carIsInOurLane && weAreAheadOf)
                { //if the car is in our lane and we are ahead of it, set blocked ahead to true
                    blockedAhead = true;
                    TrackGenerator.track.DrawLineBetweenTrackCoordinates(inputs.ourCoord, carLoc, Color.red, 0.01f);
                    log += $"Blocked Ahead by car at offset {carLoc.offset}\n";
                }
                else if (XDistance < (carLoc.SIDE_DISTANCE + inputs.ourCoord.SIDE_DISTANCE))
                { //if the car is in our lane and we are behind it, set blocked left or right to true depending on the offset of the car
                    if (carLoc.offset > inputs.ourCoord.offset)
                    {
                        blockedRight = true;
                        log += $"Blocked Right by car at offset {carLoc.offset}\n";
                    }
                    else
                    {
                        blockedLeft = true;
                        log += $"Blocked Left by car at offset {carLoc.offset}\n";
                    }
                }
                carClose = true; //set car close to true if the car is in our lane and we are ahead of it
            }
        }
        if (!blockedLeft && targetLane <= -OVERDRIVE_OUTER_LANE)
        {
            blockedLeft = true;
            log += "Blocked Left by wall\n";
        } //if we are close to the left wall, set blocked left to true
        if (!blockedRight && targetLane >= OVERDRIVE_OUTER_LANE)
        {
            blockedRight = true;
            log += "Blocked Right by wall\n";
        } //if we are close to the right wall, set blocked right to true
        log += carClose ? "Car Close\n" : "No Car Close\n"; //add to the log if a car is close or not
        // Log blocked status
        if (blockedAhead) log += "Blocked Ahead!\n";



        bool upcomingTurn = futureTrack[0] == SegmentType.Turn || futureTrack[1] == SegmentType.Turn;

        // Implement state-specific behavior
        switch (inputs.state)
        {
            case AIState.Speed:
                // Focus on maximum speed
                targetSpeed = 1000;

                if (blockedAhead)
                {
                    // Overtaking logic - try to pass blocked cars
                    targetSpeed = 800;
                    log += "Slowing down due to obstacle ahead\n";

                    // Try to overtake - left by default, right if left is blocked
                    if (!blockedLeft)
                    {
                        targetLane = inputs.currentTargetOffset - CAR_SPACING * 2;
                        log += "Overtaking to the left\n";
                    }
                    else if (!blockedRight)
                    {
                        targetLane = inputs.currentTargetOffset + CAR_SPACING * 2;
                        log += "Overtaking to the right\n";
                    }
                    else
                    {
                        targetLane = inputs.currentTargetOffset;
                        targetSpeed = 600; // Significant speed reduction if both sides blocked
                        log += "Can't overtake - both sides blocked\n";
                    }
                }
                else
                {
                    // No car blocking us - position for optimal racing line
                    if (futureTrack.Take(3).Any(s => s == SegmentType.Turn))
                    {
                        // There's a turn coming up - position for it
                        int turnIndex = 0;
                        for (int i = 0; i < 3 && i < futureTrack.Length; i++)
                        {
                            if (futureTrack[i] == SegmentType.Turn)
                            {
                                turnIndex = i;
                                break;
                            }
                        }

                        bool turnReversed = TrackGenerator.track.GetSegmentReversed(turnIndex + inputs.ourCoord.idx);
                        // Inside of the turn (negative offset for left turn, positive for right turn)
                        targetLane = turnReversed ? OVERDRIVE_OUTER_LANE : -OVERDRIVE_OUTER_LANE;
                        log += $"Positioning for upcoming turn at {targetLane}\n";
                    }
                    else
                    {
                        // No turn coming, stay toward center
                        targetLane = 4f; // Center of track
                        shouldBoost = true;
                        log += "Boosting!\n";
                    }
                }
                break;
            case AIState.Target:
                // Find optimal path through track
                if (upcomingTurn)
                {
                    bool onTurnNow = futureTrack[0] == SegmentType.Turn;
                    int turnIndex = onTurnNow ? 0 : 1;
                    bool turnReversed = TrackGenerator.track.GetSegmentReversed(turnIndex + inputs.ourCoord.idx);

                    // Take ideal racing line but adjust if blocked
                    float idealOffset = turnReversed ? 38.25f : -38.25f;
                    targetLane = idealOffset;

                    // If ideal racing line is blocked, adjust
                    if ((idealOffset < 0 && blockedLeft) || (idealOffset > 0 && blockedRight))
                    {
                        targetLane = -idealOffset * 0.5f; // Use half of the opposite side
                        log += $"Adjusting racing line to avoid obstacle: {targetLane}\n";
                    }

                    targetSpeed = 600 + (onTurnNow ? 0 : 150);
                    log += $"Target racing line: {targetLane}\n";
                }
                else
                {
                    targetLane = 4f;
                    targetSpeed = 850;
                }
                break;

            case AIState.Persuit:
                // Stay close behind target and look for overtaking opportunities
                if (inputs.currentTarget != null)
                {
                    // Calculate optimal overtaking position
                    float overtakingOffset;

                    // Decide which side to overtake based on track position and available space
                    bool leftClear = !blockedLeft;
                    bool rightClear = !blockedRight;

                    if (inputs.currentTarget.offset > 0)
                    {
                        // If car ahead is on right side, prefer overtaking on left if clear
                        if (leftClear)
                        {
                            overtakingOffset = inputs.currentTarget.offset - CAR_SPACING;
                            log += "Planning left overtake\n";
                        }
                        else if (rightClear)
                        {
                            // If left blocked but right clear, try wide right overtake
                            overtakingOffset = inputs.currentTarget.offset + CAR_SPACING;
                            log += "Planning wide right overtake (left blocked)\n";
                        }
                        else
                        {
                            // Both sides blocked, maintain distance
                            overtakingOffset = inputs.currentTarget.offset;
                            targetSpeed = 600; // Slow down
                            log += "Both sides blocked, maintaining position\n";
                        }
                    }
                    else
                    {
                        // If car ahead is on left side, prefer overtaking on right if clear
                        if (rightClear)
                        {
                            overtakingOffset = inputs.currentTarget.offset + CAR_SPACING;
                            log += "Planning right overtake\n";
                        }
                        else if (leftClear)
                        {
                            // If right blocked but left clear, try wide left overtake
                            overtakingOffset = inputs.currentTarget.offset - CAR_SPACING;
                            log += "Planning wide left overtake (right blocked)\n";
                        }
                        else
                        {
                            // Both sides blocked, maintain distance
                            overtakingOffset = inputs.currentTarget.offset;
                            targetSpeed = 600; // Slow down
                            log += "Both sides blocked, maintaining position\n";
                        }
                    }

                    // Clamp the overtaking position to valid track bounds
                    overtakingOffset = Mathf.Clamp(overtakingOffset, -72f, 72f);

                    // If we're close enough and path isn't blocked, attempt overtaking
                    if (inputs.currentTarget.DistanceY(inputs.ourCoord) < 2.0f && !blockedAhead)
                    {
                        targetLane = overtakingOffset;
                        targetSpeed = 1000; // Maximum speed for overtaking
                        if (!upcomingTurn) { shouldBoost = true; } // Boost if not in a turn
                        log += $"Overtaking maneuver at offset {targetLane}\n";
                    }
                    else
                    {
                        // Not close enough to overtake or path blocked, just follow for now
                        targetLane = inputs.currentTarget.offset;
                        targetSpeed = blockedAhead ? 700 : 900; // Slow down if blocked
                        log += $"Following at distance {inputs.currentTarget.DistanceY(inputs.ourCoord)}\n";
                    }
                }
                else
                {
                    // No car ahead, revert to speed mode
                    //state = AIState.Speed;
                }
                break;

            case AIState.Defence:
                // Find safest path away from other cars using CAR_SPACING
                if (carClose || blockedAhead)
                {
                    float opposingOffset = 0f;

                    // Find the most immediate threat
                    if (carClose)
                    {
                        for (int i = 0; i < inputs.carLocations.Length; i++)
                        {
                            if (inputs.carLocations[i].idx == inputs.ourCoord.idx)
                            {
                                opposingOffset = inputs.carLocations[i].offset;
                                break;
                            }
                        }
                    }
                    else if (blockedAhead && inputs.currentTarget != null)
                    {
                        opposingOffset = inputs.currentTarget.offset;
                    }

                    // Choose the safest direction based on blocked status
                    if (opposingOffset > 0)
                    {
                        // Threat is on the right, try to move left
                        if (!blockedLeft)
                        {
                            targetLane = Mathf.Max(-72f, opposingOffset - CAR_SPACING);
                            log += $"Defensive move left {targetLane}\n";
                        }
                        else if (!blockedRight)
                        {
                            // Left is blocked, try moving right instead (wide turn)
                            targetLane = Mathf.Min(72f, opposingOffset + CAR_SPACING);
                            log += $"Defensive move right (wide) {targetLane}\n";
                        }
                        else
                        {
                            // Both sides blocked, slow down drastically
                            targetSpeed = 300;
                            log += "All directions blocked, emergency slow down!\n";
                        }
                    }
                    else
                    {
                        // Threat is on the left, try to move right
                        if (!blockedRight)
                        {
                            targetLane = Mathf.Min(72f, opposingOffset + CAR_SPACING);
                            log += $"Defensive move right {targetLane}\n";
                        }
                        else if (!blockedLeft)
                        {
                            // Right is blocked, try moving left instead (wide turn)
                            targetLane = Mathf.Max(-72f, opposingOffset - CAR_SPACING);
                            log += $"Defensive move left (wide) {targetLane}\n";
                        }
                        else
                        {
                            // Both sides blocked, slow down drastically
                            targetSpeed = 300;
                            log += "All directions blocked, emergency slow down!\n";
                        }
                    }

                    // Be cautious with speed in defensive mode
                    targetSpeed = Mathf.Min(targetSpeed, upcomingTurn ? 500 : 700);
                }
                else
                {
                    // Revert to target mode if no immediate threats
                    //state = AIState.Target;
                }
                break;

            case AIState.Block:
                // Block overtaking attempts by staying in the same lane but offsetting slightly
                // Note: This is the only mode where we intentionally don't avoid other cars
                if (inputs.currentTarget != null && !inputs.currentTarget.IsAhead(inputs.ourCoord))
                {
                    // Check if car behind is attempting to overtake
                    float behindOffset = inputs.currentTarget.offset;
                    bool possibleOvertakeAttempt = Mathf.Abs(behindOffset - inputs.ourCoord.offset) > (CAR_SPACING * 0.5f);

                    if (possibleOvertakeAttempt)
                    {
                        // Move in the direction they're trying to overtake
                        targetLane = behindOffset;
                        log += $"Blocking overtake attempt at {targetLane}\n";
                    }
                    else
                    {
                        // Just stay in their path
                        targetLane = behindOffset;
                        log += $"Blocking path at {targetLane}\n";
                    }

                    // Moderate speed for blocking
                    targetSpeed = upcomingTurn ? 450 : 550;
                }
                else
                {
                    // If no suitable car to block, revert to speed mode
                    //state = AIState.Speed;
                }
                break;
        }

        // Apply common avoidance logic for all states except Block
        // This provides a final safety check against collisions
        if (inputs.state != AIState.Block)
        {
            // Emergency avoidance if we're about to hit something head-on
            if (blockedAhead && targetSpeed > 600)
            {
                targetSpeed = 600;
                log += "Emergency speed reduction!\n";
            }

            // Final adjustment to avoid blocked sides in any state
            if (targetLane < inputs.currentTargetOffset && blockedLeft)
            {
                targetLane = inputs.currentTargetOffset;
            }
            else if (targetLane > inputs.currentTargetOffset && blockedRight)
            {
                targetLane = inputs.currentTargetOffset;
            }
        }
        // Apply turn-specific logic regardless of state
        if (upcomingTurn && inputs.state != AIState.Block)
        {
            bool onTurnNow = futureTrack[0] == SegmentType.Turn;

            // Always be more cautious in turns
            if (targetSpeed > 750) targetSpeed = 750;
            // If in turn currently, reduce speed further
            if (onTurnNow && targetSpeed > 600) targetSpeed = 600;
            // Further reduce speed in turns if obstacles present
            if ((blockedLeft || blockedRight) && targetSpeed > 500)
            {
                targetSpeed = 500;
                log += "Reducing speed for turn with obstacles\n";
            }

            // Ensure we're not boosting in turns
            shouldBoost = false;
        }
        targetLane = Mathf.Clamp(targetLane, -OVERDRIVE_OUTER_LANE, OVERDRIVE_OUTER_LANE); // Clamp target lane to valid track bounds
        return (targetSpeed, targetLane, shouldBoost, log);
    }
    
    public static float GetProgress(SegmentType segment, bool reversed, TrackCoordinate car, float deltaTime){
        //pre start = 340mm
        //start = 220mm
        //straight = 560mm
        //turn inside = 280mm
        //turn outside = 640mm
        int distanceMM = 560; //default distance for straight track
        if(segment == SegmentType.Turn){
            //offset is between -72.25 and 72.25, so we can use this to determine the distance
            float offset = car.offset / 72.25f; //scale offset to -1 to 1
            if(reversed){ offset = -offset;  } //reverse the offset if the segment is reversed
            distanceMM = (int)Mathf.Lerp(280, 640, offset); //scale distance to 280 to 640
        }
        distanceMM = Mathf.RoundToInt(distanceMM * 1.1f); //tolerance
        //speed is in mm/s  
        return ((float)car.speed / distanceMM) * deltaTime;
    }
}
