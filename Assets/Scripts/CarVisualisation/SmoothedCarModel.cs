using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmoothedCarModel : MonoBehaviour
{
    Transform followTarget;
    [SerializeField] float smoothSpeedA = 6f, smoothSpeedB = 2f;
    Vector3 smoothPositionA;
    // Start is called before the first frame update
    void Start()
    {
        followTarget = transform.parent;
        transform.parent = null;
    }

    // Update is called once per frame
    void Update()
    {
        if(followTarget == null){ Destroy(gameObject); return; }
        if(Vector3.Distance(transform.position, followTarget.position) < 0.01f){ return; }
        smoothPositionA = Vector3.Lerp(smoothPositionA, followTarget.position, smoothSpeedA * Time.deltaTime);

        transform.position = Vector3.Lerp(transform.position, smoothPositionA, smoothSpeedB * Time.deltaTime);

        transform.LookAt(smoothPositionA);
        Debug.DrawLine(followTarget.position, smoothPositionA, Color.red);
        Debug.DrawLine(transform.position, smoothPositionA, Color.green);
    }
}
