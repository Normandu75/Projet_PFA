using UnityEngine;

/// <summary>
/// Génère et colore le mesh du cône de détection affiché au sol.
/// Appelez SetColor(...) depuis EnemyAI à chaque changement d'état.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class EnemyVisionCone : MonoBehaviour
{
    [SerializeField] private int segments = 20;
    [SerializeField] private float yOffset = 0.05f; // évite le z-fighting avec le sol

    private Mesh _mesh;
    private MeshRenderer _renderer;
    private MaterialPropertyBlock _propBlock;

    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor"); // URP/Lit

    private void Awake()
    {
        _mesh = new Mesh { name = "VisionCone" };
        GetComponent<MeshFilter>().mesh = _mesh;
        _renderer = GetComponent<MeshRenderer>();
        _propBlock = new MaterialPropertyBlock();
    }

    /// <summary>Reconstruit le mesh du cône selon l'angle/distance courants.</summary>
    public void Rebuild(float viewAngle, float viewDistance)
    {
        int vertCount = segments + 2;
        Vector3[] vertices = new Vector3[vertCount];
        int[] triangles = new int[segments * 3];

        vertices[0] = new Vector3(0f, yOffset, 0f);

        float halfAngle = viewAngle * 0.5f;
        float startAngle = -halfAngle;
        float step = viewAngle / segments;

        for (int i = 0; i <= segments; i++)
        {
            float angleRad = (startAngle + step * i) * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Sin(angleRad), 0f, Mathf.Cos(angleRad));
            vertices[i + 1] = dir * viewDistance + new Vector3(0f, yOffset, 0f);
        }

        for (int i = 0; i < segments; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }

        _mesh.Clear();
        _mesh.vertices = vertices;
        _mesh.triangles = triangles;
        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();
    }

    public void SetColor(Color color)
    {
        if (_renderer == null) return;
        _renderer.GetPropertyBlock(_propBlock);
        _propBlock.SetColor(ColorId, color);
        _propBlock.SetColor(BaseColorId, color);
        _renderer.SetPropertyBlock(_propBlock);
    }

    public void SetVisible(bool visible)
    {
        if (_renderer != null) _renderer.enabled = visible;
    }
}
