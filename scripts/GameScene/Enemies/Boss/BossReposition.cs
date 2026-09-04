using UnityEngine;

/// <summary>
/// 무한맵에서 보스가 플레이어와 너무 멀어졌을 때,
/// 플레이어 주변 전투 가능 거리로 다시 배치하는 보스 전용 Reposition.
/// 
/// 역할:
/// - 보스가 플레이어와 너무 멀어져 전투가 끊기는 상황을 방지한다.
/// - 일반 몬스터 Reposition과 분리해서 보스 전용 거리 보정만 담당한다.
/// 
/// 수정 핵심:
/// - 재배치 시 Rigidbody2D velocity와 angularVelocity를 모두 초기화한다.
/// - 플레이어가 늦게 생성되어도 GameManager.PlayerTransform 기준으로 계속 확인한다.
/// </summary>
[DisallowMultipleComponent]
public class BossReposition : MonoBehaviour
{
    [Header("Distance Rule")]
    [SerializeField] private float maxDistanceFromPlayer = 35f;
    [SerializeField] private float repositionDistanceFromPlayer = 18f;

    [Header("Random Offset")]
    [SerializeField] private Vector2 randomOffset = new Vector2(3f, 3f);

    [Header("Timing")]
    [SerializeField] private float checkInterval = 0.5f;
    [SerializeField] private float repositionCooldown = 2f;

    [Header("Physics")]
    [SerializeField] private bool clearVelocityOnReposition = true;

    private float nextCheckTime;
    private float nextAllowedRepositionTime;

    private Rigidbody2D rb;
    private Collider2D col;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
    }

    private void Update()
    {
        GameManager gm = GameManager.Instance;

        if (gm == null)
            return;

        if (!gm.isLive || gm.isPaused)
            return;

        if (Time.time < nextCheckTime)
            return;

        nextCheckTime = Time.time + Mathf.Max(0.05f, checkInterval);

        TryRepositionIfTooFar();
    }

    private void TryRepositionIfTooFar()
    {
        if (Time.time < nextAllowedRepositionTime)
            return;

        Transform player = GetPlayerTransform();

        if (player == null)
            return;

        if (col != null && !col.enabled)
            return;

        Vector3 playerPos = player.position;
        Vector3 bossPos = transform.position;

        Vector3 toBoss = bossPos - playerPos;
        toBoss.z = 0f;

        float sqrDistance = toBoss.sqrMagnitude;
        float maxSqrDistance = maxDistanceFromPlayer * maxDistanceFromPlayer;

        if (sqrDistance <= maxSqrDistance)
            return;

        Vector3 dir = GetRepositionDirection(player, toBoss);
        Vector3 newPos = CalculateRepositionPosition(playerPos, dir);

        ApplyPosition(newPos);

        nextAllowedRepositionTime = Time.time + Mathf.Max(0f, repositionCooldown);
    }

    private Vector3 GetRepositionDirection(Transform player, Vector3 toBoss)
    {
        if (toBoss.sqrMagnitude > 0.0001f)
            return toBoss.normalized;

        PlayerInputSource input = player.GetComponent<PlayerInputSource>();

        if (input != null && input.MoveInput.sqrMagnitude > 0.0001f)
        {
            Vector2 move = input.MoveInput.normalized;
            return new Vector3(-move.x, -move.y, 0f);
        }

        return Vector3.right;
    }

    private Vector3 CalculateRepositionPosition(Vector3 playerPos, Vector3 dir)
    {
        float rx = Random.Range(-randomOffset.x, randomOffset.x);
        float ry = Random.Range(-randomOffset.y, randomOffset.y);

        Vector3 offset = new Vector3(rx, ry, 0f);
        Vector3 result = playerPos + dir * repositionDistanceFromPlayer + offset;

        result.z = transform.position.z;

        return result;
    }

    private void ApplyPosition(Vector3 newPos)
    {
        if (rb != null)
        {
            if (clearVelocityOnReposition)
            {
                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }

            rb.position = newPos;
            return;
        }

        transform.position = newPos;
    }

    private Transform GetPlayerTransform()
    {
        if (GameManager.Instance == null)
            return null;

        return GameManager.Instance.PlayerTransform;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, maxDistanceFromPlayer);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, repositionDistanceFromPlayer);
    }
#endif
}