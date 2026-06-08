using UnityEngine;
[CreateAssetMenu]
public class S_ItemSO : ScriptableObject
{
    public string itemName;
    public StatToChange statToChange = new StatToChange();
    public int amountToChangeStat;

    public bool UseItem()
    {
        if(statToChange == StatToChange.health)
        {
            S_HealthBar healthBar = GameObject.Find("Character").GetComponent<S_HealthBar>();
            if(healthBar.currentHealth == healthBar.maxHealth)
            {
                return false;
            }
            else
            {
                healthBar.AddHealth(amountToChangeStat);
                Debug.Log("Heal de 20 HP");
                return true;
            }    
        }
        return false;
        
    }

    public enum StatToChange
    {
        none,
        health,
        stamina
    };
}
