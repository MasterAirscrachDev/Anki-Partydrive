using UnityEngine;

public class AbilityTrailEmitter : MonoBehaviour
{
    [SerializeField] GameObject trailPrefab;
    [SerializeField] float emitterLifetime = 2.5f;
    [SerializeField] float emitDistance = 0.1f;
    [SerializeField] float hazardRange = 0.1f;
    [SerializeField] float lifetime = 3f;
    Vector3 lastEmitPosition;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        lastEmitPosition = transform.position;
        Destroy(gameObject, emitterLifetime);
    }

    // Update is called once per frame
    void Update()
    {
        if(Vector3.Distance(transform.position, lastEmitPosition) >= emitDistance){
            GameObject trail = Instantiate(trailPrefab, transform.position, transform.rotation);
            AbilityHazardZone hazard = trail.GetComponent<AbilityHazardZone>();
            hazard.Setup(hazardRange, lifetime, GetComponent<AbilityController>().GetCarController());
            lastEmitPosition = transform.position;
        }
    }
}
