using UnityEngine;

public static class S_Physics_Extension
{
    static readonly RaycastHit[] arcHits = new RaycastHit[32];

    static public bool ArcCast(
        Vector3 center,
        Quaternion rotation,
        float angle,
        float radius,
        int resolution,
        LayerMask layer,
        out RaycastHit hit,
        bool drawGizmo = false,
        Transform ignoredRoot = null,
        Vector3? referenceNormal = null,
        Vector3? transitionDirection = null,
        float minimumNormalDot = 0.5f,
        bool prioritizeNormalTransition = false,
        int preferredTransitionLayer = -1)
    {
        hit = new RaycastHit();
        rotation *= Quaternion.Euler(-angle/2, 0, 0);

        float dAngle = angle / resolution;
        Vector3 forwardRadius = Vector3.forward * radius;

        Vector3 A, B, AB;
        A = forwardRadius;
        B = Quaternion.Euler(dAngle, 0, 0) * forwardRadius;
        AB = B - A;
        float AB_magnitude = AB.magnitude * 1.001f;
        RaycastHit fallbackHit = default;
        RaycastHit transitionHit = default;
        bool hasFallbackHit = false;
        bool hasTransitionHit = false;
        float bestTransitionScore = float.NegativeInfinity;

        for (int i = 0; i < resolution; i++)
        {
            A = center + rotation * forwardRadius;
            rotation *= Quaternion.Euler(dAngle, 0, 0);
            B = center + rotation * forwardRadius;
            AB = B - A;

            int hitCount = Physics.RaycastNonAlloc(A, AB, arcHits, AB_magnitude, layer);
            bool foundHit = false;
            float nearestHitDistance = float.PositiveInfinity;
            RaycastHit nearestHit = default;
            bool foundTransitionInSegment = false;
            float segmentTransitionScore = float.NegativeInfinity;
            RaycastHit segmentTransitionHit = default;

            for (int hitIndex = 0; hitIndex < hitCount; hitIndex++)
            {
                RaycastHit candidate = arcHits[hitIndex];
                Transform candidateTransform = candidate.transform;

                if (ignoredRoot != null && (candidateTransform == ignoredRoot || candidateTransform.IsChildOf(ignoredRoot)))
                    continue;

                if (referenceNormal.HasValue
                    && transitionDirection.HasValue
                    && Vector3.Dot(candidate.normal, referenceNormal.Value) < minimumNormalDot)
                {
                    Vector3 direction = transitionDirection.Value;
                    if (direction.sqrMagnitude < 0.0001f
                        || Vector3.Dot(direction.normalized, -candidate.normal) <= minimumNormalDot)
                        continue;

                    bool matchesPreferredLayer = preferredTransitionLayer < 0
                        || IsInLayerHierarchy(candidateTransform, preferredTransitionLayer);
                    float transitionScore = Vector3.Dot(direction.normalized, -candidate.normal);
                    if (matchesPreferredLayer
                        && (transitionScore > segmentTransitionScore || !foundTransitionInSegment))
                    {
                        segmentTransitionScore = transitionScore;
                        segmentTransitionHit = candidate;
                        foundTransitionInSegment = true;
                    }
                }

                if (candidate.distance < nearestHitDistance)
                {
                    nearestHit = candidate;
                    nearestHitDistance = candidate.distance;
                    foundHit = true;
                }
            }

            if (foundHit)
            {
                if (!prioritizeNormalTransition)
                {
                    hit = nearestHit;
                    if (drawGizmo)
                        Gizmos.DrawLine(A, hit.point);

                    return true;
                }

                if (!hasFallbackHit)
                {
                    fallbackHit = nearestHit;
                    hasFallbackHit = true;
                }

                if (foundTransitionInSegment
                    && (!hasTransitionHit || segmentTransitionScore > bestTransitionScore))
                {
                    transitionHit = segmentTransitionHit;
                    bestTransitionScore = segmentTransitionScore;
                    hasTransitionHit = true;
                }
            }

            if (drawGizmo)
                Gizmos.DrawLine(A, B);
        }

        if (prioritizeNormalTransition && (hasTransitionHit || hasFallbackHit))
        {
            hit = hasTransitionHit ? transitionHit : fallbackHit;
            if (drawGizmo)
                Gizmos.DrawLine(center, hit.point);
            return true;
        }

        hit = new RaycastHit();
        return false;
    }

    public static bool IsInLayerHierarchy(Transform target, int layer)
    {
        if (target == null || layer < 0)
            return false;

        for (Transform current = target; current != null; current = current.parent)
        {
            if (current.gameObject.layer == layer)
                return true;
        }

        return false;
    }
}