using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class S_Field_Of_View : MonoBehaviour
{
    public float viewRadius;
    [Range(0, 360)]
    public float viewAngle;
    public Transform viewDirection;

    public Vector3 ViewDirection => viewDirection != null ? viewDirection.forward : transform.forward;

    public LayerMask targetMask;
    public LayerMask obstacleMask;
    public LayerMask objectMask;

    public List<Transform> visibleTargets = new List<Transform>();
    public List<Transform> visibleObjects = new List<Transform>();

    readonly HashSet<MeshRenderer> hiddenTargets = new HashSet<MeshRenderer>();
    readonly HashSet<MeshRenderer> hiddenObjects = new HashSet<MeshRenderer>();

    void Start()
    {
        StartCoroutine(FindTargetsWithDelay(0.2f));
    }

    void OnDisable()
    {
        RestoreRenderers(hiddenTargets);
        RestoreRenderers(hiddenObjects);
    }

    IEnumerator FindTargetsWithDelay(float delay)
    {
        while (true)
        {
            yield return new WaitForSeconds(delay);
            FindVisibleTargets();
            FindVisibleObjects();
        }
    }

    void FindVisibleTargets()
    {
        UpdateVisibility(targetMask, visibleTargets, hiddenTargets);
    }

    void FindVisibleObjects()
    {
        UpdateVisibility(objectMask, visibleObjects, hiddenObjects);
    }

    void UpdateVisibility(LayerMask elementMask, List<Transform> visibleElements, HashSet<MeshRenderer> previouslyHidden)
    {
        visibleElements.Clear();
        HashSet<MeshRenderer> hiddenThisScan = new HashSet<MeshRenderer>();
        Collider[] elementsInRange = Physics.OverlapSphere(transform.position, viewRadius, elementMask);

        foreach (Collider element in elementsInRange)
        {
            Transform elementTransform = element.transform;
            bool isVisible = IsVisible(elementTransform);

            if (isVisible && !visibleElements.Contains(elementTransform))
                visibleElements.Add(elementTransform);

            MeshRenderer elementRenderer = element.GetComponent<MeshRenderer>();
            if (elementRenderer == null)
                continue;

            elementRenderer.enabled = isVisible;
            if (!isVisible)
                hiddenThisScan.Add(elementRenderer);
        }

        foreach (MeshRenderer previouslyHiddenRenderer in previouslyHidden)
        {
            if (previouslyHiddenRenderer != null && !hiddenThisScan.Contains(previouslyHiddenRenderer))
                previouslyHiddenRenderer.enabled = true;
        }

        previouslyHidden.Clear();
        previouslyHidden.UnionWith(hiddenThisScan);
    }

    bool IsVisible(Transform element)
    {
        Vector3 direction = element.position - transform.position;

        return Vector3.Angle(ViewDirection, direction) < viewAngle / 2
            && !Physics.Raycast(transform.position, direction.normalized, direction.magnitude, obstacleMask);
    }

    static void RestoreRenderers(HashSet<MeshRenderer> renderers)
    {
        foreach (MeshRenderer renderer in renderers)
        {
            if (renderer != null)
                renderer.enabled = true;
        }

        renderers.Clear();
    }
}
