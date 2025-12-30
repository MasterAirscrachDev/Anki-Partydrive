using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using static OverdriveServer.NetStructures;
public class TrackGenerator : MonoBehaviour
{
    [SerializeField] TrackCamera trackCamera;
    [SerializeField] Material validConfirmedMat;
    [SerializeField] PostProcessProfile post;
    [SerializeField] Segment[] segments;
    [SerializeField] GameObject[] trackPrefabs, scannningPrefabs;
    [SerializeField] List<GameObject> trackPieces;
    public bool hasTrack = false;
    int lastSegmentCount = 0;
    public static TrackGenerator track;
    Dictionary<int, SegmentLength> segmentLengthCache = new Dictionary<int, SegmentLength>();
    bool positionCameraOnLoadSet = false; //flag to ensure we correctly position the camera if a track is loaded before the first frame

    [ContextMenu("Generate Track From Segments")]
    public void TEST_GenerateTrackFromSegments()
    {
        if (segments == null || segments.Length == 0) { return; } //if there are no segments, do nothing  
        //set all segments to validated
        for (int i = 0; i < segments.Length; i++)
        {
            segments[i].validated = true;
        }
        //generate the track
        Generate(segments, true);
        Debug.Log($"Track validated with {segments.Length} segments, {trackPieces.Count} track pieces");
    }

    void Awake()
    {
        if (track == null) { track = this; } else { DestroyImmediate(this); }
    }
    int LoopIndex(int index)
    {
        if(segments == null || segments.Length == 0) { return 0; } //if there are no segments, return 0
        if (index < 0) { return segments.Length + index; }
        if (index >= segments.Length) { return index - segments.Length; }
        return index;
    }

    public TrackSpline GetTrackSpline(int index)
    {
        if (index >= trackPieces.Count) { index -= trackPieces.Count; }
        if (trackPieces[index] == null) { return null; }
        return trackPieces[index].GetComponent<TrackSpline>();
    }
    public SegmentType GetSegmentType(int index) { index = LoopIndex(index); return segments[index].type; }
    public bool GetSegmentReversed(int index) { index = LoopIndex(index); return segments[index].flipped; }
    public int GetSegmentID(int index) { index = LoopIndex(index); return segments[index].internalID; }
    public Segment[] GetTrackPieces()
    {
        return segments;
    }
    public int GetTrackLength()
    {
        return segments.Length;
    }

