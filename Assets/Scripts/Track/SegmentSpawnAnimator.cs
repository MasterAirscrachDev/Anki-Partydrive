using UnityEngine;

public class SegmentSpawnAnimator : MonoBehaviour
{
    Vector3 truePosition, animStart;
    Quaternion trueRotation, animStartRot;
    float animDuration = 0.5f, animTime = 0f;
    // Start is called before the first frame update
    void Start()
    {
        truePosition = transform.position;
        trueRotation = transform.rotation;

        animStart = truePosition;
        animStart.y += 6f;
        //move animStart upp to 3units on the xz plane
        animStart.x += Random.Range(-1f, 1f);
        animStart.z += Random.Range(-1f, 1f);

        transform.position = animStart;

        animStartRot = Quaternion.Euler(Random.Range(-30f, 30f), Random.Range(0f, 360f), Random.Range(-30f, 30f));

        transform.rotation = animStartRot;
    }

    // Update is called once per frame
    void Update()
    {
        animTime += Time.deltaTime / animDuration;
        if (animTime < 1f)
        {
            transform.position = Vector3.Lerp(animStart, truePosition, animTime);
            transform.rotation = Quaternion.Slerp(animStartRot, trueRotation, animTime);
        }
        else
        {
            transform.position = truePosition;
            transform.rotation = trueRotation;
            Destroy(this);
        }
    }
}
