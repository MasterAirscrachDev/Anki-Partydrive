using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AbilityOrbitalLazer : MonoBehaviour
{
    [SerializeField] Animation orbitalLazerAnim;
    [SerializeField] int damage = 500;
    [SerializeField] float damageDelay = 1.0f;
    Transform targetTransform;
    AbilityController abilityController;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void Setup(AbilityController ab, Transform targetModel, CarController targetCar) {
        abilityController = ab;
        targetTransform = targetModel;
        orbitalLazerAnim.Play();
        StartCoroutine(DealDamageAfterDelay(targetCar, damageDelay));
    }
    IEnumerator DealDamageAfterDelay(CarController targetCar, float delay) {
        yield return new WaitForSeconds(delay);
        targetCar.UseEnergy(damage);
        SR.sfx?.PlaySFX(SFXEvent.OrbitalLaserFire);
        // Report damage back to the ability owner
        abilityController?.ReportDamage(damage); //this can report more damage than is actually dealt, TODO: report real damage dealt
        yield return new WaitForSeconds(1.0f);
        Destroy(this.gameObject);
    }
    void LateUpdate() {
        if(targetTransform != null)
        { transform.position = targetTransform.position; }
    }

}
