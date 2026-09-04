using UnityEngine;

/// <summary>
/// 무한맵용 재배치 컴포넌트.
/// 
/// Ground 모드:
/// - Area 트리거를 벗어나면 플레이어 이동 방향 기준으로 타일맵을 반복 이동.
/// 
/// Enemy 모드:
/// - 적이 카메라 화면 바깥으로 너무 멀어지면 플레이어 주변 바깥쪽으로 재배치.
/// 
/// 수정 핵심:
/// - BoxCollider2D에만 의존하지 않고 MapBoundary.WorldBounds / ClampPosition 사용.
/// - 프리팹 방식 맵에서도 MapBoundary를 자동 탐색.
/// - PlayerInputSource가 없어도 플레이어 위치 변화량으로 이동 방향을 추정.
/// </summary>
[DisallowMultipleComponent]
public class Reposition : MonoBehaviour
{
    private enum RepositionMode
    {
        Auto,
        Ground,
        Enemy
    }

    [Header("Mode")]
    [SerializeField] private RepositionMode mode = RepositionMode.Auto;

    [Header("Area Trigger")]
    [SerializeField] private string areaTag = "Area";
    [SerializeField] private bool acceptLegacyLowercaseAreaTag = true;

    [Header("Ground Reposition")]
    [SerializeField] private float groundRepositionDistance = 40f;

    [Header("Enemy Reposition")]
    [SerializeField] private float enemyRepositionMargin = 4f;
    [SerializeField] private float enemySpawnOutsideMargin = 2.5f;
    [SerializeField] private Vector2 enemyRandomOffset = new Vector2(3f, 3f);
    [SerializeField] private float enemyCheckInterval = 0.25f;
    [SerializeField] private float fallbackEnemyDistance = 12f;

    [Header("Boundary Clamp")]
    [SerializeField] private MapBoundary mapBoundary;
    [SerializeField] private Vector2 boundaryPadding = new Vector2(0.5f, 0.5f);

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Camera targetCamera;

    private Enemy enemy;
    private Collider2D ownCollider;

    private float nextEnemyCheckTime;

    private Vector3 lastPlayerPosition;
    private Vector2 lastPlayerMoveDirection = Vector2.right;

    private void Awake()
    {
        ownCollider = GetComponent<Collider2D>();
        enemy = GetComponent<Enemy>();

        if (targetCamera == null)
            targetCamera = Camera.main;

        TryBindReferences();
    }

    private void OnEnable()
    {
        TryBindReferences();

        if (player != null)
            lastPlayerPosition = player.position;
    }

    private void Update()
    {
        TryBindReferences();
        UpdatePlayerMoveDirection();

        if (IsEnemyMode())
            UpdateEnemyReposition();
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!IsAreaTrigger(collision))
            return;

        TryBindReferences();

        if (player == null)
            return;

        if (IsGroundMode())
        {
            RepositionGround();
            return;
        }

