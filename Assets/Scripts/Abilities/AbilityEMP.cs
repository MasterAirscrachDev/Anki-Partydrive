using System.Collections.Generic;
using UnityEngine;


public class AbilityEMP : MonoBehaviour
{
    [SerializeField] AnimationCurve sizeOverTime, fadeCurve, intensityCurve;
    [SerializeField] float activationDelay = 0.5f, maxLifetime = 1.1f;
    float lifetime = 0;
    bool activated = false;
    Material mat1, mat2;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mat1 = GetComponent<Renderer>().materials[0];
        mat2 = transform.GetChild(0).GetComponent<Renderer>().materials[0];
    }

    // Update is called once per frame
    void Update()
    {
        lifetime += Time.deltaTime;
        float size = sizeOverTime.Evaluate(lifetime);
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
        List<CarController> hits = SR.cms.SphereCheckControllers(transform.position, 0.6f);
        CarController owner = GetComponent<AbilityController>().GetCarController();
        foreach(CarController hit in hits){
            if(hit == owner) continue; //don't hit self
            hit.AddSpeedModifier(10, true, 3f, "EMP");
            hit.UseEnergy(15f); //Drain 15 energy
        }
    }
}
