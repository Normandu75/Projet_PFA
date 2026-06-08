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

//----- Utilisation de la clef
    public bool UseItem(S_Door nearDoor)
    {
        if (statToChange == StatToChange.key)
        {
            if (nearDoor == null)
            {
                return false;
            }
            return nearDoor.TryUnlock(itemName);
        }

        return UseItem();
    }

    public enum StatToChange
    {
        none,
        health,
        stamina,
        key
    };
}
