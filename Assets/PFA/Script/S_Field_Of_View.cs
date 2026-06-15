using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using System;
using UnityEngine.Assertions.Must;
using UnityEngine.Tilemaps;

public class S_Field_Of_View : MonoBehaviour
{
    public float viewRadius;
    [Range(0, 360)]
    public float viewAngle;
    public float circleRadius;

    public S_Field_Of_View_Target fovTarget;

    public LayerMask targetMask;
    public LayerMask obstacleMask;
    public LayerMask objectMask;

    public List<Transform> visibleTargets = new List<Transform>();
    public List<Transform> visibleObjects = new List<Transform>();

    public float meshResolution;
    public int edgeResolveIterations;
    public float edgeThreshold;

    public float maskCutDistance = 0.1f;

    public MeshFilter viewMeshFilter;
    public MeshFilter circleMeshFilter;
    Mesh viewMesh;
    Mesh circleMesh;

    void Start()
    {
        //On set la variable viewMesh pour le FielOfView
        viewMesh = new Mesh();
        viewMesh.name = "viewMesh";
        viewMeshFilter.mesh = viewMesh;

        //On set la variable circleMesh pour le CircleOfView
        circleMesh = new Mesh();
        circleMesh.name = "circleMesh";
        circleMeshFilter.mesh = circleMesh;

        //On lance la coroutine pour détecter les targets
        StartCoroutine(FindTargetsWithDelay(0.2f));
    }

    //La coroutine
    IEnumerator FindTargetsWithDelay(float delay)
    {
        while (true)
        {
            yield return new WaitForSeconds(delay);

            FindVisibleTargets();
            FindVisibleObjects();
        }
    }

    //On lance appelle les fonctions Draw / Circle OfView
        void LateUpdate()
    {
        DrawFieldOfView();
        DrawCircleOfView();
    }

    //Fonction qui permet de trouver ou non s'il y a un / plusieurs ennemi(s) dans notre FOV
    void FindVisibleTargets()
    {
        visibleTargets.Clear();
        Collider[] targetsInViewRadius = Physics.OverlapSphere(transform.position, viewRadius, targetMask); //Raycast qui nous permet de détecter ou non les cibles

        for (int i = 0; i < targetsInViewRadius.Length; i++)
        {
            Transform target = targetsInViewRadius[i].transform; //déterminier la position des cibles

            Vector3 dirToTarget = (target.position - transform.position).normalized; //déterminer la distance jusqu'à la cible

            if (Vector3.Angle(transform.forward, dirToTarget) < viewAngle / 2)
            {
                float disToTarget = Vector3.Distance(transform.position, target.position);

                if (!Physics.Raycast(transform.position, dirToTarget, disToTarget, obstacleMask)) //Permet de verifier s'il (Raycast) ne touhe pas de d'obstacle 
                {
                    visibleTargets.Add(target); //Ajoute la cible dans le tableau
                }
            }
        }
        
        for (int i = 0; i < targetsInViewRadius.Length; i++)
        {
            GameObject targetInRange = targetsInViewRadius[i].gameObject; //Permet de récupérer le MeshRenderer de la cible touchée
            bool isVisible = visibleTargets.Contains(targetsInViewRadius[i].transform); //Permet de vérifier s'il y a bien des ennemis dans le tableau visibleTargets 

            if (targetInRange != isVisible) //S'il n'y a pas d'ennemis dans notre champs de vision
            {
                targetInRange.gameObject.GetComponent<MeshRenderer>().enabled = false; //Désactive les MeshRenderer des targets
            }
            else //S'il y a des ennemis dans notre champs de vision
            {
                targetInRange.gameObject.GetComponent<MeshRenderer>().enabled = true; //Active les MeshRenderer des targets
                targetInRange.gameObject.GetComponent<S_Random_Movement>().isInLight = true; //Permet aux ennemis de se dirigiger vers nous
                targetInRange.gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(Color.blue, Color.red, Mathf.PingPong(Time.time, 1));
            }
        }
    }

