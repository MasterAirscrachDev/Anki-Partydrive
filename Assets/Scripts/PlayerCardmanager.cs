using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCardmanager : MonoBehaviour
{
    [SerializeField] GameObject cardPrefab;
    [SerializeField] Transform cardParent;
    // Start is called before the first frame update
    void Start()
    {
        
    }
    public void UpdateCardCount(){
        int count = FindObjectsOfType<CarController>().Length;
        //clear all cards
        foreach(Transform child in cardParent){
            Destroy(child.gameObject);
        }
        //create new cards cented wish 250 spacing
        int spacing = 240;
        float startX = -spacing * (count - 1) / 2;
        for(int i = 0; i < count; i++){
            GameObject card = Instantiate(cardPrefab, cardParent);
            card.transform.localPosition = new Vector3(startX + spacing * i, 0, 0);

        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
