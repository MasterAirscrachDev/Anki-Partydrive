using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TrackElementManager : MonoBehaviour
{
    [SerializeField] GameObject energyElementPrefab, powerupElementPrefab;
    TrackElementSlot[] elementSlots;
    //index in elementSlots, spawned GameObject
    List<(int, GameObject)> spawnedElements = new List<(int, GameObject)>();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }
    public void SpawnElements(float distribution = 0.1f)
    {
        ClearElements();
        List<TrackElementSlot> slots = new List<TrackElementSlot>();
        List<GameObject> segments = SR.track.GetSegmentsWithTrackElementSlots();
        //for each segment in the track
        bool firstSegment = true;
        foreach(GameObject segment in segments)
        {
            //roll a check to see if we should spawn for this segments element slots
            float r = Random.Range(0f, 1f);
            if(firstSegment)
            { firstSegment = false; r = 0f; } //always spawn on first segment
            if(r < distribution)
            {
                //get all element slots in the segment
                TrackElementSlot[] segmentSlots = segment.GetComponentsInChildren<TrackElementSlot>();
                slots.AddRange(segmentSlots);
            }
        }
        elementSlots = slots.ToArray();
        //for each slot, spawn an element
        for(int i = 0; i < elementSlots.Length; i++)
        { SpawnElementAtSlot(i); }
    }
    void SpawnElementAtSlot(int index)
    {
        TrackElementSlot slot = elementSlots[index];
        GameObject element;
        if(slot.type == TrackElementSlot.TrackElementType.Energy) {
            element = Instantiate(energyElementPrefab, slot.transform.position, slot.transform.rotation, slot.transform);
            
        }
        else if(slot.type == TrackElementSlot.TrackElementType.Powerup) {
            element = Instantiate(powerupElementPrefab, slot.transform.position, slot.transform.rotation, slot.transform);
        }
        else{ //Any type
            float r = Random.Range(0f, 1f);
            if(r < 0.1f){//10% chance to spawn energy
                element = Instantiate(energyElementPrefab, slot.transform.position, slot.transform.rotation, slot.transform);
            }
            else {
                element = Instantiate(powerupElementPrefab, slot.transform.position, slot.transform.rotation, slot.transform);
            }
        }
        element.GetComponent<TrackCarCollider>().SetElementValue(index);
        spawnedElements.Add((index, element));
    }
    IEnumerator RespawnElementAfterDelay(int slotIndex)
    {
        yield return new WaitForSeconds(5f);
        SpawnElementAtSlot(slotIndex);
    }

    public void ElementCollected(int slotIndex)
    {
        for(int i = 0; i < spawnedElements.Count; i++)
        {
            if(spawnedElements[i].Item1 == slotIndex)
            {
                Destroy(spawnedElements[i].Item2);
                spawnedElements.RemoveAt(i);
                StartCoroutine(RespawnElementAfterDelay(slotIndex));
                return;
            }
        }
    }
    public void ClearElements()
    {
        foreach((int, GameObject) pair in spawnedElements){
            Destroy(pair.Item2);
        }
        spawnedElements.Clear();
    }
}
