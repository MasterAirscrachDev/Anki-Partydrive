using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class GameSetupUIController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] List<ModeCatagory> modeCategories;
    const string DIVIDER = "--------------------------------------------------";
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Wire up select/hover listeners for every mode in every category
        foreach (var category in modeCategories)
        {
            if (category.explainText == null)
            {
                Debug.LogWarning($"GameSetupUI: Category '{category.name}' has no explainText assigned!");
            }
            foreach (var mode in category.modes)
            {
                if (mode.button == null)
                {
                    Debug.LogWarning($"GameSetupUI: A mode in category '{category.name}' has no button assigned!");
                    continue;
                }
                ModeOption capturedMode = mode;
                ModeCatagory capturedCategory = category;
                
                // Add EventTrigger for pointer enter (mouse hover)
                EventTrigger trigger = mode.button.gameObject.GetComponent<EventTrigger>();
                if (trigger == null) trigger = mode.button.gameObject.AddComponent<EventTrigger>();
                
                // Add EventTrigger for select (keyboard/gamepad navigation)
                EventTrigger.Entry select = new EventTrigger.Entry { eventID = EventTriggerType.Select };
                select.callback.AddListener((_) => OnModeSelected(capturedCategory, capturedMode));
                trigger.triggers.Add(select);
            }
        }
    }
    
    void OnModeSelected(ModeCatagory selectedCategory, ModeOption selectedMode)
    {
        //Debug.Log($"GameSetupUI: Mode '{selectedMode.explainMesssage}' selected in category '{selectedCategory.name}'.");
        foreach (var category in modeCategories)
        {
            if (category == selectedCategory)
                category.explainText.text = selectedMode.explainMesssage;
            else
                category.explainText.text = DIVIDER;
        }
    }

    [System.Serializable]
    class ModeCatagory
    {
        public string name;
        public TMP_Text explainText;
        public List<ModeOption> modes;
    }
    [System.Serializable]
    public class ModeOption
    {
        public Button button;
        public string explainMesssage;
    }
}
