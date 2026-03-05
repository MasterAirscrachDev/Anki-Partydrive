using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCardmanager : MonoBehaviour
{
    [SerializeField] GameObject cardPrefab;
    [SerializeField] Transform cardParent;
    List<GameObject> cards = new List<GameObject>();
    public void UpdateCardCount(){
        int count = SR.cms.controllers.Count;
        //clear all cards
        foreach(Transform child in cardParent){ Destroy(child.gameObject); }
        cards.Clear();
        //create new cards cented with
        int offset = -10;
        const int spacing = 160;
        for(int i = 0; i < count; i++){
            GameObject card = Instantiate(cardPrefab, cardParent);
            card.transform.localPosition = new Vector3(10, offset, 0);
            cards.Add(card);
            SR.cms.controllers[i].SetCard(card.GetComponent<PlayerCardSystem>());

            offset -= spacing;
        }
    }
}
