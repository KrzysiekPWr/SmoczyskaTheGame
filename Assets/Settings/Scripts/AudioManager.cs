using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("UI Sound Effects")]
    [SerializeField] private AudioClip cardPlaceSound;
    [SerializeField] private AudioClip cardFlipSound;

    [Header("Audio Settings")]
    [SerializeField] private float cardPlaceVolume = 0.5f;
    [SerializeField] private float cardFlipVolume = 0.5f;

    private AudioSource _audioSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _audioSource = gameObject.AddComponent<AudioSource>();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlayCardPlace()
    {
        if (cardPlaceSound != null)
        {
            _audioSource.PlayOneShot(cardPlaceSound, cardPlaceVolume);
        }
    }

    public void PlayCardFlip()
    {
        if (cardFlipSound != null)
        {
            _audioSource.PlayOneShot(cardFlipSound, cardFlipVolume);
        }
    }
} 