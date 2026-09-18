using UnityEngine;

public class ShopkeeperIdle : MonoBehaviour
{
    [Header("Animator")]
    public Animator animator;

    [Header("Idle Variations (optional)")]
    [Tooltip("Trigger parameter names in your Animator for extra idle animations " +
             "(e.g. 'LookAround', 'Stretch'). Leave empty if you only have one looping idle clip.")]
    public string[] idleVariationTriggers;
    public float minTimeBetweenVariations = 5f;
    public float maxTimeBetweenVariations = 12f;

    private float variationTimer;
    private float nextVariationTime;

    void Start()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        SetNextVariationTime();
    }

    void Update()
    {
        if (idleVariationTriggers == null || idleVariationTriggers.Length == 0) return;
        if (animator == null) return;

        variationTimer += Time.deltaTime;

        if (variationTimer >= nextVariationTime)
        {
            PlayRandomVariation();
            variationTimer = 0f;
            SetNextVariationTime();
        }
    }

    private void PlayRandomVariation()
    {
        string trigger = idleVariationTriggers[Random.Range(0, idleVariationTriggers.Length)];
        animator.SetTrigger(trigger);
    }

    private void SetNextVariationTime()
    {
        nextVariationTime = Random.Range(minTimeBetweenVariations, maxTimeBetweenVariations);
    }
}