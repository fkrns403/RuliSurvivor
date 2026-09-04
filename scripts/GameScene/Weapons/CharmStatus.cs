using UnityEngine;

/// <summary>
/// 적에게 부여되는 매혹 상태
/// 
/// 역할:
/// 1. 일정 시간 동안 플레이어 대신 다른 적을 추적하게 만든다.
/// 2. 매혹 상태 유지 중, 일정 주기마다 주변 적에게 피해를 준다.
/// 3. 매혹 종료 시 원래대로 플레이어를 다시 추적하게 되돌린다.
/// 
/// 설계 의도:
/// - 현재 프로젝트는 "적 대 적 전투 시스템"이 기본 설계로 열려 있지 않다.
/// - 그래서 적이 실제 공격 AI를 새로 갖는 구조 대신,
///   "다른 적을 추적 + 주기 피해" 방식으로 매혹 공격 체감을 만든다.
/// - 이 방식은 기존 Enemy 이동 구조를 활용하면서 리스크를 줄인다.
/// 
/// 전제:
/// - Enemy에 다음 함수가 있어야 한다.
////  1. OverrideTarget(Transform newTarget)
///  2. ClearTargetOverride()
/// </summary>
public class CharmStatus : MonoBehaviour
{
    [Header("Charm Duration")]
    [Tooltip("매혹 지속 시간 종료 시각")]
    private float endTime;

    [Header("Target Search")]
    [Tooltip("다른 적을 찾는 반경")]
    [SerializeField] private float searchRadius = 30f;

    [Header("Charm Attack")]
    [Tooltip("매혹 상태에서 주변 적에게 주는 주기 피해")]
    [SerializeField] private float tickDamage = 2f;

    [Tooltip("주기 피해가 적용될 반경")]
    [SerializeField] private float tickRadius = 2.5f;

    [Tooltip("몇 초마다 한 번 피해를 줄지")]
    [SerializeField] private float tickInterval = 0.75f;

    [Tooltip("적 탐색용 레이어 마스크")]
    private LayerMask enemyMask;

    /// <summary>
    /// 현재 이 상태가 붙어 있는 적
    /// </summary>
    private Enemy enemy;

    /// <summary>
    /// 다음 주기 피해 시각
    /// </summary>
    private float nextTickTime;

    /// <summary>
    /// 매혹 상태 적용
    /// 
    /// 규칙:
    /// - 이미 매혹 상태가 붙어 있어도 시간을 갱신한다.
    /// - 피해량/간격은 현재 컴포넌트 값 사용
    /// </summary>
    public void Apply(float duration, LayerMask mask)
    {
        enemyMask = mask;
        endTime = Time.time + Mathf.Max(0.1f, duration);
        nextTickTime = Time.time + tickInterval;

        if (enemy == null)
            enemy = GetComponent<Enemy>();
    }

    private void Update()
    {
        if (enemy == null)
            return;

        // ------------------------------------------
        // 1) 지속시간 종료 시 원래 플레이어 추적으로 복귀
        // ------------------------------------------
        if (Time.time >= endTime)
        {
            enemy.ClearTargetOverride();
            Destroy(this);
            return;
        }

        // ------------------------------------------
        // 2) 현재 가장 가까운 다른 적을 찾아 타겟으로 설정
        // ------------------------------------------
        Transform nearest = FindNearestEnemy();
        if (nearest != null)
            enemy.OverrideTarget(nearest);

        // ------------------------------------------
        // 3) 일정 주기마다 주변 적에게 피해
        //    -> "매혹된 적이 다른 적을 공격하는 느낌"을 만들기 위한 처리
        // ------------------------------------------
        if (Time.time >= nextTickTime)
        {
            nextTickTime = Time.time + Mathf.Max(0.05f, tickInterval);
            ApplyCharmTickDamage();
        }
    }

    /// <summary>
    /// 현재 자신 주변의 다른 적들에게 주기 피해를 준다.
    /// 
    /// 주의:
    /// - 자기 자신은 제외
    /// - 무적 상태면 피해를 주지 않음
    /// - 기존 킬/드랍/해금 로직은 EnemyHealth/BossHealth가 그대로 처리
    /// </summary>
    private void ApplyCharmTickDamage()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, tickRadius, enemyMask);

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null)
                continue;

            Enemy otherEnemy = hits[i].GetComponentInParent<Enemy>();
            if (otherEnemy == null)
                continue;

            // 자기 자신은 제외
            if (otherEnemy.gameObject == gameObject)
                continue;

            IDamageable damageable = hits[i].GetComponent<IDamageable>();
            if (damageable == null)
                damageable = hits[i].GetComponentInParent<IDamageable>();

            if (damageable == null)
                continue;

            if (damageable.IsInvincible)
                continue;

            damageable.TakeDamage(tickDamage);
        }
    }

    /// <summary>
    /// 자신을 제외한 가장 가까운 다른 적을 찾는다.
    /// 
    /// 반환:
    /// - 찾으면 그 적의 Transform
    /// - 없으면 null
    /// </summary>
    private Transform FindNearestEnemy()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, searchRadius, enemyMask);

        float best = float.MaxValue;
        Transform bestTr = null;

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null)
                continue;

            Enemy otherEnemy = hits[i].GetComponentInParent<Enemy>();
            if (otherEnemy == null)
                continue;

            // 자기 자신 제외
            if (otherEnemy.gameObject == gameObject)
                continue;

            float sqrDist = (otherEnemy.transform.position - transform.position).sqrMagnitude;
            if (sqrDist < best)
            {
                best = sqrDist;
                bestTr = otherEnemy.transform;
            }
        }

        return bestTr;
    }

#if UNITY_EDITOR
    /// <summary>
    /// 에디터에서 반경 확인용
    /// - 파란색: 타겟 탐색 범위
    /// - 빨간색: 주기 피해 범위
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, searchRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, tickRadius);
    }
#endif
}