using System.Collections.Generic;
using UnityEngine;


public class AbilityEMP : MonoBehaviour
{
    [SerializeField] AnimationCurve sizeOverTime, fadeCurve, intensityCurve;
    [SerializeField] float activationDelay = 0.5f, maxLifetime = 1.1f, hitRadius = 1.1f;
    [SerializeField] float RangeScale = 1f; // Scale factor for the hit radius to allow for easy adjustments without changing the particle effect
    float lifetime = 0;
    bool activated = false;

    int damage = 10; // Amount of energy to drain and damage to report to the ability owner
    float slowAmount = 0.8f; // Percentage to slow targets by (0.8 means 20% speed reduction)
    float slowDuration = 3f; // Duration of the speed reduction
    float scrambleDuration = 4f; // Duration of the control scrambling


    Material mat1, mat2;
    AbilityController abilityController;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void Setup(AbilityController ab, int damage = 10, float slowAmount = 0.8f, float slowDuration = 3f, float scrambleDuration = 4f, float rangeScale = 1f)
    {
        abilityController = ab;
        mat1 = GetComponent<Renderer>().materials[0];
        mat2 = transform.GetChild(0).GetComponent<Renderer>().materials[0];
        this.damage = damage;
        this.slowAmount = slowAmount;
        this.slowDuration = slowDuration;
        this.scrambleDuration = scrambleDuration;
        this.RangeScale = rangeScale;
    }

    // Update is called once per frame
    void Update()
    {
        lifetime += Time.deltaTime;
        float size = sizeOverTime.Evaluate(lifetime) * RangeScale;
        transform.localScale = new Vector3(size, size, size);
        float alpha = fadeCurve.Evaluate(lifetime);
        mat1.SetFloat("_EdgeSoftness", alpha);
        float intensity = intensityCurve.Evaluate(lifetime);
        mat2.SetFloat("_Intensity", intensity);
        if(!activated && lifetime >= activationDelay){
            activated = true;
            ActivateEMP();
        }
        if(lifetime >= maxLifetime){
            Destroy(gameObject);
        }
    }
    void ActivateEMP(){
        List<CarController> hits = SR.cms.SphereCheckControllers(transform.position, hitRadius * RangeScale);
        CarController owner = abilityController.GetCarController();
        foreach(CarController hit in hits){
            if(hit == owner) continue; //don't hit self
            hit.AddSpeedModifier(new PercentSpeedModifier(slowAmount, slowDuration, "EMP"));
            hit.SetStatusEffect(CarStatus.Scrambled, scrambleDuration); // Scramble controls for 3 seconds
            hit.UseEnergy(damage); //Drain 10 energy
            // Report damage back to the ability owner
            abilityController?.ReportDamage(damage);
        }
    }
    void OnDestroy(){
        // Clean up materials to prevent memory leaks
        if(mat1 != null) Destroy(mat1);
        if(mat2 != null) Destroy(mat2);
    }
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, hitRadius * RangeScale); // Scale the hit radius by the current size of the EMP effect
    }
}
