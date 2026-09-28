using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class S_HealthBar : MonoBehaviour
{
    public Slider healthBarSlider;
    public int maxHealth = 100;
    public int currentHealth;
    public int heal = 20;
    public Image fill;

    void Start()
    {
        currentHealth = maxHealth;
        healthBarSlider.maxValue = maxHealth;
        healthBarSlider.value = currentHealth;
    }

    void Update()
    {
        healthBarSlider.value = currentHealth;

        if (Input.GetKeyDown(KeyCode.T))
        {
            AddHealth(heal);
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            TakeDamage(heal);
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
   public bool AddHealth(int healAmount)
    {
        if (currentHealth >= maxHealth)
        {
            Debug.Log("Vie déjà au maximum.");
            return false;
        }
    
        int oldHealth = currentHealth;
    
        currentHealth += healAmount;
    
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }
    
        int realHeal = currentHealth - oldHealth;
    
        Debug.Log("Heal : +" + realHeal);
    
        return true;
    }
    
    void Dead()
    {
        Debug.Log("Dead");

        fill.gameObject.SetActive(false); // Efface le reste de la barre de vie lorsque le joueur meurt.

        SceneManager.LoadSceneAsync(1);
    }
}
