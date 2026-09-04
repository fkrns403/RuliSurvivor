using UnityEngine;

/// <summary>
/// 적 정리 전용 매니저.
/// 
/// 역할:
/// - 오버타임 연출 전 일반 적 제거
/// - 보스 등장 전 주변 적 제거
/// - 승리 / 패배 / 컷신 진입 시 남은 일반 적 제거
/// 
/// 호환 메서드:
/// - ClearEnemies()
/// - ClearAllEnemies()
/// - ClearAll()
/// 
/// 주의:
/// - EnemyCleaner2D를 EnemyCleaner로 이름 변경했다면
///   프로젝트 안에 EnemyCleaner 클래스는 반드시 1개만 있어야 한다.
/// - 파일 이름은 EnemyCleaner.cs
/// - 클래스 이름은 public class EnemyCleaner
/// </summary>
[DisallowMultipleComponent]
public class EnemyCleaner : MonoBehaviour
{
    [Header("Enemy Layer")]
    [SerializeField] private LayerMask enemyLayer;

    [Header("Clear Radius")]
    [SerializeField] private float clearRadius = 100f;

    [Header("Center Target")]
    [SerializeField] private Transform centerTarget;

    [Header("Options")]
    [SerializeField] private bool ignoreBoss = true;
    [SerializeField] private bool ignoreInactive = true;

    [Header("Debug")]
    [SerializeField] private bool verboseLog = true;

    /// <summary>
    /// 기존 코드 호환용.
    /// OvertimeLetterSequenceUI에서 enemyCleaner.ClearEnemies()를 호출해도 동작한다.
    /// </summary>
    public void ClearEnemies()
    {
        ClearAllEnemies();
    }

    /// <summary>
    /// GameFlow에서 enemyCleaner.ClearAll()을 호출해도 동작한다.
    /// </summary>
    public void ClearAll()
    {
        ClearAllEnemies();
    }

    /// <summary>
    /// 범위 안의 일반 적을 정리한다.
    /// </summary>
    public void ClearAllEnemies()
    {
        Vector2 center = GetCenterPosition();

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            center,
            clearRadius,
            enemyLayer
        );

        int clearedCount = 0;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D col = hits[i];

            if (col == null)
                continue;

            if (ignoreInactive && !col.gameObject.activeInHierarchy)
                continue;

            Enemy enemy = col.GetComponent<Enemy>();

            if (enemy == null)
                enemy = col.GetComponentInParent<Enemy>();

            if (enemy == null)
                continue;

            GameObject enemyObj = enemy.gameObject;

            if (ignoreBoss && enemyObj.GetComponent<BossHealth>() != null)
                continue;

            ReleaseEnemy(enemyObj);
            clearedCount++;
        }

        if (verboseLog)
            Debug.Log($"EnemyCleaner: 적 제거 완료 count={clearedCount}", this);
    }

    private void ReleaseEnemy(GameObject enemyObj)
    {
        if (enemyObj == null)
            return;

        Rigidbody2D rb = enemyObj.GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        PoolManager pool = null;

        if (GameManager.Instance != null)
            pool = GameManager.Instance.poolManager;

        if (pool != null)
            pool.Release(enemyObj);
        else
            enemyObj.SetActive(false);
    }

    private Vector2 GetCenterPosition()
    {
        if (centerTarget != null)
            return centerTarget.position;

        if (GameManager.Instance != null &&
            GameManager.Instance.PlayerTransform != null)
        {
            return GameManager.Instance.PlayerTransform.position;
        }

        return Vector2.zero;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(GetCenterPosition(), clearRadius);
    }
}