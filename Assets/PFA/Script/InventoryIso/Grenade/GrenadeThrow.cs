using UnityEngine;
using UnityEngine.InputSystem;

public class GrenadeThrow : MonoBehaviour
{
    [Header("Grenade")]
    [SerializeField] private GameObject grenadePrefab;
    [SerializeField] private Transform throwPoint;

    [Header("Throw Force")]
    [SerializeField] private float minThrowForce = 4f;
    [SerializeField] private float maxThrowForce = 18f;

    [Tooltip("Vitesse à laquelle la force augmente quand LT est maintenu.")]
    [SerializeField] private float chargeSpeed = 8f;

    [Tooltip("Vitesse à laquelle la force redescend quand LT est relâché.")]
    [SerializeField] private float dischargeSpeed = 12f;

    [Header("Trajectory")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private int trajectoryPoints = 30;
    [SerializeField] private float timeBetweenPoints = 0.08f;

    [Header("Input")]
    [SerializeField] private float triggerDeadzone = 0.2f;

    [Header("UI")]
    [SerializeField] private RuntimeInventoryUI inventoryUI;

    private float currentThrowForce;
    private bool isAiming = false;

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        currentThrowForce = minThrowForce;

        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }
    }

    private void Update()
    {

        if (!HasGrenadeEquipped())
        {
            ResetThrow();
            return;
        }

        HandleInput();
    }

    // =========================================================
    // INPUT
    // =========================================================

    private void HandleInput()
    {
        bool aimHeld = false;
        bool throwPressed = false;

        // =====================================================
        // CLAVIER DEBUG
        //
        // H = AIM / CHARGE
        // J = THROW
        // =====================================================

        if (Keyboard.current != null)
        {
            if (Keyboard.current.hKey.isPressed)
            {
                aimHeld = true;
            }

            if (Keyboard.current.jKey.wasPressedThisFrame)
            {
                throwPressed = true;
            }
        }

        // =====================================================
        // GAMEPAD
        //
        // LT = AIM / CHARGE
        // RT = THROW
        // =====================================================

        if (Gamepad.current != null)
        {
            float leftTrigger =
                Gamepad.current.leftTrigger.ReadValue();

            if (leftTrigger > triggerDeadzone)
            {
                aimHeld = true;
            }

            if (Gamepad.current.rightTrigger.wasPressedThisFrame)
            {
                throwPressed = true;
            }
        }

        // =====================================================
        // AIM / CHARGE
        // =====================================================

        UpdateThrowCharge(aimHeld);

        // =====================================================
        // THROW
        // =====================================================

        if (throwPressed && isAiming)
        {
            ThrowGrenade();
        }
    }

    // =========================================================
    // CHARGE
    // =========================================================

    private void UpdateThrowCharge(bool aimHeld)
    {
        if (aimHeld)
        {
            // =================================================
            // DÉBUT DE VISÉE
            // =================================================

            if (!isAiming)
            {
                isAiming = true;

                Debug.Log("[GRENADE] Visée activée.");

                // =============================================
                // AFFICHER RT
                // =============================================

                if (inventoryUI != null)
                {
                    Debug.Log(
                        "[GRENADE UI] Affichage du bouton RT."
                    );
                }
                else
                {
                    Debug.LogWarning(
                        "[GRENADE UI] RuntimeInventoryUI n'est pas assigné dans GrenadeThrow !"
                    );
                }
            }

            // =================================================
            // AUGMENTER LA FORCE
            // =================================================

            currentThrowForce =
                Mathf.MoveTowards(
                    currentThrowForce,
                    maxThrowForce,
                    chargeSpeed * Time.deltaTime
                );

            // =================================================
            // TRAJECTOIRE
            // =================================================

            if (lineRenderer != null)
            {
                lineRenderer.enabled = true;

                DrawTrajectory();
            }
        }
        else
        {
            // =================================================
            // FIN DE VISÉE
            // =================================================

            if (isAiming)
            {
                isAiming = false;

                Debug.Log("[GRENADE] Visée désactivée.");

            }

            // =================================================
            // DIMINUER LA FORCE
            // =================================================

            currentThrowForce =
                Mathf.MoveTowards(
                    currentThrowForce,
                    minThrowForce,
                    dischargeSpeed * Time.deltaTime
                );

            // =================================================
            // CACHER LA TRAJECTOIRE
            // =================================================

            if (lineRenderer != null)
            {
                lineRenderer.enabled = false;
            }
        }
    }

    // =========================================================
    // CHECK GRENADE
    // =========================================================

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

    // =========================================================
    // TRAJECTOIRE
    // =========================================================

    private void DrawTrajectory()
    {
        if (throwPoint == null)
            return;

        if (lineRenderer == null)
            return;

        Vector3 startPosition =
            throwPoint.position;

        // Utilise la force actuellement chargée.
        Vector3 startVelocity =
            throwPoint.forward *
            currentThrowForce;

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

    // =========================================================
    // THROW
    // =========================================================

    private void ThrowGrenade()
    {
        if (!HasGrenadeEquipped())
        {
            return;
        }

        if (grenadePrefab == null)
        {
            Debug.LogError(
                "[GRENADE] Grenade Prefab manquant."
            );

            return;
        }

        if (throwPoint == null)
        {
            Debug.LogError(
                "[GRENADE] ThrowPoint manquant."
            );

            return;
        }

        // Sauvegarde la force avant le Reset.
        float forceAtThrow =
            currentThrowForce;

        // =====================================================
        // CRÉATION GRENADE
        // =====================================================

        GameObject grenade =
            Instantiate(
                grenadePrefab,
                throwPoint.position,
                throwPoint.rotation
            );

        // =====================================================
        // RIGIDBODY
        // =====================================================

        Rigidbody rb =
            grenade.GetComponent<Rigidbody>();

        // Dans ton prefab le Rigidbody peut être
        // sur l'enfant Grenade.
        if (rb == null)
        {
            rb =
                grenade.GetComponentInChildren<Rigidbody>();
        }

        if (rb == null)
        {
            Debug.LogError(
                "[GRENADE] Aucun Rigidbody trouvé sur le prefab."
            );

            Destroy(grenade);

            return;
        }

        rb.isKinematic = false;
        rb.useGravity = true;

        // =====================================================
        // VELOCITY
        // =====================================================

        Vector3 velocity =
            throwPoint.forward *
            forceAtThrow;

        rb.linearVelocity =
            velocity;

        Debug.Log(
            "[GRENADE] LANCER | Force = "
            + forceAtThrow
            + " / "
            + maxThrowForce
        );

        // =====================================================
        // INVENTAIRE
        // =====================================================

        InventoryManager.Instance.RemoveQuickItem();

        // =====================================================
        // RESET
        // =====================================================

        ResetThrow();
    }

    // =========================================================
    // RESET
    // =========================================================

    private void ResetThrow()
    {

        isAiming = false;

        currentThrowForce =
            minThrowForce;

        // =====================================================
        // CACHER TRAJECTOIRE
        // =====================================================

        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }
    }
}