using UnityEngine;

/// <summary>
/// 적중 시 중심점에서 범위 폭발을 일으키는 투사체 보조 컴포넌트.
/// 
/// 사용 예:
/// - 불의 활 열화판 탄환
/// - FireArrow 캐릭터의 폭발 화살
/// 
/// 역할:
/// 1. 적에게 처음 닿았을 때 한 번만 발동한다.
/// 2. 폭발 이펙트를 생성한다.
/// 3. 중심점 주변 Enemy Layer 대상에게 범위 피해를 준다.
/// 4. 풀링 탄환이면 Destroy하지 않고 BulletPoolManager로 반환한다.
/// </summary>
[DisallowMultipleComponent]
public class OnHitExplode : MonoBehaviour
{
    [Header("Explosion")]
    [Tooltip("적중 시 생성할 폭발 이펙트 프리팹")]
    [SerializeField] private GameObject explosionPrefab;

    [Tooltip("폭발 범위")]
    [SerializeField] private float radius = 1.4f;

    [Tooltip("폭발 범위 안 적들에게 줄 피해")]
    [SerializeField] private float explosionDamage = 12f;

    [Tooltip("폭발 피해를 줄 대상 레이어")]
    [SerializeField] private LayerMask enemyMask;

    [Header("Return")]
    [Tooltip("풀링 탄환이면 On, 스킬처럼 Instantiate 탄환이면 Off")]
    [SerializeField] private bool returnToPool = true;

    private bool triggered;

    /// <summary>
    /// 외부에서 폭발 값을 덮어쓸 때 사용한다.
    /// 예: 캐릭터 고유 스킬이 런타임에 폭발 범위/피해를 지정하는 경우.
    /// </summary>
    public void Init(GameObject explosionFx, float radius, float damage, LayerMask enemyMask)
    {
        explosionPrefab = explosionFx;
        this.radius = Mathf.Max(0.1f, radius);
        explosionDamage = Mathf.Max(0f, damage);
        this.enemyMask = enemyMask;
        triggered = false;
    }

    private void OnEnable()
    {
        triggered = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered)
            return;

        if (!IsInEnemyMask(other))
            return;

        IDamageable directTarget = other.GetComponent<IDamageable>();
        if (directTarget == null)
            directTarget = other.GetComponentInParent<IDamageable>();

        if (directTarget == null)
            return;

        triggered = true;

        Vector3 center = transform.position;

        SpawnExplosionFx(center);
        ApplyExplosionDamage(center);
        FinishProjectile();
    }

    private bool IsInEnemyMask(Collider2D other)
    {
        int bit = 1 << other.gameObject.layer;
        return (enemyMask.value & bit) != 0;
    }

    private void SpawnExplosionFx(Vector3 center)
    {
        if (explosionPrefab == null)
            return;

        Instantiate(explosionPrefab, center, Quaternion.identity);
    }

    private void ApplyExplosionDamage(Vector3 center)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius, enemyMask);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null)
                continue;

            IDamageable damageable = hit.GetComponent<IDamageable>();
            if (damageable == null)
                damageable = hit.GetComponentInParent<IDamageable>();

            if (damageable == null)
                continue;

            if (damageable.IsInvincible)
                continue;

            damageable.TakeDamage(explosionDamage);
        }
    }

    private void FinishProjectile()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.velocity = Vector2.zero;

        if (returnToPool && BulletPoolManager.Instance != null)
        {
            BulletPoolManager.Instance.ReleaseBullet(gameObject);
            return;
        }

        Destroy(gameObject);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
#endif
}