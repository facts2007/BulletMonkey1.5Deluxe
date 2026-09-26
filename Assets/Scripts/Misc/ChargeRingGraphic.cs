using UnityEngine;
using UnityEngine.UI;

/// <summary>A smooth UI ring without a texture dependency.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public class ChargeRingGraphic : MaskableGraphic
{
    [Range(0f, 1f)] [SerializeField] private float amount = 1f;
    [Min(1f)] public float thickness = 3f;
    public float FillAmount
    {
        get => amount;
        set { amount = Mathf.Clamp01(value); SetVerticesDirty(); }
    }

    protected override void OnPopulateMesh(VertexHelper helper)
    {
        helper.Clear();
        float outer = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * 0.5f;
        float inner = Mathf.Max(0f, outer - thickness);
        Vector2 center = rectTransform.rect.center;
        int segments = Mathf.CeilToInt(96 * amount);
        for (int i = 0; i < segments; i++)
        {
            float a = Mathf.PI * 0.5f - 2 * Mathf.PI * Mathf.Min(i / 96f, amount);
            float b = Mathf.PI * 0.5f - 2 * Mathf.PI * Mathf.Min((i + 1) / 96f, amount);
            Vector2 d1 = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            Vector2 d2 = new Vector2(Mathf.Cos(b), Mathf.Sin(b));
            int index = helper.currentVertCount;
            helper.AddVert(center + d1 * outer, color, Vector2.zero);
            helper.AddVert(center + d2 * outer, color, Vector2.zero);
            helper.AddVert(center + d2 * inner, color, Vector2.zero);
            helper.AddVert(center + d1 * inner, color, Vector2.zero);
            helper.AddTriangle(index, index + 1, index + 2);
            helper.AddTriangle(index + 2, index + 3, index);
        }
    }
}
