using UnityEngine;
using UnityEngine.UI;

public enum MenuIcon { Home, Chapters, Gallery, Challenges, Shop }

// Small UI symbols use Canvas geometry, so they do not depend on font glyph coverage.
[RequireComponent(typeof(CanvasRenderer))]
public class MenuIconGraphic : MaskableGraphic
{
    [SerializeField] private MenuIcon icon;
    public MenuIcon Icon { get => icon; set { icon = value; SetVerticesDirty(); } }
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        var r = rectTransform.rect;
        float side = Mathf.Min(r.width, r.height);
        Vector2 P(float x, float y) => r.center + new Vector2(x, y) * side;
        void Line(float x1, float y1, float x2, float y2)
        {
            Vector2 a = P(x1, y1), b = P(x2, y2);
            Vector2 n = new Vector2(-(b-a).y, (b-a).x).normalized * Mathf.Min(r.width,r.height) * 0.035f;
            int start = mesh.currentVertCount;
            mesh.AddVert(a-n,color,Vector2.zero); mesh.AddVert(a+n,color,Vector2.zero);
            mesh.AddVert(b+n,color,Vector2.zero); mesh.AddVert(b-n,color,Vector2.zero);
            mesh.AddTriangle(start,start+1,start+2); mesh.AddTriangle(start,start+2,start+3);
        }
        void Box(float x1,float y1,float x2,float y2)
        { Line(x1,y1,x2,y1); Line(x2,y1,x2,y2); Line(x2,y2,x1,y2); Line(x1,y2,x1,y1); }
        switch(icon)
        {
            case MenuIcon.Home:
                Line(-.4f,0,0,.36f); Line(0,.36f,.4f,0); Box(-.28f,-.35f,.28f,.06f); Box(-.09f,-.35f,.09f,-.12f); break;
            case MenuIcon.Chapters:
                Box(-.39f,-.31f,.39f,.31f); Line(0,-.31f,0,.31f);
                Line(-.3f,.16f,-.09f,.16f); Line(.09f,.16f,.3f,.16f); Line(-.3f,0,-.09f,0); Line(.09f,0,.3f,0); break;
            case MenuIcon.Gallery:
                Box(-.39f,-.31f,.39f,.31f); Line(-.3f,-.2f,-.04f,.08f); Line(-.04f,.08f,.3f,-.2f); Box(.17f,.13f,.24f,.2f); break;
            case MenuIcon.Challenges:
                Box(-.33f,-.33f,.33f,.33f); Line(-.33f,0,.33f,0); Line(0,-.33f,0,.33f);
                Box(-.07f,-.07f,.07f,.07f); break;
            case MenuIcon.Shop:
                Box(-.32f,-.34f,.32f,.17f); Line(-.15f,.17f,-.15f,.34f); Line(-.15f,.34f,.15f,.34f); Line(.15f,.34f,.15f,.17f); break;
        }
    }
}
