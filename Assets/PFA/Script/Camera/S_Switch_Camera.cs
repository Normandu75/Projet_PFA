using UnityEngine;

public class S_Switch_Camera : MonoBehaviour
{
    public Camera cam1;
    public Camera cam2;

    public GameObject ui;

    public int camManager;

    public void CamSwitchManager()
    {
        if (camManager == 0)
        {
            Cam1();
            
            camManager = 1;
        }
        else
        {
            Cam2();
            
            camManager = 0;
        }
    }

    void Cam1()
    {
        cam1.gameObject.SetActive(false);
        cam2.gameObject.SetActive(true);
        ui.gameObject.SetActive(true);
    }

    void Cam2()
    {
        cam1.gameObject.SetActive(true);
        cam2.gameObject.SetActive(false);  
        ui.gameObject.SetActive(false);
    }
}
