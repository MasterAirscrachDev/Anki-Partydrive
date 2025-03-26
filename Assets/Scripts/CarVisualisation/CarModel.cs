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
    float timeout = 5;
    TrackGenerator track;
    // Start is called before the first frame update
    void Start()
    {
        timeout = 5;
        track = FindObjectOfType<TrackGenerator>();
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
        timeout -= Time.deltaTime;
        if(timeout <= 0){
            FindObjectOfType<CarEntityTracker>().RemoveEntity(gameObject.name);
        }
    }
    public void SetTrackSpline(TrackSpline trackSpline, int idx){
        timeout = 5;
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
        this.horizontalOffset = offset; //change this to smooth when changed
    }
    public void SetTrustedPosition(bool trusted){
        GetComponent<MeshRenderer>().material = trusted ? solidMaterial : transparentMaterial;
    }

}
