using UnityEngine;

public static class S_Physics_Extension
{
    static readonly RaycastHit[] arcHits = new RaycastHit[32];

    static public bool ArcCast(Vector3 center, Quaternion rotation, float angle, float radius, int resolution, LayerMask layer, out RaycastHit hit, bool drawGizmo = false, Transform ignoredRoot = null)
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

        for (int i = 0; i < resolution; i++)
        {
            A = center + rotation * forwardRadius;
            rotation *= Quaternion.Euler(dAngle, 0, 0);
            B = center + rotation * forwardRadius;
            AB = B - A;

            int hitCount = Physics.RaycastNonAlloc(A, AB, arcHits, AB_magnitude, layer);
            bool foundHit = false;
            float nearestHitDistance = float.PositiveInfinity;

            for (int hitIndex = 0; hitIndex < hitCount; hitIndex++)
            {
                RaycastHit candidate = arcHits[hitIndex];
                Transform candidateTransform = candidate.transform;

                if (ignoredRoot != null && (candidateTransform == ignoredRoot || candidateTransform.IsChildOf(ignoredRoot)))
                    continue;

                if (candidate.distance < nearestHitDistance)
                {
                    hit = candidate;
                    nearestHitDistance = candidate.distance;
                    foundHit = true;
                }
            }

            if (foundHit)
            {
                if (drawGizmo)
                    Gizmos.DrawLine(A, hit.point);

                return true;
            }

            if (drawGizmo)
                Gizmos.DrawLine(A, B);
        }

        hit = new RaycastHit();
        return false;
    }
}