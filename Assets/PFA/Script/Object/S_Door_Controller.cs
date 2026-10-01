using UnityEngine;
using System.Collections.Generic;

public class S_Door_Controller : MonoBehaviour
{
    [Header("Door Settings")]
    public List<GameObject>buttonDoors= new List<GameObject>();
    public List<GameObject> doors = new List<GameObject>();

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
}
