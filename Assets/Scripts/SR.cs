using UnityEngine;

//SCENE REFERENCES

/// <summary>
/// cms(Car Management System), cet(Car Entity Tracker), gas(Global Ability System),
/// tem(Track Element Manager), ui(UI Manager), track(Track Generator), io(Car Interface), pa(Audio Announcer)
/// </summary>
public class SR : MonoBehaviour
{
    /// <summary>
    /// Central Management System
    /// </summary>
    public static CMS cms;
    /// <summary>
    /// Car Entity Tracker
    /// </summary>
    public static CarEntityTracker cet;
    /// <summary>
    /// Global Ability System
    /// </summary>
    public static GlobalAbilitySystem gas;
    /// <summary>
    /// Track Element Manager
    /// </summary>
    public static TrackElementManager tem;
    /// <summary>
    /// UI Manager
    /// </summary>
    public static UIManager ui;
    /// <summary>
    /// Track Generator and Manager
    /// </summary>
    public static TrackGenerator track;
    /// <summary>
    /// Car Interface to Overdrive Server
    /// </summary>
    public static CarInteraface io;
    /// <summary>
    /// Audio Announcer Manager for live commentary
    /// </summary>
    public static AudioAnnouncerManager pa;
    void Awake()
    {
        cms = FindFirstObjectByType<CMS>();
        cet = FindFirstObjectByType<CarEntityTracker>();
        gas = FindFirstObjectByType<GlobalAbilitySystem>();
        tem = FindFirstObjectByType<TrackElementManager>();
        ui = FindFirstObjectByType<UIManager>();
        track = FindFirstObjectByType<TrackGenerator>();
        io = FindFirstObjectByType<CarInteraface>();
        pa = FindFirstObjectByType<AudioAnnouncerManager>();

    }
}
