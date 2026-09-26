using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;

    [SerializeField] private float currentHealth;

    [Header("Optional UI")]
    [SerializeField] private Slider healthBar;

    public float CurrentHealth => currentHealth;

    public float MaxHealth => maxHealth;

    private void Start()
    {
        currentHealth = maxHealth;

        UpdateUI();
    }

    public bool Heal(float amount)
    {
        if (currentHealth >= maxHealth)
            return false;

        float oldHealth = currentHealth;

        currentHealth = Mathf.Clamp(
            currentHealth + amount,
            0f,
            maxHealth
        );

        UpdateUI();

        return currentHealth > oldHealth;
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;

        currentHealth = Mathf.Clamp(
            currentHealth,
            0f,
            maxHealth
        );

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (healthBar != null)
        {
            healthBar.maxValue = maxHealth;
            healthBar.value = currentHealth;
        }
    }
}