using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering.PostProcessing;

public class SettingsManager : MonoBehaviour
{
    [SerializeField] UnityEngine.Events.UnityEvent<SettingsState> onSettingsChanged;

    [Header("Volume Settings")]
    [SerializeField] Slider musicVolumeSlider;
    [SerializeField] Slider announcerVolumeSlider;
    [SerializeField] Slider sfxVolumeSlider;
    [Header("Graphics Settings")]
    [SerializeField] Toggle postProcessingToggle;
    [SerializeField] PostProcessVolume postProcessVolume;
    [Header("Gameplay Settings")]
    [SerializeField] Toggle balancedBaseStatsToggle;
    [SerializeField] Toggle uniqueOverdrivePowersToggle;

    [Header("Other Settings")]
    [SerializeField] TMP_Dropdown carBalanceDropdown;
    [SerializeField] Toggle phoneControllerSupportToggle;


    SettingsState currentSettings = new SettingsState();
    bool isApplyingSettings = false;
    const float settingsSaveDelay = 10f;
    float settingsSaveTimer = 0f;
    bool isSaveTimerRunning = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        LoadSettingsAsync();
    }
    public SettingsState Read() { return currentSettings; }
    void ReadUIToSettings()
    {
        bool changed = false;
        if(currentSettings.musicVolume != musicVolumeSlider.value){ currentSettings.musicVolume = musicVolumeSlider.value; changed = true; }
        if(currentSettings.announcerVolume != announcerVolumeSlider.value){ currentSettings.announcerVolume = announcerVolumeSlider.value; changed = true; }
        if(currentSettings.sfxVolume != sfxVolumeSlider.value){ currentSettings.sfxVolume = sfxVolumeSlider.value; changed = true; }
        if(currentSettings.postProcessingEnabled != postProcessingToggle.isOn){ 
            currentSettings.postProcessingEnabled = postProcessingToggle.isOn; 
            changed = true;
            ApplyPostProcessing();
        }
        if(currentSettings.balancedBaseStats != balancedBaseStatsToggle.isOn){ currentSettings.balancedBaseStats = balancedBaseStatsToggle.isOn; changed = true; }
        if(currentSettings.uniqueOverdrivePowers != uniqueOverdrivePowersToggle.isOn){ currentSettings.uniqueOverdrivePowers = uniqueOverdrivePowersToggle.isOn; changed = true; }
        if(currentSettings.phoneControllerSupport != phoneControllerSupportToggle.isOn){ 
            currentSettings.phoneControllerSupport = phoneControllerSupportToggle.isOn;
            changed = true;
            _ = SaveAndQuit(); // Changing phone controller support requires restart
        }
        if(changed)
        {
            onSettingsChanged?.Invoke(currentSettings);
        }
    }
    
    void ApplyPostProcessing()
    {
        if(postProcessVolume != null)
        {
            postProcessVolume.enabled = currentSettings.postProcessingEnabled;
        }
    }
    
    void ApplySettingsToUI()
    {
        isApplyingSettings = true;
        musicVolumeSlider.value = currentSettings.musicVolume;
        announcerVolumeSlider.value = currentSettings.announcerVolume;
        sfxVolumeSlider.value = currentSettings.sfxVolume;
        postProcessingToggle.isOn = currentSettings.postProcessingEnabled;
        balancedBaseStatsToggle.isOn = currentSettings.balancedBaseStats;
        uniqueOverdrivePowersToggle.isOn = currentSettings.uniqueOverdrivePowers;
        phoneControllerSupportToggle.isOn = currentSettings.phoneControllerSupport;
        //Debug.Log($"Settings loaded.\n{currentSettings.Log()}");
        isApplyingSettings = false;
        ApplyPostProcessing();
        onSettingsChanged?.Invoke(currentSettings);
    }
    async Task LoadSettingsAsync()
    {
        FileSuper fs = new FileSuper("Partydrive", "ReplayStudios");
        Save save = await fs.LoadFile("settings");
        if(save != null)
        {
            currentSettings.musicVolume = save.GetVar("musicVolume", currentSettings.musicVolume);
            currentSettings.announcerVolume = save.GetVar("announcerVolume", currentSettings.announcerVolume);
            currentSettings.sfxVolume = save.GetVar("sfxVolume", currentSettings.sfxVolume);
            currentSettings.postProcessingEnabled = save.GetVar("postProcessingEnabled", currentSettings.postProcessingEnabled);
            currentSettings.balancedBaseStats = save.GetVar("balancedBaseStats", currentSettings.balancedBaseStats);
            currentSettings.uniqueOverdrivePowers = save.GetVar("uniqueOverdrivePowers", currentSettings.uniqueOverdrivePowers);
            currentSettings.phoneControllerSupport = save.GetVar("phoneControllerSupport", currentSettings.phoneControllerSupport);
            if (currentSettings.phoneControllerSupport)
            { FindFirstObjectByType<RemoteControlLink>().Activate(); }
            // Apply loaded settings to UI
            ApplySettingsToUI();
        }
    }
    async Task SaveSettingsAsync()
    {
        FileSuper fs = new FileSuper("Partydrive", "ReplayStudios");
        Save save = new Save();
        save.SetVar("musicVolume", currentSettings.musicVolume);
        save.SetVar("announcerVolume", currentSettings.announcerVolume);
        save.SetVar("sfxVolume", currentSettings.sfxVolume);
        save.SetVar("postProcessingEnabled", currentSettings.postProcessingEnabled);
        save.SetVar("balancedBaseStats", currentSettings.balancedBaseStats);
        save.SetVar("uniqueOverdrivePowers", currentSettings.uniqueOverdrivePowers);
        save.SetVar("phoneControllerSupport", currentSettings.phoneControllerSupport);
        await fs.SaveFile("settings", save);
        Debug.Log($"Settings saved.\n{currentSettings.Log()}");
    }

    async Task SaveAndQuit()
    {
        await SaveSettingsAsync();
        Application.Quit();
    }

    public void OnSettingsUpdated()
    {
        if (isApplyingSettings) return;
        settingsSaveTimer = settingsSaveDelay;
        if(!isSaveTimerRunning)
        {
            StartCoroutine(SaveSettingsWithDelay());
        }
        ReadUIToSettings();
    }

    IEnumerator SaveSettingsWithDelay()
    {
        isSaveTimerRunning = true;
        while(settingsSaveTimer > 0f)
        {
            settingsSaveTimer -= Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }
        SaveSettingsAsync();
        isSaveTimerRunning = false;
    }
    public void OnSettingsRendered()
    {
        CarInteraface carInterface = SR.io;
        if(carInterface != null)
        {
            carBalanceDropdown.ClearOptions();
            List<TMP_Dropdown.OptionData> balanceOptions = new List<TMP_Dropdown.OptionData>();
            if(carInterface.cars == null || carInterface.cars.Length == 0)
            { balanceOptions.Add(new TMP_Dropdown.OptionData("NoCarConnected")); }
            else {
                foreach(var car in carInterface.cars)
                { balanceOptions.Add(new TMP_Dropdown.OptionData($"{car.name}                             :-){car.id}")); }
            }
            carBalanceDropdown.AddOptions(balanceOptions);
        }
    }
    public void OnBalanceRequested()
    {
        //get the selected car id from the dropdown
        string selectedOption = carBalanceDropdown.options[carBalanceDropdown.value].text;
        int separatorIndex = selectedOption.LastIndexOf(":-)");
        if(separatorIndex >= 0)
        {
            string carIdStr = selectedOption.Substring(separatorIndex + 3);
            UIManager ui = SR.ui;
            ui.SetUILayer("TrackScanning"); //go to track scanning UI
            ui.ToggleUILayer("CarBalance", true); //enable balancing UI
            FindFirstObjectByType<CarBalancer>().Setup(carIdStr);
        }
    }
}
public class SettingsState
{
    public float musicVolume = 0.15f;
    public float announcerVolume = 1f;
    public float sfxVolume = 0.757f;
    
    public bool postProcessingEnabled = true;

    public bool balancedBaseStats = true;
    public bool uniqueOverdrivePowers = true;

    public bool phoneControllerSupport = false;
    public string Log() {
        return @$"SettingsState:
MusicVolume: {musicVolume}
AnnouncerVolume: {announcerVolume}
SFXVolume: {sfxVolume}
PostProcessingEnabled: {postProcessingEnabled}
BalancedBaseStats: {balancedBaseStats}
UniqueOverdrivePowers: {uniqueOverdrivePowers}
PhoneControllerSupport: {phoneControllerSupport}";
    }
}