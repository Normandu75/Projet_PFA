using UnityEngine;
using UnityEditor;


[CustomEditor(typeof(S_Character_Controller))]
public class S_Character_Editor : Editor
{
    private void OnSceneGUI()
    {
        S_Character_Controller character = (S_Character_Controller)target;
        if (character == null || character.detectionRadius <= 0f)
        {
            return;
        }

        Color previousColor = Handles.color;
        Handles.color = Color.yellow;

        Vector3 center = character.transform.position;
        float radius = character.detectionRadius;
        Handles.DrawWireDisc(center, Vector3.up, radius);
        Handles.DrawWireDisc(center, Vector3.right, radius);
        Handles.DrawWireDisc(center, Vector3.forward, radius);

        Handles.color = previousColor;
    }
}
