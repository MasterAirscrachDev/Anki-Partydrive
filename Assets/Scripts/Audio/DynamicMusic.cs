using System.Collections.Generic;
using UnityEngine;

public class DynamicMusic : MonoBehaviour
{
    [SerializeField] AudioSource audioSource;
    [SerializeField] List<AudioClip> musicClips;
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
        audioSource.clip = musicClips[Random.Range(0, musicClips.Count)];
        audioSource.Play();
    }

    // Update is called once per frame
    void Update()
    {
        if(!audioSource.isPlaying)
        {
            audioSource.clip = musicClips[Random.Range(0, musicClips.Count)];
            audioSource.Play();
        }
    }
}
