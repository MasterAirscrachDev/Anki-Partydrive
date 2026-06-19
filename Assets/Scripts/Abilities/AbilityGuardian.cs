using UnityEngine;

public class AbilityGuardian : MonoBehaviour
{
    [SerializeField] Transform light, cone;
    Vector3 coneTargetScale;
    float targetSize;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void Setup(string carID, float size)
    {
        GetComponent<FollowWorldCar>().Setup(carID, "GuardianLight");
        coneTargetScale = cone.localScale;
        light.localScale = Vector3.zero;
        targetSize = size;
    }

    // Update is called once per frame
    void Update()
    {
        // Smoothly scale up the light and cone to their target sizes
        light.localScale = Vector3.Lerp(light.localScale, Vector3.one * targetSize, Time.deltaTime * 4);
        cone.localScale = Vector3.Lerp(cone.localScale, coneTargetScale, Time.deltaTime * 4);
        light.Rotate(Vector3.up, 90 * Time.deltaTime * 2); // Rotate the light for a dynamic effect
    }
}
