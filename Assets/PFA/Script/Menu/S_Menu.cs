using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class S_Menu : MonoBehaviour
{
    public Animator anim;
    public GameObject cryoPod;
    public GameObject cryoPodText;

    public void LaunchGame()
    {
        StartCoroutine(WaitAnim());
    }

    public void ExitGame()
    {
        Application.Quit();
    }

    IEnumerator WaitAnim()
    {
        
        cryoPod.gameObject.SetActive(true);

        cryoPodText.gameObject.SetActive(false);

        anim.SetTrigger("Play");

        yield return new WaitForSeconds(6f);

        SceneManager.LoadSceneAsync(1);
    }
}
