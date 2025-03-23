using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarModel : MonoBehaviour
{
    [SerializeField] float speedTuning = 0.01f;
    [SerializeField] Material solidMaterial, transparentMaterial;
    TrackSpline trackSpline;
    int speed = 0, pieceIdx = 0, shift = 0;
    float trackPieceProgression = 0, trackPieceLength = 0;
    float horizontalOffset = 0;
    Vector3 lastPosition;

    int timout;
    TrackGenerator track;
    // Start is called before the first frame update
    void Start()
    {
        timout = 5;
        StartCoroutine(Timeout());
        track = FindObjectOfType<TrackGenerator>();
    }
    IEnumerator Timeout(){
        while(timout > 0){
            yield return new WaitForSeconds(1);
            timout--;
        }
        FindObjectOfType<CarEntityTracker>().RemoveEntity(gameObject.name);
    }

    // Update is called once per frame
    void Update()
    {
        trackPieceProgression += ((speedTuning * speed) * trackPieceLength) * Time.deltaTime;
        if(trackSpline != null){
            transform.position = trackSpline.GetPoint(trackPieceProgression, horizontalOffset);
            transform.LookAt(lastPosition);
            lastPosition = transform.position;
            transform.Rotate(0, 180, 0);
        }
        if(trackPieceProgression >= 1 && shift > 2){
            trackPieceProgression = 0;
            shift++;
            trackSpline = track.GetTrackPiece(pieceIdx + shift);
            if(trackSpline == null){trackSpline = track.GetTrackPiece(pieceIdx + ++shift); }
        }
    }
    public void SetTrackSpline(TrackSpline trackSpline, int idx){
        timout = 5;
        if(idx != this.pieceIdx){ //only update if the track spline is different
            this.pieceIdx = idx;
            this.trackSpline = trackSpline;
            trackPieceProgression = 0;
            shift = 0;
            trackPieceLength = trackSpline.GetLength(horizontalOffset);
        }
    }
    public void SetSpeedAndOffset(int speed, float offset){
        this.speed = speed;
        this.horizontalOffset = offset;
    }
    public void SetTrustedPosition(bool trusted){
        GetComponent<MeshRenderer>().material = trusted ? solidMaterial : transparentMaterial;
    }

}
