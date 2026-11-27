using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class AdventureShortcutIcon : MaskableGraphic
{
    public bool ShowMap { get; set; }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        if (ShowMap)
        {
            Draw(mesh, new Vector2(-.45f, .23f), new Vector2(-.15f, .36f), new Vector2(.15f, .23f),
                new Vector2(.45f, .36f), new Vector2(.45f, -.23f), new Vector2(.15f, -.36f),
                new Vector2(-.15f, -.23f), new Vector2(-.45f, -.36f), new Vector2(-.45f, .23f));
            Draw(mesh, new Vector2(-.15f, .36f), new Vector2(-.15f, -.23f));
            Draw(mesh, new Vector2(.15f, .23f), new Vector2(.15f, -.36f));
        }
        else
        {
            Draw(mesh, new Vector2(-.28f, .40f), new Vector2(.24f, .40f), new Vector2(.24f, -.32f),
                new Vector2(-.28f, -.32f), new Vector2(-.28f, .40f));
            Draw(mesh, new Vector2(.24f, .30f), new Vector2(.40f, .26f), new Vector2(.34f, -.46f),
                new Vector2(-.18f, -.42f), new Vector2(-.17f, -.32f));
            Draw(mesh, new Vector2(-.16f, .22f), new Vector2(.12f, .22f));
            Draw(mesh, new Vector2(-.16f, .06f), new Vector2(.12f, .06f));
        }
    }

    private void Draw(VertexHelper mesh, params Vector2[] points)
    {
        Rect rect = rectTransform.rect;
        float scale = Mathf.Min(rect.width, rect.height);
        for (int i = 1; i < points.Length; i++)
        {
            Vector2 from = rect.center + points[i - 1] * scale;
            Vector2 to = rect.center + points[i] * scale;
            Vector2 normal = Vector2.Perpendicular(to - from).normalized * (scale * .03f);
            int start = mesh.currentVertCount;
            mesh.AddVert(from - normal, color, Vector2.zero);
            mesh.AddVert(from + normal, color, Vector2.zero);
            mesh.AddVert(to + normal, color, Vector2.zero);
            mesh.AddVert(to - normal, color, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2);
            mesh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
