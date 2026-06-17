using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class S_Menu : MonoBehaviour
{
    public Animator anim;

    public void LaunchGame()
    {
        StartCoroutine(WaitAnim());
    }

    IEnumerator WaitAnim()
    {
        anim.SetTrigger("Play");
        
        yield return new WaitForSeconds(4f);

        SceneManager.LoadSceneAsync(1);
    }
}
