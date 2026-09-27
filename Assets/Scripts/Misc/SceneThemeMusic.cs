using UnityEngine;
[RequireComponent(typeof(AudioSource))]
public class SceneThemeMusic : MonoBehaviour
{
    [Tooltip("Optional override. You can also assign the music directly to Audio Source > AudioClip.")]
    public AudioClip theme;
    [Range(0,1)] public float themeVolume=1;
    public bool loop=true;
    private AudioSource source;
    private bool started;
    private float nextRefresh;
    private void Awake()
    {
        source=GetComponent<AudioSource>();
        if(theme==null)theme=source.clip;
        source.playOnAwake=false;source.spatialBlend=0;
    }
    private void OnEnable(){started=false;RefreshTheme();}
    private void Update(){if(Time.unscaledTime>=nextRefresh){nextRefresh=Time.unscaledTime+.25f;RefreshTheme();}}
    private void RefreshTheme()
    {
        if(source==null)return;
        if(theme==null && source.clip!=null)theme=source.clip;
        source.volume=themeVolume*Mathf.Clamp01(PlayerPrefs.GetFloat("BM.MusicVolume",.7f));source.loop=loop;
        if(source.clip!=theme){source.Stop();source.clip=theme;started=false;}
        if(theme!=null && (!started || (loop&&!source.isPlaying&&!AudioListener.pause))){source.Play();started=true;}
    }
    private void OnDisable(){if(source!=null)source.Stop();started=false;}
}
