using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static OverdriveServer.NetStructures;

public class AiTesting : MonoBehaviour
{
    Segment[] requestedTrack;
    // Start is called before the first frame update
    void Start()
    {
        requestedTrack = new Segment[9];
        requestedTrack[0] = new Segment(SegmentType.PreFinishLine, 0, false);
        requestedTrack[1] = new Segment(SegmentType.FinishLine, 0, false);
        requestedTrack[2] = new Segment(SegmentType.Straight, 0, true);
        requestedTrack[3] = new Segment(SegmentType.Turn, 0, false);
        requestedTrack[4] = new Segment(SegmentType.Turn, 0, false);
        requestedTrack[5] = new Segment(SegmentType.Straight, 0, false);
        requestedTrack[6] = new Segment(SegmentType.Straight, 0, false);
        requestedTrack[7] = new Segment(SegmentType.Turn, 0, false);
        requestedTrack[8] = new Segment(SegmentType.Turn, 0, false);
        requestedTrack[0].validated = true;
        requestedTrack[1].validated = true;
        requestedTrack[2].validated = true;
        requestedTrack[3].validated = true;
        requestedTrack[4].validated = true;
        requestedTrack[5].validated = true;
        requestedTrack[6].validated = true;
        requestedTrack[7].validated = true;
        requestedTrack[8].validated = true;
        
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

        // --- new: support multiple cars ---
        int carCount = 2;
        TrackCoordinate[] testCars = new TrackCoordinate[carCount];
        int[] targetSpeeds = new int[carCount];
        float[] targetOffsets = new float[carCount];
        Color[] carColors = new Color[] { Color.red, Color.green, Color.blue, Color.yellow };

        // initialize each car
        for (int i = 0; i < carCount; i++)
        {
            testCars[i] = new TrackCoordinate(1, 0, 0);
            targetSpeeds[i] = 500;
            targetOffsets[i] = 0f;
            testCars[i].speed = targetSpeeds[i];
        }
        // --- end new ---

        for (int step = 0; step < 900; step++)
        {
            // loop each car
            for (int c = 0; c < carCount; c++)
            {
                TrackCoordinate car = testCars[c];
                int idx = car.idx;
                TrackSpline spline = TrackGenerator.track.GetTrackSpline(idx);
                if (spline == null)
                {
                    idx++;
                    spline = TrackGenerator.track.GetTrackSpline(idx);
                }
                SegmentType segType = TrackGenerator.track.GetSegmentType(idx);

                //create an array of the track coordinates for the other cars
                List<TrackCoordinate> otherCars = new List<TrackCoordinate>();
                for (int j = 0; j < carCount; j++)
                {
                    if (j != c) // don't include self
                    {
                        otherCars.Add(testCars[j]);
                    }
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
                targetSpeeds[c] = tSpd;
                targetOffsets[c] = tOff;
                car.speed = tSpd;
                car.offset = tOff;

                float prog = TrackPathSolver.GetProgress(segType, false, car, 0.05f);
                car.Progress(prog);

                Vector3 pos = spline.GetPoint(car);
                Debug.DrawLine(pos, pos + Vector3.up * 0.1f, carColors[c], 0.2f);

                if (car.progression >= 1)
                {
                    car.SetIdx(car.idx + 1);
                    if (car.idx >= TrackGenerator.track.GetTrackLength())
                        car.idx = 1;
                }

                // optional per-car log
                Debug.Log($"Car {c} Log={log}");
                //Debug.Log($"Car {c} Step {step}: Pos={pos} Seg={segType} Prog={car.progression}");
            }

            yield return new WaitForSeconds(0.05f);
        }
    }
}