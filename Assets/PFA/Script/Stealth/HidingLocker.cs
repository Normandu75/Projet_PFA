using UnityEngine;

/// <summary>
/// Casier / cachette. Le joueur interagit avec (depuis un script d'input séparé)
/// via EnterLocker/Exit sur PlayerStealth. Un ennemi peut le "casser" s'il repère
/// le joueur à l'intérieur alors que le casier est dans son cône de détection.
/// </summary>
public class HidingLocker : MonoBehaviour
{
    [Tooltip("Point où le joueur est repositionné quand il se cache (optionnel).")]
    public Transform hidePoint;

    [SerializeField] private GameObject brokenVFXPrefab;
    [SerializeField] private Animator doorAnimator; // optionnel, anim d'ouverture/casse

    public bool IsOccupied { get; private set; }
    public bool IsBroken { get; private set; }

    public void SetOccupied(bool occupied)
    {
        IsOccupied = occupied;
    }

    /// <summary>Appelé par l'ennemi quand il casse le casier.</summary>
    public void Break()
    {
        if (IsBroken) return;
        IsBroken = true;

        if (doorAnimator != null)
            doorAnimator.SetTrigger("Break");

        if (brokenVFXPrefab != null)
            Instantiate(brokenVFXPrefab, transform.position, transform.rotation);

        // Le joueur est forcé de sortir de sa cachette : il vient d'être repéré.
        if (PlayerStealth.Instance != null && PlayerStealth.Instance.CurrentLocker == this)
        {
            PlayerStealth.Instance.ExitLocker();
        }
    }

    public Vector3 Position => hidePoint != null ? hidePoint.position : transform.position;
}
