using System.Collections;
using UnityEngine;

public class ShowMorter : MonoBehaviour
{
    [SerializeField] float range = 2f;
    IInput input;
    
    // Charging system variables
    float charge = 0f;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        input = new IInput();
        input.Show.Enable();
        input.Show.Fire.performed += ctx => HandleFirePress();
        StartCoroutine(FiringLoop());
    }
    IEnumerator FiringLoop(){
        while (true)
        {
            if (charge > 0f)
            {
                Fire();
                charge -= 0.5f; // Decrease charge by 0.5 seconds after firing
                yield return new WaitForSeconds(0.5f); // Wait for 0.5 seconds before the next fire
            }else{
                yield return new WaitForSeconds(0.1f); // Check every 0.1 seconds
            }
        }
    }
    
    public void HandleFirePress()
    {
        if (charge <= 0f) { charge = 1.6f; } // First press starts charging with initial charge time
        else { charge += 0.51f; } // Subsequent presses add more charge time
        charge = Mathf.Min(charge, 3f); // Cap the charge time to prevent excessive charging
    }
    
    public void Fire()
    {
        //get all cars in range and show morter on them
        CarController[] cars = SR.cms.SphereCheckControllers(transform.position, range).ToArray();
        //pick a random car id
        if (cars.Length == 0) return;
        int randomIndex = Random.Range(0, cars.Length);
        CarController targetCar = cars[randomIndex];
        SR.gas.SpawnSeekingMissileFromPosition(transform.position + new Vector3(0,0.1f,0), targetCar.GetID());
        transform.localScale = new Vector3(1.1f, 0.7f, 1.1f); // Bounce effect on fire
    }
    
    void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one, Time.deltaTime * 5f);
    }
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
