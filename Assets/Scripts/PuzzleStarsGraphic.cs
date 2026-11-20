using UnityEngine;
using UnityEngine.UI;

// Three stars authored on the Canvas; geometry avoids missing star glyphs in fonts.
[RequireComponent(typeof(CanvasRenderer))]
public class PuzzleStarsGraphic : MaskableGraphic
{
    [SerializeField, Range(0, 3)] private int stars;
    [SerializeField, Range(1, 3)] private int slots = 3;
    public int Slots { get => slots; set { slots = Mathf.Clamp(value, 1, 3); SetVerticesDirty(); } }
    [SerializeField] private Color emptyColor = new Color(0.25f, 0.32f, 0.34f, 1);
    public int Stars
    {
        get => stars;
        set { stars = Mathf.Clamp(value, 0, 3); SetVerticesDirty(); }
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        var rect = rectTransform.rect;
        float size = Mathf.Min(rect.height, rect.width / (slots == 1 ? 1 : slots * 1.2f - 0.1f));
        for (int star = 0; star < slots; star++)
        {
            Vector2 center = rect.center + Vector2.right * ((star - (slots - 1) / 2f) * size * 1.2f);
            Color tint = star < stars ? color : emptyColor;
            int first = mesh.currentVertCount;
            mesh.AddVert(center, tint, Vector2.zero);
            for (int point = 0; point < 10; point++)
            {
                float angle = (90 + point * 36) * Mathf.Deg2Rad;
                float radius = size * (point % 2 == 0 ? 0.48f : 0.21f);
                mesh.AddVert(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, tint, Vector2.zero);
            }
            for (int point = 0; point < 10; point++) mesh.AddTriangle(first, first + 1 + point, first + 1 + (point + 1) % 10);
        }
    }
}
