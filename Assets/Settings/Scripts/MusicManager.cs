using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MusicManager : MonoBehaviour
{
    [System.Serializable]
    public class MusicTrack
    {
        public AudioClip trackA;
        public AudioClip trackB;
        public string trackName;
    }

    [Header("Music Settings")]
    [SerializeField] private List<MusicTrack> musicTracks = new List<MusicTrack>();
    [SerializeField] private float fadeTime = 2f;
    [SerializeField] private int loopsPerTrack = 6;
    [SerializeField] private float volume = 1f;

    private AudioSource sourceA;
    private AudioSource sourceB;
    private int currentTrackIndex = 0;
    private int currentLoopCount = 0;
    private bool isTransitioning = false;

    private void Awake()
    {
        // Create two audio sources for seamless transitions
        sourceA = gameObject.AddComponent<AudioSource>();
        sourceB = gameObject.AddComponent<AudioSource>();

        sourceA.volume = volume;
        sourceB.volume = volume;
        sourceA.loop = false;
        sourceB.loop = true;
    }

    private void Start()
    {
        if (musicTracks.Count > 0)
        {
            PlayCurrentTrack();
        }
    }

    private void PlayCurrentTrack()
    {
        if (currentTrackIndex >= musicTracks.Count) return;

        MusicTrack track = musicTracks[currentTrackIndex];
        
        // Play track A first
        sourceA.clip = track.trackA;
        sourceA.Play();

        // Schedule track B to play when track A ends
        double scheduledTime = AudioSettings.dspTime + track.trackA.length;
        sourceB.clip = track.trackB;
        sourceB.PlayScheduled(scheduledTime);

        // Start monitoring for track completion
        StartCoroutine(MonitorTrackCompletion());
    }

    private IEnumerator MonitorTrackCompletion()
    {
        while (true)
        {
            // Wait for track B to complete one loop
            yield return new WaitForSeconds(musicTracks[currentTrackIndex].trackB.length);

            currentLoopCount++;
            
            // If we've reached the desired number of loops, transition to next track
            if (currentLoopCount >= loopsPerTrack)
            {
                currentLoopCount = 0;
                currentTrackIndex = (currentTrackIndex + 1) % musicTracks.Count;
                StartCoroutine(TransitionToNextTrack());
                break;
            }
        }
    }

    private IEnumerator TransitionToNextTrack()
    {
        if (isTransitioning) yield break;
        isTransitioning = true;

        // Fade out current tracks
        float startVolume = sourceA.volume;
        float timer = 0;

        while (timer < fadeTime)
        {
            timer += Time.deltaTime;
            float newVolume = Mathf.Lerp(startVolume, 0, timer / fadeTime);
            sourceA.volume = newVolume;
            sourceB.volume = newVolume;
            yield return null;
        }

        // Stop current tracks
        sourceA.Stop();
        sourceB.Stop();

        // Reset volumes
        sourceA.volume = volume;
        sourceB.volume = volume;

        // Play next track
        PlayCurrentTrack();
        isTransitioning = false;
    }

    // Call this method to manually change the volume
    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);
        sourceA.volume = volume;
        sourceB.volume = volume;
    }
} 