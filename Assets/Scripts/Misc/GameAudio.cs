using UnityEngine;

/// <summary>Assign clips here; volume settings persist between runs.</summary>
public class GameAudio : MonoBehaviour
{
    public static GameAudio Instance { get; private set; }
    [Header("Sound effects — optional until your recordings are ready")]
    public AudioClip shoot;
    public AudioClip enemyExplode;
    public AudioClip enemyStomped;
    public AudioClip superShoot;
    [Range(0f, 1f)] public float shootGain = 0.6f;
    [Range(0f, 1f)] public float superShootGain = 1f;

    [Header("Soundtracks")]
    public AudioClip mainGameMusic;
    public AudioClip pauseMenuMusic;
    [Range(0f, 1f)] public float defaultMusicVolume = 0.7f;
    [Range(0f, 1f)] public float defaultSfxVolume = 0.8f;
    public float MusicVolume { get; private set; }
    public float SfxVolume { get; private set; }

    private AudioSource mainMusicSource;
    private AudioSource pauseMusicSource;
    private AudioSource[] voices;
    private int nextVoice;
    private bool paused;

    private void Awake()
    {
        Instance = this;
        MusicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat("BM.MusicVolume", defaultMusicVolume));
        SfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat("BM.SfxVolume", defaultSfxVolume));
        mainMusicSource = MakeSource(true);
        pauseMusicSource = MakeSource(true);
        mainMusicSource.clip = mainGameMusic;
        pauseMusicSource.clip = pauseMenuMusic;
        voices = new AudioSource[24];
        for (int i = 0; i < voices.Length; i++) voices[i] = MakeSource(false);
        ApplyVolumes();
    }

    private void Start()
    {
        if (mainMusicSource.clip != null && !paused) mainMusicSource.Play();
    }

    private AudioSource MakeSource(bool loop)
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;
        return source;
    }

    public void PlayShot(bool superShot)
    {
        PlayEffect(superShot && superShoot != null ? superShoot : shoot,
            superShot ? superShootGain : shootGain);
    }

    public void PlayEffect(AudioClip clip, float gain = 1f)
    {
        if (clip == null || voices == null || paused) return;
        AudioSource voice = voices[nextVoice];
        nextVoice = (nextVoice + 1) % voices.Length;
        voice.Stop();
        voice.volume = SfxVolume;
        voice.pitch = 1f;
        voice.PlayOneShot(clip, gain);
    }

    public void SetPaused(bool value)
    {
        if (paused == value) return;
        paused = value;
        if (paused)
        {
            mainMusicSource.Pause();
            if (pauseMusicSource.clip != null) pauseMusicSource.Play();
            foreach (AudioSource voice in voices) voice.Pause();
        }
        else
        {
            pauseMusicSource.Stop();
            mainMusicSource.UnPause();
            foreach (AudioSource voice in voices) voice.UnPause();
        }
    }

    public void SetMusicVolume(float value)
    {
        MusicVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat("BM.MusicVolume", MusicVolume);
        ApplyVolumes();
    }

    public void SetSfxVolume(float value)
    {
        SfxVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat("BM.SfxVolume", SfxVolume);
        ApplyVolumes();
    }

    private void ApplyVolumes()
    {
        if (mainMusicSource != null) mainMusicSource.volume = MusicVolume;
        if (pauseMusicSource != null) pauseMusicSource.volume = MusicVolume;
        if (voices != null) foreach (AudioSource voice in voices) voice.volume = SfxVolume;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
