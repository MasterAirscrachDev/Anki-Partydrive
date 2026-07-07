using System.Collections.Generic;
using UnityEngine;

public class AbilityMissile : MonoBehaviour, IAbilityExtras
{
    [SerializeField] Transform target;
    Vector3 fixedTargetPosition;
    [SerializeField] AnimationCurve handlingCurve;
    [SerializeField] AnimationCurve speedCurve;

    [SerializeField] Material normalMaterial, seekingMaterial;
    [SerializeField] int damage = 25;
    [SerializeField] float explosionRadius = 0.25f;
    float lifetime = 0;
    AbilityController abilityController;

    public void Setup(AbilityController ab, Transform targetTransform, float explosionRadius = 0.25f)
    {
        abilityController = ab;
        ab.SetEntityType("AbilityMissile");
        SR.sfx?.PlaySFX(SFXEvent.SeekingMissileLaunch);
        this.explosionRadius = explosionRadius;
        target = targetTransform;
        if(target == null){
            Debug.LogError("AbilityMissile Setup called with null target Transform");
            Vector2 randomCircle = Random.insideUnitCircle * 2f;
            fixedTargetPosition = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);
        }
        SetHeadMaterial(true);
    }
    public void Setup(AbilityController ab, Vector3 targetPosition, float explosionRadius = 0.25f)
    {
        abilityController = ab;
        ab.SetEntityType("AbilityMissile");
        SR.sfx?.PlaySFX(SFXEvent.MissileLaunch);
        this.explosionRadius = explosionRadius;
        fixedTargetPosition = targetPosition;
        SetHeadMaterial(false);
    }

    public void PopulateExtras(Dictionary<string, string> extras)
    {
        bool isSeeking = target != null;
        Vector3 tp = isSeeking ? target.position : fixedTargetPosition;
        extras["tx"] = tp.x.ToString("F3");
        extras["ty"] = tp.y.ToString("F3");
        extras["tz"] = tp.z.ToString("F3");
        extras["seeking"] = isSeeking ? "1" : "0";
    }

    void SetHeadMaterial(bool seeking){
        Material[] mats = GetComponent<Renderer>().materials;
        mats[1] = seeking ? seekingMaterial : normalMaterial;
        GetComponent<Renderer>().materials = mats;
    }

    void Update()
    {
        bool isSeeking = target != null;
        Debug.DrawRay(transform.position, transform.forward * 0.1f, isSeeking ? Color.bisque : Color.red, 5f);

        lifetime += Time.deltaTime;
        transform.Translate(Vector3.forward * speedCurve.Evaluate(lifetime) * Time.deltaTime);

        Vector3 targetPosition = target != null ? target.position : fixedTargetPosition;
        float flatDist = GetFlatDistanceToTarget(targetPosition);
        if(flatDist > 1f){
            targetPosition.y += 0.5f;
        }

        Vector3 directionToTarget = targetPosition - transform.position;
        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget, Vector3.forward);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, handlingCurve.Evaluate(lifetime) * Time.deltaTime);

        if(transform.position.y < 0f && lifetime > 0.2f){
            List<CarController> hits = SR.cms.SphereCheckControllers(transform.position, explosionRadius);
            foreach(CarController hit in hits){
                hit.UseEnergy(damage);
                abilityController?.ReportDamage(damage);
            }
            SR.gas.SpawnMissileParticle(transform.position, target != null);
            Destroy(gameObject);
        }
    }
    float GetFlatDistanceToTarget(Vector3 targetPosition){
        Vector3 flatMissilePos = new Vector3(transform.position.x, 0f, transform.position.z);
        Vector3 flatTargetPos = new Vector3(targetPosition.x, 0f, targetPosition.z);
        return Vector3.Distance(flatMissilePos, flatTargetPos);
    }
}
