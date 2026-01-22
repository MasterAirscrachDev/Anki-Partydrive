using System.Collections.Generic;
using UnityEngine;

public class AbilityMissile : MonoBehaviour
{
    [SerializeField] Transform target;
    Vector3 fixedTargetPosition;
    [SerializeField] AnimationCurve handlingCurve;
    [SerializeField] AnimationCurve speedCurve;

    [SerializeField] Material normalMaterial, seekingMaterial;
    float explosionRadius = 0.25f;
    float lifetime = 0;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void Setup(Transform targetTransform, float explosionRadius = 0.25f)
    {
        this.explosionRadius = explosionRadius;
        target = targetTransform;
        if(target == null){
            Debug.LogError("AbilityMissile Setup called with null target Transform");
            //pick a random point in a flat 2unit radius circle around current position
            Vector2 randomCircle = Random.insideUnitCircle * 2f;
            fixedTargetPosition = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);
        }
        SetHeadMaterial(true);
    }
    public void Setup(Vector3 targetPosition, float explosionRadius = 0.25f)
    {
        this.explosionRadius = explosionRadius;
        fixedTargetPosition = targetPosition;
        SetHeadMaterial(false);
    }
    void SetHeadMaterial(bool seeking){
        Material[] mats = GetComponent<Renderer>().materials;
        mats[1] = seeking ? seekingMaterial : normalMaterial;
        GetComponent<Renderer>().materials = mats;
    }

    // Update is called once per frame
    void Update()
    {
        lifetime += Time.deltaTime;
        transform.Translate(Vector3.forward * speedCurve.Evaluate(lifetime) * Time.deltaTime);

        Vector3 targetPosition = target != null ? target.position : fixedTargetPosition;

        float flatDist = GetFlatDistanceToTarget(targetPosition);
        if(flatDist > 1f){ //we are not close enough to dive
            targetPosition.y += 0.5f; //aim slightly above the target to create a diving arc
        }

        //rotate towards target
        Vector3 directionToTarget = targetPosition - transform.position;
        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget, Vector3.forward);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, handlingCurve.Evaluate(lifetime) * Time.deltaTime);

        if(transform.position.y < 0f){
            List<CarController> hits = SR.cms.SphereCheckControllers(transform.position, explosionRadius);
            foreach(CarController hit in hits){
                hit.UseEnergy(20f); //Deal 20 energy damage
            }
            Destroy(gameObject);
        }
    }
    float GetFlatDistanceToTarget(Vector3 targetPosition){
        Vector3 flatMissilePos = new Vector3(transform.position.x, 0f, transform.position.z);
        Vector3 flatTargetPos = new Vector3(targetPosition.x, 0f, targetPosition.z);
        return Vector3.Distance(flatMissilePos, flatTargetPos);
    }
}
