using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AbilityOrbitalLazer : MonoBehaviour
{
    [SerializeField] Animation orbitalLazerAnim;
    [SerializeField] int damage = 500;
    [SerializeField] float damageDelay = 1.0f;
    Transform targetTransform;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void Setup(Transform targetModel, CarController targetCar)
    {
        targetTransform = targetModel;
        orbitalLazerAnim.Play();
        StartCoroutine(DealDamageAfterDelay(targetCar, damageDelay));
    }
    IEnumerator DealDamageAfterDelay(CarController targetCar, float delay)
    {
        yield return new WaitForSeconds(delay);
        targetCar.UseEnergy(damage);
        yield return new WaitForSeconds(1.0f);
        Destroy(this.gameObject);
    }
    void LateUpdate()
    {
        transform.position = targetTransform.position;
    }

}
