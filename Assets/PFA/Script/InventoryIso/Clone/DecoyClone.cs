
using System.Collections;
using UnityEngine;

public class DecoyClone : MonoBehaviour
{
    public static DecoyClone ActiveClone { get; private set; }

    public static bool IsActive =>
        ActiveClone != null;

    public static Transform Target =>
        IsActive ? ActiveClone.transform : null;

    private Coroutine lifetimeRoutine;

    public void Activate(float duration)
    {
        if (ActiveClone != null && ActiveClone != this)
            return;

        ActiveClone = this;

        Rigidbody rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
        lifetimeRoutine = StartCoroutine(
            LifetimeRoutine(Mathf.Max(0f, duration))
        );
    }

    private IEnumerator LifetimeRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);

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
