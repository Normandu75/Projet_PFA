using UnityEngine;

public class S_Energy_Ally : MonoBehaviour
{

    [Header("Energy Ally")]
    private float energyAlly;
    private S_Procedural_Robot_Movement robotMovement;
    public float energyAllyMax;
    public float energyDecreaseRate;
    
    [Header("UI Energy Ally")]
    public GameObject energyUI;


    void Awake()
    {
        energyAlly = energyAllyMax;
        robotMovement = GetComponentInParent<S_Procedural_Robot_Movement>();

        if (robotMovement == null)
            robotMovement = GetComponentInChildren<S_Procedural_Robot_Movement>();
        if (robotMovement == null)
            robotMovement = FindFirstObjectByType<S_Procedural_Robot_Movement>();
        if (robotMovement == null)
            Debug.LogError("S_Energy_Ally : S_Procedural_Robot_Movement introuvable.", this);

        foreach (GameObject obj in FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (obj.name == "Scrollbar")
            {
                energyUI = obj;
                break;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (S_Camera_Controller.instance.robotMode)
        {
            energyUI.SetActive(true);

            if (robotMovement != null && robotMovement.HasMovementInput)
                energyAlly -= energyDecreaseRate * Time.deltaTime;

            energyAlly = Mathf.Clamp(energyAlly, 0, energyAllyMax);
            
            energyUI.GetComponent<UnityEngine.UI.Scrollbar>().size = energyAllyMax > 0f ? energyAlly / energyAllyMax : 0f;

            if (energyAlly <= 0f)
            {
                Debug.Log("Énergie épuisée !");
            }
        }
        else
        {
            energyUI.SetActive(false);
        }
    }
}
