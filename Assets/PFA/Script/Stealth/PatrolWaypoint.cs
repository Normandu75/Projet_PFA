using UnityEngine;

/// <summary>
/// Un point de la patrouille. Si une porte est assignée, l'ennemi l'ouvrira
/// avant de continuer vers ce point.
/// </summary>
public class PatrolWaypoint : MonoBehaviour
{
    [Tooltip("Porte à ouvrir pour atteindre ce point (laisser vide si aucune).")]
    public Door doorToOpen;

    [Tooltip("Temps d'attente de l'ennemi une fois arrivé sur ce point.")]
    public float waitTime = 1.5f;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
    }
}
