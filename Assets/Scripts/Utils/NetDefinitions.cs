using UnityEngine;
[System.Serializable]
public class CarData{
    public string name {get; set;}
    public string id {get; set;}
    public int trackPosition {get; set;}
    public int trackID {get; set;}
    public float laneOffset {get; set;}
    public int speed {get; set;}
    public int battery {get; set;}
    public bool charging {get; set;}
    public CarData(string name, string id){
        this.name = name;
        this.id = id;
    }
}
[System.Serializable]
public class TransitionData {
    public string carID {get; set;}
    public int trackPiece {get; set;}
    public int oldTrackPiece {get; set;}           
    public float offset {get; set;}
    public int uphillCounter {get; set;}
    public int downhillCounter {get; set;}
    public int leftWheelDistance {get; set;}
    public int rightWheelDistance {get; set;}
    public bool crossedStartingLine {get; set;}
}
[System.Serializable]
public class LocationData {
    public string carID {get; set;}
    public int trackLocation {get; set;}
    public int trackID {get; set;}
    public float offset {get; set;}
    public int speed {get; set;}
    public bool clockwise {get; set;}
}
[System.Serializable]
public class CarLocationData {
    public string carID {get; set;}
    public int trackIndex {get; set;}
    public int speed {get; set;}
    public float offset {get; set;}
    public bool positionTrusted {get; set;}
}
public static class NetDefinitions{
    // Define event types for webhooks
    public const string EVENT_SYSTEM_LOG = "system_log";
    public const string EVENT_UTILITY_LOG = "utility_log";
    public const string EVENT_CAR_LOCATION = "car_location";
    public const string EVENT_CAR_TRANSITION = "car_transition";
    public const string EVENT_CAR_FELL = "car_fell";
    public const string EVENT_CAR_TRACKING_UPDATE = "car_tracking_update";
}