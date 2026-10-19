using UnityEngine;

public class TriggerDoor : MonoBehaviour
{
    public Animator doorAnimator; //Ref door
    void Start()
    {
        doorAnimator = GetComponent<Animator>();
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            doorAnimator.SetTrigger("Open");
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            doorAnimator.SetTrigger("Closed");
        }
    }
}
