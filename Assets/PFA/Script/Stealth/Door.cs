using System.Collections;
using UnityEngine;

public class Door : MonoBehaviour
{
    [SerializeField] private Transform doorPivot;
    [SerializeField] private float openAngle = 100f;
    [SerializeField] private float openSpeed = 120f; // degrés/seconde

    private Quaternion _closedRotation;
    private Quaternion _openRotation;

    public bool IsOpen { get; private set; }

    private void Awake()
    {
        if (doorPivot == null) doorPivot = transform;
        _closedRotation = doorPivot.localRotation;
        _openRotation = Quaternion.Euler(0f, openAngle, 0f) * _closedRotation;
    }

    public IEnumerator Open()
    {
        if (IsOpen) yield break;
        IsOpen = true;
        while (Quaternion.Angle(doorPivot.localRotation, _openRotation) > 0.5f)
        {
            doorPivot.localRotation = Quaternion.RotateTowards(
                doorPivot.localRotation, _openRotation, openSpeed * Time.deltaTime);
            yield return null;
        }
        doorPivot.localRotation = _openRotation;
    }

    public IEnumerator Close()
    {
        if (!IsOpen) yield break;
        IsOpen = false;
        while (Quaternion.Angle(doorPivot.localRotation, _closedRotation) > 0.5f)
        {
            doorPivot.localRotation = Quaternion.RotateTowards(
                doorPivot.localRotation, _closedRotation, openSpeed * Time.deltaTime);
            yield return null;
        }
        doorPivot.localRotation = _closedRotation;
    }
}
