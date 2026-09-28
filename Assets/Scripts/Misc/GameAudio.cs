using UnityEngine;

/// <summary>Assign clips here; volume settings persist between runs.</summary>
public class GameAudio : MonoBehaviour
{
    public static GameAudio Instance { get; private set; }
    [Header("Sound effects — optional until your recordings are ready")]
    public AudioClip shoot;
    [Header("Quick multi-kill cheer")]
    public AudioClip playerCheer;
    public CheerPopup cheerPopup;
    [Min(.1f)] public float multiKillWindow=2;
    [Min(0)] public float cheerCooldown=4;
    [Range(0,1)] public float cheerGain=.65f;
    private float lastKill=-100,nextCheer;
    private int killChain;
    private AudioSource cheerSource;
    public AudioClip enemyNoscope;
    public AudioClip enemyNoscopeHit;
    public AudioClip shopMusic;
    public AudioClip enemyExplode;
    public AudioClip enemyStomped;
    [Tooltip("Nonlethal bullet/ordinary damage only.")]
    public AudioClip enemyDamaged;
    [Tooltip("Nonlethal stomp impact only. Enemy Stomped is the killing stomp.")]
    public AudioClip enemyStompDamaged;
    public AudioClip superShoot;
    [Tooltip("Looped while Cocey banan is active. Uses the SFX volume slider.")]
    public AudioClip angryMonkey;
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
    private AudioSource shopMusicSource;
    private AudioSource pauseMusicSource;
    private AudioSource angryMonkeySource;
    private AudioSource[] voices;
    private int nextVoice;
    private bool paused, shopOpen, minibossActive;
    private AudioSource minibossSource;
    private float cinematicMusicGain = 1f;
    public void SetCinematicMusicDucked(bool ducked) { cinematicMusicGain = ducked ? 0f : 1f; ApplyVolumes(); }

    private void Awake()
    {
        Instance = this;
        MusicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat("BM.MusicVolume", defaultMusicVolume));
        SfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat("BM.SfxVolume", defaultSfxVolume));
        cheerSource = MakeSource(false);
        mainMusicSource = MakeSource(true);
        minibossSource = MakeSource(true);
        shopMusicSource = MakeSource(true);
        pauseMusicSource = MakeSource(true);
        angryMonkeySource = MakeSource(true);
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
            minibossSource.Pause();
            if (pauseMusicSource.clip != null) pauseMusicSource.Play();
            foreach (AudioSource voice in voices) voice.Pause();
            angryMonkeySource.Pause();cheerSource.Pause();
        }
        else
        {
            pauseMusicSource.Stop();
            if(!minibossActive && !shopOpen)mainMusicSource.UnPause();
            if(minibossActive)minibossSource.UnPause();
            foreach (AudioSource voice in voices) voice.UnPause();
            angryMonkeySource.UnPause();cheerSource.UnPause();
        }
    }

    public void SetSpeedBoostActive(bool active)
    {
        if (angryMonkeySource == null) return;
        if (!active) { angryMonkeySource.Stop(); return; }
        if (angryMonkey == null) return;
        angryMonkeySource.clip = angryMonkey;
        angryMonkeySource.Play();
        if (paused) angryMonkeySource.Pause();
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
        if (minibossSource != null) minibossSource.volume = MusicVolume * cinematicMusicGain;
        if (shopMusicSource != null) shopMusicSource.volume = MusicVolume;
        if (mainMusicSource != null) mainMusicSource.volume = MusicVolume * cinematicMusicGain;
        if (pauseMusicSource != null) pauseMusicSource.volume = MusicVolume;
        if (voices != null) foreach (AudioSource voice in voices) voice.volume = SfxVolume;
        if (angryMonkeySource != null) angryMonkeySource.volume = SfxVolume;
        if (cheerSource != null) cheerSource.volume = SfxVolume*cheerGain;
    }

    public void SetShopOpen(bool open)
    {
        shopOpen=open;
        if(open){mainMusicSource.Pause();shopMusicSource.clip=shopMusic;if(shopMusic!=null)shopMusicSource.Play();}
        else {shopMusicSource.Stop();if(!paused && !minibossActive)mainMusicSource.UnPause();}
    }
    public void RegisterPlayerKill()
    {
        if(paused || BossFusionEncounter.IsCutsceneActive)return;
        killChain=Time.time-lastKill<=multiKillWindow?killChain+1:1;lastKill=Time.time;
        if(killChain<2 || Time.time<nextCheer || (playerCheer==null && cheerPopup==null) || cheerSource.isPlaying)return;
        cheerSource.volume=SfxVolume*cheerGain;cheerSource.clip=playerCheer;if(playerCheer!=null)cheerSource.Play();
        if(cheerPopup!=null)cheerPopup.Show();
        nextCheer=Time.time+Mathf.Max(cheerCooldown,playerCheer!=null?playerCheer.length:0);killChain=0;
    }
    public void PlayPlayerCheer()
    {
        cheerSource.volume=SfxVolume*cheerGain;cheerSource.clip=playerCheer;
        if(playerCheer!=null)cheerSource.Play();
        if(cheerPopup!=null)cheerPopup.Show();
        nextCheer=Time.time+cheerCooldown;
    }
    public bool BeginMinibossMusic(AudioClip clip)
    {
        if(clip==null || minibossActive)return false;
        minibossActive=true;mainMusicSource.Pause();
        minibossSource.clip=clip;minibossSource.Play();if(paused)minibossSource.Pause();
        return true;
    }
    public void EndMinibossMusic()
    {
        if(!minibossActive)return;
        minibossActive=false;minibossSource.Stop();
        // UnPause preserves the gameplay track's exact playback position.
        if(!paused && !shopOpen)mainMusicSource.UnPause();
    }
    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}




