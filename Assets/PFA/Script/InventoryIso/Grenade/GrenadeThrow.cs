using UnityEngine;
using UnityEngine.InputSystem;

public class GrenadeThrow : MonoBehaviour
{
    [Header("Grenade")]
    [SerializeField] private GameObject grenadePrefab;
    [SerializeField] private Transform throwPoint;

    [Header("Throw")]
    [SerializeField] private float throwForce = 10f;

    [Header("Trajectory")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private int trajectoryPoints = 30;
    [SerializeField] private float timeBetweenPoints = 0.1f;

    [Header("Input")]
    [SerializeField] private float triggerDeadzone = 0.2f;

    private bool isAiming = false;

    private void Awake()
    {
        if (lineRenderer != null)
            lineRenderer.enabled = false;
    }

    private void Update()
    {
        // ==========================================
        // Vérifier qu'une grenade est équipée
        // ==========================================

        if (!HasGrenadeEquipped())
        {
            StopAiming();
            return;
        }

        HandleInput();
    }

    // =====================================================
    // INPUT
    // =====================================================

    private void HandleInput()
    {
        bool aimHeld = false;
        bool throwPressed = false;

        // ==========================================
        // CLAVIER DEBUG
        //
        // H = viser
        // J = lancer
        // ==========================================

        if (Keyboard.current != null)
        {
            if (Keyboard.current.hKey.isPressed)
            {
                aimHeld = true;
            }

            if (Keyboard.current.jKey.wasPressedThisFrame)
            {
                throwPressed = true;

                Debug.Log(
                    "[GRENADE] J pressé -> demande de lancer."
                );
            }
        }

        // ==========================================
        // MANETTE
        //
        // LT = viser
        // RT = lancer
        // ==========================================

        if (Gamepad.current != null)
        {
            float lt =
                Gamepad.current.leftTrigger.ReadValue();

            if (lt > triggerDeadzone)
            {
                aimHeld = true;
            }

            if (Gamepad.current.rightTrigger.wasPressedThisFrame)
            {
                throwPressed = true;

                Debug.Log(
                    "[GRENADE] RT pressé -> demande de lancer."
                );
            }
        }

        // ==========================================
        // VISÉE
        // ==========================================

        if (aimHeld)
        {
            StartAiming();
        }
        else
        {
            StopAiming();
        }

        // ==========================================
        // LANCER
        // ==========================================

        if (throwPressed)
        {
            if (isAiming)
            {
                ThrowGrenade();
            }
            else
            {
                Debug.LogWarning(
                    "[GRENADE] Lancer refusé : " +
                    "il faut maintenir H ou LT."
                );
            }
        }
    }

    // =====================================================
    // CHECK INVENTAIRE
    // =====================================================

    private bool HasGrenadeEquipped()
    {
        if (InventoryManager.Instance == null)
        {
            return false;
        }

        InventoryItem item =
            InventoryManager.Instance.GetQuickItem();

        if (item == null)
        {
            return false;
        }

        if (InventoryManager.Instance.GetQuickItemAmount() <= 0)
        {
            return false;
        }

        return item.itemType ==
               InventoryItemType.Grenade;
    }

    // =====================================================
    // VISÉE
    // =====================================================

    private void StartAiming()
    {
        // Log seulement au début,
        // pas toutes les frames.
        if (!isAiming)
        {
            Debug.Log(
                "[GRENADE] Visée activée."
            );
        }

        isAiming = true;

        if (lineRenderer == null)
        {
            Debug.LogError(
                "[GRENADE] LineRenderer manquant."
            );

            return;
        }

        lineRenderer.enabled = true;

        DrawTrajectory();
    }

    private void StopAiming()
    {
        if (isAiming)
        {
            Debug.Log(
                "[GRENADE] Visée désactivée."
            );
        }

        isAiming = false;

        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }
    }

    // =====================================================
    // TRAJECTOIRE
    // =====================================================

    private void DrawTrajectory()
    {
        if (throwPoint == null)
            return;

        if (lineRenderer == null)
            return;

        Vector3 startPosition =
            throwPoint.position;

        Vector3 startVelocity =
            throwPoint.forward * throwForce;

        lineRenderer.positionCount =
            trajectoryPoints;

        for (int i = 0; i < trajectoryPoints; i++)
        {
            float time =
                i * timeBetweenPoints;

            Vector3 position =
                startPosition
                + startVelocity * time
                + 0.5f
                * Physics.gravity
                * time
                * time;

            lineRenderer.SetPosition(
                i,
                position
            );
        }
    }

    // =====================================================
    // LANCER
    // =====================================================

    private void ThrowGrenade()
    {
        Debug.Log(
            "[GRENADE] ThrowGrenade() appelé."
        );

        // ==========================================
        // PREFAB
        // ==========================================

        if (grenadePrefab == null)
        {
            Debug.LogError(
                "[GRENADE] Grenade Prefab non assigné !"
            );

            return;
        }

        // ==========================================
        // THROW POINT
        // ==========================================

        if (throwPoint == null)
        {
            Debug.LogError(
                "[GRENADE] ThrowPoint non assigné !"
            );

            return;
        }

        // ==========================================
        // CRÉER LA GRENADE
        // ==========================================

        GameObject grenade =
            Instantiate(
                grenadePrefab,
                throwPoint.position,
                throwPoint.rotation
            );

        Debug.Log(
            "[GRENADE] Grenade créée : "
            + grenade.name
        );

        // ==========================================
        // RIGIDBODY
        // ==========================================

        Rigidbody rb =
            grenade.GetComponent<Rigidbody>();

        if (rb == null)
        {
            // Au cas où le Rigidbody serait
            // sur un enfant du prefab.
            rb =
                grenade.GetComponentInChildren<Rigidbody>();
        }

        if (rb == null)
        {
            Debug.LogError(
                "[GRENADE] Aucun Rigidbody trouvé " +
                "sur le prefab !"
            );

            Destroy(grenade);

            return;
        }

        Debug.Log(
            "[GRENADE] Rigidbody trouvé."
        );

        // Au cas où le prefab serait configuré
        // incorrectement.
        rb.isKinematic = false;
        rb.useGravity = true;

        // ==========================================
        // VITESSE
        // ==========================================

        Vector3 velocity =
            throwPoint.forward * throwForce;

        rb.linearVelocity = velocity;

        Debug.Log(
            "[GRENADE] LANCÉE ! Velocity = "
            + velocity
        );

        // ==========================================
        // INVENTAIRE
        // ==========================================

        bool removed =
            InventoryManager.Instance.RemoveQuickItem();

        Debug.Log(
            "[GRENADE] Retrait inventaire = "
            + removed
        );

        // ==========================================
        // FIN VISÉE
        // ==========================================

        StopAiming();
    }
}