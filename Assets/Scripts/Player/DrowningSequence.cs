using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DrowningSequence : MonoBehaviour
{
    [Header("Your performance")]
    public AudioClip heavenlyVoice;
    [TextArea(2,4)] public string message = "You have drowned my child...\nMay you rest in peace..";
    [Min(1)] public float minimumMessageSeconds = 6;
    [Min(0)] public float ragdollDelay = 1.2f;
    [Min(0)] public float ragdollRestSeconds = 2;
    [Min(0)] public float ascentHeight = 5;
    [Min(.1f)] public float whiteFadeSeconds = 7;
    [Range(0,1)] public float voiceVolume = 1;
    [Header("Heavenly choir — optional MP3 / AudioClip")]
    public AudioClip heavenlyChoir;
    [Range(0,1)] public float choirVolume = .35f;
    public bool loopChoir = true;
    [Header("Ragdoll halo")]
    public Material haloMaterial;
    [Min(.1f)] public float haloHeight = .6f;
    [Min(.1f)] public float haloRadius = .36f;
    [Header("Scene references")]
    public Transform arrival;
    public GameObject heavenlyRoom;
    public Camera heavenlyCamera;
    public Canvas presentation;
    public TMP_Text caption;
    public Image whiteFade;
    public GameObject deathPanel;
    public string mainMenuScene = "MainMenu";
    public bool IsRunning { get; private set; }
    public bool IsFinished { get; private set; }
    public HeavenlyRagdoll Ragdoll { get; private set; }
    private AudioSource voice;
    private AudioSource choir;
    private bool voicePaused;
    private void Awake()
    {
        heavenlyCamera.gameObject.SetActive(false);
        presentation.gameObject.SetActive(false);
        heavenlyRoom.SetActive(false);
        if(!Application.CanStreamedLevelBeLoaded(mainMenuScene))
            foreach(var button in deathPanel.GetComponentsInChildren<Button>(true))
                if(button.name=="Main menu")button.GetComponentInChildren<TMP_Text>().text="Quit game";
        voice=gameObject.AddComponent<AudioSource>();voice.playOnAwake=false;voice.spatialBlend=0;
        choir=gameObject.AddComponent<AudioSource>();choir.playOnAwake=false;choir.spatialBlend=0;
    }
    public void Begin(PlayerHealth player)
    {
        if(BossFusionEncounter.IsCutsceneActive || IsRunning || player==null || player.currentHealth<=0 || !player.gameObject.activeInHierarchy)return;
        IsRunning=true;
        if(GameAudio.Instance!=null)GameAudio.Instance.SetCinematicMusicDucked(true);
        StartCoroutine(Sequence(player));
    }
    private IEnumerator Sequence(PlayerHealth player)
    {
        // Isolate the cinematic without changing ordinary combat/death behavior.
        var pause=FindFirstObjectByType<PauseManager>();
        if(pause!=null){if(PauseManager.GameIsPaused)pause.Resume();pause.enabled=false;}
        foreach(var b in player.GetComponentsInChildren<MonoBehaviour>())
            if(b is PlayerMovement || b is MouseLook || b is Gun || b is ShootCameraShake || b is SuperShootAbility || b is SpeedBoostAbility || b is PlayerDamageContact || b is CharacterAnimationDriver)b.enabled=false;
        player.enabled=false;
        foreach(var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))if(canvas!=presentation && canvas.renderMode==RenderMode.ScreenSpaceOverlay)canvas.gameObject.SetActive(false);
        foreach(var c in player.GetComponentsInChildren<Collider>())c.enabled=false;
        foreach(var c in player.GetComponentsInChildren<Camera>())c.gameObject.SetActive(false);
        heavenlyRoom.SetActive(true);
        player.transform.SetPositionAndRotation(arrival.position,arrival.rotation);
        var visual=player.transform.Find("BulletMonkeyVisual");
        if(visual!=null)
        {
            var corpse=Instantiate(visual.gameObject,visual.position,visual.rotation);
            corpse.name="Heavenly monkey ragdoll";corpse.transform.localScale=visual.lossyScale;
            foreach(var a in corpse.GetComponentsInChildren<Animator>())a.enabled=false;
            Ragdoll=corpse.AddComponent<HeavenlyRagdoll>();
            Ragdoll.haloMaterial=haloMaterial;Ragdoll.haloHeight=haloHeight;Ragdoll.haloRadius=haloRadius;
            visual.gameObject.SetActive(false);
        }
        heavenlyCamera.gameObject.SetActive(true);
        presentation.gameObject.SetActive(true);
        deathPanel.SetActive(false);whiteFade.color=new Color(1,1,1,0);
        caption.text=message;caption.gameObject.SetActive(true);
        voice.clip=heavenlyVoice;voice.volume=voiceVolume*(GameAudio.Instance!=null?GameAudio.Instance.SfxVolume:1);
        if(voice.clip!=null)voice.Play();
        choir.clip=heavenlyChoir;choir.loop=loopChoir;
        choir.volume=choirVolume*(GameAudio.Instance!=null?GameAudio.Instance.MusicVolume:1);
        if(choir.clip!=null)choir.Play();
        float elapsed=0;
        while(elapsed<ragdollDelay)
        {
            elapsed+=Time.deltaTime;
            yield return null;
        }
        if(Ragdoll!=null)Ragdoll.Flop();
        yield return new WaitForSeconds(ragdollRestSeconds);
        if(Ragdoll!=null)Ragdoll.BeginAscent(whiteFadeSeconds,ascentHeight);
        for(float t=0;t<whiteFadeSeconds;t+=Time.deltaTime)
        {
            whiteFade.color=new Color(1,1,1,Mathf.SmoothStep(0,1,t/whiteFadeSeconds));
            yield return null;
        }
        whiteFade.color=Color.white;caption.gameObject.SetActive(false);
        choir.Stop();
        // A long recording may finish over white, without delaying the ascent/fade cue.
        float remainingMessage=Mathf.Max(minimumMessageSeconds,heavenlyVoice!=null?heavenlyVoice.length+.4f:0)
            -elapsed-ragdollRestSeconds-whiteFadeSeconds;
        if(remainingMessage>0)yield return new WaitForSeconds(remainingMessage);
        IsFinished=true;
        player.currentHealth=0;player.Die();
        yield break;
    }
    private void Update()
    {
        if(IsRunning && voice!=null)
        {
            voice.volume=voiceVolume*(GameAudio.Instance!=null?GameAudio.Instance.SfxVolume:1);
            choir.volume=choirVolume*(GameAudio.Instance!=null?GameAudio.Instance.MusicVolume:1)*(1-whiteFade.color.a);
            if(Time.timeScale==0 && !voicePaused){voice.Pause();choir.Pause();voicePaused=true;}
            else if(Time.timeScale>0 && voicePaused){voice.UnPause();choir.UnPause();voicePaused=false;}
        }
        if(IsFinished && Input.GetKeyDown(KeyCode.R))Retry();
    }
    private void OnDisable()
    {
        if(voice!=null)voice.Stop();
        if(choir!=null)choir.Stop();
    }
    public void Retry(){Time.timeScale=1;SceneManager.LoadScene(SceneManager.GetActiveScene().name);}
    public void MainMenu()
    {
        Time.timeScale=1;
        if(Application.CanStreamedLevelBeLoaded(mainMenuScene)){SceneManager.LoadScene(mainMenuScene);return;}
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying=false;
#else
        Application.Quit();
#endif
    }
}
