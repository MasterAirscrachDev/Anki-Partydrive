using System.Collections.Generic;
using UnityEngine;
using static OverdriveServer.NetStructures;

public class ShowFliptool : MonoBehaviour
{
    IInput iinput;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        iinput = new IInput();
        iinput.Show.Enable();
        iinput.Show.Flip.performed += ctx => TryFlip();
    }

    void TryFlip()
    {
        // Collect all AI controllers
        List<AIController> aiControllers = new List<AIController>();
        foreach (CarController controller in SR.cms.controllers)
        {
            AIController ai = controller.GetComponent<AIController>();
            if (ai != null) aiControllers.Add(ai);
        }

        if (aiControllers.Count != 4) return;

        // Build a set of car IDs already assigned to any controller (players and AI)
        HashSet<string> usedIDs = new HashSet<string>();
        foreach (CarController controller in SR.cms.controllers)
            usedIDs.Add(controller.GetDesiredCarID());

        // Find available cars that are not charging and not already assigned
        List<string> availableCars = new List<string>();
        foreach (UCarData car in SR.io.cars)
        {
            if (!car.charging && !usedIDs.Contains(car.id))
                availableCars.Add(car.id);
        }

        if (availableCars.Count != 4) return;

        // Switch each AI car to one of the available cars
        for (int i = 0; i < aiControllers.Count; i++)
            aiControllers[i].SetID(availableCars[i]);

        //disconnect all the old cars
        foreach (string id in usedIDs){
            SR.io.DisconnectCar(id);
        }
    }
}
