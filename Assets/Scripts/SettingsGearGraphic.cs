using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class SettingsGearGraphic : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        var rect = rectTransform.rect;
        float size = Mathf.Min(rect.width, rect.height);
        const int segments = 64;
        for (int i = 0; i < segments; i++)
        {
            float angle = 2 * Mathf.PI * i / segments;
            var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            int tooth = i % 8;
            float radius = tooth >= 2 && tooth <= 5 ? 0.44f : 0.34f;
            mesh.AddVert(rect.center + direction * size * radius, color, Vector2.zero);
            mesh.AddVert(rect.center + direction * size * 0.16f, color, Vector2.zero);
        }
        for (int i = 0; i < segments; i++)
        {
            int current = i * 2, next = (i + 1) % segments * 2;
            mesh.AddTriangle(current, next, current + 1);
            mesh.AddTriangle(next, next + 1, current + 1);
        }
    }
}
