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
        requestedTrack[3] = new Segment(SegmentType.Turn, 0, true);
        requestedTrack[4] = new Segment(SegmentType.Turn, 0, true);
        requestedTrack[5] = new Segment(SegmentType.Straight, 0, false);
        requestedTrack[6] = new Segment(SegmentType.Straight, 0, false);
        requestedTrack[7] = new Segment(SegmentType.Turn, 0, true);
        requestedTrack[8] = new Segment(SegmentType.Turn, 0, true);
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

    IEnumerator Tests()
    {
        yield return new WaitForSeconds(1f);
        TrackGenerator.track.Generate(requestedTrack, true);
        yield return new WaitForSeconds(3f);

        TrackCoordinate testCar = new TrackCoordinate(1, 0, 0);
        testCar.speed = 500;

        float targetOffset = 0f;
        int targetSpeed = 500;

        for (int i = 0; i < 70; i++)
        {
            int idx = testCar.idx;
            TrackSpline trackSpline = TrackGenerator.track.GetTrackSpline(idx);
            if (trackSpline == null)
            {
                idx++;
                trackSpline = TrackGenerator.track.GetTrackSpline(idx);
            }
            SegmentType segmentType = TrackGenerator.track.GetSegmentType(idx);

            TrackPathSolver.PathingInputs inputs = new TrackPathSolver.PathingInputs
            {
                currentTargetSpeed = targetSpeed,
                currentTargetOffset = targetOffset,
                ourCoord = testCar,
                carLocations = new TrackCoordinate[] { },
                currentTarget = null,
                state = TrackPathSolver.AIState.Speed,
                depth = 3
            };


            (int tSpeed, float tOffset, bool boost, string log) = TrackPathSolver.GetBestPath(inputs);

            targetSpeed = tSpeed;
            targetOffset = tOffset;

            testCar.speed = targetSpeed;
            testCar.offset = targetOffset;


            float progress = TrackPathSolver.GetProgress(segmentType, false, testCar, 0.1f);
            Debug.Log("Progress: " + progress + " | Segment Type: " + segmentType);
            testCar.Progress(progress);

            Vector3 carPosition = trackSpline.GetPoint(testCar);
            Debug.DrawLine(carPosition, carPosition + Vector3.up * 0.1f, Color.red, 100f);


            if (testCar.progression >= 1)
            {
                testCar.SetIdx(testCar.idx + 1);
                if (testCar.idx >= TrackGenerator.track.GetTrackLength())
                { testCar.idx = 1; }
            }

            yield return new WaitForSeconds(0.1f);
            Debug.Log("Car Position: " + carPosition + " | Segment Type: " + segmentType + " | Progression: " + testCar.progression);
        }
    }
}