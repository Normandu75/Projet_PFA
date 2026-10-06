using System.Collections.Generic;
using UnityEngine;


[DefaultExecutionOrder(1)]
public class S_Pivot_Players : MonoBehaviour
{
    [SerializeField] Transform pivot;
    [SerializeField] S_Scan scan;
    [SerializeField, Range(0, 1)] float positionWeight = 0;
    [SerializeField, Range(0, 1)] float rotationWeight = 1;
    [SerializeField, Min(0f)] float rotationSpeed = 180f;
    [SerializeField, Min(0.1f)] float rotationSmoothing = 3f;

    S_Robot_Controller robotController;

    public Transform Pivot { get => pivot; }

    void Awake()
    {
        robotController = GetComponent<S_Robot_Controller>();
    }

    void OnDisable()
    {
        if (robotController != null)
            robotController.SetScanSurfaceTarget(Quaternion.identity, false);

        if (pivot != null)
        {
            pivot.localPosition = Vector3.zero;
            pivot.localRotation = Quaternion.identity;
        }
    }

    void Update()
    {
        UpdatePivot();
    }

    void UpdatePivot()
    {
        if (pivot == null || scan == null)
        {
            if (robotController != null)
                robotController.SetScanSurfaceTarget(Quaternion.identity, false);
            return;
        }

        List<(Vector3 pos, Quaternion rot, float weight)> points = scan.Points();
        if (points == null || points.Count == 0)
        {
            if (robotController != null)
                robotController.SetScanSurfaceTarget(Quaternion.identity, false);
            return;
        }

        Quaternion rotAvg;
        List<Quaternion> rots = new List<Quaternion>();
        List<float> weights = new List<float>();

        Vector3 posAvg = Vector3.zero;
        int nbPoint = 0;

        foreach ((Vector3 pos, Quaternion rot, float weight) point in points)
        {
            rots.Add(point.rot);
            weights.Add(point.weight);
            posAvg += point.pos;
            nbPoint++;
        }

        if (nbPoint == 0)
            return;

        posAvg /= nbPoint;
        rotAvg = S_Math_Extension.QuatAvgApprox(rots.ToArray(), weights.ToArray());
        float rotationMagnitude = rotAvg.x * rotAvg.x + rotAvg.y * rotAvg.y + rotAvg.z * rotAvg.z + rotAvg.w * rotAvg.w;
        if (rotationMagnitude < 0.000001f || float.IsNaN(rotationMagnitude) || float.IsInfinity(rotationMagnitude))
        {
            rotAvg = pivot.rotation;
            if (robotController != null)
                robotController.SetScanSurfaceTarget(Quaternion.identity, false);
        }
        else
        {
            if (robotController != null)
                robotController.SetScanSurfaceTarget(rotAvg, true);
        }

        pivot.position = Vector3.Lerp(transform.position, posAvg, positionWeight);
        Quaternion targetRotation = Quaternion.Lerp(transform.rotation, rotAvg, rotationWeight);
        float smoothingFactor = 1f - Mathf.Exp(-rotationSmoothing * Time.deltaTime);
        Quaternion smoothedTarget = Quaternion.Slerp(pivot.rotation, targetRotation, smoothingFactor);
        pivot.rotation = Quaternion.RotateTowards(pivot.rotation, smoothedTarget, rotationSpeed * Time.deltaTime);
    }
}