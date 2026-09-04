using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ProgressiveBossSpawner : MonoBehaviour
{
    public static ProgressiveBossSpawner Active { get; private set; }

    private const string KEY_MAP = "SelectedMapIndex";

    [Header("Tutorial Block")]
    [SerializeField] private bool disableOnTutorialMap = true;
    [SerializeField] private int tutorialMapIndex = 0;
    [SerializeField] private string tutorialMapId = "tutorial";

    [Header("OverTime Spawn Rule")]
    [SerializeField] private bool spawnOnOverTime = true;
    [SerializeField] private bool ignoreSpawnTimeWhenSpawnOnOverTime = true;

    [Header("Pre Boss Sequence")]
    [SerializeField] private bool playPreBossSequence = true;
    [SerializeField] private bool playPreBossSequenceEveryTime = true;
    [SerializeField] private string preBossSequenceSaveKey = "normal_pre_boss_letter";
    [SerializeField] private OvertimeLetterSequenceUI preBossSequenceUI;

    [TextArea]
    [SerializeField]
    private string[] preBossSequenceLines =
    {
        "여기까지 와줘서 고마워요.",
        "이제부터 진짜 보스전이 시작됩니다.",
        "준비되었다면,",
        "함께 끝을 향해\n나아가봅시다."
    };

    [Header("Challenge Rule")]
    [SerializeField] private int challengeMapIndex = 2;
    [SerializeField] private string challengeMapId = "challenge";
    [SerializeField] private bool loopBossesOnChallengeMap = true;
    [SerializeField] private float challengeNextWaveDelay = 3f;
    [SerializeField] private float challengeExtraBossSpawnInterval = 0.35f;
    [SerializeField] private int challengeMaxExtraBossCount = 3;
    [SerializeField] private int challengeExtraTierStep = 2;

    [Header("Normal Map Boss Progress Rule")]
    [SerializeField] private bool normalAlwaysRunFullBossChain = true;
    [SerializeField] private bool normalUnlockFullChainAfterStage1Clear = false;
    [SerializeField] private bool normalClearAfterStage1WhenSinglePhase = false;

    [Header("Progress / Clear Sequence Key")]
    [SerializeField] private string firstStageBossId = "boss_challenge_stage1";
    [SerializeField] private string normalStage1ClearSequenceId = "normal_map_stage1";
    [SerializeField] private string normalStage3ClearSequenceId = "normal_map_stage3";

    [Header("Spawn Timing")]
    [SerializeField] private float spawnTime = 1800f;

    [Tooltip("페이즈 사이 대기 시간. 2분이면 120")]
    [SerializeField] private float nextStageDelay = 120f;

    [Header("Boss Pool Indices")]
    [SerializeField] private int[] stageBossIndices = { 0, 1, 2 };

    [Header("References")]
    [SerializeField] private BossPoolManager bossPoolManager;
    [SerializeField] private BossDirector bossDirector;
    [SerializeField] private GameFlow gameFlow;
    [SerializeField] private Transform spawnPoint;

    [Header("Spawn Position Correction")]
    [SerializeField] private bool spawnNearPlayer = true;
    [SerializeField] private Vector2 nearPlayerOffset = new Vector2(4f, 2f);
    [SerializeField] private float cameraPadding = 1.5f;
    [SerializeField] private float mapPadding = 1f;

    [Header("Difficulty")]
    [SerializeField] private BossDifficulty normalDifficulty = BossDifficulty.Normal;
    [SerializeField] private bool useOverTimeDifficulty = true;

    [Header("Debug")]
    [SerializeField] private bool verboseLog = true;

    private bool started;
    private bool startRequested;
    private bool completed;
    private bool chainMode;
    private bool waitingNextStage;
    private bool subscribedOverTime;

    private int currentStageIndex = -1;
    private GameObject currentBossObject;
    private BossHealth currentBossHealth;

    private bool challengeMode;
    private int challengeWaveIndex = 1;
    private readonly List<BossHealth> challengeExtraBosses = new List<BossHealth>();

    private const string KEY_PRE_BOSS_SEQUENCE_PREFIX = "PRE_BOSS_SEQUENCE_SEEN_";

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        Active = this;

        started = false;
        startRequested = false;
        completed = false;
        chainMode = false;
        waitingNextStage = false;

        currentStageIndex = -1;
        currentBossObject = null;
        currentBossHealth = null;

        challengeMode = false;
        challengeWaveIndex = 1;
        challengeExtraBosses.Clear();

        ResolveReferences();
        SubscribeOverTimeEvent();
    }

    private void Start()
    {
        ResolveReferences();
        SubscribeOverTimeEvent();

        GameManager gm = GameManager.Instance;

        if (spawnOnOverTime &&
            gm != null &&
            gm.isOverTime &&
            !startRequested &&
            !started &&
            !completed)
        {
            StartCoroutine(PreBossSequenceThenStartRoutine());
        }
    }

    private void OnDisable()
    {
        UnsubscribeOverTimeEvent();

        if (Active == this)
            Active = null;
    }

    private void Update()
    {
        if (startRequested || started || completed)
            return;

        GameManager gm = GameManager.Instance;

        if (gm == null)
            return;

        if (!gm.isLive || gm.isPaused)
            return;

        if (IsTutorialMap(gm))
            return;

        if (spawnOnOverTime && ignoreSpawnTimeWhenSpawnOnOverTime)
            return;

        if (gm.gameTime < spawnTime)
            return;

        StartCoroutine(PreBossSequenceThenStartRoutine());
    }

    private void SubscribeOverTimeEvent()
    {
        if (subscribedOverTime)
            return;

        GameManager gm = GameManager.Instance;

        if (gm == null)
            return;

        gm.OnEnterOverTime += HandleEnterOverTime;
        subscribedOverTime = true;
    }

    private void UnsubscribeOverTimeEvent()
    {
        if (!subscribedOverTime)
            return;

        GameManager gm = GameManager.Instance;

        if (gm != null)
            gm.OnEnterOverTime -= HandleEnterOverTime;

        subscribedOverTime = false;
    }

    private void HandleEnterOverTime()
    {
        if (!spawnOnOverTime)
            return;

        if (startRequested || started || completed)
            return;

        if (IsTutorialMap(GameManager.Instance))
            return;

        StartCoroutine(PreBossSequenceThenStartRoutine());
    }

    private IEnumerator PreBossSequenceThenStartRoutine()
    {
        if (startRequested || started || completed)
            yield break;

        startRequested = true;

        ResolveReferences();

        GameManager gm = GameManager.Instance;

        if (ShouldPlayPreBossSequence())
        {
            if (gm != null)
                gm.PauseGame();
            else
                Time.timeScale = 0f;

            preBossSequenceUI.PlayLines(preBossSequenceLines);

            yield return null;

            while (preBossSequenceUI != null && preBossSequenceUI.IsPlaying)
                yield return null;

            SavePreBossSequenceSeen();

            if (gm != null)
                gm.ResumeGame();
            else
                Time.timeScale = 1f;
        }

        StartBossFlow();
    }

    private bool ShouldPlayPreBossSequence()
    {
        if (!playPreBossSequence)
            return false;

        if (IsChallengeMap(GameManager.Instance))
            return false;

        if (preBossSequenceUI == null)
            preBossSequenceUI = FindObjectOfType<OvertimeLetterSequenceUI>(true);

        if (preBossSequenceUI == null)
            return false;

        if (preBossSequenceLines == null || preBossSequenceLines.Length == 0)
            return false;

        if (playPreBossSequenceEveryTime)
            return true;

        string key = GetPreBossSequenceSaveKey();
        return PlayerPrefs.GetInt(key, 0) == 0;
    }

    private void SavePreBossSequenceSeen()
    {
        if (playPreBossSequenceEveryTime)
            return;

        string key = GetPreBossSequenceSaveKey();
        PlayerPrefs.SetInt(key, 1);
        PlayerPrefs.Save();
    }

    private string GetPreBossSequenceSaveKey()
    {
        string mapId = "map_" + PlayerPrefs.GetInt(KEY_MAP, 0);

        GameManager gm = GameManager.Instance;

        if (gm != null && !string.IsNullOrEmpty(gm.CurrentMapId))
            mapId = gm.CurrentMapId;

        return KEY_PRE_BOSS_SEQUENCE_PREFIX + mapId + "_" + preBossSequenceSaveKey;
    }

    public static bool TryHandleBossDeath(BossHealth bossHealth)
    {
        if (Active == null)
            return false;

        return Active.HandleBossDeath(bossHealth);
    }

    private bool IsTutorialMap(GameManager gm)
    {
        if (!disableOnTutorialMap)
            return false;

        int selectedMapIndex = PlayerPrefs.GetInt(KEY_MAP, 0);

        if (selectedMapIndex == tutorialMapIndex)
            return true;

        if (gm != null &&
            !string.IsNullOrEmpty(tutorialMapId) &&
            !string.IsNullOrEmpty(gm.CurrentMapId) &&
            gm.CurrentMapId == tutorialMapId)
        {
            return true;
        }

        return false;
    }

    private bool IsChallengeMap(GameManager gm)
    {
        int selectedMapIndex = PlayerPrefs.GetInt(KEY_MAP, 0);

        if (selectedMapIndex == challengeMapIndex)
            return true;

        if (gm != null &&
            !string.IsNullOrEmpty(challengeMapId) &&
            !string.IsNullOrEmpty(gm.CurrentMapId) &&
            gm.CurrentMapId == challengeMapId)
        {
            return true;
        }

        return false;
    }

    private void ResolveReferences()
    {
        if (spawnPoint == null)
            spawnPoint = transform;

        if (bossPoolManager == null && GameManager.Instance != null)
            bossPoolManager = GameManager.Instance.bossPoolManager;

        if (bossPoolManager == null)
            bossPoolManager = FindObjectOfType<BossPoolManager>(true);

        if (bossDirector == null)
            bossDirector = FindObjectOfType<BossDirector>(true);

        if (gameFlow == null)
            gameFlow = FindObjectOfType<GameFlow>(true);

        if (preBossSequenceUI == null)
            preBossSequenceUI = FindObjectOfType<OvertimeLetterSequenceUI>(true);
    }

    private void StartBossFlow()
    {
        ResolveReferences();

        if (IsTutorialMap(GameManager.Instance))
        {
            Log("튜토리얼 맵이므로 보스 스폰을 차단합니다.");
            return;
        }

        if (bossPoolManager == null)
        {
            LogError("BossPoolManager를 찾지 못했습니다.");
            return;
        }

        if (stageBossIndices == null || stageBossIndices.Length == 0)
        {
            LogError("Stage Boss Indices가 비어 있습니다.");
            return;
        }

        started = true;
        completed = false;
        waitingNextStage = false;

        challengeMode = IsChallengeMap(GameManager.Instance);

        if (challengeMode)
        {
            chainMode = true;
        }
        else
        {
            bool firstStageCleared = BossUnlock.IsFirstStageCleared(firstStageBossId);

            chainMode =
                normalAlwaysRunFullBossChain ||
                (normalUnlockFullChainAfterStage1Clear && firstStageCleared);
        }

        currentStageIndex = 0;
        currentBossObject = null;
        currentBossHealth = null;
        challengeExtraBosses.Clear();

        Log($"보스 플로우 시작 / challenge={challengeMode}, chainMode={chainMode}");

        SpawnCurrentStageBoss();
    }

    private void SpawnCurrentStageBoss()
    {
        ResolveReferences();

        if (completed)
            return;

        if (IsTutorialMap(GameManager.Instance))
            return;

        if (bossPoolManager == null)
            return;

        if (currentStageIndex < 0 || currentStageIndex >= stageBossIndices.Length)
        {
            FinishBossFlowAsWin(GetClearSequenceIdByStageIndex(stageBossIndices.Length - 1));
            return;
        }

        int bossIndex = Mathf.Clamp(stageBossIndices[currentStageIndex], 0, bossPoolManager.MaxIndex);

        GameObject boss = SpawnBossByPoolIndex(bossIndex, ResolveBossSpawnPosition());

        if (boss == null)
            return;

        currentBossObject = boss;
        currentBossHealth = boss.GetComponent<BossHealth>();

        Log($"보스 스폰 완료 / stage={currentStageIndex + 1}, bossIndex={bossIndex}");
    }

    private GameObject SpawnBossByPoolIndex(int bossIndex, Vector3 position)
    {
        if (bossPoolManager == null)
            return null;

        GameObject boss = bossPoolManager.GetBoss(bossIndex);

        if (boss == null)
        {
            LogError($"보스를 풀에서 가져오지 못했습니다. index={bossIndex}");
            return null;
        }

        position.z = 0f;

        boss.transform.position = position;
        boss.transform.rotation = Quaternion.identity;

        BossDifficulty difficulty = GetDifficulty();

        if (bossDirector != null)
        {
            bossDirector.StartBossSequence(boss, difficulty);
        }
        else
        {
            BossController controller = boss.GetComponent<BossController>();

            if (controller != null)
                controller.Setup(difficulty);
        }

        return boss;
    }

    private Vector3 ResolveBossSpawnPosition()
    {
        Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;

        GameManager gm = GameManager.Instance;

        if (spawnNearPlayer && gm != null && gm.PlayerTransform != null)
        {
            Vector3 playerPos = gm.PlayerTransform.position;
            pos = playerPos + new Vector3(nearPlayerOffset.x, nearPlayerOffset.y, 0f);
        }

        pos = ClampToCamera(pos);
        pos = ClampToMapBoundary(pos);
        pos.z = 0f;

        return pos;
    }

    private Vector3 ClampToCamera(Vector3 pos)
    {
        Camera cam = Camera.main;

        if (cam == null || !cam.orthographic)
            return pos;

        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;

        Vector3 camPos = cam.transform.position;

        float minX = camPos.x - halfWidth + cameraPadding;
        float maxX = camPos.x + halfWidth - cameraPadding;
        float minY = camPos.y - halfHeight + cameraPadding;
        float maxY = camPos.y + halfHeight - cameraPadding;

        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);

        return pos;
    }

    private Vector3 ClampToMapBoundary(Vector3 pos)
    {
        GameManager gm = GameManager.Instance;

        if (gm == null || gm.CurrentMapBoundary == null)
            return pos;

        Collider2D col = gm.CurrentMapBoundary.BoundaryCollider2D;

        if (col == null)
            col = gm.CurrentMapBoundary.GetComponent<Collider2D>();

        if (col == null)
            col = gm.CurrentMapBoundary.GetComponentInChildren<Collider2D>(true);

        if (col == null)
            return pos;

        Bounds b = col.bounds;

        pos.x = Mathf.Clamp(pos.x, b.min.x + mapPadding, b.max.x - mapPadding);
        pos.y = Mathf.Clamp(pos.y, b.min.y + mapPadding, b.max.y - mapPadding);

        return pos;
    }

    private bool HandleBossDeath(BossHealth bossHealth)
    {
        if (!started || completed)
            return false;

        if (bossHealth == null)
            return false;

        Debug.Log(
            $"ProgressiveBossSpawner: BossDeath / bossId={bossHealth.BossId}, " +
            $"stage={currentStageIndex}, maxStage={stageBossIndices.Length - 1}, " +
            $"chainMode={chainMode}, nextStageDelay={nextStageDelay}",
            this
        );

        if (currentBossHealth != null && currentBossHealth != bossHealth)
        {
            Debug.LogWarning("ProgressiveBossSpawner: 현재 관리 중인 보스가 아니므로 무시", this);
            return false;
        }

        string deadBossId = bossHealth.BossId;

        if (!string.IsNullOrEmpty(deadBossId))
            BossUnlock.SaveBossCleared(deadBossId);

        if (currentStageIndex == 0)
            BossUnlock.SaveFirstStageCleared(firstStageBossId);

        bool shouldRunFullChain =
            challengeMode ||
            chainMode ||
            normalAlwaysRunFullBossChain;

        if (shouldRunFullChain)
        {
            if (currentStageIndex < stageBossIndices.Length - 1)
            {
                Debug.Log($"ProgressiveBossSpawner: 다음 페이즈 보스 스폰 예약 / delay={nextStageDelay}", this);

                if (!waitingNextStage)
                    StartCoroutine(SpawnNextStageRoutine());

                return true;
            }

            Debug.Log("ProgressiveBossSpawner: 마지막 페이즈 처치, 클리어 처리", this);

            FinishBossFlowAsWin(normalStage3ClearSequenceId);
            return true;
        }

        if (!chainMode)
        {
            if (normalClearAfterStage1WhenSinglePhase)
            {
                Debug.Log("ProgressiveBossSpawner: 1페이즈 단독 클리어 처리", this);
                FinishBossFlowAsWin(normalStage1ClearSequenceId);
            }
            else
            {
                Debug.Log("ProgressiveBossSpawner: 1페이즈 단독 처치, 클리어 처리 안 함", this);
            }

            return true;
        }

        return true;
    }

    private IEnumerator SpawnNextStageRoutine()
    {
        waitingNextStage = true;

        currentBossObject = null;
        currentBossHealth = null;

        float delay = Mathf.Max(0f, nextStageDelay);

        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        currentStageIndex++;
        waitingNextStage = false;

        SpawnCurrentStageBoss();
    }

    private string GetClearSequenceIdByStageIndex(int stageIndex)
    {
        if (stageIndex <= 0)
            return normalStage1ClearSequenceId;

        if (stageIndex >= stageBossIndices.Length - 1)
            return normalStage3ClearSequenceId;

        return string.Empty;
    }

    private BossDifficulty GetDifficulty()
    {
        GameManager gm = GameManager.Instance;

        if (useOverTimeDifficulty && gm != null && gm.isOverTime)
            return BossDifficulty.OverTime;

        return normalDifficulty;
    }

    private void FinishBossFlowAsWin(string clearSequenceId)
    {
        if (completed)
            return;

        if (challengeMode)
        {
            Log("첼린지 맵이므로 클리어 처리하지 않습니다.");
            return;
        }

        completed = true;
        waitingNextStage = false;

        ResolveReferences();

        if (gameFlow != null)
            gameFlow.WinByBossId(clearSequenceId);
        else if (GameManager.Instance != null)
            GameManager.Instance.GameVictory();
    }

    private void Log(string message)
    {
        if (verboseLog)
            Debug.Log($"ProgressiveBossSpawner: {message}", this);
    }

    private void LogError(string message)
    {
        Debug.LogError($"ProgressiveBossSpawner: {message}", this);
    }
}