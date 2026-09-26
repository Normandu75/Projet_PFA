using System;
using System.Collections;
using UnityEngine;

public class DetectionUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform icon;
    [SerializeField] private float popInDuration = 0.25f;
    [SerializeField] private float holdDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.25f;

    private Camera _mainCamera;
    private Coroutine _routine;

    private void Awake()
    {
        _mainCamera = Camera.main;
        if (canvasGroup != null) canvasGroup.alpha = 0f;
    }

    private void LateUpdate()
    {
        // Billboard vers la caméra.
        if (_mainCamera != null)
            transform.forward = _mainCamera.transform.forward;
    }

    public void Show(Action onComplete)
    {
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(PlayRoutine(onComplete));
    }

    private IEnumerator PlayRoutine(Action onComplete)
    {
        // Pop-in
        float t = 0f;
        Vector3 startScale = Vector3.one * 0.3f;
        Vector3 endScale = Vector3.one;
        while (t < popInDuration)
        {
            t += Time.deltaTime;
            float p = t / popInDuration;
            canvasGroup.alpha = p;
            if (icon != null) icon.localScale = Vector3.Lerp(startScale, endScale, p);
            yield return null;
        }
        canvasGroup.alpha = 1f;
        if (icon != null) icon.localScale = endScale;

        yield return new WaitForSeconds(holdDuration);

        // Fade out
        t = 0f;
        while (t < fadeOutDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = 1f - (t / fadeOutDuration);
            yield return null;
        }
        canvasGroup.alpha = 0f;

        onComplete?.Invoke();
    }
}
