using UnityEngine;
using UnityEditor;

[CustomEditor (typeof (S_Field_Of_View_Target))]
public class S_Field_Of_View_Target_Editor : Editor
{
    void OnSceneGUI()
    {
        S_Field_Of_View_Target fow = (S_Field_Of_View_Target)target;

        Handles.color = Color.white;
        Handles.DrawWireArc(fow.transform.position, Vector3.up, Vector3.forward, 360, fow.viewRadius);

        Vector3 viewAngleA = fow.DirFromAngle(-fow.viewAngle / 2, false);
        Vector3 viewAngleB = fow.DirFromAngle(fow.viewAngle / 2, false);

        Handles.DrawLine(fow.transform.position, fow.transform.position + viewAngleA * fow.viewRadius);
        Handles.DrawLine(fow.transform.position, fow.transform.position + viewAngleB * fow.viewRadius);

        Handles.color = Color.red;

        foreach (Transform visibleTarget in fow.visibleCharacter)
        {
            Handles.DrawLine(fow.transform.position, visibleTarget.position);
        }
    }
}
