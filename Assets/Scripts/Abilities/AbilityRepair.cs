using UnityEngine;

public class AbilityRepair : MonoBehaviour
{
    void Update() { //this isnt very efficent but its less than 50 objects per frame so whatever
        //sphere check with a radius of 0.04
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 0.04f);
        foreach (var hitCollider in hitColliders)
        {
            SmoothedCarModel smoothedCarModel = hitCollider.GetComponent<SmoothedCarModel>();
            if(smoothedCarModel != null) { 
                //Debug.Log("Collision Detected with " + hitCollider.gameObject.name);
                string collectingCarID = smoothedCarModel.GetCarID();
                if(!string.IsNullOrEmpty(collectingCarID)){
                    TryRepair(collectingCarID);
                }
            }
        }
    }
    void TryRepair(string id)
    {
        //Debug.Log($"Repair ability checking car {id} for negative status effects");
        CarController car = SR.cms.GetController(id);
        if(car != null){
            bool consumed = false;
            //check each negative status effect, if any are present remove them and set consumed to true
            if(car.GetStatusEffect(CarStatus.Scrambled)){ 
                car.SetStatusEffect(CarStatus.Scrambled, -1);
                consumed = true;
            }
            if(car.GetStatusEffect(CarStatus.Meltdown)){
                car.SetStatusEffect(CarStatus.Meltdown, -1);
                consumed = true;
            }
            if(car.GetStatusEffect(CarStatus.Frozen)){
                car.SetStatusEffect(CarStatus.Frozen, -1);
                consumed = true;
            }

            if(consumed){
                Debug.Log($"Car {id} repaired by {gameObject.name}");
                Destroy(gameObject);
            }
        }
    }
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.04f);
    }
}
