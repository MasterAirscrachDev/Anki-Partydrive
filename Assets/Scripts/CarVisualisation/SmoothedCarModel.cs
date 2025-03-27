using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmoothedCarModel : MonoBehaviour
{
    Transform followTarget;
    [SerializeField] float smoothSpeed = 0.125f;
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
        transform.LookAt(followTarget);
        transform.position = Vector3.Lerp(transform.position, followTarget.position, smoothSpeed * Time.deltaTime);
    }
}
