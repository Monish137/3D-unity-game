using UnityEngine;

[DisallowMultipleComponent]
public class AnimalHealth : MonoBehaviour
{
    [SerializeField] private string animalName = "Animal";
    [SerializeField] private int animalLevel = 1;
    [SerializeField] private int maxHealth = 30;
    [SerializeField] private int meatDropCount = 1;
    [SerializeField] private float destroyDelay = 1.5f;
    [SerializeField] private Vector3 meatSpawnOffset = new Vector3(0f, 0.6f, 0f);
    [SerializeField] private Vector3 healthBarOffset = new Vector3(0f, 1.7f, 0f);
    [SerializeField] private float healthBarWidth = 1.2f;
    [SerializeField] private float healthBarHeight = 0.04f;
    [SerializeField] private float healthBarVisibleDuration = 2f;

    private int currentHealth;
    private bool isDead;
    private GameObject healthBarBackground;
    private GameObject healthBarFill;
    private Camera mainCamera;
    private float healthBarVisibleTimer;

    private void Awake()
    {
        AutoConfigureFromName();
        currentHealth = Mathf.Max(1, maxHealth);
        CreateHealthBar();
        UpdateHealthBar();
        SetHealthBarVisible(false);
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (healthBarBackground != null && mainCamera != null)
        {
            healthBarBackground.transform.LookAt(mainCamera.transform);
            healthBarBackground.transform.Rotate(0, 180, 0);
        }

        if (healthBarVisibleTimer > 0f)
        {
            healthBarVisibleTimer -= Time.deltaTime;
            if (healthBarVisibleTimer <= 0f)
            {
                SetHealthBarVisible(false);
            }
        }
    }

    public void TakeDamage(int damage, GameObject attacker)
    {
        if (isDead)
        {
            return;
        }

        currentHealth -= Mathf.Max(1, damage);
        UpdateHealthBar();
        ShowHealthBar();

        if (currentHealth <= 0)
        {
            Die(attacker);
        }
    }

    private void Die(GameObject attacker)
    {
        isDead = true;

        SpawnMeatDrops();
        DisableBehaviours();

        if (healthBarBackground != null)
        {
            Destroy(healthBarBackground);
        }

        Destroy(gameObject, destroyDelay);
        Debug.Log($"{animalName} was defeated by {(attacker != null ? attacker.name : "unknown")}.");
    }

    private void CreateHealthBar()
    {
        healthBarBackground = new GameObject("HealthBar");
        healthBarBackground.transform.SetParent(transform, false);
        healthBarBackground.transform.localPosition = healthBarOffset;

        GameObject background = GameObject.CreatePrimitive(PrimitiveType.Quad);
        background.name = "Background";
        background.transform.SetParent(healthBarBackground.transform, false);
        background.transform.localPosition = Vector3.zero;
        background.transform.localScale = new Vector3(healthBarWidth, healthBarHeight, 1);

        Renderer bgRenderer = background.GetComponent<Renderer>();
        if (bgRenderer != null)
        {
            bgRenderer.material = new Material(Shader.Find("Unlit/Color"));
            bgRenderer.material.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        }

        healthBarFill = GameObject.CreatePrimitive(PrimitiveType.Quad);
        healthBarFill.name = "Fill";
        healthBarFill.transform.SetParent(healthBarBackground.transform, false);
        healthBarFill.transform.localPosition = new Vector3(0, 0, -0.01f);
        healthBarFill.transform.localScale = new Vector3(healthBarWidth, healthBarHeight * 0.72f, 1f);

        Renderer fillRenderer = healthBarFill.GetComponent<Renderer>();
        if (fillRenderer != null)
        {
            fillRenderer.material = new Material(Shader.Find("Unlit/Color"));
            fillRenderer.material.color = new Color(0.9f, 0.2f, 0.2f, 0.9f);
        }

        Destroy(background.GetComponent<Collider>());
        Destroy(healthBarFill.GetComponent<Collider>());
    }

    private void UpdateHealthBar()
    {
        if (healthBarFill == null)
            return;

        float healthPercentage = (float)currentHealth / maxHealth;

        Vector3 newScale = healthBarFill.transform.localScale;
        newScale.x = healthBarWidth * healthPercentage;
        newScale.y = healthBarHeight * 0.72f;
        healthBarFill.transform.localScale = newScale;

        Vector3 newPosition = healthBarFill.transform.localPosition;
        newPosition.x = -(healthBarWidth - newScale.x) / 2f;
        healthBarFill.transform.localPosition = newPosition;
    }

    private void SpawnMeatDrops()
    {
        for (int i = 0; i < Mathf.Max(1, meatDropCount); i++)
        {
            GameObject meat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            meat.name = $"{animalName}_Meat_{i + 1}";
            meat.transform.position = transform.position + meatSpawnOffset + new Vector3((i - 0.5f) * 0.4f, 0f, 0f);
            meat.transform.localScale = new Vector3(0.35f, 0.25f, 0.45f);

            Renderer renderer = meat.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(0.66f, 0.18f, 0.16f);
            }

            Collider collider = meat.GetComponent<Collider>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }

            MeatPickup pickup = meat.AddComponent<MeatPickup>();
            pickup.SetMeatLabel($"{animalName} Meat");
        }
    }

    private void DisableBehaviours()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }

        Animator animator = GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.enabled = false;
        }

        BearMovement bearMovement = GetComponent<BearMovement>();
        if (bearMovement != null)
        {
            bearMovement.enabled = false;
        }

        if (healthBarBackground != null)
        {
            healthBarBackground.SetActive(false);
        }
    }

    private void ShowHealthBar()
    {
        healthBarVisibleTimer = healthBarVisibleDuration;
        SetHealthBarVisible(true);
    }

    private void SetHealthBarVisible(bool isVisible)
    {
        if (healthBarBackground != null)
        {
            healthBarBackground.SetActive(isVisible);
        }
    }

    private void AutoConfigureFromName()
    {
        string lowerName = gameObject.name.ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(animalName) || animalName == "Animal")
        {
            animalName = gameObject.name.Replace("(Clone)", string.Empty).Trim();
        }

        if (lowerName.Contains("bear"))
        {
            animalLevel = Mathf.Max(animalLevel, 3);
            maxHealth = Mathf.Max(maxHealth, 45);
            meatDropCount = Mathf.Max(meatDropCount, 2);
            return;
        }

        if (lowerName.Contains("wolf") || lowerName.Contains("boar"))
        {
            animalLevel = Mathf.Max(animalLevel, 2);
            maxHealth = Mathf.Max(maxHealth, 30);
            return;
        }

        if (lowerName.Contains("stag") || lowerName.Contains("moose") || lowerName.Contains("doe") || lowerName.Contains("calf"))
        {
            animalLevel = Mathf.Max(animalLevel, 2);
            maxHealth = Mathf.Max(maxHealth, 25);
            meatDropCount = Mathf.Max(meatDropCount, 2);
            return;
        }

        animalLevel = Mathf.Max(animalLevel, 1);
        maxHealth = Mathf.Max(maxHealth, 20);
    }
}
