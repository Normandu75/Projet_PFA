using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using System;
using UnityEngine.Assertions.Must;

public class S_Field_Of_View_Target : MonoBehaviour
{
    public float viewRadius;
    [Range(0, 360)]
    public float viewAngle;

    public LayerMask characterMask;
    public LayerMask obstacleMask;

    public List<Transform> visibleCharacter = new List<Transform>();

    public float meshResolution;
    public int edgeResolveIterations;
    public float edgeThreshold;
    public bool isInSight;
    public bool checkHide;
    public bool isDetecting;

    public S_Random_Movement movement;


    void Start()
    {
        movement = GetComponent<S_Random_Movement>();

        //On lance la coroutine pour détecter les targets
        StartCoroutine(FindTargetsWithDelay(0.2f));
    }

    //La coroutine
    IEnumerator FindTargetsWithDelay(float delay)
    {
        while (true)
        {
            yield return new WaitForSeconds(delay);

            FindVisibleCharacter();
        }
    }

    //On lance appelle les fonctions Draw / Circle OfView
        void LateUpdate()
    {
        DrawFieldOfView();
    }

    //Fonction qui permet de trouver ou non s'il y a un / plusieurs ennemi(s) dans notre FOV
    void FindVisibleCharacter()
    {
        visibleCharacter.Clear();
        Collider[] targetsInViewRadius = Physics.OverlapSphere(transform.position, viewRadius, characterMask); //Raycast qui nous permet de détecter ou non les cibles

        for (int i = 0; i < targetsInViewRadius.Length; i++)
        {
            Transform target = targetsInViewRadius[i].transform; //déterminier la position des cibles

            Vector3 dirToTarget = (target.position - transform.position).normalized; //déterminer la distance jusqu'à la cible

            if (Vector3.Angle(transform.forward, dirToTarget) < viewAngle / 2)
            {
                float disToTarget = Vector3.Distance(transform.position, target.position);

                if (!Physics.Raycast(transform.position, dirToTarget, disToTarget, obstacleMask)) //Permet de verifier s'il (Raycast) ne touhe pas de d'obstacle 
                {
                    visibleCharacter.Add(target); //Ajoute la cible dans le tableau
                }

                if (visibleCharacter.Count > 0)
                {
                    isInSight = true;
                    checkHide = false;
                    isDetecting = false; // on annule la détection
                }
                else
                {
                    if (!isDetecting)
                    StartCoroutine(DetectionTime());
                }
            }
        }
    }

    void DrawFieldOfView()
    {
        int stepCount = Mathf.RoundToInt(viewAngle * meshResolution); //Crée 
        float stepAngleSize = viewAngle / stepCount; //Crée l'angle de notre FOV

        List<Vector3> viewPoints = new List<Vector3>(); //Crée une liste de Vector3

        ViewCastInfo oldViewCast = new ViewCastInfo();

        for (int i = 0; i <= stepCount; i++)
        {
            float angle = transform.eulerAngles.y - viewAngle / 2 + stepAngleSize * i; //Détermine l'angle uniquement sur l'axe y

            ViewCastInfo newViewCast = ViewCast(angle);

            if (i > 0)
            {   
                bool edgeThresholdExceeded = Mathf.Abs(oldViewCast.distance - newViewCast.distance) > edgeThreshold; //Récupere la valeur absolue de la distance entre l'ancien et le nouveau ViewCast lorsqu'elle supérieure à edgeThreshold

                if (oldViewCast.hit != newViewCast.hit || (oldViewCast.hit && newViewCast.hit && edgeThresholdExceeded)) //Vérifie si le hit de l'ancien ViewCast et différent du nouveau
                {
                    EdgeInfo edge = FindEdge(oldViewCast, newViewCast); //Permet de set une variable pour les bords détectés par l'ancien et nouveau ViewCast

                    if (edge.pointA != Vector3.zero) //Verifie s'il détecte bien un bord pour le pointA
                    {
                        viewPoints.Add(edge.pointA);
                    }
                    if (edge.pointB != Vector3.zero) //Verifie s'il détecte bien un bord pour le pointB
                    {
                        viewPoints.Add(edge.pointB);
                    }
                }
            }

            viewPoints.Add(newViewCast.point);

            oldViewCast = newViewCast;
        }
    }

    EdgeInfo FindEdge(ViewCastInfo minViewCast, ViewCastInfo maxViewCast)
    {
        float minAngle = minViewCast.angle;
        float maxAngle = maxViewCast.angle;
        Vector3 minPoint = Vector3.zero;
        Vector3 maxPoint = Vector3.zero;

        for (int i = 0; i < edgeResolveIterations; i++)
        {
            float angle = (minAngle + maxAngle) / 2;

            ViewCastInfo newViewCast = ViewCast(angle);

            bool edgeThresholdExceeded = Mathf.Abs(minViewCast.distance - newViewCast.distance) > edgeThreshold;

            if (newViewCast.hit == minViewCast.hit && !edgeThresholdExceeded)
            {
                minAngle = angle;
                minPoint = newViewCast.point;
            }
            else
            {
                maxAngle = angle;
                maxPoint = newViewCast.point;
            }
        }

        return new EdgeInfo(minPoint, maxPoint);
    }

    ViewCastInfo ViewCast(float globalAngle)
    {
        Vector3 dir = DirFromAngle(globalAngle, true);
        RaycastHit[] hits = Physics.RaycastAll(transform.position, dir, viewRadius, obstacleMask | characterMask);

        if (hits.Length > 0)
        {
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (RaycastHit hit in hits)
            {
                if (((1 << hit.collider.gameObject.layer) & obstacleMask) != 0)
                {
                    return new ViewCastInfo(true, hit.point, hit.distance, globalAngle);
                }
                if (((1 << hit.collider.gameObject.layer) & characterMask) != 0)
                {
                    return new ViewCastInfo(true, hit.point, hit.distance, globalAngle);
                }
            }
        }

        return new ViewCastInfo(false, transform.position + dir * viewRadius, viewRadius, globalAngle);
    }

    public Vector3 DirFromAngle(float angleInDegrees, bool angleIsGlobal)
    {
        if (!angleIsGlobal)
        {
            angleInDegrees += transform.eulerAngles.y;
        }

        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }

    public struct ViewCastInfo
    {
        public bool hit;
        public Vector3 point;
        public float distance;
        public float angle;

        public ViewCastInfo(bool _hit, Vector3 _point, float _distance, float _angle)
        {
            this.hit = _hit;
            this.point = _point;
            this.distance = _distance;
            this.angle = _angle;
        }
    }

    public struct EdgeInfo
    {
        public Vector3 pointA;
        public Vector3 pointB;

        public EdgeInfo(Vector3 _pointA, Vector3 _pointB)
        {
            pointA = _pointA;
            pointB = _pointB;
        }
    }

    IEnumerator DetectionTime()
    {
        isDetecting = true;
        isInSight = false;

        yield return new WaitForSeconds(5f);

        if (!isInSight)
        {
            movement.isInLight = false;

            if (!checkHide)
            {
                movement.NearestHide();
                checkHide = true;
            }
        }
        else
        {
            movement.isInLight = true;
        }

        isDetecting = false;
    }
}
