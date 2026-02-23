using System.Collections.Generic;
using UnityEngine;

public class DynamicMusic : MonoBehaviour
{
    [SerializeField] AudioSource audioSource;
    [SerializeField] List<AudioClip> musicClips;
    bool isPlaying = true;
    public void OnSettingsChanged(SettingsState settings)
    {
        if(audioSource != null)
        {
            audioSource.volume = settings.musicVolume;
            if(settings.musicVolume == 0f && isPlaying)
            {
                audioSource.Pause();
                isPlaying = false;
            }
            else if(settings.musicVolume > 0f && !isPlaying)
            {
                audioSource.UnPause();
                isPlaying = true;
            }
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        audioSource.clip = musicClips[Random.Range(0, musicClips.Count)];
        audioSource.Play();
        isPlaying = true;
    }

    // Update is called once per frame
    void Update()
    {
        if(!audioSource.isPlaying && isPlaying) // If the music has finished and we should be playing, start a new track
        {
            audioSource.clip = musicClips[Random.Range(0, musicClips.Count)];
            audioSource.Play();
        }
    }
}
