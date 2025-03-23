using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarEntityTracker : MonoBehaviour
{
    TrackGenerator track;
    [SerializeField] GameObject carPrefab;
    [SerializeField] List<ModelEntity> entities = new List<ModelEntity>();
    
    // Start is called before the first frame update
    void Start()
    {
        track = FindObjectOfType<TrackGenerator>();
    }
    
    public void SetPosition(string id, int trackIndex, int speed, float horizontalOffset, bool positionTrusted){
        ModelEntity entity = entities.Find(x => x.id == id);
        TrackSpline trackPiece = track.GetTrackPiece(trackIndex);
        if(trackPiece == null){ //either we are on a PreStart or an error has occured
            trackIndex++;
            if(track.GetTrackPieceType(trackIndex) == TrackPieceType.FinishLine){
                trackPiece = track.GetTrackPiece(trackIndex);
            }else{ return; }
        }
        if(entity == null){
            entity = new ModelEntity {
                id = id,
                entity = Instantiate(carPrefab, Vector3.zero, Quaternion.identity).GetComponent<CarModel>()
            };
            entities.Add(entity);
        }
        entity.entity.SetTrustedPosition(positionTrusted);
        entity.entity.SetTrackSpline(trackPiece, trackIndex);
        entity.entity.SetSpeedAndOffset(speed, horizontalOffset);
    }
    public void RemoveEntity(string id){
        ModelEntity entity = entities.Find(x => x.id == id);
        if(entity != null){
            Destroy(entity.entity.gameObject);
            entities.Remove(entity);
        }
    }
    [System.Serializable]
    class ModelEntity{
        public string id;
        public CarModel entity;
    }
}