        if (IsEnemyMode())
        {
            RepositionEnemyNearScreen();
        }
    }

    private void TryBindReferences()
    {
        if (player == null && GameManager.Instance != null)
            player = GameManager.Instance.PlayerTransform;

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

            if (playerObj != null)
                player = playerObj.transform;
        }

        if (targetCamera == null)
            targetCamera = Camera.main;

        if (mapBoundary == null && GameManager.Instance != null)
            mapBoundary = GameManager.Instance.CurrentMapBoundary;

        if (mapBoundary == null)
            mapBoundary = FindObjectOfType<MapBoundary>(true);
    }

    private void UpdatePlayerMoveDirection()
    {
        if (player == null)
            return;

        Vector3 current = player.position;
        Vector3 delta = current - lastPlayerPosition;

        if (delta.sqrMagnitude > 0.0001f)
            lastPlayerMoveDirection = ((Vector2)delta).normalized;

        lastPlayerPosition = current;
    }

    private bool IsAreaTrigger(Collider2D collision)
    {
        if (collision == null)
            return false;

        if (!string.IsNullOrEmpty(areaTag) && collision.CompareTag(areaTag))
            return true;

        if (acceptLegacyLowercaseAreaTag && collision.gameObject.tag == "area")
            return true;

        return false;
    }

    private bool IsGroundMode()
    {
        if (mode == RepositionMode.Ground)
            return true;

        if (mode != RepositionMode.Auto)
            return false;

        return CompareTag("Ground");
    }

    private bool IsEnemyMode()
    {
        if (mode == RepositionMode.Enemy)
            return true;

        if (mode != RepositionMode.Auto)
            return false;

        return enemy != null;
    }

    private void RepositionGround()
    {
        if (player == null)
            return;

        Vector3 playerPos = player.position;
        Vector3 myPos = transform.position;

        float diffX = Mathf.Abs(playerPos.x - myPos.x);
        float diffY = Mathf.Abs(playerPos.y - myPos.y);

        Vector2 dir = GetMoveOrRelativeDirection(playerPos, myPos);

        if (Mathf.Abs(dir.x) < 0.01f && Mathf.Abs(dir.y) < 0.01f)
            dir = Vector2.right;

        if (diffX > diffY)
        {
            float sign = Mathf.Sign(dir.x);

            if (Mathf.Abs(sign) < 0.01f)
                sign = 1f;

            transform.Translate(Vector3.right * sign * groundRepositionDistance, Space.World);
        }
        else
        {
            float sign = Mathf.Sign(dir.y);

            if (Mathf.Abs(sign) < 0.01f)
                sign = 1f;

            transform.Translate(Vector3.up * sign * groundRepositionDistance, Space.World);
        }

        if (mapBoundary != null)
            mapBoundary.Recalculate();
    }

    private void UpdateEnemyReposition()
    {
        if (Time.time < nextEnemyCheckTime)
            return;

        nextEnemyCheckTime = Time.time + Mathf.Max(0.05f, enemyCheckInterval);

        if (player == null)
            return;

        if (ownCollider != null && !ownCollider.enabled)
            return;

        if (!IsEnemyTooFarFromScreen())
            return;

        RepositionEnemyNearScreen();
    }

    private bool IsEnemyTooFarFromScreen()
    {
        if (player == null)
            return false;

        Vector3 playerPos = player.position;
        Vector3 myPos = transform.position;

        if (targetCamera != null && targetCamera.orthographic)
        {
            float halfH = targetCamera.orthographicSize;
            float halfW = halfH * targetCamera.aspect;

            float limitX = halfW + enemyRepositionMargin;
            float limitY = halfH + enemyRepositionMargin;

            Vector3 diff = myPos - playerPos;

            return Mathf.Abs(diff.x) > limitX || Mathf.Abs(diff.y) > limitY;
        }

        float fallbackSqr = fallbackEnemyDistance * fallbackEnemyDistance;
        return (myPos - playerPos).sqrMagnitude > fallbackSqr;
    }

    private void RepositionEnemyNearScreen()
    {
        if (player == null)
            return;

        Vector3 playerPos = player.position;
        Vector3 enemyPos = transform.position;

        Vector3 relative = enemyPos - playerPos;
        relative.z = 0f;

        if (relative.sqrMagnitude < 0.0001f)
            relative = lastPlayerMoveDirection;

        Vector3 newPos;

        if (targetCamera != null && targetCamera.orthographic)
        {
            float halfH = targetCamera.orthographicSize;
            float halfW = halfH * targetCamera.aspect;

            bool horizontalDominant = Mathf.Abs(relative.x) > Mathf.Abs(relative.y);

            if (horizontalDominant)
            {
                float side = relative.x >= 0f ? 1f : -1f;

                newPos = playerPos + new Vector3(
                    side * (halfW + enemySpawnOutsideMargin),
                    Random.Range(-halfH, halfH),
                    0f
                );
            }
            else
            {
                float side = relative.y >= 0f ? 1f : -1f;

                newPos = playerPos + new Vector3(
                    Random.Range(-halfW, halfW),
                    side * (halfH + enemySpawnOutsideMargin),
                    0f
                );
            }
        }
        else
        {
            Vector3 dir = relative.normalized;

            if (dir.sqrMagnitude < 0.0001f)
                dir = Vector3.right;

            newPos = playerPos + dir * fallbackEnemyDistance;
        }

        newPos += new Vector3(
            Random.Range(-enemyRandomOffset.x, enemyRandomOffset.x),
            Random.Range(-enemyRandomOffset.y, enemyRandomOffset.y),
            0f
        );

        newPos = ClampToBoundary(newPos);
        newPos.z = 0f;

        transform.position = newPos;
    }

    private Vector2 GetMoveOrRelativeDirection(Vector3 playerPos, Vector3 myPos)
    {
        if (lastPlayerMoveDirection.sqrMagnitude > 0.0001f)
            return lastPlayerMoveDirection.normalized;

        Vector2 relative = myPos - playerPos;

        if (relative.sqrMagnitude < 0.0001f)
            return Vector2.right;

        return relative.normalized;
    }

    private Vector3 ClampToBoundary(Vector3 pos)
    {
        if (mapBoundary == null)
            return pos;

        return mapBoundary.ClampPosition(pos, boundaryPadding);
    }
}