using UnityEngine;

/// <summary>
/// 플레이어 주변에 적을 스폰하는 스포너.
/// 
/// 현재 프로젝트 구조 기준:
/// - SpawnDataSet에는 GetData() 함수가 없다.
/// - SpawnDataSet.spawnList 배열을 직접 읽어서 현재 시간에 맞는 SpawnData를 선택한다.
/// - SpawnData.spriteType은 현재 PoolManager의 prefab index로 사용한다.
/// 
/// 기존 문제 수정:
/// 1. SpawnDataSet.GetData() 호출 제거
/// 2. 기존 GetCurrentSpawnData(gameTime) 방식 복구
/// 3. 스폰 위치를 플레이어 주변 원형 랜덤 위치로 계산
/// 4. MapBoundary 밖으로 스폰되지 않게 Clamp 처리
/// 5. 플레이어 주변 스폰 포인트 자식이 있으면 기존 방식도 사용 가능
/// </summary>
[DisallowMultipleComponent]
public class Spawner : MonoBehaviour
{
    [Header("Spawn Interval")]
    [Tooltip("기본 스폰 간격. 값이 작을수록 적이 자주 나온다.")]
    [SerializeField] private float spawnInterval = 0.5f;

    [Tooltip("오버타임 상태에서 사용할 스폰 간격. 0 이하이면 spawnInterval을 그대로 사용한다.")]
    [SerializeField] private float overtimeSpawnInterval = 0.2f;

    [Header("Data")]
    [Tooltip("맵별 스폰 데이터. GameBootstrap이 MapEntry.spawnDataSet을 주입한다.")]
    [SerializeField] private SpawnDataSet spawnDataSet;

    [Header("Spawn Position Mode")]
    [Tooltip("true이면 플레이어 주변 원형 랜덤 위치에 스폰한다. false이면 spawnPoints 중 하나를 사용한다.")]
    [SerializeField] private bool spawnAroundPlayer = true;

    [Tooltip("플레이어 주변 스폰 최소 거리")]
    [SerializeField] private float minSpawnDistance = 7f;

    [Tooltip("플레이어 주변 스폰 최대 거리")]
    [SerializeField] private float maxSpawnDistance = 10f;

    [Header("Spawn Points")]
    [Tooltip("스폰 포인트를 자식 Transform에서 자동 수집할지 여부")]
    [SerializeField] private bool autoCollectSpawnPointsFromChildren = true;

