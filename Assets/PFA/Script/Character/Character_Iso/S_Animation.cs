using UnityEngine;

public class S_Animation : MonoBehaviour
{
    public static S_Animation instance;

    public Animator anim;
    public string currentAnim = "";

    void Awake()
    {
        instance = this;

        anim = GetComponentInChildren<Animator>();
    } 

    public void ChangeAnim(string animation, float crossfade = 0.2f)
    {
        if (currentAnim != animation)
        {
            currentAnim = animation;
            anim.CrossFade(animation, crossfade);
        }
    }
}
