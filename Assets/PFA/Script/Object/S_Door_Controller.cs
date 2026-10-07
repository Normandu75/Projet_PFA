using UnityEngine;
using System.Collections.Generic;

public class S_Door_Controller : MonoBehaviour
{
    [Header("Door Settings")]
    public List<GameObject>buttonDoors= new List<GameObject>();
    public List<GameObject> doors = new List<GameObject>();
    public List<GameObject> lights = new List<GameObject>();

    public static S_Door_Controller instance;

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

    public void OpenDoor(int index)
    {
        if (index < 0 || index >= doors.Count)
        {
            Debug.LogWarning("Aucune porte ne correspond à l'index " + index);
            return;
        }

        GameObject door = doors[index];
        
        if (door != null)
        {
            doors[index] = null;
            Destroy(door);
        }
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
