using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SceneThemeMusic : MonoBehaviour
{
    [Tooltip("Drop this scene's theme MP3 or AudioClip here. Empty means silence.")]
    public AudioClip theme;
    [Range(0,1)] public float themeVolume = 1;
    public bool loop = true;
    private AudioSource source;

    private void Awake()
    {
        source=GetComponent<AudioSource>();
        source.playOnAwake=false;source.spatialBlend=0;
    }
    private void OnEnable(){RefreshTheme();}
    private void Update(){RefreshTheme();}
    private void RefreshTheme()
    {
        if(source==null)return;
        source.volume=themeVolume*Mathf.Clamp01(PlayerPrefs.GetFloat("BM.MusicVolume",.7f));
        source.loop=loop;
        if(source.clip==theme)return;
        source.Stop();source.clip=theme;
        if(theme!=null)source.Play();
    }
    private void OnDisable(){if(source!=null){source.Stop();source.clip=null;}}
}
