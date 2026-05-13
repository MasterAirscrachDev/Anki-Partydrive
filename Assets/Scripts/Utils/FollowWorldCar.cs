using UnityEngine;

public class FollowWorldCar : MonoBehaviour
{
    string carID;
    string tag;
    Transform carTransform;
    public void Setup(string carID, string tag = null)
    {
        this.carID = carID;
        this.tag = tag;
        carTransform = SR.cet.GetCarVisualTransform(carID);
    }

    // Update is called once per frame
    void Update()
    {
        if(carTransform != null)
        {
            transform.position = carTransform.position;
            transform.rotation = carTransform.rotation;
        }
        else
        {
            //try getting the transform again in case it was not available at setup
            carTransform = SR.cet.GetCarVisualTransform(carID);
            transform.position = new Vector3(0, -50, 0); //move it out of the way until we can find the car
        }
    }
    public void DestoryIfForCar(string carID, string tag)
    {
        if(this.carID == carID && (this.tag == null || this.tag == tag))
        {
            Destroy(gameObject);
        }
    }
}
