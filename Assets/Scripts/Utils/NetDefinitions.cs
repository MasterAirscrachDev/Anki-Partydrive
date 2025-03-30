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
public class UCarData{ //mirron class because unity cannot serialize CarData
    public string name;
    public string id;
    public int trackPosition;
    public int trackID;
    public float laneOffset;
    public int speed;
    public int battery;
    public bool charging;
    public UCarData(CarData carData){
        this.name = carData.name;
        this.id = carData.id;
        this.trackPosition = carData.trackPosition;
        this.trackID = carData.trackID;
        this.laneOffset = carData.laneOffset;
        this.speed = carData.speed;
        this.battery = carData.battery;
        this.charging = carData.charging;
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
    public const string EVENT_CAR_DELOCALIZED = "car_delocalized";
    public const string EVENT_CAR_TRACKING_UPDATE = "car_tracking_update";
    public const string EVENT_CAR_MOVE = "car_move_update";
    public const string EVENT_REFRESH_CONFIGS = "refresh_configs";
}