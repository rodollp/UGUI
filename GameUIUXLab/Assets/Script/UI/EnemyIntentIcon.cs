using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class EnemyIntentIcon : MaskableGraphic
{
    private EnemyActionType kind;

    public void SetKind(EnemyActionType value)
    {
        if (kind == value) return;
        kind = value;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        switch (kind)
        {
            case EnemyActionType.Block:
                Stroke(mesh, new(-.34f, .31f), new(.34f, .31f), new(.30f, -.06f),
                    new(.18f, -.26f), new(0, -.40f), new(-.18f, -.26f),
                    new(-.30f, -.06f), new(-.34f, .31f));
                Stroke(mesh, new(-.15f, .01f), new(-.04f, -.10f), new(.17f, .13f));
                break;
            case EnemyActionType.Strength:
                Stroke(mesh, new(-.34f, -.01f), new(0, .32f), new(.34f, -.01f));
                Stroke(mesh, new(-.34f, -.34f), new(0, -.01f), new(.34f, -.34f));
                break;
            case EnemyActionType.Debuff:
                Stroke(mesh, new(0, .40f), new(.36f, 0), new(0, -.40f), new(-.36f, 0), new(0, .40f));
                Stroke(mesh, new(0, .17f), new(0, -.04f));
                Stroke(mesh, new(0, -.16f), new(0, -.20f));
                break;
            default:
                Stroke(mesh, new(-.18f, -.04f), new(.16f, .30f), new(.38f, .38f),
                    new(.30f, .16f), new(-.04f, -.18f));
                Stroke(mesh, new(-.29f, .04f), new(.04f, -.29f));
                Stroke(mesh, new(-.13f, -.13f), new(-.36f, -.36f));
                break;
        }
    }

    private void Stroke(VertexHelper mesh, params Vector2[] points)
    {
        Rect rect = rectTransform.rect;
        float size = Mathf.Min(rect.width, rect.height);
        for (int i = 1; i < points.Length; i++)
        {
            Vector2 from = rect.center + points[i - 1] * size;
            Vector2 to = rect.center + points[i] * size;
            Vector2 normal = Vector2.Perpendicular(to - from).normalized * (size * .043f);
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
