using UnityEngine;

/// <summary>
/// 독 하트 한 개의 동작 담당
/// 
/// 상태:
/// - Idle    : 플레이어 주변을 회전
/// - Flying  : 타겟에게 날아감
/// - Attached: 타겟 옆에 붙어서 독 틱 데미지
/// 
/// 독 데미지 규칙:
/// - tickInterval마다 대상 주변 aoeRadius 범위 내 적에게 데미지
/// - 대상 본체도 포함
/// </summary>
public class PoisonOrbNode : MonoBehaviour
{
    private enum NodeState
    {
        Idle,
        Flying,
        Attached
    }

    [Header("Orbit")]
    [SerializeField] private float orbitRadius = 1.25f;
    [SerializeField] private float orbitSpeedDeg = 180f;

    [Header("Attach")]
    [SerializeField] private float attachDistance = 0.35f;
    [SerializeField] private float keepDistanceFromOwner = 12f;

    private Transform owner;
    private int orbitIndex;
    private int orbitCount = 1;
    private float orbitAngle;

    private Transform currentTarget;
    private NodeState state = NodeState.Idle;

    private LayerMask enemyMask;
    private float searchRange = 6f;
    private float flySpeed = 14f;
    private float tickDamage = 5f;
    private float tickInterval = 0.3f;
    private float aoeRadius = 1.5f;

    private float tickTimer;
    private Vector3 attachOffset;

    private readonly Collider2D[] aoeBuffer = new Collider2D[32];

    public bool IsIdle => state == NodeState.Idle;
    public Transform CurrentTarget => currentTarget;

    /// <summary>
    /// 회전 궤도 정보 설정
    /// </summary>
    public void Setup(Transform ownerTransform, int index, int count)
    {
        owner = ownerTransform;
        orbitIndex = index;
        orbitCount = Mathf.Max(1, count);
    }

    /// <summary>
    /// 전투 수치 설정
    /// </summary>
    public void SetCombatStats(
        LayerMask targetMask,
        float searchRangeValue,
        float flySpeedValue,
        float tickDamageValue,
        float tickIntervalValue,
        float aoeRadiusValue
    )
    {
        enemyMask = targetMask;
        searchRange = Mathf.Max(0.1f, searchRangeValue);
        flySpeed = Mathf.Max(0.1f, flySpeedValue);
        tickDamage = Mathf.Max(0f, tickDamageValue);
        tickInterval = Mathf.Max(0.05f, tickIntervalValue);
        aoeRadius = Mathf.Max(0.1f, aoeRadiusValue);
    }

    /// <summary>
    /// 지정한 적에게 날아가도록 전환
    /// </summary>
    public void LaunchTo(Transform target)
    {
        if (target == null)
            return;

        currentTarget = target;
        state = NodeState.Flying;
        tickTimer = 0f;
    }

    private void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm != null && gm.isPaused)
            return;

        if (owner == null)
            return;

        switch (state)
        {
            case NodeState.Idle:
                UpdateIdleOrbit();
                break;

            case NodeState.Flying:
                UpdateFlying();
                break;

            case NodeState.Attached:
                UpdateAttached();
                break;
        }
    }

    private void UpdateIdleOrbit()
    {
        orbitAngle += orbitSpeedDeg * Time.deltaTime;

        float step = 360f / Mathf.Max(1, orbitCount);
        float angle = orbitAngle + step * orbitIndex;
        float rad = angle * Mathf.Deg2Rad;

        Vector3 offset = new Vector3(
            Mathf.Cos(rad) * orbitRadius,
            Mathf.Sin(rad) * orbitRadius,
            0f
        );

        transform.position = owner.position + offset;
    }

    private void UpdateFlying()
    {
        if (!IsTargetValid(currentTarget))
        {
            ReturnToIdle();
            return;
        }

        transform.position = Vector3.MoveTowards(
            transform.position,
            currentTarget.position,
            flySpeed * Time.deltaTime
        );

        float dist = Vector3.Distance(transform.position, currentTarget.position);
        if (dist <= attachDistance)
        {
            Vector3 dir = (currentTarget.position - owner.position);
            dir.z = 0f;

            if (dir.sqrMagnitude < 0.0001f)
                dir = Vector3.right;

            dir.Normalize();

            attachOffset = dir * 0.35f;
            state = NodeState.Attached;
            tickTimer = 0f;
        }
    }

    private void UpdateAttached()
    {
        if (!IsTargetValid(currentTarget))
        {
            ReturnToIdle();
            return;
        }

        if (Vector3.Distance(owner.position, currentTarget.position) > keepDistanceFromOwner)
        {
            ReturnToIdle();
            return;
        }

        transform.position = currentTarget.position + attachOffset;

        tickTimer += Time.deltaTime;
        if (tickTimer < tickInterval)
            return;

        tickTimer = 0f;
        DoPoisonTick();
    }

    private void DoPoisonTick()
    {
        int hitCount = Physics2D.OverlapCircleNonAlloc(
            currentTarget.position,
            aoeRadius,
            aoeBuffer,
            enemyMask
        );

        if (hitCount <= 0)
            return;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D col = aoeBuffer[i];
            if (col == null)
                continue;

            IDamageable dmg = col.GetComponent<IDamageable>();
            if (dmg == null)
                dmg = col.GetComponentInParent<IDamageable>();

            if (dmg == null)
                continue;

            if (dmg.IsInvincible)
                continue;

            dmg.TakeDamage(tickDamage);
        }
    }

    private bool IsTargetValid(Transform target)
    {
        if (target == null)
            return false;

        if (!target.gameObject.activeInHierarchy)
            return false;

        if (Vector3.Distance(owner.position, target.position) > searchRange + 4f)
            return false;

        return true;
    }

    private void ReturnToIdle()
    {
        currentTarget = null;
        state = NodeState.Idle;
        tickTimer = 0f;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, aoeRadius);
    }
}