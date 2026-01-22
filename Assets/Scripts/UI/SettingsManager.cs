using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;

public class SettingsManager : MonoBehaviour
{
    [SerializeField] UnityEngine.Events.UnityEvent<SettingsState> onSettingsChanged;

    [Header("Volume Settings")]
    [SerializeField] Slider musicVolumeSlider;
    [SerializeField] Slider announcerVolumeSlider;
    [SerializeField] Slider sfxVolumeSlider;
    [Header("Graphics Settings")]
    [SerializeField] Toggle postProcessingToggle;
    [Header("Gameplay Settings")]
    [SerializeField] Toggle balancedBaseStatsToggle;
    [SerializeField] Toggle uniqueOverdrivePowersToggle;

    [Header("Other Settings")]
    [SerializeField] TMP_Dropdown carBalanceDropdown;


    SettingsState currentSettings = new SettingsState();
    const float settingsSaveDelay = 10f;
    float settingsSaveTimer = 0f;
    bool isSaveTimerRunning = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Initialize settings from currentSettings
        ReadUIToSettings();
    }
    public SettingsState Read() { return currentSettings; }
    void ReadUIToSettings()
    {
        bool changed = false;
        if(currentSettings.musicVolume != musicVolumeSlider.value){ currentSettings.musicVolume = musicVolumeSlider.value; changed = true; }
        if(currentSettings.announcerVolume != announcerVolumeSlider.value){ currentSettings.announcerVolume = announcerVolumeSlider.value; changed = true; }
        if(currentSettings.sfxVolume != sfxVolumeSlider.value){ currentSettings.sfxVolume = sfxVolumeSlider.value; changed = true; }
        if(currentSettings.postProcessingEnabled != postProcessingToggle.isOn){ currentSettings.postProcessingEnabled = postProcessingToggle.isOn; changed = true; }
        if(currentSettings.balancedBaseStats != balancedBaseStatsToggle.isOn){ currentSettings.balancedBaseStats = balancedBaseStatsToggle.isOn; changed = true; }
        if(currentSettings.uniqueOverdrivePowers != uniqueOverdrivePowersToggle.isOn){ currentSettings.uniqueOverdrivePowers = uniqueOverdrivePowersToggle.isOn; changed = true; }
        if(changed)
        {
            onSettingsChanged?.Invoke(currentSettings);
        }
    }
    void ApplySettingsToUI()
    {
        musicVolumeSlider.value = currentSettings.musicVolume;
        announcerVolumeSlider.value = currentSettings.announcerVolume;
        sfxVolumeSlider.value = currentSettings.sfxVolume;
        postProcessingToggle.isOn = currentSettings.postProcessingEnabled;
        balancedBaseStatsToggle.isOn = currentSettings.balancedBaseStats;
        uniqueOverdrivePowersToggle.isOn = currentSettings.uniqueOverdrivePowers;
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
        await fs.SaveFile("settings", save);
    }

    public void OnSettingsUpdated()
    {
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
        // Update currentSettings with UI values
        currentSettings.musicVolume = musicVolumeSlider.value;
        currentSettings.announcerVolume = announcerVolumeSlider.value;
        currentSettings.sfxVolume = sfxVolumeSlider.value;
        currentSettings.postProcessingEnabled = postProcessingToggle.isOn;
        currentSettings.balancedBaseStats = balancedBaseStatsToggle.isOn;
        currentSettings.uniqueOverdrivePowers = uniqueOverdrivePowersToggle.isOn;

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
            ui.SetUILayer(3); //go to track scanning UI
            ui.ToggleUILayer(4, true); //enable balancing UI
            FindFirstObjectByType<CarBalancer>().Setup(carIdStr);
        }
    }
}
public class SettingsState
{
    public float musicVolume;
    public float announcerVolume;
    public float sfxVolume;
    
    public bool postProcessingEnabled;

    public bool balancedBaseStats;
    public bool uniqueOverdrivePowers;
}