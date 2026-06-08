using UnityEngine;
using UnityEngine.UI;

public class S_HealthBar : MonoBehaviour
{
    //public Slider healthBarSlider;
    public int maxHealth = 100;
    public int currentHealth;
    public int heal = 20;
    //public Image fill;

    void Start()
    {
        currentHealth = maxHealth;
        // healthBarSlider.maxValue = maxHealth;
        // healthBarSlider.value = currentHealth;
    }

    void Update()
    {
        //healthBarSlider.value = currentHealth;

        if (Input.GetKeyDown(KeyCode.T))
        {
            AddHealth(heal);
        }
    }

    public void TakeDamage(int damage) // Perds en appuyant sur R la vie.
    {
        currentHealth -= damage;

        if (currentHealth < 0)
        {
            currentHealth = 0;
            Dead();
        }
            

        Debug.Log("Damage : -" + damage);
    }
    public void AddHealth(int heal) // Ajoute en appuyant sur T la vie.
    {
        currentHealth += heal;

        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }

        Debug.Log("Heal : " + heal);
    }
    
    void Dead()
    {
        Debug.Log("Dead");

        //fill.gameObject.SetActive(false); // Efface le reste de la barre de vie lorsque le joueur meurt.

        Debug.Log("GG");
        
    }
}
