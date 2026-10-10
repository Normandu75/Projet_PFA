
using UnityEngine;

public class PlayerDecoyAbility : MonoBehaviour
{
    [Header("Clone")]
    [SerializeField] private GameObject clonePrefab;
    [SerializeField] private float cloneDuration = 6f;

    private DecoyClone activeClone;

    public DecoyClone ActiveClone => activeClone;

    public bool ActivateClone()
    {
        // Un clone est déjà actif.
        if (DecoyClone.IsActive || activeClone != null)
            return false;

        if (clonePrefab == null)
        {
            Debug.LogWarning("Clone Prefab non assigné.");
            return false;
        }

        GameObject cloneObject = Instantiate(
            clonePrefab,
            transform.position,
            transform.rotation
        );

        DecoyClone clone = cloneObject.GetComponent<DecoyClone>();

        if (clone == null)
        {
            Debug.LogError("Le prefab ne contient pas DecoyClone.");
            Destroy(cloneObject);
            return false;
        }

        activeClone = clone;

        // Active le clone pendant sa durée de vie.
        clone.Activate(cloneDuration);

        return true;
    }
}

