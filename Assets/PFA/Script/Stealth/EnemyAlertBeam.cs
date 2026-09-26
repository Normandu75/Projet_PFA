using System;
using UnityEngine;

/// <summary>
/// A poser sur un prefab (ex: une sphère/particule avec trail renderer, couleur rouge).
/// Se déplace en ligne droite vers la cible à une vitesse modifiable, puis déclenche
/// le callback d'alerte et se détruit.
/// </summary>
public class EnemyAlertBeam : MonoBehaviour
{
    private Transform _target;
    private Vector3 _fallbackTargetPosition;
    private float _speed;
    private Action _onArrival;
    private bool _launched;

    /// <param name="target">Transform de l'ennemi à alerter (peut être null si détruit en route).</param>
    /// <param name="speed">Vitesse de déplacement du rayon, unités/seconde.</param>
    /// <param name="onArrival">Callback déclenché à l'arrivée (ex: EnemyAI.OnAlerted).</param>
    public void Launch(Transform target, float speed, Action onArrival)
    {
        _target = target;
        _fallbackTargetPosition = target != null ? target.position : transform.position;
        _speed = Mathf.Max(0.01f, speed);
        _onArrival = onArrival;
        _launched = true;
    }

    private void Update()
    {
        if (!_launched) return;

        Vector3 targetPos = _target != null ? _target.position : _fallbackTargetPosition;
        Vector3 toTarget = targetPos - transform.position;
        float step = _speed * Time.deltaTime;

        if (toTarget.magnitude <= step)
        {
            transform.position = targetPos;
            Arrive();
            return;
        }

        transform.position += toTarget.normalized * step;
        if (toTarget.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(toTarget);
    }

    private void Arrive()
    {
        _launched = false;
        _onArrival?.Invoke();
        Destroy(gameObject);
    }
}
