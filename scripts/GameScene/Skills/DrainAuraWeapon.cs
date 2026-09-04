using UnityEngine;

/// <summary>
/// Drain 전용 오라 무기.
/// 
/// 역할:
/// - 플레이어 주변 원형 범위 안의 적에게 주기적으로 피해를 준다.
/// - 구버전 Drain의 CircleCollider2D 기반 지속 피해 구조를
///   현버전 IDamageable 기반으로 재구성한 버전이다.
/// 
/// 특징:
/// - 별도 탄환을 발사하지 않는다.
/// - 플레이어 위치를 계속 따라간다.
/// - 범위, 틱 간격, 피해량은 ItemData 레벨과 Inspector 값을 기준으로 계산한다.
/// </summary>
[DisallowMultipleComponent]
public class DrainAuraWeapon : MonoBehaviour, IWeaponRuntime
{
    [Header("Visual")]
    [Tooltip("오라 범위를 보여줄 시각 오브젝트. 없어도 동작한다.")]
    [SerializeField] private Transform auraVisual;

    [Header("Target")]
    [Tooltip("피해를 줄 대상 레이어")]
    [SerializeField] private LayerMask enemyMask;

    [Header("Aura")]
    [SerializeField] private float baseRadius = 1.5f;
    [SerializeField] private float radiusPerLevel = 0.15f;
    [SerializeField] private float tickInterval = 0.5f;

    [Header("Fallback Stats")]
    [SerializeField] private float fallbackBaseDamage = 5f;

    private Transform owner;
    private ItemData itemData;
    private PlayerStatSystem statSystem;

    private int level = 1;
    private float timer;

    private readonly Collider2D[] hitBuffer = new Collider2D[64];

    public void OnEquip(Transform ownerTransform, ItemData data)
    {
        owner = ownerTransform;
        itemData = data;
        level = 1;
        timer = 0f;

        if (owner != null)
            statSystem = owner.GetComponent<PlayerStatSystem>();

        UpdateVisualScale();
    }

    public void OnUnequip()
    {
    }

    public void SetLevel(int newLevel)
    {
        level = Mathf.Max(1, newLevel);

        if (itemData != null)
            level = itemData.ClampLevel(level);

        UpdateVisualScale();
    }

    private void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm != null && (!gm.isLive || gm.isPaused))
            return;

        if (owner == null)
            return;

        transform.position = owner.position;

        timer += Time.deltaTime;

        if (timer < GetTickInterval())
            return;

        timer = 0f;
        ApplyDrainDamage();
    }

    private void ApplyDrainDamage()
    {
        int hitCount = Physics2D.OverlapCircleNonAlloc(
            transform.position,
            GetRadius(),
            hitBuffer,
            enemyMask
        );

        if (hitCount <= 0)
            return;

        float damage = GetDamageByLevel(level);

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D col = hitBuffer[i];

            if (col == null)
                continue;

            IDamageable dmg = col.GetComponent<IDamageable>();
            if (dmg == null)
                dmg = col.GetComponentInParent<IDamageable>();

            if (dmg == null)
                continue;

            if (dmg.IsInvincible)
                continue;

            dmg.TakeDamage(damage);
        }
    }

    private float GetRadius()
    {
        return Mathf.Max(0.1f, baseRadius + (level - 1) * radiusPerLevel);
    }

    private float GetDamageByLevel(int currentLevel)
    {
        currentLevel = Mathf.Max(1, currentLevel);

        if (itemData != null)
            return Mathf.Max(0.01f, itemData.GetDamageAtLevel(currentLevel));

        return fallbackBaseDamage + (currentLevel - 1) * 2f;
    }

    private float GetTickInterval()
    {
        float mul = 1f;

        if (statSystem != null)
            mul = Mathf.Max(0.01f, statSystem.GetAttackSpeedMultiplier());

        return Mathf.Max(0.05f, tickInterval / mul);
    }

    private void UpdateVisualScale()
    {
        if (auraVisual == null)
            return;

        float diameter = GetRadius() * 2f;
        auraVisual.localScale = new Vector3(diameter, diameter, 1f);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, GetRadius());
    }
#endif
}