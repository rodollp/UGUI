using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class PlayerBuffIcon : MaskableGraphic
{
    private bool isDexterity;

    public void SetDexterity(bool value)
    {
        if (isDexterity == value) return;
        isDexterity = value;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        if (isDexterity)
        {
            Stroke(mesh, new(-.40f, -.17f), new(-.06f, -.17f), new(.34f, .23f), new(.12f, .23f));
            Stroke(mesh, new(-.40f, .04f), new(-.11f, .04f), new(.15f, .30f));
            Stroke(mesh, new(-.25f, -.35f), new(.04f, -.35f), new(.31f, -.08f));
        }
        else
        {
            Stroke(mesh, new(-.31f, -.33f), new(0, -.05f), new(.31f, -.33f));
            Stroke(mesh, new(-.31f, 0), new(0, .28f), new(.31f, 0));
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
            Vector2 normal = Vector2.Perpendicular(to - from).normalized * (size * .051f);
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
