using System.Collections;
using UnityEngine;

public class S_Attack_System : MonoBehaviour
{
    public S_HealthBar healthBar;

    public int damage;
    private bool canAttack;

    public LayerMask character;

    void Start()
    {
        healthBar = GameObject.Find("Character").GetComponent<S_HealthBar>();
        canAttack = true;
    }

    // Update is called once per frame
    void Update()
    {
        DetectionAndAttack();
    }

    void DetectionAndAttack()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, 4f, character);

        if (colliders.Length > 0)
        {
            if (canAttack)
            {
                StartCoroutine(AttackRoutine());
            }
        }
    }

    IEnumerator AttackRoutine()
    {
        canAttack = false;

        healthBar.TakeDamage(damage);
        
        Debug.Log(healthBar.currentHealth);

        yield return new WaitForSeconds(5f);

        canAttack = true;
    }
}
