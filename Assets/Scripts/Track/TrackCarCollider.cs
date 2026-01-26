using UnityEngine;
public class TrackCarCollider : MonoBehaviour
{
    [SerializeField] EType elementType = EType.EnergyCore;
    [SerializeField] GameObject spawnOnCollect;
    int elementSlotIndex = -1;
    public void SetElementValue(int value){
        this.elementSlotIndex = value;
    }
    public void SetElementType(EType type){
        this.elementType = type;
    }
    void Update() { //this isnt very efficent but its less than 50  objects per frame so whatever
        //sphere check with a radius of 0.04
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 0.04f);
        foreach (var hitCollider in hitColliders)
        {
            SmoothedCarModel smoothedCarModel = hitCollider.GetComponent<SmoothedCarModel>();
            if(smoothedCarModel != null)
            { 
                //Debug.Log("Collision Detected with " + hitCollider.gameObject.name);
                string collectingCarID = smoothedCarModel.GetCarID();
                if(!string.IsNullOrEmpty(collectingCarID)){
                    OnCollect(collectingCarID);
                }
            }
        }
    }
    void OnCollect(string collectingCarID)
    {
        //Debug.Log($"4.Car {collectingCarID} collected {elementType}({elementSlotIndex}) element.");
        SR.cms.CarCollectedElement(collectingCarID, elementType);
        if(elementSlotIndex >= 0){
            SR.tem.ElementCollected(elementSlotIndex); //reset this slot to allow respawn
        }
        if(spawnOnCollect != null){
            Destroy(Instantiate(spawnOnCollect, transform.position, Quaternion.identity), 3f);
        }
    }
    public enum EType
    {
        EnergyCore = 0, ItemBox = 1, SlowTrap = 2, StunTrap = 3, SpeedBoost = 4, DamageTrap = 5,
    }
}
