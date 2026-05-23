using System;
using UnityEngine;

[DisallowMultipleComponent]
public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private string enemyName = "Enemy";
    [SerializeField] private int maxHealth = 40;
    [SerializeField] private float destroyDelay = 4f;
    [SerializeField] private Vector3 healthBarOffset = new Vector3(0f, 2.2f, 0f);
    [SerializeField] private bool canBeTargeted = true;

    private int currentHealth;
    private bool isDead;

    public bool IsDead => isDead;
    public bool CanBeTargeted => canBeTargeted && enabled && gameObject.activeInHierarchy && !isDead;
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public event Action<EnemyHealth> Died;
    public event Action<EnemyHealth, int, int> HealthChanged;

    private void Awake()
    {
        if (string.IsNullOrWhiteSpace(enemyName) || enemyName == "Enemy")
        {
            enemyName = gameObject.name.Replace("(Clone)", string.Empty).Trim();
        }

        currentHealth = Mathf.Max(1, maxHealth);
        EnsureHealthBarExists();
        HealthChanged?.Invoke(this, currentHealth, maxHealth);
    }

    public void TakeDamage(int damage, GameObject attacker)
    {
        if (isDead || !canBeTargeted)
        {
            return;
        }

        currentHealth -= Mathf.Max(1, damage);
        currentHealth = Mathf.Clamp(currentHealth, 0, Mathf.Max(1, maxHealth));
        HealthChanged?.Invoke(this, currentHealth, maxHealth);

        if (currentHealth > 0)
        {
            return;
        }

        Die(attacker);
    }

    private void Die(GameObject attacker)
    {
        isDead = true;
        Died?.Invoke(this);

        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }

        Animator animator = GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.enabled = false;
        }

        Debug.Log($"{enemyName} was defeated by {(attacker != null ? attacker.name : "unknown")}.");
        Destroy(gameObject, destroyDelay);
    }

    public Vector3 GetHealthBarWorldPosition()
    {
        return transform.position + healthBarOffset;
    }

    public void SetBattleTargetEnabled(bool isEnabled)
    {
        canBeTargeted = isEnabled;

        EnemyHealthBar[] healthBars = GetComponentsInChildren<EnemyHealthBar>(true);
        for (int i = 0; i < healthBars.Length; i++)
        {
            if (healthBars[i] != null)
            {
                healthBars[i].gameObject.SetActive(isEnabled);
            }
        }
    }

    public void SetMaxHealth(int newMaxHealth)
    {
        maxHealth = Mathf.Max(1, newMaxHealth);
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        HealthChanged?.Invoke(this, currentHealth, maxHealth);
    }

    public void ResetHealth()
    {
        isDead = false;
        currentHealth = Mathf.Max(1, maxHealth);
        HealthChanged?.Invoke(this, currentHealth, maxHealth);
    }

    private void EnsureHealthBarExists()
    {
        if (!canBeTargeted || GetComponentInChildren<EnemyHealthBar>(true) != null)
        {
            return;
        }

        GameObject healthBarObject = new GameObject("EnemyHealthBar");
        healthBarObject.transform.SetParent(transform, false);
        healthBarObject.transform.localPosition = healthBarOffset;
        healthBarObject.transform.localRotation = Quaternion.identity;

        EnemyHealthBar healthBar = healthBarObject.AddComponent<EnemyHealthBar>();
        healthBar.AutoBind(this);
    }
}
