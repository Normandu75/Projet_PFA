using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class S_Procedural_Animation : MonoBehaviour
{
    [Header("Steps")]
    [SerializeField, Min(0.01f)] float stepDistance = 0.2f;
    [SerializeField, Min(0f)] float stepHeight = 0.12f;
    [SerializeField, Min(0.01f)] float stepSpeed = 5f;

    [Header("Feet")]
    [SerializeField] Transform[] legIkTargets;
    [SerializeField] LayerMask groundLayers;
    [SerializeField, Min(0f)] float sphereCastRadius = 0.08f;
    [SerializeField, Min(0f)] float groundProbeHeight = 0.25f;
    [SerializeField, Min(0.01f)] float groundProbeDistance = 0.75f;

    sealed class LegState
    {
        public int index;
        public Transform target;
        public Vector3 homeLocalPosition;
        public Vector3 plantedPosition;
        public Vector3 stepStart;
        public Vector3 stepEnd;
        public float stepStartTime;
        public int lastStepCycle = int.MinValue;
        public bool isStepping;
    }

    Rigidbody rb;
    LegState[] legs;
    float gaitClock;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        InitializeLegs();
    }

    void LateUpdate()
    {
        if (legs == null)
            return;

        Vector3 horizontalVelocity = Vector3.ProjectOnPlane(rb.linearVelocity, Vector3.up);
        float speed = horizontalVelocity.magnitude;
        float frequency = speed / Mathf.Max(stepDistance * 2f, 0.01f);
        bool isMoving = speed > 0.05f && frequency > 0.01f;

        if (!isMoving)
        {
            ResetFeetToHomePositions();
            return;
        }

        gaitClock += Time.deltaTime * frequency;
        float stepDuration = Mathf.Min(stepDistance / stepSpeed, 0.45f / frequency);
        float swingFraction = Mathf.Clamp(stepDuration * frequency, 0.05f, 0.45f);
        Vector3 moveDirection = horizontalVelocity / speed;

        foreach (LegState leg in legs)
        {
            if (leg == null || leg.target == null)
                continue;

            int cycle = Mathf.FloorToInt(gaitClock + GetPhaseOffset(leg.index));
            float phase = Mathf.Repeat(gaitClock + GetPhaseOffset(leg.index), 1f);

            if (!leg.isStepping && phase >= 1f - swingFraction && leg.lastStepCycle != cycle)
                BeginStep(leg, cycle, moveDirection);

            if (leg.isStepping)
                UpdateStep(leg, stepDuration);
            else
                leg.target.position = leg.plantedPosition;
        }
    }

    void InitializeLegs()
    {
        if (legIkTargets == null || legIkTargets.Length == 0)
        {
            Debug.LogWarning("S_Procedural_Animation n'a aucune cible IK de patte assignée.", this);
            legs = new LegState[0];
            return;
        }

        legs = new LegState[legIkTargets.Length];
        for (int i = 0; i < legIkTargets.Length; i++)
        {
            Transform target = legIkTargets[i];
            if (target == null)
            {
                Debug.LogWarning($"La cible IK de la patte {i} n'est pas assignée.", this);
                continue;
            }

            legs[i] = new LegState
            {
                index = i,
                target = target,
                homeLocalPosition = transform.InverseTransformPoint(target.position),
                plantedPosition = target.position
            };
        }
    }

    void ResetFeetToHomePositions()
    {
        foreach (LegState leg in legs)
        {
            if (leg == null || leg.target == null)
                continue;

            leg.isStepping = false;
            leg.plantedPosition = transform.TransformPoint(leg.homeLocalPosition);
            leg.target.position = leg.plantedPosition;
        }
    }

    float GetPhaseOffset(int index)
    {
        if (legs.Length == 4)
            return index == 0 || index == 3 ? 0f : 0.5f;

        return index / (float)legs.Length;
    }

    void BeginStep(LegState leg, int cycle, Vector3 moveDirection)
    {
        Vector3 homePosition = transform.TransformPoint(leg.homeLocalPosition);
        Vector3 probeOrigin = homePosition
            + moveDirection * stepDistance
            + Vector3.up * groundProbeHeight;
        int layerMask = groundLayers.value != 0 ? groundLayers.value : Physics.DefaultRaycastLayers;

        RaycastHit[] hits = Physics.SphereCastAll(
            probeOrigin,
            sphereCastRadius,
            Vector3.down,
            groundProbeDistance,
            layerMask,
            QueryTriggerInteraction.Ignore);

        float nearestDistance = float.PositiveInfinity;
        Vector3 landingPosition = homePosition;

        foreach (RaycastHit hit in hits)
        {
            if (hit.rigidbody == rb || hit.transform == transform || hit.transform.IsChildOf(transform))
                continue;

            if (hit.distance >= nearestDistance)
                continue;

            nearestDistance = hit.distance;
            Vector3 horizontalOffset = Vector3.ProjectOnPlane(hit.point - homePosition, Vector3.up);
            landingPosition = homePosition + Vector3.ClampMagnitude(horizontalOffset, stepDistance);
            landingPosition.y = hit.point.y;
        }

        if (float.IsPositiveInfinity(nearestDistance))
            return;

        leg.lastStepCycle = cycle;
        leg.isStepping = true;
        leg.stepStart = leg.plantedPosition;
        leg.stepEnd = landingPosition;
        leg.stepStartTime = Time.time;
    }

    void UpdateStep(LegState leg, float stepDuration)
    {
        float progress = Mathf.Clamp01((Time.time - leg.stepStartTime) / stepDuration);
        Vector3 position = Vector3.Lerp(leg.stepStart, leg.stepEnd, progress);
        position += Vector3.up * (Mathf.Sin(progress * Mathf.PI) * stepHeight);
        leg.target.position = position;

        if (progress < 1f)
            return;

        leg.plantedPosition = leg.stepEnd;
        leg.target.position = leg.plantedPosition;
        leg.isStepping = false;
    }
}
