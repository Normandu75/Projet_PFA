using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class S_Door_Controller : MonoBehaviour
{
    [Header("Door Settings")]
    public List<GameObject>buttonDoors= new List<GameObject>();
    public List<GameObject> doors = new List<GameObject>();
    public List<GameObject> lights = new List<GameObject>();

    [Header("Door Animation")]
    public float doorLiftHeight = 3f;
    public float doorMoveDuration = 1.5f;
    public float doorOpenDuration = 10f;

    public static S_Door_Controller instance;

    private readonly Dictionary<GameObject, Vector3> closedDoorPositions = new Dictionary<GameObject, Vector3>();
    private readonly Dictionary<GameObject, Coroutine> doorAnimations = new Dictionary<GameObject, Coroutine>();
    private readonly HashSet<GameObject> unlockedDoors = new HashSet<GameObject>();

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            DontDestroyOnLoad(gameObject);
        }

        AddButtonDoors(GameObject.FindGameObjectsWithTag("ButtonDoor"));
        AddDoors(GameObject.FindGameObjectsWithTag("Door"));
        AddLights(GameObject.FindGameObjectsWithTag("Light"));

        foreach (GameObject door in doors)
        {
            if (door == null)
            {
                continue;
            }

            Collider doorCollider = door.GetComponent<Collider>();
            if (doorCollider != null)
            {
                doorCollider.isTrigger = false;
            }
        }
    }

    private void AddButtonDoors(GameObject[] buttonDoorsFound)
    {
        foreach (GameObject buttonDoor in buttonDoorsFound)
        {
            AddButtonDoor(buttonDoor);
        }
    }

    private void AddButtonDoor(GameObject buttonDoor)
    {
        if (buttonDoor != null && !buttonDoors.Contains(buttonDoor))
        {
            buttonDoors.Add(buttonDoor);
        }
    }

    private void AddDoors(GameObject[] doorsFound)
    {
        foreach (GameObject door in doorsFound)
        {
            AddDoor(door);
        }
    }

    private void AddDoor(GameObject door)
    {
        if (door != null && !doors.Contains(door))
        {
            doors.Add(door);
        }
    }

    private void AddLights(GameObject[] lightsFound)
    {
        foreach (GameObject light in lightsFound)
        {
            AddLight(light);
        }
    }

    private void AddLight(GameObject light)
    {
        if (light != null && !lights.Contains(light))
        {
            lights.Add(light);
        }
    }

    public void UnlockDoor(int index)
    {
        if (index < 0 || index >= doors.Count)
        {
            Debug.LogWarning("Aucune porte ne correspond au bouton d'index " + index);
            return;
        }

        GameObject door = doors[index];
        if (door == null)
        {
            return;
        }

        Collider doorCollider = door.GetComponent<Collider>();
        if (doorCollider == null)
        {
            Debug.LogWarning("La porte " + door.name + " n'a pas de Collider.");
            return;
        }

        Debug.Log("Trigger = true");

        doorCollider.isTrigger = true;
        unlockedDoors.Add(door);
    }

    public void OpenDoor(int index)
    {
        if (index < 0 || index >= doors.Count)
        {
            Debug.LogWarning("Aucune porte ne correspond à l'index " + index);
            return;
        }

        GameObject door = doors[index];
        if (door == null)
        {
            return;
        }

        if (!unlockedDoors.Remove(door))
        {
            return;
        }

        if (!closedDoorPositions.ContainsKey(door))
        {
            closedDoorPositions.Add(door, door.transform.position);
        }

        if (doorAnimations.TryGetValue(door, out Coroutine animation))
        {
            StopCoroutine(animation);
        }

        doorAnimations[door] = StartCoroutine(OpenAndCloseDoor(door));
    }

    private IEnumerator OpenAndCloseDoor(GameObject door)
    {
        Vector3 closedPosition = closedDoorPositions[door];
        Vector3 openPosition = closedPosition + Vector3.up * doorLiftHeight;

        yield return MoveDoor(door.transform, door.transform.position, openPosition);
        yield return new WaitForSeconds(doorOpenDuration);
        yield return MoveDoor(door.transform, door.transform.position, closedPosition);

        Collider doorCollider = door.GetComponent<Collider>();

        if (doorCollider != null)
        {
            // doorCollider.isTrigger = false;
        }

        doorAnimations.Remove(door);
    }

    private IEnumerator MoveDoor(Transform door, Vector3 startPosition, Vector3 targetPosition)
    {
        if (doorMoveDuration <= 0f)
        {
            door.position = targetPosition;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < doorMoveDuration)
        {
            elapsed += Time.deltaTime;
            door.position = Vector3.Lerp(startPosition, targetPosition, elapsed / doorMoveDuration);
            yield return null;
        }

        door.position = targetPosition;
    }

    public void ChangeLightColor(int index, Color newColor)
    {
        if (index < 0 || index >= lights.Count)
        {
            Debug.LogWarning("Aucune lumière ne correspond à l'index " + index);
            return;
        }

        GameObject light = lights[index];

        if (light != null)
        {
            Light lightComponent = light.GetComponent<Light>();
            
            if (lightComponent != null)
            {
                lightComponent.color = newColor;
            }
        }
    }
}