    void FindVisibleObjects()
    {
        visibleObjects.Clear();
        Collider[] objectsInViewRadius = Physics.OverlapSphere(transform.position, viewRadius, objectMask); //Raycast qui nous permet de détecter ou non les cibles

        for (int i = 0; i < objectsInViewRadius.Length; i++)
        {
            Transform target = objectsInViewRadius[i].transform; //déterminier la position des cibles

            Vector3 dirToTarget = (target.position - transform.position).normalized; //déterminer la distance jusqu'à la cible

            if (Vector3.Angle(transform.forward, dirToTarget) < viewAngle / 2)
            {
                float disToTarget = Vector3.Distance(transform.position, target.position);

                if (!Physics.Raycast(transform.position, dirToTarget, disToTarget, obstacleMask)) //Permet de verifier s'il (Raycast) ne touhe pas de d'obstacle 
                {
                    visibleObjects.Add(target); //Ajoute la cible dans le tableau
                }
            }
        }
        
        for (int i = 0; i < objectsInViewRadius.Length; i++)
        {
            GameObject objectInRange = objectsInViewRadius[i].gameObject; //Permet de récupérer le MeshRenderer de la cible touchée
            bool isVisible = visibleObjects.Contains(objectsInViewRadius[i].transform); //Permet de vérifier s'il y a bien des ennemis dans le tableau visibleTargets 

            if (objectInRange != isVisible) //S'il n'y a pas d'ennemis dans notre champs de vision
            {
                objectInRange.gameObject.GetComponent<MeshRenderer>().enabled = false; //Désactive les MeshRenderer des targets
            }
            else //S'il y a des ennemis dans notre champs de vision
            {
                objectInRange.gameObject.GetComponent<MeshRenderer>().enabled = true; //Active les MeshRenderer des targets
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

        int vertexCount = viewPoints.Count + 1;
        Vector3[] vertices = new Vector3[vertexCount];
        int[] triangles = new int[(vertexCount - 2) * 3];

        vertices[0] = Vector3.zero;
        
        for (int i = 0; i < vertexCount - 1; i++)
        {
            vertices[i + 1] = transform.InverseTransformPoint(viewPoints[i]) + Vector3.forward * maskCutDistance;

            if (i < vertexCount - 2)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }
        }

        viewMesh.Clear();
        viewMesh.vertices = vertices;
        viewMesh.triangles = triangles;
        viewMesh.RecalculateNormals();
    }

    void DrawCircleOfView()
    {
        int stepCount = Mathf.RoundToInt(360f * meshResolution);
        float stepAngleSize = 360f / stepCount;

        List<Vector3> viewPoints = new List<Vector3>();
        ViewCastInfo oldViewCast = new ViewCastInfo();

        for (int i = 0; i <= stepCount; i++)
        {
            float angle = transform.eulerAngles.y + stepAngleSize * i;

            ViewCastInfo newViewCast = ViewCastCircle(angle);

            if (i > 0)
            {
                bool edgeThresholdExceeded = Mathf.Abs(oldViewCast.distance - newViewCast.distance) > edgeThreshold;

                if (oldViewCast.hit != newViewCast.hit || (oldViewCast.hit && newViewCast.hit && edgeThresholdExceeded))
                {
                    EdgeInfo edge = FindEdge(oldViewCast, newViewCast);

                    if (edge.pointA != Vector3.zero)
                        viewPoints.Add(edge.pointA);

                    if (edge.pointB != Vector3.zero)
                        viewPoints.Add(edge.pointB);
                }
            }

            viewPoints.Add(newViewCast.point);
            oldViewCast = newViewCast;
        }

        int vertexCount = viewPoints.Count + 1;
        Vector3[] vertices = new Vector3[vertexCount];
        int[] triangles = new int[(vertexCount - 2) * 3];

        vertices[0] = Vector3.zero;

        for (int i = 0; i < vertexCount - 1; i++)
        {
            vertices[i + 1] = transform.InverseTransformPoint(viewPoints[i]);

            if (i < vertexCount - 2)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }
        }

        circleMesh.Clear();
        circleMesh.vertices = vertices;
        circleMesh.triangles = triangles;
        circleMesh.RecalculateNormals();
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
        RaycastHit[] hits = Physics.RaycastAll(transform.position, dir, viewRadius, obstacleMask);

        if (hits.Length > 0)
        {
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (RaycastHit hit in hits)
            {
                if (((1 << hit.collider.gameObject.layer) & obstacleMask) != 0)
                {
                    return new ViewCastInfo(true, hit.point, hit.distance, globalAngle);
                }
                /*sif (((1 << hit.collider.gameObject.layer) & targetMask) != 0)
                {
                    return new ViewCastInfo(true, hit.point, hit.distance, globalAngle);
                }*/
            }
        }

        return new ViewCastInfo(false, transform.position + dir * viewRadius, viewRadius, globalAngle);
    }

    ViewCastInfo ViewCastCircle(float globalAngle)
    {
        Vector3 dir = DirFromAngle(globalAngle, true);
        RaycastHit[] hits = Physics.RaycastAll(transform.position, dir, circleRadius, obstacleMask | targetMask);

        if (hits.Length > 0)
        {
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (RaycastHit hit in hits)
            {
                if (((1 << hit.collider.gameObject.layer) & obstacleMask) != 0)
                {
                    return new ViewCastInfo(true, hit.point, hit.distance, globalAngle);
                }
                if (((1 << hit.collider.gameObject.layer) & targetMask) != 0)
                {
                    return new ViewCastInfo(true, hit.point, hit.distance, globalAngle);
                }
            }
        }

        return new ViewCastInfo(false, transform.position + dir * circleRadius, circleRadius, globalAngle);
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
}
