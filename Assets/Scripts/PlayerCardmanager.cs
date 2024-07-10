using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCardmanager : MonoBehaviour
{
    [SerializeField] GameObject cardPrefab;
    [SerializeField] Transform cardParent;
    CMS cms;
    List<GameObject> cards = new List<GameObject>();
    void Start()
    {
        cms = FindObjectOfType<CMS>();
    }
    public void UpdateCardCount(){
        int count = cms.controllers.Count;
        //clear all cards
        foreach(Transform child in cardParent){
            Destroy(child.gameObject);
        }
        cards.Clear();
        //create new cards cented wish 250 spacing
        int spacing = 240;
        float startX = -spacing * (count - 1) / 2;
        for(int i = 0; i < count; i++){
            GameObject card = Instantiate(cardPrefab, cardParent);
            card.transform.localPosition = new Vector3(startX + spacing * i, 0, 0);
            cards.Add(card);
            cms.controllers[i].SetCard(card.GetComponent<PlayerCardSystem>());
        }
    }
}
