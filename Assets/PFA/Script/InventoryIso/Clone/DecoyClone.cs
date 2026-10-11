
using System.Collections;
using UnityEngine;

public class DecoyClone : MonoBehaviour
{
    public static DecoyClone ActiveClone { get; private set; }

    public static bool IsActive => ActiveClone != null;

    public static Transform Target =>
        IsActive ? ActiveClone.transform : null;

    private float duration;
    private float endTime;
    private Coroutine lifetimeRoutine;

    public float Duration => duration;

    public float RemainingTime =>
        IsActive && ActiveClone == this
            ? Mathf.Max(0f, endTime - Time.time)
            : 0f;

    public void Activate(float cloneDuration)
    {
        if (ActiveClone != null && ActiveClone != this)
            return;

        ActiveClone = this;

        duration = Mathf.Max(0f, cloneDuration);
        endTime = Time.time + duration;

        Rigidbody rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        if (lifetimeRoutine != null)
            StopCoroutine(lifetimeRoutine);

        lifetimeRoutine = StartCoroutine(LifetimeRoutine());
    }

    private IEnumerator LifetimeRoutine()
    {
        while (RemainingTime > 0f)
            yield return null;

        if (ActiveClone == this)
            ActiveClone = null;

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (ActiveClone == this)
            ActiveClone = null;
    }
}
