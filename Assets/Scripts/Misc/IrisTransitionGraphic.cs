using UnityEngine;
using UnityEngine.UI;

/// <summary>Black outside a circular opening, measured in screen units so it stays round.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public class IrisTransitionGraphic : MaskableGraphic
{
    private float openness = 1f;
    private float fade;
    public float Openness => openness;
    public void SetOpening(float value, float blackFade)
    {
        openness = Mathf.Clamp01(value);
        fade = Mathf.Clamp01(blackFade);
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = rectTransform.rect;
        if (fade > 0f)
        {
            Color c = new Color(0f, 0f, 0f, fade);
            vh.AddVert(new Vector3(r.xMin, r.yMin), c, Vector2.zero);
            vh.AddVert(new Vector3(r.xMin, r.yMax), c, Vector2.zero);
            vh.AddVert(new Vector3(r.xMax, r.yMax), c, Vector2.zero);
            vh.AddVert(new Vector3(r.xMax, r.yMin), c, Vector2.zero);
            vh.AddTriangle(0, 1, 2); vh.AddTriangle(0, 2, 3);
        }
        if (openness >= 1f) return;
        float maximum = r.size.magnitude * .5f + 2f;
        float radius = maximum * openness;
        const int segments = 128;
        for (int n = 0; n < segments; n++)
        {
            float a = n * Mathf.PI * 2f / segments;
            float b = (n + 1) * Mathf.PI * 2f / segments;
            Vector2 p = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            Vector2 q = new Vector2(Mathf.Cos(b), Mathf.Sin(b));
            int index = vh.currentVertCount;
            vh.AddVert(r.center + p * radius, Color.black, Vector2.zero);
            vh.AddVert(r.center + p * maximum * 2f, Color.black, Vector2.zero);
            vh.AddVert(r.center + q * maximum * 2f, Color.black, Vector2.zero);
            vh.AddVert(r.center + q * radius, Color.black, Vector2.zero);
            vh.AddTriangle(index, index + 1, index + 2);
            vh.AddTriangle(index, index + 2, index + 3);
        }
    }
}
