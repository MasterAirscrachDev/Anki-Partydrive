using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbilityStampede : MonoBehaviour
{
    [SerializeField] Transform horns;
    [SerializeField] Vector3 hitboxSize = new Vector3(0.5f, 0.5f, 1f);
    [SerializeField] Vector3 hitboxOffset = new Vector3(0f, 0f, 0.5f);
    AbilityController ab;
    List<string> hitCars = new List<string>(); // List to track cars already hit by this stampede instance
    int damage;
    int layer;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void Setup(AbilityController ab, int damage, float duration)
    {
        horns.localScale = Vector3.zero;
        StartCoroutine(ScaleHorns());
        this.damage = damage;
        this.ab = ab;
        hitCars.Add(ab.GetCarController().GetID()); // Add the user to the hit list to prevent self-hits
        layer = LayerMask.GetMask("Cars");
        Destroy(gameObject, duration);
    }
    IEnumerator ScaleHorns()
    {
        float duration = 0.5f;
        float elapsed = 0f;
        while(elapsed < duration)
        {
            float t = elapsed / duration;
            horns.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        horns.localScale = Vector3.one;
    }

    // Update is called once per frame
    void Update()
    {
        Collider[] hits = Physics.OverlapBox(transform.position + transform.rotation * hitboxOffset, hitboxSize / 2f, transform.rotation, layer);
        foreach(Collider hit in hits)
        {
            SmoothedCarModel car = hit.GetComponent<SmoothedCarModel>();
            if(car != null )
            {
                CarController carController = SR.cms.GetController(car.GetCarID());
                if(carController != null && !hitCars.Contains(car.GetCarID()))
                {
                    carController.UseEnergy(damage);
                    ab.ReportDamage(damage);
                    carController.KnockToSide();
                    hitCars.Add(car.GetCarID());
                    Debug.Log($"Stampede hit car {car.GetCarID()} for {damage} damage");
                }
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position + transform.rotation * hitboxOffset, hitboxSize);
        
    }

}
