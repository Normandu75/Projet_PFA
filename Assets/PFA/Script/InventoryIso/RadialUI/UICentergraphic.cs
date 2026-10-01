using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class UICircleGraphic : MaskableGraphic
{
    [SerializeField, Range(12, 128)]
    private int segments = 64;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect rect = rectTransform.rect;

        float radius =
            Mathf.Min(
                rect.width,
                rect.height
            ) * 0.5f;

        // Centre
        UIVertex center =
            UIVertex.simpleVert;

        center.position =
            Vector2.zero;

        center.color =
            color;

        vh.AddVert(center);

        // Bord du cercle
        for (int i = 0; i <= segments; i++)
        {
            float angle =
                ((float)i / segments) *
                Mathf.PI * 2f;

            Vector2 position =
                new Vector2(
                    Mathf.Cos(angle),
                    Mathf.Sin(angle)
                ) * radius;

            UIVertex vertex =
                UIVertex.simpleVert;

            vertex.position =
                position;

            vertex.color =
                color;

            vh.AddVert(vertex);
        }

        // Triangles
        for (int i = 0; i < segments; i++)
        {
            vh.AddTriangle(
                0,
                i + 1,
                i + 2
            );
        }
    }
}
