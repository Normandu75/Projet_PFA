using System.Collections;
using System.Linq;
using UnityEngine;




[RequireComponent(typeof(S_Procedural_Robot_Movement)), DefaultExecutionOrder(2)]
public class S_Spider_Bot : MonoBehaviour
{
    [SerializeField] Transform[] legs, orbits, targets, targetsG1;
    float[] orbitsLegsDists, orbitsOriginsDists;
    
    [SerializeField] float stepTime = 0.25f;
    [SerializeField, Range(0, 1)] float stepDelayRand = 0f;
    [SerializeField] float stepHeight = 0.6f;
    [SerializeField, Min(0.01f)] float movementSpeedReference = 3.5f;
    [SerializeField] AnimationCurve stepHeightCurve;
    [SerializeField, Range(0, 1)] float desyncG2 = 1;

    [SerializeField] UpdateOrbitsMethode updateOrbitsMethode = UpdateOrbitsMethode.LegDistance;
    enum UpdateOrbitsMethode { LegDistance, Score }

    [SerializeField, Range(0, 360)] float arcAngle = 270;
    [SerializeField] float arcRadius = 0.3f;
    [SerializeField, Min(1)] int arcResolution = 4;
    [SerializeField] LayerMask arcLayer;

    // [SerializeField] AudioClip[] stepStartSounds;
    // [SerializeField] AudioClip[] stepMoveSounds;
    // [SerializeField] AudioClip[] stepEndSounds;
    // [SerializeField, Range(0, 1)] float stepSoundSpeedProgress = 0.3f;
    // [SerializeField, Range(0, 1)] float stepSoundVolume = 1;
    // [SerializeField] float stepSoundPitchMin = 0.9f, stepSoundPitchMax = 1.1f;

    [SerializeField] bool drawGizmos = true;

    S_Procedural_Robot_Movement player3D;
    Vector3 MovementVelocity => Vector3.ProjectOnPlane(player3D.rb.linearVelocity, transform.up);
    float MovementSpeedProgress => Mathf.Clamp01(MovementVelocity.magnitude / movementSpeedReference);


    public Transform[] Targets { get => targets; }
    public Transform[] TargetsG1 { get => targetsG1; }
    public Transform[] Orbits { get => orbits; }


    void OnDrawGizmosSelected()
    {
        if (!drawGizmos)
            return;

        if (legs == null || targets == null || orbits == null ||
            legs.Length != targets.Length || legs.Length != orbits.Length)
            return;

        if (!Application.isPlaying)
            Cache();

        UpdateOrbits(true);
        GizmoDrawEndStep();
    }



    void Awake()
    {
        Cache();
        if (!HasCompleteLegSetup())
            Debug.LogWarning("S_Spider_Bot nécessite autant d'éléments dans legs, orbits et targets pour animer les pas.", this);
    }

    void Cache()
    {
        player3D = GetComponent<S_Procedural_Robot_Movement>();
        CacheOrbitsDists();
    }

    void CacheOrbitsDists()
    {
        if (!HasCompleteLegSetup())
        {
            orbitsLegsDists = new float[0];
            orbitsOriginsDists = new float[0];
            return;
        }

        orbitsLegsDists    = new float[orbits.Length];
        orbitsOriginsDists = new float[orbits.Length];

        for (int i = 0; i < orbits.Length; i++)
        {
            orbitsLegsDists[i]    = (orbits[i].position - legs[i]         .position).magnitude;
            orbitsOriginsDists[i] = (orbits[i].position - orbits[i].parent.position).magnitude;
        }
    }

    private void OnEnable()
    {
        if (HasCompleteLegSetup())
            StartCoroutine(StepLoop());
    }

    void OnDisable()
    {
        StopAllCoroutines();
    }

    void Update()
    {
        UpdateOrbits();
    }

    bool HasCompleteLegSetup()
    {
        return legs != null
            && orbits != null
            && targets != null
            && legs.Length > 0
            && legs.Length == orbits.Length
            && legs.Length == targets.Length;
    }

    public void UpdateOrbits(bool gizmo = false)
    {
        if (!HasCompleteLegSetup())
            return;

        if (updateOrbitsMethode == UpdateOrbitsMethode.LegDistance)
            for (int i = 0; i < orbits.Length; i++)
                UpdateOrbitLegDistance(orbits[i], orbits[i].parent, legs[i], orbitsLegsDists[i], orbitsOriginsDists[i], gizmo);

        else if (updateOrbitsMethode == UpdateOrbitsMethode.Score)
            for (int i = 0; i < orbits.Length; i++)
                UpdateOrbitScore(orbits[i], orbits[i].parent, legs[i], orbitsLegsDists[i], orbitsOriginsDists[i], gizmo);
    }



