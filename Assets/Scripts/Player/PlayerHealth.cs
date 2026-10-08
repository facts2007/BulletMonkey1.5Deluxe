using System.Collections;
using UnityEngine;
using TMPro;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;
    public int currentHealth;
    public bool HasEscaped {get;private set;}
    public bool IsDying {get;private set;}
    public bool Escape(string scene){if(IsDying)return false;if(HasEscaped)return true;if(!Application.CanStreamedLevelBeLoaded(scene))return false;HasEscaped=true;GameSceneFlow.Load(scene);return true;}
    [Header("Normal death — ragdoll, explosion, then circle fade")]
    public Camera deathCamera;
    public GameObject deathExplosionVfx;
    public AudioClip deathExplosionSound;
    [Min(.1f)] public float deathRagdollSeconds = 1.5f;
    [Min(.1f)] public float deathExplosionSeconds = .85f;
    [Min(.1f)] public float deathExplosionScale = 2.5f;
    [Min(0)] public float deathCameraShake = .18f;

    [Header("UI References")]
    public RectTransform healthBar;
    public RectTransform redBar;
    public TextMeshProUGUI healthText;

    [Header("Bar Settings")]
    public float healthBarSpeed = 8f;
    public float redBarSpeed = 4f;
    public float redBarDelay = 0.5f;

    private float targetHealthScale = 1f;
    private float targetRedScale = 1f;
    private Coroutine redBarRoutine;

    private void Awake()
    {
        currentHealth = maxHealth;
        var cheer=GetComponent<PlayerCheer>();
        if(deathCamera==null && cheer!=null)deathCamera=cheer.cheerCamera;
        UpdateText();
    }

    private void Update()
    {
        AnimateBar(healthBar, targetHealthScale, healthBarSpeed);
        AnimateBar(redBar, targetRedScale, redBarSpeed);
    }

    private void AnimateBar(RectTransform bar, float target, float speed)
    {
        if (bar == null) return;
        Vector3 scale = bar.localScale;
        scale.x = Mathf.Lerp(scale.x, target, speed * Time.deltaTime);
        bar.localScale = scale;
    }

    public void TakeDamage(int amount)
    {
        if(HasEscaped || IsDying)return;
        if(BossFusionEncounter.IsCutsceneActive || DungeonEncounter.IsCutsceneActive || PlayerCheer.IsCutsceneActive)return;
        var rescue=GetComponent<PlayerUnstuck>();if(rescue!=null && rescue.IsRecovering)return;
        currentHealth = Mathf.Clamp(currentHealth - amount, 0, maxHealth);

        targetHealthScale = (float)currentHealth / maxHealth;
        UpdateText();

        if (redBarRoutine != null)
        {
            StopCoroutine(redBarRoutine);
        }
        redBarRoutine = StartCoroutine(DelayRedBar());

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        if(currentHealth<=0 || amount<=0)return;
        currentHealth=Mathf.Min(maxHealth,currentHealth+amount);
        targetHealthScale=targetRedScale=(float)currentHealth/maxHealth;UpdateText();
    }

    public void IncreaseMaxHealth(int amount)
    {
        maxHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);

        targetHealthScale = (float)currentHealth / maxHealth;
        UpdateText();
    }

    private IEnumerator DelayRedBar()
    {
        yield return new WaitForSeconds(redBarDelay);
        targetRedScale = targetHealthScale;
    }

    private void UpdateText()
    {
        if (healthText != null) healthText.text = currentHealth + "/" + maxHealth;
    }

    public void Die()
    {
        Die("DeathScene");
    }
    public void Die(string deathScene)
    {
        if (HasEscaped || IsDying || GameSceneFlow.IsLoading) return;
        var drowning=FindFirstObjectByType<DrowningSequence>();
        if(drowning!=null && drowning.IsRunning)
        {
            if(drowning.IsFinished)GameSceneFlow.Load(deathScene);
            return;
        }
        currentHealth = 0;
        targetHealthScale=targetRedScale=0;UpdateText();
        IsDying=true;
        StartCoroutine(NormalDeath(deathScene));
    }

    private IEnumerator NormalDeath(string deathScene)
    {
        var pause=FindFirstObjectByType<PauseManager>();
        if(pause!=null){if(PauseManager.GameIsPaused)pause.Resume();pause.enabled=false;}
        var shop=FindFirstObjectByType<ShopManager>();if(shop!=null)shop.enabled=false;
        Time.timeScale=1;
        var cheer=GetComponent<PlayerCheer>();if(cheer!=null)cheer.enabled=false;
        foreach(var behaviour in GetComponentsInChildren<MonoBehaviour>())
            if(behaviour!=null && (behaviour is PlayerCheer || behaviour is PlayerUnstuck || behaviour is PlayerMovement ||
                behaviour is MouseLook || behaviour is Gun || behaviour is ShootCameraShake || behaviour is SuperShootAbility ||
                behaviour is SpeedBoostAbility || behaviour is PlayerDamageContact || behaviour is CharacterAnimationDriver))
                behaviour.enabled=false;
        foreach(var collider in GetComponentsInChildren<Collider>())collider.enabled=false;
        foreach(var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            if(canvas.renderMode==RenderMode.ScreenSpaceOverlay && canvas.GetComponentInChildren<RetroCamera>()==null &&
                !canvas.name.StartsWith("Retro camera presentation") && canvas.GetComponentInChildren<IrisTransitionGraphic>(true)==null)
                canvas.enabled=false;

        HeavenlyRagdoll ragdoll=null;
        var visual=transform.Find("BulletMonkeyVisual");
        if(visual!=null)
        {
            var corpse=Instantiate(visual.gameObject,visual.position,visual.rotation);
            corpse.name="Defeated monkey ragdoll";corpse.transform.localScale=visual.lossyScale;
            foreach(var behaviour in corpse.GetComponentsInChildren<MonoBehaviour>())if(behaviour!=null)behaviour.enabled=false;
            foreach(var animator in corpse.GetComponentsInChildren<Animator>())animator.enabled=false;
            ragdoll=corpse.AddComponent<HeavenlyRagdoll>();ragdoll.showHalo=false;ragdoll.Flop();
            visual.gameObject.SetActive(false);
        }
        var gameplayCamera=Camera.main;
        Camera view=deathCamera!=null?deathCamera:gameplayCamera;
        if(view!=null && gameplayCamera!=null && view!=gameplayCamera)
        {
            view.CopyFrom(gameplayCamera);view.targetTexture=null;
            view.transform.SetPositionAndRotation(gameplayCamera.transform.position,gameplayCamera.transform.rotation);
            var gameplayRetro=gameplayCamera.GetComponent<RetroCamera>();
            if(gameplayRetro!=null)
            {
                int resolution=gameplayRetro.verticalResolution;var material=gameplayRetro.pixelMaterial;
                gameplayRetro.enabled=false;
                var retro=view.GetComponent<RetroCamera>();if(retro==null)retro=view.gameObject.AddComponent<RetroCamera>();
                retro.verticalResolution=resolution;retro.pixelMaterial=material;retro.enabled=true;
            }
            gameplayCamera.enabled=false;view.tag="MainCamera";view.gameObject.SetActive(true);view.enabled=true;
        }
        Vector3 cameraStart=view!=null?view.transform.position:Vector3.zero;
        for(float t=0;t<deathRagdollSeconds;t+=Time.deltaTime)
        {
            if(view!=null && ragdoll!=null)
                view.transform.rotation=Quaternion.Slerp(view.transform.rotation,Quaternion.LookRotation(ragdoll.TorsoPosition-view.transform.position),1f-Mathf.Exp(-4*Time.deltaTime));
            yield return null;
        }
        Vector3 point=ragdoll!=null?ragdoll.TorsoPosition:transform.position+Vector3.up;
        if(ragdoll!=null)ragdoll.Explode();
        if(deathExplosionVfx!=null)
        {
            var effect=Instantiate(deathExplosionVfx,point,Quaternion.identity);
            effect.name="Player death explosion";effect.transform.localScale*=deathExplosionScale;Destroy(effect,5);
        }
        else SpawnFog.Poof(point,3);
        if(GameAudio.Instance!=null)GameAudio.Instance.PlayEffect(deathExplosionSound!=null?deathExplosionSound:GameAudio.Instance.enemyExplode);
        var flashCanvas=new GameObject("Player death flash",typeof(Canvas));
        var overlay=flashCanvas.GetComponent<Canvas>();overlay.renderMode=RenderMode.ScreenSpaceOverlay;overlay.sortingOrder=25000;
        var flashObject=new GameObject("Explosion flash",typeof(RectTransform),typeof(UnityEngine.UI.Image));
        flashObject.transform.SetParent(flashCanvas.transform,false);
        var flash=flashObject.GetComponent<UnityEngine.UI.Image>();flash.raycastTarget=false;
        flash.rectTransform.anchorMin=Vector2.zero;flash.rectTransform.anchorMax=Vector2.one;
        flash.rectTransform.offsetMin=flash.rectTransform.offsetMax=Vector2.zero;
        for(float t=0;t<deathExplosionSeconds;t+=Time.deltaTime)
        {
            flash.color=new Color(1,1,1,Mathf.Lerp(.6f,0,Mathf.Clamp01(t/.22f)));
            if(view!=null)view.transform.position=cameraStart+Random.insideUnitSphere*deathCameraShake*(1-Mathf.Clamp01(t/deathExplosionSeconds));
            yield return null;
        }
        if(view!=null)view.transform.position=cameraStart;
        Destroy(flashCanvas);
        // Keep gameplay music through the impact, then cut it as the black iris closes.
        foreach(var source in FindObjectsByType<AudioSource>(FindObjectsSortMode.None))if(source.loop)source.Stop();
        GameSceneFlow.Load(deathScene);
    }
}

