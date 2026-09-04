using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(ParticleSystem))]
public class BossParticleDamage : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private float damage = 10f;

    [Header("Target")]
    [SerializeField] private LayerMask targetMask;

    [Header("Hit Rule")]
    [SerializeField] private float sameTargetHitCooldown = 0.25f;

    [Header("Attack Animation")]
    [SerializeField] private BossAttackAnimationController attackAnimationController;
    [SerializeField] private bool playAttackAnimationOnEnable = true;

    [Header("Auto Release")]
    [SerializeField] private bool destroyAfterLifetime = true;
    [SerializeField] private float lifeTime = 5f;

    private readonly Dictionary<int, float> lastHitTimes =
        new Dictionary<int, float>();

    private ParticleSystem particleSystemCache;
    private bool initialized;

    private void Awake()
    {
        particleSystemCache = GetComponent<ParticleSystem>();
    }

    private void OnEnable()
    {
        lastHitTimes.Clear();

        if (playAttackAnimationOnEnable &&
            attackAnimationController != null)
        {
            attackAnimationController.PlayAttackAnimation();
        }

        if (!initialized)
            ScheduleDestroy(lifeTime);
    }

    public void Init(
        float newDamage,
        LayerMask newTargetMask,
        float newLifeTime,
        float newHitCooldown)
    {
        initialized = true;

        damage = Mathf.Max(0f, newDamage);
        targetMask = newTargetMask;
        lifeTime = Mathf.Max(0.1f, newLifeTime);
        sameTargetHitCooldown = Mathf.Max(0f, newHitCooldown);

        lastHitTimes.Clear();

        ScheduleDestroy(lifeTime);
    }

    public void Init(
        float newDamage,
        LayerMask newTargetMask,
        float newLifeTime,
        float newHitCooldown,
        BossAttackAnimationController newAttackAnimationController)
    {
        attackAnimationController = newAttackAnimationController;

        Init(
            newDamage,
            newTargetMask,
            newLifeTime,
            newHitCooldown
        );

        if (attackAnimationController != null)
            attackAnimationController.PlayAttackAnimation();
    }

    private void OnParticleCollision(GameObject other)
    {
        if (other == null)
            return;

        if (!IsInTargetMask(other))
            return;

        IDamageable damageable =
            other.GetComponent<IDamageable>();

        if (damageable == null)
            damageable = other.GetComponentInParent<IDamageable>();

        if (damageable == null)
            return;

        if (damageable.IsInvincible)
            return;

        int id = damageable.GetHashCode();

        if (sameTargetHitCooldown > 0f)
        {
            if (lastHitTimes.TryGetValue(id, out float lastTime))
            {
                if (Time.time - lastTime < sameTargetHitCooldown)
                    return;
            }
        }

        lastHitTimes[id] = Time.time;
        damageable.TakeDamage(damage);
    }

    private void ScheduleDestroy(float delay)
    {
        if (!destroyAfterLifetime)
            return;

        CancelInvoke(nameof(DestroySelf));
        Invoke(nameof(DestroySelf), Mathf.Max(0.1f, delay));
    }

    private void DestroySelf()
    {
        Destroy(gameObject);
    }

    private bool IsInTargetMask(GameObject other)
    {
        int bit = 1 << other.layer;
        return (targetMask.value & bit) != 0;
    }
}