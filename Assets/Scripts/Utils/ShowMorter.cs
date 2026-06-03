using UnityEngine;

public class ShowMorter : MonoBehaviour
{
    [SerializeField] float range = 2f;
    IInput input;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        input = new IInput();
        input.Show.Enable();
        input.Show.Fire.performed += ctx => Fire();
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
        transform.localScale = new Vector3(1f, 0.5f, 1f);
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
