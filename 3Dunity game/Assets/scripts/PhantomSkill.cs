using UnityEngine;

public class PhantomSkill : MonoBehaviour
{
    private PlayerSkills owner;
    private int damage;
    private float range;
    private float radius;
    private float damageDelay;
    private float lifetime;
    private float damageTimer;
    private float age;
    private bool damageApplied;
    private GameObject visualInstance;
    private string visualLayerName;
    private GameObject swordPrefab;
    private string swordHandBoneName;
    private string[] swordHandBoneFallbackNames;
    private Vector3 swordLocalPosition;
    private Vector3 swordLocalRotation;
    private Vector3 swordLocalScale;

    public void Initialize(PlayerSkills skillOwner, int slashDamage, float slashRange, float slashRadius, float slashDamageDelay, float slashLifetime, GameObject visualPrefab, GameObject phantomSwordVisualPrefab, string layerName, string handBoneName, string[] handBoneFallbackNames, Vector3 swordPosition, Vector3 swordRotation, Vector3 swordScale)
    {
        owner = skillOwner;
        damage = slashDamage;
        range = slashRange;
        radius = slashRadius;
        damageDelay = Mathf.Max(0f, slashDamageDelay);
        lifetime = Mathf.Max(damageDelay + 0.05f, slashLifetime);
        damageTimer = damageDelay;
        visualLayerName = layerName;
        swordPrefab = phantomSwordVisualPrefab;
        swordHandBoneName = handBoneName;
        swordHandBoneFallbackNames = handBoneFallbackNames;
        swordLocalPosition = swordPosition;
        swordLocalRotation = swordRotation;
        swordLocalScale = swordScale;

        BuildVisual(visualPrefab);
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        age += Time.deltaTime;

        if (!damageApplied)
        {
            damageTimer -= Time.deltaTime;
            if (damageTimer <= 0f)
            {
                damageApplied = true;
                if (owner != null)
                {
                    Vector3 origin = transform.position + Vector3.up;
                    Vector3 direction = transform.forward;
                    owner.DealSlashDamage(origin, direction, range, radius, damage, owner.gameObject, owner.transform);
                }
            }
        }

        if (visualInstance != null)
        {
            float normalized = lifetime <= 0f ? 1f : Mathf.Clamp01(age / lifetime);
            ApplyFade(normalized);
        }
    }

    private void BuildVisual(GameObject visualPrefab)
    {
        if (visualPrefab != null)
        {
            visualInstance = Instantiate(visualPrefab, transform);
            visualInstance.transform.localPosition = Vector3.zero;
            visualInstance.transform.localRotation = Quaternion.identity;
            DisableGameplayComponents(visualInstance);
            AttachSwordIfNeeded();
            TriggerAttackAnimation();
            return;
        }

        visualInstance = new GameObject("PhantomFallbackVisual");
        visualInstance.transform.SetParent(transform, false);

        Renderer[] renderers = new Renderer[3];

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "PhantomBody";
        body.transform.SetParent(visualInstance.transform, false);
        body.transform.localPosition = new Vector3(0f, 1f, 0f);
        body.transform.localScale = new Vector3(0.6f, 1f, 0.6f);
        renderers[0] = body.GetComponent<Renderer>();
        Destroy(body.GetComponent<Collider>());

        GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
        arm.name = "PhantomArm";
        arm.transform.SetParent(visualInstance.transform, false);
        arm.transform.localPosition = new Vector3(0.35f, 1.1f, 0.15f);
        arm.transform.localRotation = Quaternion.Euler(0f, 0f, -35f);
        arm.transform.localScale = new Vector3(0.15f, 0.8f, 0.15f);
        renderers[1] = arm.GetComponent<Renderer>();
        Destroy(arm.GetComponent<Collider>());

        GameObject slash = GameObject.CreatePrimitive(PrimitiveType.Cube);
        slash.name = "PhantomSlashVisual";
        slash.transform.SetParent(visualInstance.transform, false);
        slash.transform.localPosition = new Vector3(0f, 1.1f, 0.9f);
        slash.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        slash.transform.localScale = new Vector3(0.1f, 1.4f, 0.2f);
        renderers[2] = slash.GetComponent<Renderer>();
        Destroy(slash.GetComponent<Collider>());

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            renderer.material = new Material(Shader.Find("Standard"));
            renderer.material.color = new Color(0.45f, 0.8f, 1f, 0.55f);
        }
    }

    private void AttachSwordIfNeeded()
    {
        if (visualInstance == null || swordPrefab == null)
        {
            return;
        }

        Transform handBone = ResolveHandBone(visualInstance.transform);
        if (handBone == null)
        {
            return;
        }

        GameObject sword = Instantiate(swordPrefab, handBone);
        sword.transform.localPosition = swordLocalPosition;
        sword.transform.localRotation = Quaternion.Euler(swordLocalRotation);
        sword.transform.localScale = swordLocalScale;

        Collider[] colliders = sword.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }
    }

    private void TriggerAttackAnimation()
    {
        if (visualInstance == null)
        {
            return;
        }

        Animator animator = visualInstance.GetComponentInChildren<Animator>();
        if (animator == null)
        {
            return;
        }

        for (int i = 0; i < animator.parameterCount; i++)
        {
            AnimatorControllerParameter parameter = animator.parameters[i];
            if (parameter.type == AnimatorControllerParameterType.Trigger && parameter.name == "Attack")
            {
                animator.SetTrigger("Attack");
                return;
            }
        }
    }

    private Transform ResolveHandBone(Transform root)
    {
        Transform result = FindChildRecursive(root, swordHandBoneName);
        if (result != null)
        {
            return result;
        }

        if (swordHandBoneFallbackNames != null)
        {
            for (int i = 0; i < swordHandBoneFallbackNames.Length; i++)
            {
                string candidate = swordHandBoneFallbackNames[i];
                if (string.IsNullOrWhiteSpace(candidate))
                {
                    continue;
                }

                result = FindChildRecursive(root, candidate);
                if (result != null)
                {
                    return result;
                }
            }
        }

        return null;
    }

    private Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null || string.IsNullOrWhiteSpace(childName))
        {
            return null;
        }

        if (root.name == childName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform result = FindChildRecursive(root.GetChild(i), childName);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    private void DisableGameplayComponents(GameObject root)
    {
        int targetLayer = LayerMask.NameToLayer(visualLayerName);
        if (targetLayer >= 0)
        {
            SetLayerRecursively(root.transform, targetLayer);
        }

        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }

        Behaviour[] behaviours = root.GetComponentsInChildren<Behaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            Behaviour behaviour = behaviours[i];
            if (behaviour is Renderer || behaviour is Animator)
            {
                continue;
            }

            if (behaviour == this)
            {
                continue;
            }

            behaviour.enabled = false;
        }
    }

    private void ApplyFade(float normalizedLifetime)
    {
        if (visualInstance == null)
        {
            return;
        }

        float alpha = Mathf.Lerp(0.6f, 0f, normalizedLifetime);
        Renderer[] renderers = visualInstance.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            Material material = renderer.material;
            Color color = material.color;
            color.a = alpha;
            material.color = color;
        }
    }

    private void SetLayerRecursively(Transform root, int layer)
    {
        root.gameObject.layer = layer;
        for (int i = 0; i < root.childCount; i++)
        {
            SetLayerRecursively(root.GetChild(i), layer);
        }
    }
}
