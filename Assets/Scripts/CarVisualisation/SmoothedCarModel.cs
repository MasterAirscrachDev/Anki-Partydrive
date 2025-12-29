using UnityEngine;

public class SmoothedCarModel : MonoBehaviour
{
    Transform followTarget;
    [SerializeField] float smoothSpeedA = 8f, smoothSpeedB = 7.5f;
    Vector3 smoothPositionA;
    CarController controller;
    // Start is called before the first frame update
    void Start()
    {
        followTarget = transform.parent;
        controller = followTarget.GetComponent<CarController>();
        transform.parent = null;
    }

    // Update is called once per frame
    void Update()
    {
        if(followTarget == null){ Destroy(gameObject); return; }
        Vector3 targetPos = followTarget.position;
        Vector3 ourPos = transform.position;

        float distance = Vector3.Distance(ourPos, targetPos);
        if(distance < 0.01f){ return; }
        smoothPositionA = Vector3.Lerp(smoothPositionA, targetPos, smoothSpeedA * Time.deltaTime);

        transform.position = Vector3.Lerp(ourPos, smoothPositionA, smoothSpeedB * Time.deltaTime);

        transform.LookAt(smoothPositionA);
        Debug.DrawLine(targetPos, smoothPositionA, Color.red);
        Debug.DrawLine(ourPos, smoothPositionA, Color.green);

        if(distance > 1.2f){
            smoothPositionA = targetPos;
            transform.position = targetPos;
        }
    }
}
