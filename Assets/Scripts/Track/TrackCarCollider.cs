using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class TrackCarCollider : MonoBehaviour
{
    [SerializeField] EType elementType = EType.EnergyCore;
    int elementSlotIndex = -1;
    public void SetElementValue(int value){
        this.elementSlotIndex = value;
    }
    public void SetElementType(EType type){
        this.elementType = type;
    }

    void OnTriggerEnter(Collider other){
        Debug.Log("Trigger Detected with " + other.gameObject.name);
    }
    void OnCollisionEnter(Collision collision){
        Debug.Log("Collision Detected with " + collision.gameObject.name);
    }
    void OnCollect(string collectingCarID)
    {
        if(elementSlotIndex >= 0){
            TrackElementManager.tem.ElementCollected(elementSlotIndex); //reset this slot to allow respawn
        }
    }
    public enum EType
    {
        EnergyCore = 0, PowerupBox = 1, SlowTrap = 2, StunTrap = 3, SpeedBoost = 4, DamageTrap = 5,
    }
}
