using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using static OverdriveServer.NetStructures;

public class AiTesting : MonoBehaviour
{
    [SerializeField] bool generateMat = false;
    Segment[] requestedTrack;
    // Start is called before the first frame update
    void Start()
    {   
        if(generateMat){
            List<Segment> trackSegments = new List<Segment>
            {
                new Segment(SegmentType.Bottleneck, 78, false),
                new Segment(SegmentType.Bottleneck, 79, false),
                new Segment(SegmentType.Bottleneck, 80, false),
                new Segment(SegmentType.Bottleneck, 81, false),
                new Segment(SegmentType.Bottleneck, 82, false),
                new Segment(SegmentType.Bottleneck, 83, false),
                new Segment(SegmentType.Bottleneck, 84, false),
                new Segment(SegmentType.Bottleneck, 85, false),
                new Segment(SegmentType.Bottleneck, 86, false),
                new Segment(SegmentType.Bottleneck, 87, false),
                new Segment(SegmentType.Bottleneck, 88, false),
                new Segment(SegmentType.Bottleneck, 89, false),
                new Segment(SegmentType.Bottleneck, 90, false),
                new Segment(SegmentType.Bottleneck, 91, false)
            };
            foreach (Segment s in trackSegments)
            {
                s.validated = true; //mark all segments as validated
            }
            requestedTrack = trackSegments.ToArray();
        }else{
            List<Segment> trackSegments = new List<Segment>
            {
                new Segment(SegmentType.PreFinishLine, 0, false),
                new Segment(SegmentType.FinishLine, 0, false),
                new Segment(SegmentType.Straight, 0, true),
                new Segment(SegmentType.Turn, 0, false),
                new Segment(SegmentType.Turn, 0, false),
                new Segment(SegmentType.Straight, 0, false),
                new Segment(SegmentType.Straight, 0, false),
                new Segment(SegmentType.Turn, 0, true),
                new Segment(SegmentType.Turn, 0, true),
                new Segment(SegmentType.Straight, 0, false),
                new Segment(SegmentType.Turn, 0, false),
                new Segment(SegmentType.Turn, 0, false),
                new Segment(SegmentType.Straight, 0, false),
                new Segment(SegmentType.Straight, 0, false),
                new Segment(SegmentType.Turn, 0, false),
                new Segment(SegmentType.Straight, 0, false),
                new Segment(SegmentType.Straight, 0, false),
                new Segment(SegmentType.Turn, 0, true),
                new Segment(SegmentType.Straight, 0, false),
                new Segment(SegmentType.Straight, 0, false),
                new Segment(SegmentType.Straight, 0, false),
                new Segment(SegmentType.Straight, 0, false),
                new Segment(SegmentType.Turn, 0, false),
                new Segment(SegmentType.Turn, 0, false),
                new Segment(SegmentType.Straight, 0, false),
                new Segment(SegmentType.Straight, 0, false),
                new Segment(SegmentType.Straight, 0, false),
                new Segment(SegmentType.Straight, 0, false),
                new Segment(SegmentType.Turn, 0, false),
                new Segment(SegmentType.Turn, 0, true),
                new Segment(SegmentType.Straight, 0, false),
            };
            foreach (Segment s in trackSegments)
            {
                s.validated = true; //mark all segments as validated
            }
            requestedTrack = trackSegments.ToArray();
        }

        StartCoroutine(Tests());
    }
    [ContextMenu("Run Tests")]
    public void RunTests()
    {
        StartCoroutine(Tests());
    }

    IEnumerator Tests()
    {
        yield return new WaitForSeconds(1f);
        TrackGenerator.track.Generate(requestedTrack, true);
        yield return new WaitForSeconds(3f);
        const float stepDelta = 0.01f;
        const float realDelta = 0.01f;

        // --- new: support multiple cars ---
        int carCount = 8;
        TrackCoordinate[] testCars = new TrackCoordinate[carCount];
        int[] targetSpeeds = new int[carCount];
        float[] targetOffsets = new float[carCount];
        Color[] carColors = new Color[] { Color.red, Color.green, Color.blue, Color.yellow, Color.cyan, Color.magenta, Color.white, Color.grey };
        string[] colorNames = new string[] { "Red", "Green", "Blue", "Yellow", "Cyan", "Magenta", "White", "Grey" };

        // initialize each car
        for (int i = 0; i < carCount; i++)
        {
            testCars[i] = new TrackCoordinate(1, Random.Range(50f, -50f), 0, 22, 0.2f);
            //testCars[i].SetIdx(i + 1); // set different starting indices for each car
            testCars[i].progression = Random.Range(0f, 0.5f);
            targetSpeeds[i] = 500;
            targetOffsets[i] = 0f;
            testCars[i].speed = targetSpeeds[i];
        }
        // --- end new ---

        for (int step = 0; step < Mathf.RoundToInt(80 / realDelta); step++) {
            for (int c = 0; c < carCount; c++) {
                TrackCoordinate car = testCars[c];
                int idx = car.idx;
                TrackSpline spline = TrackGenerator.track.GetTrackSpline(idx);
                if (spline == null)
                {
                    idx++;
                    spline = TrackGenerator.track.GetTrackSpline(idx);
                }
                SegmentType segType = TrackGenerator.track.GetSegmentType(idx);
                bool isReversed = TrackGenerator.track.GetSegmentReversed(idx);

                //create an array of the track coordinates for the other cars
                List<TrackCoordinate> otherCars = new List<TrackCoordinate>();
                for (int j = 0; j < carCount; j++)
                { if (j != c)  { otherCars.Add(testCars[j]); } // don't include self
                }

                // path inputs
                TrackPathSolver.PathingInputs inputs = new TrackPathSolver.PathingInputs
                {
                    currentTargetSpeed = targetSpeeds[c],
                    currentTargetOffset = targetOffsets[c],
                    ourCoord = car,
                    carLocations = otherCars.ToArray(),
                    currentTarget = null,
                    state = TrackPathSolver.AIState.Speed,
                    depth = 3
                };

                (int tSpd, float tOff, bool boost, string log) = TrackPathSolver.GetBestPath(inputs);
                targetSpeeds[c] = Mathf.RoundToInt((float)tSpd * (boost ? 1.1f : 1f));
                targetOffsets[c] = tOff;
                car.speed = Mathf.RoundToInt(MoveTowards(car.speed, targetSpeeds[c], 20f));
                car.offset = MoveTowards(car.offset, tOff, 1f);



                float prog = TrackPathSolver.GetProgress(segType, isReversed, car, stepDelta);
                car.Progress(prog);

                car.DebugRender(spline, carColors[c], realDelta);
                //Vector3 pos = spline.GetPoint(car);
                //Debug.DrawLine(pos, pos + Vector3.up * 0.1f, carColors[c], 0.2f);

                if (car.progression >= 1)
                {
                    car.SetIdx(car.idx + 1);
                    if (car.idx >= TrackGenerator.track.GetTrackLength())
                        car.idx = 1;
                }

                // optional per-car log
                Debug.Log($"Car {colorNames[c]} Offset={car.offset} Log={log}");
                //Debug.Log($"Car {c} Step {step}: Pos={pos} Seg={segType} Prog={car.progression}");
            }

            yield return new WaitForSeconds(realDelta);
        }
    }
    float MoveTowards(float current, float target, float step)
    {
        if (Mathf.Abs(current - target) < step)
            return target;
        return current + Mathf.Sign(target - current) * step;
    }
}