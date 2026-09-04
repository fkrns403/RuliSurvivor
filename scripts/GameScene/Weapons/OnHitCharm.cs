using UnityEngine;

/// <summary>
/// 하트 탄 적중 시 발동하는 직접 피해 + 광역 피해 + 매혹 처리 컴포넌트.
/// 
/// 역할:
/// 1. 직접 맞은 대상에게 즉시 피해를 준다.
/// 2. 적중 위치 주변 적들에게 광역 피해를 준다.
/// 3. 주변 적들에게 CharmStatus를 부여한다.
/// 4. 적중 위치에 CharmImpactFX를 생성해 매혹 범위를 시각적으로 보여준다.
/// 
/// 주의:
/// - 실제 매혹 상태의 지속 동작은 CharmStatus가 담당한다.
/// - 이 스크립트는 하트 탄환이 처음 적중했을 때의 진입 효과만 담당한다.
/// - 하트 발사체에는 Trigger Collider2D가 있어야 한다.
/// - 적 오브젝트 또는 부모 오브젝트에는 IDamageable과 Enemy가 있어야 한다.
/// </summary>
[DisallowMultipleComponent]
public class OnHitCharm : MonoBehaviour
{
    [Header("Direct Hit Damage")]
    [Tooltip("직접 맞은 대상에게 들어갈 즉시 피해")]
    [SerializeField] private float directDamage = 4f;

    [Header("Area Damage")]
    [Tooltip("광역 적용 반경. 이 값이 매혹 범위와 광역 피해 범위가 된다.")]
    [SerializeField] private float radius = 6f;

    [Tooltip("반경 내 적들에게 줄 광역 피해")]
    [SerializeField] private float areaDamage = 2f;

    [Header("Charm")]
    [Tooltip("매혹 지속 시간")]
    [SerializeField] private float charmDuration = 6f;

    [Tooltip("적 탐지용 레이어 마스크")]
    [SerializeField] private LayerMask enemyMask;

    [Header("Impact FX")]
    [Tooltip("적중 시 생성할 하트 범위 이펙트 프리팹")]
    [SerializeField] private GameObject impactFxPrefab;

    [Tooltip("이펙트 크기를 매혹 반경에 맞출지 여부")]
    [SerializeField] private bool scaleImpactFxToRadius = true;

    [Tooltip("이펙트가 너무 커지는 것을 막기 위한 배율. 1이면 반경 지름 그대로 사용한다.")]
    [SerializeField] private float impactFxScaleMultiplier = 1f;

    /// <summary>
    /// 중복 발동 방지.
    /// Trigger가 여러 Collider에 동시에 닿아도 한 번만 실행되게 한다.
    /// </summary>
    private bool triggered;

    /// <summary>
    /// PlayerAbilityController에서 초기값을 주입할 때 사용한다.
    /// 
    /// directDamage:
    /// - 직접 맞은 적에게 들어가는 피해
    /// 
    /// radius:
    /// - 매혹과 광역 피해가 적용되는 반경
    /// 
    /// areaDamage:
    /// - 범위 안 적들에게 들어가는 추가 피해
    /// 
    /// charmDuration:
    /// - 매혹 지속 시간
    /// 
    /// enemyMask:
    /// - 범위 탐색에 사용할 적 레이어
    /// 
    /// impactFxPrefab:
    /// - 적중 시 생성할 범위 표시 이펙트
    /// </summary>
    public void Init(
        float directDamage,
        float radius,
        float areaDamage,
        float charmDuration,
        LayerMask enemyMask,
        GameObject impactFxPrefab = null)
    {
        this.directDamage = Mathf.Max(0f, directDamage);
        this.radius = Mathf.Max(0.1f, radius);
        this.areaDamage = Mathf.Max(0f, areaDamage);
        this.charmDuration = Mathf.Max(0.1f, charmDuration);
        this.enemyMask = enemyMask;
        this.impactFxPrefab = impactFxPrefab;

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

        ApplyDirectDamage(directTarget);
        SpawnImpactFx(center);
        ApplyAreaCharm(center);

        Destroy(gameObject);
    }

    /// <summary>
    /// 충돌한 Collider가 enemyMask에 포함되는지 확인한다.
    /// 
    /// 이 체크를 넣는 이유:
    /// - 벽, 아이템, 플레이어, 기타 Trigger에 닿았을 때 하트가 터지는 것을 방지한다.
    /// </summary>
    private bool IsInEnemyMask(Collider2D other)
    {
        int otherLayerBit = 1 << other.gameObject.layer;
        return (enemyMask.value & otherLayerBit) != 0;
    }

    /// <summary>
    /// 직접 맞은 대상에게 즉시 피해를 준다.
    /// </summary>
    private void ApplyDirectDamage(IDamageable directTarget)
    {
        if (directTarget == null)
            return;

        if (directTarget.IsInvincible)
            return;

        if (directDamage <= 0f)
            return;

        directTarget.TakeDamage(directDamage);
    }

    /// <summary>
    /// 매혹 범위 이펙트를 생성한다.
    /// 
    /// scaleImpactFxToRadius가 true이면:
    /// - 이펙트의 최종 크기를 radius의 지름에 맞춘다.
    /// - SpriteFrameEffect의 startScale/endScale이 있어도,
    ///   이곳에서 프리팹 루트 스케일을 한번 더 조정한다.
    /// </summary>
    private void SpawnImpactFx(Vector3 center)
    {
        if (impactFxPrefab == null)
            return;

        GameObject fx = Instantiate(impactFxPrefab, center, Quaternion.identity);

        if (!scaleImpactFxToRadius)
            return;

        float diameter = radius * 2f * Mathf.Max(0.01f, impactFxScaleMultiplier);
        fx.transform.localScale = new Vector3(diameter, diameter, 1f);
    }

    /// <summary>
    /// 적중 위치 주변의 적들에게 광역 피해와 매혹 상태를 적용한다.
    /// </summary>
    private void ApplyAreaCharm(Vector3 center)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius, enemyMask);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];

            if (hit == null)
                continue;

            Enemy enemy = hit.GetComponentInParent<Enemy>();
            if (enemy == null)
                continue;

            ApplyAreaDamage(hit);
            ApplyCharmStatus(enemy);
        }
    }

    /// <summary>
    /// 범위 안 대상에게 광역 피해를 준다.
    /// </summary>
    private void ApplyAreaDamage(Collider2D hit)
    {
        if (areaDamage <= 0f)
            return;

        IDamageable damageable = hit.GetComponent<IDamageable>();
        if (damageable == null)
            damageable = hit.GetComponentInParent<IDamageable>();

        if (damageable == null)
            return;

        if (damageable.IsInvincible)
            return;

        damageable.TakeDamage(areaDamage);
    }

    /// <summary>
    /// Enemy에게 CharmStatus를 부여하거나 기존 CharmStatus를 갱신한다.
    /// </summary>
    private void ApplyCharmStatus(Enemy enemy)
    {
        if (enemy == null)
            return;

        CharmStatus status = enemy.GetComponent<CharmStatus>();
        if (status == null)
            status = enemy.gameObject.AddComponent<CharmStatus>();

        status.Apply(charmDuration, enemyMask);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
#endif
}