    void UpdateOrbitScore(Transform orbit, Transform orbitOrigin, Transform leg, float dstLeg, float dstOrigin, bool gizmo = false)
    {
        Vector3 pos = orbitOrigin.position;
        Quaternion rot = orbitOrigin.rotation;

        Quaternion dRot = Quaternion.Inverse(transform.rotation) * rot;

        if (!gizmo)
        {
            orbit.position = pos;
            orbit.rotation = rot * Quaternion.Inverse(dRot);
        }

        float score = Mathf.Pow((pos - leg.position).magnitude - dstLeg, 2) + Mathf.Pow((pos - orbitOrigin.position).magnitude - dstOrigin, 2);
        float scoreCrt = score;
        float scoreMax = (Mathf.Pow(dstLeg, 2) + Mathf.Pow(dstOrigin, 2)) * 1f;

        if (gizmo)
            Gizmos.color = new Color(1, 0.5f, 0, 0.3f);

        for (int i = 0; i < 50 && scoreCrt < scoreMax; i++)
        {
            if (S_Physics_Extension.ArcCast(
                pos, rot, arcAngle, arcRadius, Mathf.Max(1, arcResolution), arcLayer,
                out RaycastHit hit, ignoredRoot: transform))
            {
                if (gizmo)
                    Gizmos.DrawSphere(hit.point, 0.05f);

                pos = hit.point;
                rot.MatchUp(hit.normal);
                scoreCrt = Mathf.Pow((pos - leg.position).magnitude - dstLeg, 2) + Mathf.Pow((pos - orbitOrigin.position).magnitude - dstOrigin, 2);

                if (scoreCrt < score)
                {
                    if (!gizmo)
                    {
                        orbit.position = pos;
                        orbit.rotation = rot * Quaternion.Inverse(dRot);
                    }
                    score = scoreCrt;
                }
            }
            else return;
        }
    }

    void UpdateOrbitLegDistance(Transform orbit, Transform orbitOrigin, Transform leg, float distLeg, float distOrigin, bool gizmo = false)
    {
        Vector3    pos = orbitOrigin.position;
        Quaternion rot = orbitOrigin.rotation;

        Quaternion dRot = Quaternion.Inverse(transform.rotation) * rot;

        float dist = (pos - leg.position).magnitude;
        bool checkSup = dist < distLeg;

        for (int i = 0; i < 50 && dist < distLeg * 1.5f; i++)
        {
            if (S_Physics_Extension.ArcCast(
                pos, rot, arcAngle, arcRadius, Mathf.Max(1, arcResolution), arcLayer,
                out RaycastHit hit, ignoredRoot: transform))
            {
                if (gizmo)
                {
                    Gizmos.color = new Color(1, 0.5f, 0, 0.3f);
                    Gizmos.DrawSphere(hit.point, 0.05f);
                }

                Vector3 nextPos = hit.point;
                Quaternion nextRot = S_Math_Extension.RotationMatchUp(rot, hit.normal);
                float nextDist = (nextPos - leg.position).magnitude;

                if (checkSup == nextDist > distLeg)
                {
                    float progress = Mathf.InverseLerp(dist, nextDist, distLeg);

                    if (gizmo)
                    {
                        Gizmos.color = new Color(1, 0.5f, 0, 1);
                        Gizmos.DrawSphere(Vector3.Lerp(pos, nextPos, progress), 0.1f);
                    }

                    else {
                        orbit.position = Vector3.Lerp(pos, nextPos, progress);
                        orbit.rotation = Quaternion.Lerp(rot * Quaternion.Inverse(dRot), nextRot * Quaternion.Inverse(dRot), progress);
                    }

                    checkSup = !checkSup;
                    //return;
                }

                pos = nextPos;
                rot = nextRot;
                dist = nextDist;
            }
            else return;
        }
    }

