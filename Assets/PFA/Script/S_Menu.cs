using UnityEngine;
using UnityEngine.SceneManagement;

public class S_Menu : MonoBehaviour
{
    public void LaunchGame()
    {
        SceneManager.LoadSceneAsync(1);
    }
}
