using UnityEngine;
using UnityEngine.UI;

public class RadialSegment : Graphic
{
    private float innerRadius;
    private float outerRadius;

    private float startAngle;
    private float endAngle;

    private int subdivisions = 20;

    public void Setup(
        float startAngle,
        float endAngle,
        float innerRadius,
        float outerRadius)
    {
        this.startAngle = startAngle;
        this.endAngle = endAngle;
        this.innerRadius = innerRadius;
        this.outerRadius = outerRadius;

        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        int count = subdivisions;

        for (int i = 0; i <= count; i++)
        {
            float t = (float)i / count;

            float angle = Mathf.Lerp(
                startAngle,
                endAngle,
                t
            );

            float radians =
                angle * Mathf.Deg2Rad;

            Vector2 outer =
                new Vector2(
                    Mathf.Cos(radians) * outerRadius,
                    Mathf.Sin(radians) * outerRadius
                );

            Vector2 inner =
                new Vector2(
                    Mathf.Cos(radians) * innerRadius,
                    Mathf.Sin(radians) * innerRadius
                );

            vh.AddVert(
                outer,
                color,
                Vector2.zero
            );

            vh.AddVert(
                inner,
                color,
                Vector2.zero
            );
        }

        for (int i = 0; i < count; i++)
        {
            int index = i * 2;

            vh.AddTriangle(
                index,
                index + 2,
                index + 1
            );

            vh.AddTriangle(
                index + 1,
                index + 2,
                index + 3
            );
        }
    }
}