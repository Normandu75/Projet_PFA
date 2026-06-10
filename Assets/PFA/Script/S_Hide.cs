using UnityEngine;

public class S_Hide : MonoBehaviour
{
    private S_Random_Movement enemy;

    void Start()
    {
        enemy = GameObject.Find("Target").GetComponent<S_Random_Movement>();
    }

    public void OnTriggerEnter(Collider collision)
    {
        if(collision.gameObject.tag == "Player")
        {
            enemy.isInLight = false;
            Debug.Log(enemy.isInLight);
        }
    }
    public void OnTriggerExit(Collider other)
    {
        if(other.gameObject.tag == "Player")
        {
            Debug.Log("NotHide");
            Debug.Log(enemy.isInLight);
        }
    }
}
