using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class RadialSegmentGraphic : MaskableGraphic
{
    private float innerRadius = 175f;
    private float outerRadius = 350f;
    private float segmentAngle = 45f;
    private float gapAngle = 5f;

    private const int Resolution = 20;

    public void Setup(
        float inner,
        float outer,
        float angle,
        float gap)
    {
        innerRadius = inner;
        outerRadius = outer;
        segmentAngle = angle;
        gapAngle = gap;

        SetAllDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        float angleSize =
            segmentAngle - gapAngle;

        float startAngle =
            -angleSize * 0.5f;

        for (int i = 0; i <= Resolution; i++)
        {
            float t =
                (float)i / Resolution;

            float angle =
                startAngle +
                angleSize * t;

            float rad =
                angle * Mathf.Deg2Rad;

            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);

            Vector2 inner =
                new Vector2(
                    cos * innerRadius,
                    sin * innerRadius
                );

            Vector2 outer =
                new Vector2(
                    cos * outerRadius,
                    sin * outerRadius
                );

            UIVertex v1 =
                UIVertex.simpleVert;

            v1.position = inner;
            v1.color = color;

            UIVertex v2 =
                UIVertex.simpleVert;

            v2.position = outer;
            v2.color = color;

            vh.AddVert(v1);
            vh.AddVert(v2);
        }

        for (int i = 0; i < Resolution; i++)
        {
            int a = i * 2;
            int b = a + 1;
            int c = a + 2;
            int d = a + 3;

            vh.AddTriangle(a, b, d);
            vh.AddTriangle(a, d, c);
        }
    }
}