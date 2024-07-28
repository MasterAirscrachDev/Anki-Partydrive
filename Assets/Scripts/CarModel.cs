using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarModel : MonoBehaviour
{
    [SerializeField] float lerpSpeed = 0.01f;
    [SerializeField] Material solidMaterial, transparentMaterial;
    TrackSpline trackSpline;
    int speed = 0;
    float t = 0;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        t += (lerpSpeed * (speed / 1000f)) * Time.deltaTime;
        if(trackSpline != null){
            transform.position = trackSpline.GetPoint(t);
        }
    }
    public void SetTrackSpline(TrackSpline trackSpline){
        if(trackSpline != this.trackSpline){ //only update if the track spline is different
            this.trackSpline = trackSpline;
            t = 0;
        }
    }
    public void SetSpeedAndOffset(int speed, float offset){
        this.speed = speed;
    }
    public void SetTrustedPosition(bool trusted){
        GetComponent<MeshRenderer>().material = trusted ? solidMaterial : transparentMaterial;
    }

}
