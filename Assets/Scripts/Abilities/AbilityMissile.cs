using System.Collections.Generic;
using UnityEngine;

public class AbilityMissile : MonoBehaviour
{
    [SerializeField] Transform target;
    Vector3 fixedTargetPosition;
    [SerializeField] AnimationCurve handlingCurve;
    [SerializeField] AnimationCurve speedCurve;
    float explosionRadius = 0.25f;
    float lifetime = 0;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void Setup(Transform targetTransform, float explosionRadius = 0.25f)
    {
        this.explosionRadius = explosionRadius;
        target = targetTransform;
    }
    public void Setup(Vector3 targetPosition, float explosionRadius = 0.25f)
    {
        this.explosionRadius = explosionRadius;
        fixedTargetPosition = targetPosition;
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
            List<CarController> hits = CMS.cms.GetControllersInRange(transform.position, explosionRadius);
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
