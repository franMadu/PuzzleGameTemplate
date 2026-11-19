using UnityEngine;
using UnityEngine.UI;

// Draw the controls as UI geometry so their appearance does not depend on font glyphs.
[RequireComponent(typeof(CanvasRenderer))]
public class DialogueControlGraphic : MaskableGraphic
{
    private bool closeIcon;

    public void SetCloseIcon(bool close)
    {
        closeIcon = close;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        if (closeIcon)
        {
            Line(mesh, new Vector2(-0.19f, -0.19f), new Vector2(0.19f, 0.19f));
            Line(mesh, new Vector2(-0.19f, 0.19f), new Vector2(0.19f, -0.19f));
        }
        else
        {
            Line(mesh, new Vector2(-0.28f, 0), new Vector2(0.28f, 0));
            Line(mesh, new Vector2(0.04f, 0.24f), new Vector2(0.28f, 0));
            Line(mesh, new Vector2(0.28f, 0), new Vector2(0.04f, -0.24f));
        }
    }

    private void Line(VertexHelper mesh, Vector2 from, Vector2 to)
    {
        var rect = rectTransform.rect;
        float size = Mathf.Min(rect.width, rect.height);
        from = rect.center + from * size;
        to = rect.center + to * size;
        Vector2 direction = to - from;
        Vector2 normal = new Vector2(-direction.y, direction.x).normalized * size * 0.032f;
        int first = mesh.currentVertCount;
        mesh.AddVert(from - normal, color, Vector2.zero);
        mesh.AddVert(from + normal, color, Vector2.zero);
        mesh.AddVert(to + normal, color, Vector2.zero);
        mesh.AddVert(to - normal, color, Vector2.zero);
        mesh.AddTriangle(first, first + 1, first + 2);
        mesh.AddTriangle(first, first + 2, first + 3);
    }
}
