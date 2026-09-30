using UnityEngine;
using UnityEditor;

[CustomEditor (typeof (S_Field_Of_View))]
public class S_Field_Of_View_Editor : Editor
{
    void OnSceneGUI()
    {
        S_Field_Of_View fow = (S_Field_Of_View)target;
        Vector3 origin = fow.transform.position;
        Vector3 forward = fow.ViewDirection.normalized;
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

        if (right.sqrMagnitude < 0.001f)
        {
            right = Vector3.Cross(Vector3.forward, forward).normalized;
        }

        Vector3 up = Vector3.Cross(forward, right).normalized;
        float halfAngle = Mathf.Clamp(fow.viewAngle, 0f, 179f) * 0.5f * Mathf.Deg2Rad;
        float endRadius = Mathf.Tan(halfAngle) * fow.viewRadius;

        Handles.color = Color.white;
        Handles.DrawWireDisc(origin + forward * (fow.viewRadius * 0.5f), forward, endRadius * 0.5f);
        Handles.DrawWireDisc(origin + forward * fow.viewRadius, forward, endRadius);

        for (int i = 0; i < 8; i++)
        {
            float angle = i * Mathf.PI / 4f;
            Vector3 rimDirection = right * Mathf.Cos(angle) + up * Mathf.Sin(angle);
            Handles.DrawLine(origin, origin + forward * fow.viewRadius + rimDirection * endRadius);
        }

        Handles.color = Color.red;

        foreach (Transform visibleTarget in fow.visibleTargets)
        {
            Handles.DrawLine(origin, visibleTarget.position);
        }
    }
}
