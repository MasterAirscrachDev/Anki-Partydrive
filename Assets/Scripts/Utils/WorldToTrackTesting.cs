using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldToTrackTesting : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(TestWorldToTrack());
    }

    IEnumerator TestWorldToTrack()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.1f);

            TrackGenerator.track.WorldspaceToTrackCoordinate(transform.position);
        }
    }
}
