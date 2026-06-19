using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCardmanager : MonoBehaviour
{
    [SerializeField] GameObject cardPrefab;
    [SerializeField] Transform cardParent;
    List<GameObject> cards = new List<GameObject>();
    public void UpdateCardCount(){
        List<CarController> controllersToShow = new List<CarController>();
        
        // In demo mode, only show AI controllers (hide players)
        bool isDemoMode = false;
        if(SR.cms != null) {
            isDemoMode = SR.cms.DEMO_MODE_ENABLED; // Runtime check
        }
        
        if(isDemoMode && SR.cms.controllers.Count > 0) {
            // Demo mode: only AIs get PlayerCards
            foreach(CarController controller in SR.cms.controllers){
                if(controller != null && controller.GetComponent<AIController>() != null){
                    controllersToShow.Add(controller);
                }
            }
        } else {
            // Normal mode: all controllers (players and AIs) get PlayerCards
            foreach(CarController controller in SR.cms.controllers ?? new List<CarController>()){
                if(controller != null){
                    controllersToShow.Add(controller);
                }
            }
        }
        
        int count = controllersToShow.Count;
        
        //clear all cards
        foreach(Transform child in cardParent){ Destroy(child.gameObject); }
        cards.Clear();
        //create new cards centered with
        int offset = -10;
        const int spacing = 160;
        for(int i = 0; i < count; i++){
            GameObject card = Instantiate(cardPrefab, cardParent);
            card.transform.localPosition = new Vector3(10, offset, 0);
            cards.Add(card);
            
            // Use the pre-collected controller from controllersToShow list
            CarController controllerToUse = controllersToShow[i];
            if(controllerToUse != null){
                controllerToUse.SetCard(card.GetComponent<PlayerCardSystem>());
            }

            offset -= spacing;
        }
    }
}
