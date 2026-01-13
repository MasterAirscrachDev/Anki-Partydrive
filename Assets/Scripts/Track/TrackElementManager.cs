using UnityEngine;
using System.Collections.Generic;

public class TrackElementManager : MonoBehaviour
{
    [SerializeField] GameObject energyElementPrefab, powerupElementPrefab;
    public static TrackElementManager tem;
    TrackGenerator trackGenerator;
    TrackElementSlot[] elementSlots;
    //index in elementSlots, spawned GameObject
    List<(int, GameObject)> spawnedElements = new List<(int, GameObject)>();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        trackGenerator = TrackGenerator.track;
        tem = this;
    }
    public void SpawnElements(float distribution = 0.8f)
    {
        ClearElements();
        List<TrackElementSlot> slots = new List<TrackElementSlot>();
        List<GameObject> segments = trackGenerator.GetSegmentsWithTrackElementSlots();
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
        {
            if(elementSlots[i].type == TrackElementSlot.TrackElementType.Energy)
            {
                GameObject element = Instantiate(energyElementPrefab, elementSlots[i].transform.position, elementSlots[i].transform.rotation, elementSlots[i].transform);
                spawnedElements.Add((i, element));
            }
            else if(elementSlots[i].type == TrackElementSlot.TrackElementType.Powerup)
            {
                GameObject element = Instantiate(powerupElementPrefab, elementSlots[i].transform.position, elementSlots[i].transform.rotation, elementSlots[i].transform);
                spawnedElements.Add((i, element));
            }
            else //Any type
            {
                float r = Random.Range(0f, 1f);
                if(r < 0.1f)//10% chance to spawn energy
                {
                    GameObject element = Instantiate(energyElementPrefab, elementSlots[i].transform.position, elementSlots[i].transform.rotation, elementSlots[i].transform);
                    spawnedElements.Add((i, element));
                }
                else
                {
                    GameObject element = Instantiate(powerupElementPrefab, elementSlots[i].transform.position, elementSlots[i].transform.rotation, elementSlots[i].transform);
                    spawnedElements.Add((i, element));
                }
            }
        }
    }

    public void ElementCollected(int slotIndex)
    {
        for(int i = 0; i < spawnedElements.Count; i++)
        {
            if(spawnedElements[i].Item1 == slotIndex)
            {
                Destroy(spawnedElements[i].Item2);
                spawnedElements.RemoveAt(i);
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
