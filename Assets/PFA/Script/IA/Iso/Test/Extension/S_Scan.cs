using System.Collections.Generic;
using UnityEngine;

public abstract class S_Scan : MonoBehaviour
{
    public abstract List<(Vector3 pos, Quaternion rot, float weight)> Points();
}