    public (Vector3, Quaternion) EndStep(Transform orbit, bool gizmo = false)
    {
        Vector3 velocityForward = MovementVelocity;
        if (velocityForward.sqrMagnitude < 0.0001f)
            return (orbit.position, orbit.rotation);

        Vector3 pos = orbit.position;
        Quaternion rot = Quaternion.LookRotation(velocityForward.normalized, orbit.up);

        float dist = velocityForward.magnitude * stepTime / 2;

        if (gizmo)
            Gizmos.color = new Color(1, 0, 0, 0.3f);

        for (int stepCount = 0; stepCount < 100; stepCount++)
        {
            if (S_Physics_Extension.ArcCast(
                pos, rot, arcAngle, arcRadius, Mathf.Max(1, arcResolution), arcLayer,
                out RaycastHit hit, ignoredRoot: transform))
            {
                if (gizmo)
                    Gizmos.DrawSphere(hit.point, 0.05f);

                float distCrt = (hit.point - pos).magnitude;

                if (distCrt >= dist)
                {
                    Vector3 nextPos = hit.point;
                    Quaternion nextRot = S_Math_Extension.RotationMatchUp(rot, hit.normal);

                    float progress = Mathf.InverseLerp(0, distCrt, dist);

                    pos = Vector3   .Lerp(pos, nextPos, progress);
                    rot = Quaternion.Lerp(rot, nextRot, progress);

                    if (gizmo)
                    {
                        Gizmos.color = new Color(1, 0, 0, 1);
                        Gizmos.DrawSphere(pos, 0.1f);
                    }

                    break;
                }

                dist -= distCrt;

                pos = hit.point;
                rot.MatchUp(hit.normal);
            }
            else break;
        }

        return (pos, rot);
    }

    void GizmoDrawEndStep()
    {
        for (int i = 0; i < orbits.Length; i++)
            EndStep(orbits[i], true);
    }



    IEnumerator StepLoop()
    {
        bool G1 = true;

        while (true)
        {
            for (int i = 0; i < targets.Length; i++)
                if (targetsG1.Contains(targets[i]) == G1)
                    StartCoroutine(DelayStep(i, stepTime * stepDelayRand * Random.value));

            yield return new WaitForSeconds(stepTime * (G1 ? desyncG2 : 2 - desyncG2));

            G1 = !G1;
        }
    }

    IEnumerator DelayStep(int idx, float delay)
    {
        if (delay > 0)
            yield return new WaitForSeconds(delay);

        StartCoroutine(Step(idx));
    }

    IEnumerator Step(int idx)
    {
        float t = 0, progress;

        Transform target = targets[idx];
        Transform orbit = orbits[idx];
        Transform leg = legs[idx];

        Vector3    targetStartPosProj = leg.InverseTransformPoint(target.position);
        Quaternion targetStartRotProj = Quaternion.Inverse(leg.rotation) * target.rotation;

        // PlayStepSound(target.position, stepStartSounds);
        // PlayStepSound(target.position, stepMoveSounds);

        while (t < stepTime)
        {
            t += Time.deltaTime;
            progress = Mathf.Clamp01(t / stepTime);

            Vector3    startPos, endPos;
            Quaternion startRot, endRot;
            startPos = leg.TransformPoint(targetStartPosProj);
            startRot = leg.rotation * targetStartRotProj;
            (endPos, endRot) = EndStep(orbit);

            target.position = Vector3.Lerp(startPos, endPos, progress);
            target.position += stepHeight * MovementSpeedProgress * stepHeightCurve.Evaluate(progress) * leg.up;

            target.rotation = Quaternion.Lerp(startRot, endRot, progress);

            if (t < stepTime)
                yield return new WaitForEndOfFrame();
        }

        // PlayStepSound(target.position, stepEndSounds);
    }

    // void PlayStepSound(Vector3 pos, AudioClip[] sounds)
    // {
    //     if (sounds.Length == 0 || player3D.SpeedProgress <= stepSoundSpeedProgress)
    //         return;

    //     AudioClip clip = sounds[Random.Range(0, sounds.Length)];
    //     if (clip == null)
    //         return;

    //     float volume = Mathf.InverseLerp(stepSoundSpeedProgress, 1, player3D.SpeedProgress) * stepSoundVolume;
    //     float pitch = Random.Range(stepSoundPitchMin, stepSoundPitchMax);

    //     if (sounds == stepMoveSounds)
    //         pitch *= clip.length / stepTime;

    //     GameObject audioObject = new GameObject("Spider Step Audio");
    //     audioObject.transform.position = pos;
    //     AudioSource audioSource = audioObject.AddComponent<AudioSource>();
    //     audioSource.clip = clip;
    //     audioSource.volume = volume;
    //     audioSource.pitch = pitch;
    //     audioSource.spatialBlend = 1f;
    //     audioSource.Play();

    //     float playbackSpeed = Mathf.Max(Mathf.Abs(pitch), 0.01f);
    //     Destroy(audioObject, clip.length / playbackSpeed);
    // }

}