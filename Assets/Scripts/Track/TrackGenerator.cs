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
    Dictionary<int, SegmentLength> segmentLengthCache = new Dictionary<int, SegmentLength>();

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
    public bool GetSegmentReversed(int index) { index = LoopIndex(index); return segments[index].reversed; }
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
                SR.ui.SetScanningStatusText("");
                StartCoroutine(OnFinalGenerate());
                return;
            }
            if (segments == null || segments.Length == 0) { return; } //if there are no segments, do nothing  
            SR.ui.SetScanningStatusText(segments[0].validated ? "Double Checking..." : "Scanning...");
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
        //Debug.Log("Positioning track camera based on generated track pieces");
        if(trackPieces == null || trackPieces.Count == 0){
            Debug.LogWarning("No track pieces to position camera around.");
            return;
        }
        
        // Calculate accurate bounds using min/max detection
        Vector3 min = Vector3.positiveInfinity;
        Vector3 max = Vector3.negativeInfinity;
        
        for (int i = 0; i < trackPieces.Count; i++)
        {
            if (trackPieces[i] == null) { continue; }
            Vector3 pos = trackPieces[i].transform.position;
            min = Vector3.Min(min, pos);
            max = Vector3.Max(max, pos);
        }
        
        Vector3 center = (min + max) / 2f;
        // Size is half-extents plus padding for track piece width
        Vector2 size = new Vector2(
            (max.x - min.x) / 2f + 0.5f,
            (max.z - min.z) / 2f + 0.5f
        );
        
        if(autoSwitchCamera){
            SR.ui.SwitchToTrackCamera(true);
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
        PositionTrackCamera(true);
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
        // Dictionary to map CrissCross 3D positions to their first track piece index
        Dictionary<Vector3, int> crissCrossPositions = new Dictionary<Vector3, int>();
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
            for (int i = 0; i < segments.Length; i++) {
                GameObject track = null;
                Quaternion rot = Quaternion.LookRotation(forward);
                bool useFullTrack = segments[i].validated && fullyValidated;

                //round position to 1 decimal place
                pos = new Vector3(Mathf.Round(pos.x * 10) / 10, Mathf.Round(pos.y * 10) / 10, Mathf.Round(pos.z * 10) / 10);
                if (segments[i].type == SegmentType.FinishLine) {
                    track = Instantiate(useFullTrack ? trackPrefabs[0] : scannningPrefabs[0], pos, rot, transform);
                    pos += forward;
                }
                if (segments[i].type == SegmentType.Straight) {
                    track = Instantiate(useFullTrack ? trackPrefabs[1] : scannningPrefabs[1], pos, rot, transform);
                    pos += forward;
                }
                if (segments[i].type == SegmentType.FnFSpecial) {
                    track = Instantiate(useFullTrack ? trackPrefabs[2] : scannningPrefabs[2], pos, rot, transform);
                    if (segments[i].reversed) { track.transform.localScale = new Vector3(-1, 1, 1); }
                    pos += forward;
                }
                if (segments[i].type == SegmentType.Turn) { 
                    track = Instantiate(useFullTrack ? trackPrefabs[3] : scannningPrefabs[3], pos, rot, transform);
                    track.transform.localScale = new Vector3(segments[i].reversed ? 1 : -1, 1, 1);
                    forward = Quaternion.Euler(0, segments[i].reversed ? 90 : -90, 0) * forward;
                    pos += forward;
                }
                if (segments[i].type == SegmentType.CrissCross) {
                    // Round position to avoid floating point precision issues
                    Vector3 roundedPos = new Vector3(
                        Mathf.Round(pos.x * 10) / 10, 
                        Mathf.Round(pos.y * 10) / 10, 
                        Mathf.Round(pos.z * 10) / 10
                    );
                    
                    // Check if we already have a CrissCross at this 3D position
                    if (crissCrossPositions.ContainsKey(roundedPos))
                    {
                        // This CrissCross shares the same physical location as a previous one
                        int firstTrackPieceIndex = crissCrossPositions[roundedPos];
                        
                        // Update the name of the first track piece to show it handles multiple segments
                        if (firstTrackPieceIndex >= 0 && firstTrackPieceIndex < trackPieces.Count && trackPieces[firstTrackPieceIndex] != null)
                        {
                            // Get current segment indices from the name if it already has multiple
                            string currentName = trackPieces[firstTrackPieceIndex].name;
                            if (currentName.Contains("(CrissCross)"))
                            {
                                string segmentPart = currentName.Substring(0, currentName.IndexOf(" (CrissCross)"));
                                trackPieces[firstTrackPieceIndex].name = $"{segmentPart},{i} (CrissCross)";
                            }
                            else
                            {
                                // First time adding a second segment, shouldn't normally happen
                                trackPieces[firstTrackPieceIndex].name = $"{firstTrackPieceIndex},{i} (CrissCross)";
                            }
                        }
                        
                        // Create an invisible reference track piece that splines across the intersection
                        track = Instantiate(trackPrefabs[10], pos, rot, transform); // invisible criss cross (splines across)
                        track.name = $"{i} (CrissCross Reference -> {firstTrackPieceIndex})";
                    }
                    else
                    {
                        // This is the first CrissCross at this position, create the actual track piece
                        track = Instantiate(useFullTrack ? trackPrefabs[8] : scannningPrefabs[4], pos, rot, transform);
                        
                        // Remember this position and its track piece index for future CrissCross segments
                        crissCrossPositions[roundedPos] = trackPieces.Count; // Will be added at current trackPieces.Count
                    }
                    pos += forward;
                }
                if (segments[i].type == SegmentType.JumpRamp) {
                    track = Instantiate(useFullTrack ? trackPrefabs[9] : scannningPrefabs[5], pos, rot, transform);
                    pos += forward * 2;
                }

                trackPieces.Add(track);
                if (track != null) {
                    track.name = $"{i} ({segments[i].type})";
                    if (segments[i].validated && fullyValidated) {
                        if (i == 1)
                        { track.GetComponent<TrackSpline>().flipped = segments[i].reversed; }
                        else if (i > 1) {
                            if (trackPieces[i] == null) { continue; }
                            try {
                                int offset = 1;
                                if (trackPieces[i - 1] == null) { offset = 2; }
                                if (trackPieces[i - offset] == null) { Debug.Log($"Track {i} was null"); continue; }
                                Vector3 lastTrackEndLink = trackPieces[i - offset].GetComponent<TrackSpline>().GetEndLinkPoint();
                                Vector3 lastTrackStartLink = trackPieces[i].GetComponent<TrackSpline>().GetStartLinkPoint();
                                Debug.DrawLine(lastTrackEndLink, lastTrackStartLink, Color.red, 400);
                                float linkDist = Vector3.Distance(lastTrackEndLink, lastTrackStartLink);
                                if (linkDist > 0.01f)
                                { track.GetComponent<TrackSpline>().flipped = true; }
                            }
                            catch (System.Exception e)
                            { Debug.LogError(e); }
                        }
                    }
                    if (!fullyValidated && track != null && segments[i].validated)
                    { track.GetComponent<MeshRenderer>().material = validConfirmedMat; }
                }
                Debug.DrawRay(lastPos + (Vector3.up * 0.1f), forward * 0.5f, Color.blue, 5);
                lastPos = pos;
            }
        }

        
        if (animateLastSegment && trackPieces.Count > 0) { //Animation for last segment only
            if (trackPieces[trackPieces.Count - 1] != null)
            { trackPieces[trackPieces.Count - 1].AddComponent<SegmentSpawnAnimator>(); }
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
            return ((A.type == B.type) && (A.reversed == B.reversed), null); //match if the type and flipped state are the same
        }
        else if (A.internalID == 0 && B.internalID == 0)
        { //if both pieces are fallbacks
            return ((A.type == B.type) && (A.reversed == B.reversed), null); //match if the type and flipped state are the same
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
    public Vector3 TrackCoordinateToWorldspace(TrackCoordinate tc)
    {
        if (tc == null) { return Vector3.zero; }
        TrackSpline spline = GetTrackSpline(tc.idx);
        if (spline == null) { return Vector3.zero; }
        return spline.GetPoint(tc.progression, tc.offset);
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
    public List<GameObject> GetSegmentsWithTrackElementSlots()
    {
        List<GameObject> segmentsWithSlots = new List<GameObject>();
        //for each segment in the track
        foreach (GameObject segment in trackPieces)
        {
            if(segment == null) { continue; }
            //get all element slots in the segment
            TrackElementSlot[] segmentSlots = segment.GetComponentsInChildren<TrackElementSlot>();
            if(segmentSlots != null && segmentSlots.Length > 0)
            {
                segmentsWithSlots.Add(segment);
            }
        }
        return segmentsWithSlots;
    }
    public TrackSize GetTrackSize()
    {
        if (segments.Length <= 12) { return TrackSize.Small; }
        else if (segments.Length <= 35) { return TrackSize.Medium; }
        else { return TrackSize.Large; }
    }
    public enum TrackSize { Small, Medium, Large }
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
    public readonly bool reversed;
    public int up, down, elevation, X, Y;
    public bool validated = false;
    public Segment(SegmentType type, int id, bool flipped){
        this.type = type;
        this.reversed = flipped;
        internalID = id;
        up = 0; down = 0;
    }
    public Segment(SegmentData data){
        this.type = data.type;
        this.reversed = data.reversed;
        internalID = data.internalID;
        validated = data.validated;
        up = 0; down = 0;
    }
    public void SetUpDown(int up, int down){ this.up = up; this.down = down; }
    public override bool Equals(object? obj) { // Check for null and compare run-time types.
        if (obj == null || !GetType().Equals(obj.GetType())) { return false; }
        else {
            Segment p = (Segment)obj;
            return (type == p.type) && (reversed == p.reversed);
        }
    }
    public static bool operator ==(Segment? a, Segment? b) {
        if (ReferenceEquals(a, b)) { return true; }
        if (a is null || b is null) { return false; }
        return a.Equals(b);
    }
    public static bool operator !=(Segment? a, Segment? b) { return !(a == b); }
    public override string ToString() { return $"({type}|id:{internalID}|flipped:{reversed})"; }
}