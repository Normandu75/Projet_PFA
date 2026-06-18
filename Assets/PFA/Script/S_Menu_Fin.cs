using UnityEngine;
using UnityEngine.SceneManagement;

public class S_Menu_Fin : MonoBehaviour
{
    public void Exit_Level()
    {
        SceneManager.LoadSceneAsync(0);
    }
}
