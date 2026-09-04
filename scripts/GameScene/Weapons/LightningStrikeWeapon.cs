using UnityEngine;

/// <summary>
/// 낙뢰 무기.
/// 
/// 역할:
/// - 플레이어 주변 적을 탐색한다.
/// - 가까운 적 순서대로 낙뢰 피해를 준다.
/// - 낙뢰 본체 이펙트와 바닥 그림자 이펙트를 생성한다.
/// - 전격 피격 시 HitFeedback2D의 청백색 깜빡임을 실행한다.
/// </summary>
public class LightningStrikeWeapon : MonoBehaviour, IWeaponRuntime
{
    [Header("Target Search")]
    [SerializeField] private LayerMask enemyMask = ~0;
    [SerializeField] private float searchRange = 8f;

    [Header("Attack")]
    [SerializeField] private float strikeInterval = 0.5f;
    [SerializeField] private float fallbackBaseDamage = 14f;
    [SerializeField] private int fallbackStrikeCount = 1;

    [Header("Lightning Body Effect")]
    [SerializeField] private GameObject lightningEffectPrefab;
    [SerializeField] private float spriteYOffset = 1f;
    [SerializeField] private Vector3 spriteEffectScale = Vector3.one;

    [Header("Lightning Shadow Effect")]
    [SerializeField] private GameObject lightningShadowPrefab;
    [SerializeField] private float shadowYOffset = 0f;
    [SerializeField] private Vector3 shadowEffectScale = Vector3.one;

    [Header("Fallback Line Effect")]
    [SerializeField] private float boltHeight = 4f;
    [SerializeField] private float effectDuration = 0.12f;
    [SerializeField] private float lineWidth = 0.18f;

    private Transform owner;
    private ItemData itemData;
    private int level = 1;
    private float timer;

    private PlayerStatSystem playerStatSystem;
    private readonly Collider2D[] hitBuffer = new Collider2D[32];

    public void OnEquip(Transform ownerTransform, ItemData data)
    {
        owner = ownerTransform;
        itemData = data;
        level = 1;
        timer = 0f;

        if (owner != null)
            playerStatSystem = owner.GetComponent<PlayerStatSystem>();
    }

    public void OnUnequip()
    {
    }

    public void SetLevel(int newLevel)
    {
        level = Mathf.Max(1, newLevel);

        if (itemData != null)
            level = itemData.ClampLevel(level);
    }

    private void Update()
    {
        GameManager gm = GameManager.Instance;

        if (gm != null && (!gm.isLive || gm.isPaused))
            return;

        if (owner == null)
            return;

        timer += Time.deltaTime;

        if (timer < GetStrikeInterval())
            return;

        timer = 0f;
        StrikeTargets();
    }

    private void StrikeTargets()
    {
        int hitCount = Physics2D.OverlapCircleNonAlloc(
            owner.position,
            searchRange,
            hitBuffer,
            enemyMask
        );

        if (hitCount <= 0)
            return;

        int strikeCount = GetStrikeCountByLevel(level);
        float damage = GetDamageByLevel(level);

        for (int s = 0; s < strikeCount; s++)
        {
            Transform target = GetNthNearestTarget(owner.position, hitCount, s);

            if (target == null)
                continue;

            IDamageable dmg = target.GetComponent<IDamageable>();

            if (dmg == null)
                dmg = target.GetComponentInParent<IDamageable>();

            if (dmg != null && !dmg.IsInvincible)
            {
                dmg.TakeDamage(damage);
                PlayLightningHitFeedback(dmg, target);
            }

            SpawnLightningEffect(target.position);
        }
    }

    private void PlayLightningHitFeedback(IDamageable damageable, Transform target)
    {
        HitFeedback2D hitFeedback = null;

        Component damageableComponent = damageable as Component;

        if (damageableComponent != null)
        {
            hitFeedback = damageableComponent.GetComponent<HitFeedback2D>();

            if (hitFeedback == null)
                hitFeedback = damageableComponent.GetComponentInParent<HitFeedback2D>();

            if (hitFeedback == null)
                hitFeedback = damageableComponent.GetComponentInChildren<HitFeedback2D>(true);
        }

        if (hitFeedback == null && target != null)
        {
            hitFeedback = target.GetComponent<HitFeedback2D>();

            if (hitFeedback == null)
                hitFeedback = target.GetComponentInParent<HitFeedback2D>();

            if (hitFeedback == null)
                hitFeedback = target.GetComponentInChildren<HitFeedback2D>(true);
        }

        if (hitFeedback != null)
            hitFeedback.PlayLightningHit();
    }

    private void SpawnLightningEffect(Vector3 targetPosition)
    {
        SpawnLightningShadow(targetPosition);

        if (lightningEffectPrefab != null)
        {
            Vector3 pos = targetPosition + Vector3.up * spriteYOffset;

            GameObject fx =
                Instantiate(lightningEffectPrefab, pos, Quaternion.identity);

            fx.transform.localScale = spriteEffectScale;
            return;
        }

        Vector3 end = targetPosition;
        Vector3 start = end + Vector3.up * boltHeight;

        LightningStrikeEffect.Create(
            start,
            end,
            effectDuration,
            lineWidth
        );
    }

    private void SpawnLightningShadow(Vector3 targetPosition)
    {
        if (lightningShadowPrefab == null)
            return;

        Vector3 pos = targetPosition + Vector3.up * shadowYOffset;

        GameObject shadow =
            Instantiate(lightningShadowPrefab, pos, Quaternion.identity);

        shadow.transform.localScale = shadowEffectScale;
    }

    private Transform GetNthNearestTarget(Vector3 from, int count, int order)
    {
        Transform[] sorted = new Transform[count];
        float[] distArr = new float[count];

        int validCount = 0;

        for (int i = 0; i < count; i++)
        {
            Collider2D col = hitBuffer[i];

            if (col == null)
                continue;

            Transform tr = col.transform;

            if (tr == null || !tr.gameObject.activeInHierarchy)
                continue;

            sorted[validCount] = tr;
            distArr[validCount] = (tr.position - from).sqrMagnitude;
            validCount++;
        }

        for (int i = 0; i < validCount - 1; i++)
        {
            for (int j = i + 1; j < validCount; j++)
            {
                if (distArr[j] < distArr[i])
                {
                    float tempDist = distArr[i];
                    distArr[i] = distArr[j];
                    distArr[j] = tempDist;

                    Transform tempTransform = sorted[i];
                    sorted[i] = sorted[j];
                    sorted[j] = tempTransform;
                }
            }
        }

        if (order < 0 || order >= validCount)
            return null;

        return sorted[order];
    }

    private float GetDamageByLevel(int currentLevel)
    {
        currentLevel = Mathf.Max(1, currentLevel);

        if (itemData != null)
            return Mathf.Max(0.01f, itemData.GetDamageAtLevel(currentLevel));

        return fallbackBaseDamage + (currentLevel - 1) * 4f;
    }

    private int GetStrikeCountByLevel(int currentLevel)
    {
        currentLevel = Mathf.Max(1, currentLevel);

        if (itemData != null)
            return Mathf.Max(1, itemData.GetCountAtLevel(currentLevel));

        return fallbackStrikeCount + (currentLevel - 1) / 2;
    }

    private float GetStrikeInterval()
    {
        float attackSpeedMul = 1f;

        if (playerStatSystem != null)
            attackSpeedMul = Mathf.Max(0.01f, playerStatSystem.GetAttackSpeedMultiplier());

        return Mathf.Max(0.05f, strikeInterval / attackSpeedMul);
    }
}