    public void Generate(Segment[] segments, bool validated)
    {
        hasTrack = false;
        this.segments = segments;
        try
        {
            if (validated)
            {
                UIManager.active.SetScanningStatusText("");
                StartCoroutine(OnFinalGenerate());
                PositionTrackCamera();
                return;
            }
            if (segments == null || segments.Length == 0) { return; } //if there are no segments, do nothing  
            UIManager.active.SetScanningStatusText(segments[0].validated ? "Double Checking..." : "Scanning...");
            GenerateTrackObjects(lastSegmentCount != segments.Length, false);
            lastSegmentCount = segments.Length;
            hasTrack = validated;
            if (validated)
            {
                Debug.Log($"Track validated with {segments.Length} segments, {trackPieces.Count} track pieces");
                OnTrackValidated?.Invoke(segments);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error generating track: " + e);
            return;
        }
        PositionTrackCamera();
    }
    void PositionTrackCamera(bool autoSwitchCamera = true){
        Debug.Log("Positioning track camera based on generated track pieces");
        //calculate the center and size of the track
        Vector3 center = Vector3.zero;
        for (int i = 0; i < trackPieces.Count; i++)
        {
            if (trackPieces[i] == null) { continue; }
            center += trackPieces[i].transform.position;
        }
        center /= trackPieces.Count;
        Vector2 size = new Vector2(0, 0);
        for (int i = 0; i < trackPieces.Count; i++)
        {
            if (trackPieces[i] == null) { continue; }
            Vector2 pos = new Vector2(trackPieces[i].transform.position.x, trackPieces[i].transform.position.z);
            if (pos.x > size.x) { size.x = pos.x; }
            if (pos.y > size.y) { size.y = pos.y; }
        }
        size.x -= center.x; size.y -= center.y;
        size.x *= 2; size.y *= 2;
        if(autoSwitchCamera){
            UIManager.active.SwitchToTrackCamera(true);
        }
        trackCamera.TrackUpdated(center, size);
        int specialTrack = 0;
        if(segments.Length > 0){
            if(segments[0].type == SegmentType.Oval){ specialTrack = 1; }
            if(segments[0].type == SegmentType.Bottleneck){ specialTrack = 2; }
            if(segments[0].type == SegmentType.Crossroads){ specialTrack = 3; }
            //???
        }
        if(specialTrack > 0)
        {
            Debug.Log("Positioning camera for special track type " + specialTrack);
            Vector3 fixedCenter = new Vector3(0.57f,0f,3.024f);
            Vector2 fixedSize = new Vector2(4.52f,2.75f);
            float overrideRotation = 189f;

            trackCamera.TrackUpdated(fixedCenter, fixedSize, overrideRotation);
        }
        else
        {
            Debug.Log("Positioning camera for normal track");
        }
    }
    IEnumerator OnFinalGenerate()
    {
        Bloom b = post.GetSetting<Bloom>();
        //over 0.5s spike bloom intensity to 300
        float time = 0;
        float duration = 0.5f;
        float start = b.intensity.value;
        while (time < duration)
        {
            time += Time.deltaTime;
            b.intensity.value = Mathf.Lerp(start, 200, time / duration);
            yield return new WaitForEndOfFrame();
        }
        GenerateTrackObjects(false, true);
        time = 0;
        //over 0.5s reduce bloom intensity to 1
        start = b.intensity.value;
        while (time < duration)
        {
            time += Time.deltaTime;
            b.intensity.value = Mathf.Lerp(start, 1, time / duration);
            yield return new WaitForEndOfFrame();
        }
        OnTrackValidated?.Invoke(segments);
        hasTrack = true;
        PreCacheSegmentLengths();
        lastSegmentCount = segments.Length;
    }

    void GenerateTrackObjects(bool animateLastSegment, bool fullyValidated) //spawns the actual track pieces
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            if (Application.isPlaying) { Destroy(transform.GetChild(i).gameObject); }
            else { DestroyImmediate(transform.GetChild(i).gameObject); }
        }
        if(segments == null)
        {
            Debug.LogWarning("No segments to generate track from.");
        }
        trackPieces = new List<GameObject>();
        Vector3 pos = Vector3.zero;
        Vector3 lastPos = Vector3.zero;
        Vector3 forward = Vector3.forward;
        if(segments[0].type == SegmentType.Oval || segments[0].type == SegmentType.Bottleneck || segments[0].type == SegmentType.Crossroads){
            //Spawn a mat
            GameObject mat = null;
            if(segments[0].type == SegmentType.Oval){
                mat = Instantiate(trackPrefabs[11], pos, Quaternion.identity, transform);
                mat.name = "Oval Mat";
            } else if(segments[0].type == SegmentType.Bottleneck){
                mat = Instantiate(trackPrefabs[12], pos, Quaternion.identity, transform);
                mat.name = "Bottleneck Mat";
            } else if(segments[0].type == SegmentType.Crossroads){
                mat = Instantiate(trackPrefabs[13], pos, Quaternion.identity, transform);
                mat.name = "Crossroads Mat";
            }
            if(mat != null){
                //get all the gameObjects with a track spline component and add them to the track pieces list
                TrackSpline[] splines = mat.GetComponentsInChildren<TrackSpline>();
                foreach(TrackSpline spline in splines){
                    trackPieces.Add(spline.gameObject);
                }
            }
        }else{//Overdrive Modular Track
            for (int i = 0; i < segments.Length; i++)
            {
                GameObject track = null;
                Quaternion rot = Quaternion.LookRotation(forward);
                bool useFullTrack = segments[i].validated && fullyValidated;

                //round position to 1 decimal place
                pos = new Vector3(Mathf.Round(pos.x * 10) / 10, Mathf.Round(pos.y * 10) / 10, Mathf.Round(pos.z * 10) / 10);
                if (segments[i].type == SegmentType.FinishLine)
                {
                    track = Instantiate(useFullTrack ? trackPrefabs[0] : scannningPrefabs[0], pos, rot, transform);
                    pos += forward;
                }
                if (segments[i].type == SegmentType.Straight)
                {
                    track = Instantiate(useFullTrack ? trackPrefabs[1] : scannningPrefabs[1], pos, rot, transform);
                    pos += forward;
                }
                if (segments[i].type == SegmentType.FnFSpecial)
                {
                    track = Instantiate(useFullTrack ? trackPrefabs[2] : scannningPrefabs[2], pos, rot, transform);
                    if (segments[i].flipped) { track.transform.localScale = new Vector3(-1, 1, 1); }
                    pos += forward;
                }
                if (segments[i].type == SegmentType.Turn)
                {
                    track = Instantiate(useFullTrack ? trackPrefabs[3] : scannningPrefabs[3], pos, rot, transform);
                    track.transform.localScale = new Vector3(segments[i].flipped ? 1 : -1, 1, 1);
                    forward = Quaternion.Euler(0, segments[i].flipped ? 90 : -90, 0) * forward;
                    pos += forward;
                }
                if (segments[i].type == SegmentType.CrissCross)
                {
                    bool hasCrissCross = false;
                    int matchingCrissCrossIndex = -1;
                    
                    // Look for an existing CrissCross at the same X,Y coordinates
                    for (int j = 0; j < i; j++)
                    {
                        if (segments[j].type == SegmentType.CrissCross && segments[j].X == segments[i].X && segments[j].Y == segments[i].Y)
                        {
                            hasCrissCross = true;
                            matchingCrissCrossIndex = j;
                            break;
                        }
                    }
                    
                    if (hasCrissCross && matchingCrissCrossIndex >= 0 && matchingCrissCrossIndex < trackPieces.Count)
                    {
                        // This CrissCross shares the same physical location as a previous one
                        // Update the name to show it handles multiple segments
                        if (trackPieces[matchingCrissCrossIndex] != null)
                        {
                            trackPieces[matchingCrissCrossIndex].name = $"{matchingCrissCrossIndex},{i} (CrissCross)";
                        }
                        
                        // Create an invisible reference track piece that splines across the intersection
                        track = Instantiate(trackPrefabs[10], pos, rot, transform); // invisible criss cross (splines across)
                        track.name = $"{i} (CrissCross Reference)";
                    }
                    else
                    {
                        // This is the first CrissCross at this position, create the actual track piece
                        track = Instantiate(useFullTrack ? trackPrefabs[8] : scannningPrefabs[4], pos, rot, transform);
                    }
                    pos += forward;
                }
                if (segments[i].type == SegmentType.JumpRamp)
                {
                    track = Instantiate(useFullTrack ? trackPrefabs[9] : scannningPrefabs[5], pos, rot, transform);
                    pos += forward * 2;
                }

                trackPieces.Add(track);
                if (track != null)
                {
                    track.name = $"{i} ({segments[i].type})";
                    if (segments[i].validated && fullyValidated)
                    {
                        if (i == 1)
                        {
                            track.GetComponent<TrackSpline>().flipped = segments[i].flipped;
                        }
                        else if (i > 1)
                        {
                            if (trackPieces[i] == null) { continue; }
                            try
                            {
                                int offset = 1;
                                if (trackPieces[i - 1] == null) { offset = 2; }
                                if (trackPieces[i - offset] == null) { Debug.Log($"Track {i} was null"); continue; }
                                Vector3 lastTrackEndLink = trackPieces[i - offset].GetComponent<TrackSpline>().GetEndLinkPoint();
                                Vector3 lastTrackStartLink = trackPieces[i].GetComponent<TrackSpline>().GetStartLinkPoint();
                                Debug.DrawLine(lastTrackEndLink, lastTrackStartLink, Color.red, 400);
                                float linkDist = Vector3.Distance(lastTrackEndLink, lastTrackStartLink);
                                if (linkDist > 0.01f)
                                {
                                    track.GetComponent<TrackSpline>().flipped = true;
                                }
                            }
                            catch (System.Exception e)
                            {
                                Debug.LogError(e);
                            }

                        }
                    }
                    if (!fullyValidated && track != null && segments[i].validated)
                    {
                        track.GetComponent<MeshRenderer>().material = validConfirmedMat;
                    }
                }
                Debug.DrawRay(lastPos + (Vector3.up * 0.1f), forward * 0.5f, Color.blue, 5);
                lastPos = pos;
            }
        }

        
        if (animateLastSegment && trackPieces.Count > 0)
        {
            if (trackPieces[trackPieces.Count - 1] != null)
            {
                trackPieces[trackPieces.Count - 1].AddComponent<SegmentSpawnAnimator>();
            }
        }
    }

    void PreCacheSegmentLengths()
    {
        //iterate over all segments and cache their lengths 
        //(we only need to do this once per track, values will never change so we can store them until quit)
        for(int i = 0; i < segments.Length; i++)
        {
            int id = segments[i].internalID;
            if(!segmentLengthCache.ContainsKey(id))
            {
                if(trackPieces[i] == null)
                {
                    Debug.LogWarning($"Track piece {i} is null, cannot cache lengths");
                    continue;
                }
                TrackSpline spline = trackPieces[i].GetComponent<TrackSpline>();
                if(spline == null)
                {
                    Debug.LogWarning($"Track piece {i} has no TrackSpline component, cannot cache lengths");
                    continue;
                }
                const int stepCount = 50; //estimate lengths with 50 steps
                float width = spline.GetWidth(0f);
                float leftLength = 0f;
                float rightLength = 0f;
                Vector3 previousLeftPoint = spline.GetPoint(new TrackCoordinate(0, -width, 0f));
                Vector3 previousRightPoint = spline.GetPoint(new TrackCoordinate(0, width, 0f));
                for(int j = 1; j <= stepCount; j++)
                {
                    float t = (float)j / (float)stepCount;
                    Vector3 currentLeftPoint = spline.GetPoint(new TrackCoordinate(0, -width, t));
                    Vector3 currentRightPoint = spline.GetPoint(new TrackCoordinate(0, width, t));
                    leftLength += Vector3.Distance(previousLeftPoint, currentLeftPoint);
                    rightLength += Vector3.Distance(previousRightPoint, currentRightPoint);
                    previousLeftPoint = currentLeftPoint;
                    previousRightPoint = currentRightPoint;
                }

                //if the 2 lengths are within 1% of each other, consider it a straight segment
                bool isStraight = Mathf.Abs(leftLength - rightLength) / Mathf.Max(leftLength, rightLength) < 0.01f;
                
                //convert Unity scale to anki mm (1 unit = 560mm)
                leftLength *= 560f;
                rightLength *= 560f;

                SegmentLength lengths = new SegmentLength(leftLength, rightLength, isStraight);
                segmentLengthCache.Add(id, lengths);
                //Debug.Log($"Cached lengths for segment ID {id}: Left = {leftLength}mm, Right = {rightLength}mm, IsStraight = {isStraight}");
            }
        }
    }
    public SegmentLength? GetCachedSegmentLengths(int segmentID)
    {
        if(segmentLengthCache.ContainsKey(segmentID))
        {
            return segmentLengthCache[segmentID];
        }
        return null;
    }
    public delegate void OnVerifyTrack(Segment[] segments);
    public event OnVerifyTrack? OnTrackValidated;

    public static (bool, Segment?) EvaluateMatch(Segment A, Segment B)
    {
        if (A == null || B == null) { return (false, null); } //if either piece is null, we can't match them
        else if (A.internalID != 0 && B.internalID != 0)
        { //if both pieces are not fallbacks
            return ((A.type == B.type) && (A.flipped == B.flipped), null); //match if the type and flipped state are the same
        }
        else if (A.internalID == 0 && B.internalID == 0)
        { //if both pieces are fallbacks
            return ((A.type == B.type) && (A.flipped == B.flipped), null); //match if the type and flipped state are the same
        }
        else
        { //A or B is a fallback, check if we can match them
            for (int i = 0; i < 2; i++)
            {
                Segment C = A; //copy A to C
                Segment D = B; //copy B to D
                if (i == 1) { C = B; D = A; } //swap C and D
                if (C.internalID == 0 && D.internalID != 0)
                { //if C is a fallback and D is not
                    if (C.type == D.type)
                    {
                        if (C.validated) { D.validated = true; } //if C is validated, set D to validated
                        return (true, D);
                    } //if the type is the same, its probably the same piece (return the one with the ID)
                    else
                    {
                        if (C.type == SegmentType.Straight && D.type == SegmentType.CrissCross)
                        {
                            if (C.validated) { D.validated = true; } //if C is validated, set D to validated
                            return (true, D);
                        } //crisscross is a straight piece
                        else if (C.type == SegmentType.Straight && D.type == SegmentType.FnFSpecial)
                        {
                            if (C.validated) { D.validated = true; } //if C is validated, set D to validated
                            return (true, D);
                        } //FnF is a straight piece
                    }
                }
            }
        }
        return (false, null); //no match found (should not happen)
    }

    public TrackCoordinate WorldspaceToTrackCoordinate(Vector3 worldPos)
    {
        //check if the position is within 0.5m of any track piece
        float closestDist = Mathf.Infinity;
        TrackSpline closestSpline = null;
        int bestIndex = 0;
        for (int i = 0; i < trackPieces.Count; i++)
        {
            if (trackPieces[i] == null) { continue; }
            float dist = Vector3.Distance(worldPos, trackPieces[i].transform.position);
            if (dist < 0.51f && dist < closestDist)
            {
                closestDist = dist;
                closestSpline = trackPieces[i].GetComponent<TrackSpline>();
                bestIndex = i;
            }
        }
        if (closestSpline != null)
        {
            TrackCoordinate c = closestSpline.GetTrackCoordinate(worldPos);
            c.idx = bestIndex; //set the index of the track piece
            Vector3 pointOnTrack = closestSpline.GetPoint(c.progression, c.offset);

            Debug.DrawLine(worldPos, pointOnTrack, Color.red, 5); //draw a line from the world position to the track position

            return c;//get the track coordinate from the world position
        }
        return null;
    }
    public void DrawLineBetweenTrackCoordinates(TrackCoordinate a, TrackCoordinate b, Color color, float duration = 0.1f)
    {
        if (a == null || b == null) { return; }
        TrackSpline splineA = GetTrackSpline(a.idx);
        TrackSpline splineB = GetTrackSpline(b.idx);
        if (splineA == null || splineB == null) { return; }
        Vector3 pointA = splineA.GetPoint(a.progression, a.offset);
        Vector3 pointB = splineB.GetPoint(b.progression, b.offset);
        Debug.DrawLine(pointA, pointB + new Vector3(0,0.01f,0), color, duration);
    }
}
public class SegmentLength
{
    public float leftSideLength = 0f;
    public float rightSideLength = 0f;
    public bool isStraight = false;
    public SegmentLength(float leftLength, float rightLength, bool straight)
    {
        leftSideLength = leftLength;
        rightSideLength = rightLength;
        isStraight = straight;
    }
}
[System.Serializable]
public class Segment{
    public SegmentType type;
    public int internalID;
    public readonly bool flipped;
    public int up, down, elevation, X, Y;
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
    public static bool operator ==(Segment? a, Segment? b) {
        if (ReferenceEquals(a, b)) { return true; }
        if (a is null || b is null) { return false; }
        return a.Equals(b);
    }
    public static bool operator !=(Segment? a, Segment? b) { return !(a == b); }
    public override string ToString() { return $"({type}|id:{internalID}|flipped:{flipped})"; }
}