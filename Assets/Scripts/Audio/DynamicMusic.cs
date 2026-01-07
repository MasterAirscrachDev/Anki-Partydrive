using UnityEngine;

public class DynamicMusic : MonoBehaviour
{
    [SerializeField] AudioSource audioSource;
    public void OnSettingsChanged(SettingsState settings)
    {
        if(audioSource != null)
        {
            audioSource.volume = settings.musicVolume;
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
