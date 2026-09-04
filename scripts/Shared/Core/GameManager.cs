using Cinemachine;
using UnityEngine;

[DisallowMultipleComponent]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game State")]
    public bool isLive;
    public bool isOverTime;
    public bool isPaused;

    [Header("Player Settings")]
    [SerializeField] private GameObject playerPrefab;

    private GameObject playerInstance;
    private Transform playerTransform;
    public Transform PlayerTransform => playerTransform;

    [Header("Map Settings")]
    [SerializeField] private MapBoundary currentMapBoundary;
    public MapBoundary CurrentMapBoundary => currentMapBoundary;

    private string currentMapId;
    public string CurrentMapId => currentMapId;

    [Header("PoolManager Settings")]
    public PoolManager poolManager;
    public BossPoolManager bossPoolManager;

    [Header("Time")]
    [Tooltip("현재 게임 진행 시간")]
    public float gameTime;

    [Tooltip("일반 구간 시간. 30분이면 1800")]
    public float maxGameTime = 1800f;

    [Header("Level / Exp")]
    public int level = 1;
    public int exp = 0;

    [Tooltip("초반 레벨업 요구 경험치. 배열을 넘어가면 공식으로 계속 증가")]
    public int[] nextExp =
    {
        10, 20, 30, 45, 65,
        90, 120, 155, 195, 240,
        290, 345, 405, 470, 540,
        615, 695, 780, 870, 965,
        1065, 1170, 1280, 1395, 1515,
        1640, 1770, 1905, 2045, 2190
    };

    [Header("Infinite Level Rule")]
    [SerializeField] private bool useInfiniteLevelFormula = true;
    [SerializeField] private int infiniteLevelBaseExp = 2300;
    [SerializeField] private int infiniteLevelAddPerLevel = 160;

    [Header("Health")]
    public float health = 100f;
    public float maxHealth = 100f;

    [Header("Stats")]
    public int killCount = 0;
    public int bossDefeatedCount = 0;

    public System.Action OnEnterOverTime;
    public System.Action<int> OnKillChanged;
    public System.Action<int> OnLevelUp;
    public System.Action<string> OnBossDefeated;

    private const string KEY_TOTAL_KILL = "TotalKill";
    private const string KEY_TOTAL_BOSS = "TotalBossDefeated";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        TryAutoBindPools();
        UpdateCameraConfiner();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void TryAutoBindPools()
    {
        if (poolManager == null)
            poolManager = FindObjectOfType<PoolManager>(true);

        if (bossPoolManager == null)
            bossPoolManager = FindObjectOfType<BossPoolManager>(true);
    }

    public void SetPlayerPrefab(GameObject prefab)
    {
        playerPrefab = prefab;
    }

    public void RegisterPlayer(GameObject player)
    {
        playerInstance = player;
        playerTransform = player != null ? player.transform : null;
    }

    public bool IsRegisteredPlayer(GameObject player)
    {
        return player != null && playerInstance == player;
    }

    public void UnregisterPlayer(GameObject player)
    {
        if (playerInstance != player)
            return;

        playerInstance = null;
        playerTransform = null;
    }

    public void DespawnPlayer()
    {
        if (playerInstance != null)
            Destroy(playerInstance);

        playerInstance = null;
        playerTransform = null;
    }

    public Transform SpawnPlayerAt(Vector3 position)
    {
        if (playerPrefab == null)
        {
            Debug.LogError("GameManager: playerPrefab이 null입니다.");
            return null;
        }

        DespawnPlayer();

        position.z = 0f;

        GameObject instance = Instantiate(playerPrefab, position, Quaternion.identity);

        if (!instance.CompareTag("Player"))
            instance.tag = "Player";

        RegisterPlayer(instance);

        return playerTransform;
    }

    public void SetCurrentMapId(string mapId)
    {
        currentMapId = mapId;
    }

    public void StartGame(Vector3? spawnPosition = null)
    {
        isLive = true;
        isPaused = false;
        isOverTime = false;

        gameTime = 0f;

        level = 1;
        exp = 0;

        health = maxHealth;

        killCount = 0;
        bossDefeatedCount = 0;

        Time.timeScale = 1f;

        TryAutoBindPools();

        if (UnlockManager.Instance != null)
            UnlockManager.Instance.ResetRunState();

        if (playerTransform == null)
        {
            Vector3 position = spawnPosition ?? Vector3.zero;
            SpawnPlayerAt(position);
        }

        UpdateCameraConfiner();
    }

    public void StopGame()
    {
        isLive = false;
        isPaused = false;
        Time.timeScale = 1f;
    }

    public void PauseGame()
    {
        if (!isLive || isPaused)
            return;

        isPaused = true;
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        if (!isLive || !isPaused)
            return;

        isPaused = false;
        Time.timeScale = 1f;
    }

    public void GameVictory()
    {
        StopGame();
    }

    public void HandlePlayerDeath()
    {
        if (!isLive)
            return;

        GameFlow flow = FindObjectOfType<GameFlow>(true);

        if (flow != null)
        {
            flow.Lose();
            return;
        }

        StopGame();
    }

    public void SetMapBoundary(MapBoundary boundary)
    {
        currentMapBoundary = boundary;
        UpdateCameraConfiner();
    }

    public void UpdateCameraConfiner()
    {
        if (currentMapBoundary == null)
            return;

        currentMapBoundary.Recalculate();

        Collider2D col = currentMapBoundary.BoundaryCollider2D;

        if (col == null)
            col = currentMapBoundary.GetComponent<Collider2D>();

        if (col == null)
            col = currentMapBoundary.GetComponentInChildren<Collider2D>(true);

        if (col == null)
        {
            Debug.LogWarning("GameManager: CurrentMapBoundary에 Collider2D가 없습니다.", currentMapBoundary);
            return;
        }

        CinemachineConfiner2D confiner = FindObjectOfType<CinemachineConfiner2D>(true);

        if (confiner != null)
            confiner.m_BoundingShape2D = col;

        CameraHardClamp2D hardClamp = FindObjectOfType<CameraHardClamp2D>(true);

        if (hardClamp != null)
        {
            BoxCollider2D box = col as BoxCollider2D;

            if (box == null)
                box = currentMapBoundary.BoundaryCollider;

            if (box == null)
                box = currentMapBoundary.GetComponent<BoxCollider2D>();

            if (box == null)
                box = currentMapBoundary.GetComponentInChildren<BoxCollider2D>(true);

            hardClamp.SetBoundary(box);
        }
    }

    public void AddKill(int amount = 1)
    {
        if (!isLive)
            return;

        int add = Mathf.Max(0, amount);

        if (add == 0)
            return;

        killCount += add;

        int total = PlayerPrefs.GetInt(KEY_TOTAL_KILL, 0);
        PlayerPrefs.SetInt(KEY_TOTAL_KILL, total + add);
        PlayerPrefs.Save();

        OnKillChanged?.Invoke(killCount);
    }

    public void AddExp(int amount)
    {
        if (!isLive)
            return;

        int add = Mathf.Max(0, amount);

        if (add <= 0)
            return;

        exp += add;

        while (CanLevelUp())
        {
            int required = GetRequiredExpForNextLevel();

            exp -= required;
            level++;

            OnLevelUp?.Invoke(level);

            if (UnlockManager.Instance != null)
                UnlockManager.Instance.RegisterLevel(level);
        }
    }

    private bool CanLevelUp()
    {
        int required = GetRequiredExpForNextLevel();

        if (required <= 0)
            return false;

        return exp >= required;
    }

    public int GetRequiredExpForNextLevel()
    {
        if (level <= 0)
            level = 1;

        if (nextExp != null && nextExp.Length > 0)
        {
            int arrayIndex = level - 1;

            if (arrayIndex >= 0 && arrayIndex < nextExp.Length)
                return Mathf.Max(1, nextExp[arrayIndex]);
        }

        if (!useInfiniteLevelFormula)
        {
            if (nextExp == null || nextExp.Length == 0)
                return 1;

            return Mathf.Max(1, nextExp[nextExp.Length - 1]);
        }

        int overLevel = Mathf.Max(0, level - (nextExp != null ? nextExp.Length : 0));
        int required = infiniteLevelBaseExp + overLevel * infiniteLevelAddPerLevel;

        return Mathf.Max(1, required);
    }

    public float GetExpNormalized()
    {
        int required = GetRequiredExpForNextLevel();

        if (required <= 0)
            return 0f;

        return Mathf.Clamp01(exp / (float)required);
    }

    public void NotifyBossDefeated(string bossId)
    {
        bossDefeatedCount++;

        int total = PlayerPrefs.GetInt(KEY_TOTAL_BOSS, 0);
        PlayerPrefs.SetInt(KEY_TOTAL_BOSS, total + 1);
        PlayerPrefs.Save();

        OnBossDefeated?.Invoke(bossId);
    }

    private void Update()
    {
        if (!isLive || isPaused)
            return;

        gameTime += Time.deltaTime;

        if (UnlockManager.Instance != null)
            UnlockManager.Instance.TickSurvive(Time.deltaTime);

        if (!isOverTime && gameTime >= maxGameTime)
        {
            gameTime = maxGameTime;
            isOverTime = true;

            OnEnterOverTime?.Invoke();

            if (UnlockManager.Instance != null)
                UnlockManager.Instance.RegisterOverTimeEntered();
        }
    }
}