    [Tooltip("spawnAroundPlayer가 false일 때 사용할 스폰 포인트 배열")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Boundary Clamp")]
    [Tooltip("맵 경계에서 이 값만큼 안쪽으로 보정한다.")]
    [SerializeField] private float boundaryPadding = 1.5f;

    [Header("Fallback")]
    [Tooltip("SpawnDataSet이 없을 때 사용할 기본 풀 인덱스")]
    [SerializeField] private int fallbackPoolIndex = 0;

    [Tooltip("SpawnDataSet이 없을 때 사용할 기본 체력")]
    [SerializeField] private int fallbackHealth = 10;

    [Tooltip("SpawnDataSet이 없을 때 사용할 기본 이동속도")]
    [SerializeField] private float fallbackSpeed = 2f;

    [Header("Debug")]
    [SerializeField] private bool verboseLog = true;

    private float timer;

    private void Awake()
    {
        if (autoCollectSpawnPointsFromChildren)
            CollectSpawnPointsFromChildren();
    }

    private void OnEnable()
    {
        timer = 0f;

        if (autoCollectSpawnPointsFromChildren)
            CollectSpawnPointsFromChildren();
    }

    /// <summary>
    /// GameBootstrap이 현재 선택된 맵의 SpawnDataSet을 주입할 때 사용한다.
    /// </summary>
    public void SetSpawnData(SpawnDataSet data)
    {
        spawnDataSet = data;

        if (verboseLog)
        {
            string msg = data == null
                ? "null"
                : $"spawnList length = {(data.spawnList != null ? data.spawnList.Length : 0)}";

            Debug.Log($"Spawner: SpawnDataSet 주입됨 / {msg}", this);
        }
    }

    private void CollectSpawnPointsFromChildren()
    {
        int childCount = transform.childCount;

        if (childCount <= 0)
        {
            spawnPoints = System.Array.Empty<Transform>();
            return;
        }

        spawnPoints = new Transform[childCount];

        for (int i = 0; i < childCount; i++)
            spawnPoints[i] = transform.GetChild(i);
    }

    private void Update()
    {
        GameManager gm = GameManager.Instance;

        if (gm == null)
            return;

        if (!gm.isLive || gm.isPaused)
            return;

        if (gm.poolManager == null)
            return;

        float interval = GetCurrentSpawnInterval(gm);
        timer += Time.deltaTime;

        if (timer < interval)
            return;

        timer = 0f;
        SpawnCurrentEnemy(gm);
    }

    private float GetCurrentSpawnInterval(GameManager gm)
    {
        if (gm != null && gm.isOverTime && overtimeSpawnInterval > 0f)
            return Mathf.Max(0.01f, overtimeSpawnInterval);

        return Mathf.Max(0.01f, spawnInterval);
    }

    private void SpawnCurrentEnemy(GameManager gm)
    {
        SpawnData data = GetCurrentSpawnData(gm.gameTime);

        int poolIndex;
        int hp;
        float speed;

        if (data != null)
        {
            poolIndex = data.spriteType;
            hp = data.health;
            speed = data.speed;
        }
        else
        {
            poolIndex = fallbackPoolIndex;
            hp = fallbackHealth;
            speed = fallbackSpeed;
        }

        poolIndex = Mathf.Clamp(poolIndex, 0, gm.poolManager.MaxIndex);

        GameObject enemyObj = gm.poolManager.Get(poolIndex);

        if (enemyObj == null)
        {
            LogWarning($"PoolManager에서 적을 가져오지 못했습니다. poolIndex={poolIndex}");
            return;
        }

        Vector3 pos = GetSpawnPosition(gm);

        enemyObj.transform.position = pos;
        enemyObj.transform.rotation = Quaternion.identity;
        enemyObj.SetActive(true);

        Enemy enemy = enemyObj.GetComponent<Enemy>();

        if (enemy == null)
        {
            Debug.LogError($"Spawner: 스폰된 오브젝트에 Enemy 컴포넌트가 없습니다. poolIndex={poolIndex}", enemyObj);
            enemyObj.SetActive(false);
            return;
        }

        enemy.Setup(hp, speed);
    }

    /// <summary>
    /// 현재 gameTime에 가장 알맞은 SpawnData를 찾는다.
    /// 
    /// 선택 규칙:
    /// - spawnTime <= gameTime인 데이터 중에서
    ///   spawnTime이 가장 큰 데이터를 선택한다.
    /// </summary>
    private SpawnData GetCurrentSpawnData(float gameTime)
    {
        if (spawnDataSet == null)
            return null;

        if (spawnDataSet.spawnList == null || spawnDataSet.spawnList.Length == 0)
            return null;

        SpawnData selected = null;
        float selectedTime = float.MinValue;

        for (int i = 0; i < spawnDataSet.spawnList.Length; i++)
        {
            SpawnData data = spawnDataSet.spawnList[i];

            if (data == null)
                continue;

            if (data.spawnTime > gameTime)
                continue;

            if (data.spawnTime >= selectedTime)
            {
                selected = data;
                selectedTime = data.spawnTime;
            }
        }

        return selected;
    }

    private Vector3 GetSpawnPosition(GameManager gm)
    {
        if (spawnAroundPlayer)
            return GetAroundPlayerSpawnPosition(gm);

        Transform point = GetRandomSpawnPoint();

        if (point != null)
        {
            Vector3 p = point.position;
            p.z = 0f;
            return ClampToBoundary(p, gm);
        }

        return GetAroundPlayerSpawnPosition(gm);
    }

    private Vector3 GetAroundPlayerSpawnPosition(GameManager gm)
    {
        Vector3 center = transform.position;

        if (gm != null && gm.PlayerTransform != null)
            center = gm.PlayerTransform.position;

        Vector2 dir = Random.insideUnitCircle;

        if (dir.sqrMagnitude < 0.001f)
            dir = Vector2.right;

        dir.Normalize();

        float minDist = Mathf.Max(0.1f, minSpawnDistance);
        float maxDist = Mathf.Max(minDist, maxSpawnDistance);

        float distance = Random.Range(minDist, maxDist);

        Vector3 pos = center + (Vector3)(dir * distance);
        pos.z = 0f;

        return ClampToBoundary(pos, gm);
    }

    private Vector3 ClampToBoundary(Vector3 pos, GameManager gm)
    {
        MapBoundary boundary = null;

        if (gm != null)
            boundary = gm.CurrentMapBoundary;

        if (boundary == null)
            return pos;

        boundary.RefreshBounds();

        float minX = boundary.MinX + boundaryPadding;
        float maxX = boundary.MaxX - boundaryPadding;
        float minY = boundary.MinY + boundaryPadding;
        float maxY = boundary.MaxY - boundaryPadding;

        if (minX > maxX)
        {
            float centerX = (boundary.MinX + boundary.MaxX) * 0.5f;
            minX = centerX;
            maxX = centerX;
        }

        if (minY > maxY)
        {
            float centerY = (boundary.MinY + boundary.MaxY) * 0.5f;
            minY = centerY;
            maxY = centerY;
        }

        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);
        pos.z = 0f;

        return pos;
    }

    private Transform GetRandomSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
            return null;

        int index = Random.Range(0, spawnPoints.Length);
        return spawnPoints[index];
    }

    private void LogWarning(string message)
    {
        if (!verboseLog)
            return;

        Debug.LogWarning($"Spawner: {message}", this);
    }
}