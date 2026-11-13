using UnityEngine;
using UnityEngine.UI;

// Original vector-style map marks. No texture imports or font glyph dependencies.
[RequireComponent(typeof(CanvasRenderer))]
public class MapNodeIcon : MaskableGraphic
{
    public MapNodeType Kind;
    public bool Unknown;
    public bool Completed;
    public bool Available;
    private VertexHelper mesh;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear(); mesh = vh;
        if (Available || Completed) Circle(Vector2.zero, 0.47f, Available ? 3f : 1.5f);
        if (Unknown)
        {
            Stroke(3, new Vector2(-.14f,.15f), new Vector2(-.1f,.28f), new Vector2(.08f,.29f),
                new Vector2(.18f,.19f), new Vector2(.14f,.08f), new Vector2(0,-.02f), new Vector2(0,-.13f));
            Circle(new Vector2(0,-.26f), .024f, 3);
        }
        else if (Kind == MapNodeType.Battle || Kind == MapNodeType.Elite || Kind == MapNodeType.Boss)
        {
            bool elite = Kind != MapNodeType.Battle;
            Stroke(3, new Vector2(-.26f,.12f), new Vector2(-.24f,-.2f), new Vector2(-.12f,-.32f),
                new Vector2(.12f,-.32f), new Vector2(.24f,-.2f), new Vector2(.26f,.12f),
                new Vector2(.14f,.24f), new Vector2(-.14f,.24f), new Vector2(-.26f,.12f));
            float tip = elite ? .43f : .31f;
            Stroke(3, new Vector2(-.24f,.10f), new Vector2(-.38f,tip), new Vector2(-.10f,.23f));
            Stroke(3, new Vector2(.24f,.10f), new Vector2(.38f,tip), new Vector2(.10f,.23f));
            Line(new Vector2(-.17f,.03f), new Vector2(-.04f,-.03f), 4);
            Line(new Vector2(.17f,.03f), new Vector2(.04f,-.03f), 4);
            Line(new Vector2(-.09f,-.2f), new Vector2(.09f,-.2f), 3);
            if (elite) Stroke(3, new Vector2(-.09f,-.2f), new Vector2(-.06f,-.12f),
                new Vector2(0,-.20f), new Vector2(.06f,-.12f), new Vector2(.09f,-.2f));
            if (Kind == MapNodeType.Boss)
                Stroke(3, new Vector2(-.16f,.25f), new Vector2(-.12f,.44f), new Vector2(0,.32f),
                    new Vector2(.12f,.44f), new Vector2(.16f,.25f));
        }
        else if (Kind == MapNodeType.Rest)
        {
            Stroke(3, new Vector2(0,.37f), new Vector2(-.04f,.16f), new Vector2(-.18f,.26f),
                new Vector2(-.25f,.02f), new Vector2(-.18f,-.17f), new Vector2(0,-.23f),
                new Vector2(.19f,-.13f), new Vector2(.23f,.04f), new Vector2(.11f,.20f),
                new Vector2(.09f,.06f), new Vector2(0,.37f));
            Stroke(2, new Vector2(0,.08f), new Vector2(-.07f,-.1f), new Vector2(0,-.18f), new Vector2(.07f,-.1f), new Vector2(0,.08f));
            Line(new Vector2(-.29f,-.34f),new Vector2(.29f,-.23f),4);
            Line(new Vector2(-.29f,-.23f),new Vector2(.29f,-.34f),4);
        }
        else if (Kind == MapNodeType.Treasure)
        {
            Stroke(3, new Vector2(-.28f,-.24f),new Vector2(.28f,-.24f),new Vector2(.28f,.14f),
                new Vector2(.19f,.25f),new Vector2(-.19f,.25f),new Vector2(-.28f,.14f),new Vector2(-.28f,-.24f));
            Line(new Vector2(-.28f,.04f),new Vector2(.28f,.04f),3);
            Stroke(3,new Vector2(-.04f,.08f),new Vector2(.04f,.08f),new Vector2(.04f,-.08f),
                new Vector2(-.04f,-.08f),new Vector2(-.04f,.08f));
        }
        else Circle(Vector2.zero, .23f, 3);
        if (Completed) Stroke(3,new Vector2(.19f,-.36f),new Vector2(.27f,-.43f),new Vector2(.41f,-.27f));
    }

    private Vector2 Point(Vector2 p) => rectTransform.rect.center + p * Mathf.Min(rectTransform.rect.width, rectTransform.rect.height);
    private void Line(Vector2 a, Vector2 b, float width)
    {
        a = Point(a); b = Point(b);
        Vector2 normal = new Vector2(-(b-a).y, (b-a).x).normalized * width / 2;
        int start = mesh.currentVertCount;
        mesh.AddVert(a-normal, color, Vector2.zero); mesh.AddVert(a+normal, color, Vector2.zero);
        mesh.AddVert(b+normal, color, Vector2.zero); mesh.AddVert(b-normal, color, Vector2.zero);
        mesh.AddTriangle(start,start+1,start+2); mesh.AddTriangle(start,start+2,start+3);
    }
    private void Stroke(float width, params Vector2[] points)
    {
        for (int i=1; i<points.Length; i++) Line(points[i-1],points[i],width);
    }
    private void Circle(Vector2 center, float radius, float width)
    {
        for (int i=0; i<32; i++)
        {
            float a = i * Mathf.PI * 2 / 32, b = (i+1) * Mathf.PI * 2 / 32;
            Line(center + new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,
                center + new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius,width);
        }
    }
}
