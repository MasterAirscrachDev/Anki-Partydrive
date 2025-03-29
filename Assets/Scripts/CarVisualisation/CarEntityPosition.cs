using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarEntityPosition : MonoBehaviour
{
    [SerializeField] float speedTuning = 0.0019f;
    [SerializeField] Material solidMaterial, transparentMaterial;
    public CarModelManager carModelManager;
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

        //if not the editor, destroy the mesh renderer
        if(!Application.isEditor){
            Destroy(GetComponent<MeshRenderer>());
            Destroy(GetComponent<MeshFilter>());
        }
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
            transform.position = new Vector3(0, -100, 0); //move the car out of sight
            transform.rotation = Quaternion.identity; //reset rotation
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
        carModelManager.SetHolo(!trusted);
    }
    public void Delocalise(){
        trackPieceProgression = 0;
        trackPieceLength = 0;
        trackSpline = null;
        speed = 0;
        shift = 0;
        transform.position = new Vector3(0, -100, 0); //move the car out of sight
        transform.rotation = Quaternion.identity; //reset rotation  
    }
}
