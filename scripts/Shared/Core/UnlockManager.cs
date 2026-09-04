using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전역 언락 관리 매니저.
/// 
/// 역할:
/// - 캐릭터 / 몬스터 / 기타 해금 상태 저장
/// - 몬스터 처치 수 저장
/// - 맵 클리어 저장
/// - 최고 레벨 저장
/// - 이번 판 생존 시간 조건 검사
/// - 오버타임 진입 조건 검사
/// 
/// 중요:
/// - allDefinitions 배열에 등록된 UnlockDefinition은 자동으로 평가된다.
/// - allDefinitions에 빠진 정의라도 CharacterSelectSlot이 UnlockDefinition을 넘기면
///   IsUnlockedOrEvaluate(def)를 통해 조건을 직접 평가하고 해금할 수 있다.
/// </summary>
[DisallowMultipleComponent]
public class UnlockManager : MonoBehaviour
{
    public static UnlockManager Instance { get; private set; }

    [Header("Unlock Definitions")]
    [Tooltip("전체 해금 정의 목록. 여기에 등록된 것들은 자동 평가된다.")]
    [SerializeField] private UnlockDefinition[] allDefinitions;

    [Header("Evaluate Option")]
    [SerializeField] private float surviveEvaluateInterval = 0.25f;

    [Header("Debug")]
    [SerializeField] private bool verboseLog = true;

    private readonly HashSet<string> unlockedIds = new HashSet<string>();
    private readonly Dictionary<string, int> killCounts = new Dictionary<string, int>();

    private float surviveSecondsThisRun;
    private bool overTimeEnteredThisRun;
    private float surviveEvaluateTimer;

    public event Action OnStateChanged;
    public event Action<UnlockDefinition> OnUnlocked;

    private const string KEY_UNLOCKED_PREFIX = "UNLOCKED_";
    private const string KEY_KILL_PREFIX = "KILL_COUNT_";
    private const string KEY_CLEAR_PREFIX = "CLEAR_";
    private const string KEY_OVERTIME_EVER = "OVERTIME_ENTERED";
    private const string KEY_HIGHEST_LEVEL = "PLAYER_HIGHEST_LEVEL";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadData();
    }

    private void Start()
    {
        EvaluateAll();
    }

    public void ResetRunState()
    {
        surviveSecondsThisRun = 0f;
        overTimeEnteredThisRun = false;
        surviveEvaluateTimer = 0f;

        EvaluateAll();
        OnStateChanged?.Invoke();
    }

    public void RegisterKill(string enemyId)
    {
        if (string.IsNullOrEmpty(enemyId))
            return;

        if (!killCounts.ContainsKey(enemyId))
            killCounts[enemyId] = PlayerPrefs.GetInt(KEY_KILL_PREFIX + enemyId, 0);

        killCounts[enemyId]++;

        PlayerPrefs.SetInt(KEY_KILL_PREFIX + enemyId, killCounts[enemyId]);
        PlayerPrefs.Save();

        EvaluateAll();
        OnStateChanged?.Invoke();

        if (verboseLog)
            Debug.Log($"UnlockManager: Kill 저장 / id={enemyId}, count={killCounts[enemyId]}", this);
    }

    public void RegisterClearMap(string mapId)
    {
        if (string.IsNullOrEmpty(mapId))
            return;

        PlayerPrefs.SetInt(KEY_CLEAR_PREFIX + mapId, 1);
        PlayerPrefs.Save();

        EvaluateAll();
        OnStateChanged?.Invoke();

        if (verboseLog)
            Debug.Log($"UnlockManager: 맵 클리어 저장 / mapId={mapId}", this);
    }

    public void RegisterLevel(int level)
    {
        int safeLevel = Mathf.Max(0, level);
        int highest = PlayerPrefs.GetInt(KEY_HIGHEST_LEVEL, 0);

        if (safeLevel > highest)
        {
            PlayerPrefs.SetInt(KEY_HIGHEST_LEVEL, safeLevel);
            PlayerPrefs.Save();
        }

        EvaluateAll();
        OnStateChanged?.Invoke();

        if (verboseLog)
            Debug.Log($"UnlockManager: 최고 레벨 저장 / level={safeLevel}", this);
    }

    public void RegisterOverTimeEntered()
    {
        overTimeEnteredThisRun = true;

        PlayerPrefs.SetInt(KEY_OVERTIME_EVER, 1);
        PlayerPrefs.Save();

        EvaluateAll();
        OnStateChanged?.Invoke();

        if (verboseLog)
            Debug.Log("UnlockManager: 오버타임 진입 저장", this);
    }

    public void TickSurvive(float deltaTime)
    {
        if (deltaTime <= 0f)
            return;

        surviveSecondsThisRun += deltaTime;
        surviveEvaluateTimer += deltaTime;

        if (surviveEvaluateTimer < surviveEvaluateInterval)
            return;

        surviveEvaluateTimer = 0f;
        EvaluateAll();
    }

    public bool IsUnlocked(string id)
    {
        if (string.IsNullOrEmpty(id))
            return false;

        if (unlockedIds.Contains(id))
            return true;

        return PlayerPrefs.GetInt(KEY_UNLOCKED_PREFIX + id, 0) == 1;
    }

    /// <summary>
    /// UnlockDefinition을 직접 검사한다.
    /// 
    /// 용도:
    /// - UnlockManager의 allDefinitions 배열에 실수로 빠진 정의가 있어도
    ///   슬롯에 UnlockDefinition이 연결되어 있으면 조건을 평가해서 해금할 수 있게 한다.
    /// </summary>
    public bool IsUnlockedOrEvaluate(UnlockDefinition def)
    {
        if (def == null)
            return false;

        if (string.IsNullOrEmpty(def.id))
            return false;

        if (IsUnlocked(def.id))
        {
            if (!unlockedIds.Contains(def.id))
                unlockedIds.Add(def.id);

            return true;
        }

        if (!IsAllConditionsMet(def))
            return false;

        Unlock(def);
        OnStateChanged?.Invoke();

        return true;
    }

    public int GetKillCount(string enemyId)
    {
        if (string.IsNullOrEmpty(enemyId))
            return 0;

        if (killCounts.TryGetValue(enemyId, out int count))
            return count;

        int saved = PlayerPrefs.GetInt(KEY_KILL_PREFIX + enemyId, 0);
        killCounts[enemyId] = saved;

        return saved;
    }

    public bool IsMapCleared(string mapId)
    {
        if (string.IsNullOrEmpty(mapId))
            return false;

        return PlayerPrefs.GetInt(KEY_CLEAR_PREFIX + mapId, 0) == 1;
    }

    public int GetHighestLevel()
    {
        return PlayerPrefs.GetInt(KEY_HIGHEST_LEVEL, 0);
    }

    public float GetSurviveSecondsThisRun()
    {
        return surviveSecondsThisRun;
    }

    public bool HasEnteredOverTimeThisRun()
    {
        return overTimeEnteredThisRun;
    }

    public bool HasEnteredOverTimeEver()
    {
        return PlayerPrefs.GetInt(KEY_OVERTIME_EVER, 0) == 1;
    }

    [ContextMenu("Evaluate All Unlocks")]
    public void EvaluateAll()
    {
        if (allDefinitions == null)
            return;

        bool changed = false;

        for (int i = 0; i < allDefinitions.Length; i++)
        {
            UnlockDefinition def = allDefinitions[i];

            if (def == null)
                continue;

            if (string.IsNullOrEmpty(def.id))
                continue;

            if (IsUnlocked(def.id))
            {
                if (!unlockedIds.Contains(def.id))
                    unlockedIds.Add(def.id);

                continue;
            }

            if (!IsAllConditionsMet(def))
                continue;

            Unlock(def);
            changed = true;
        }

        if (changed)
            OnStateChanged?.Invoke();
    }

    private bool IsAllConditionsMet(UnlockDefinition def)
    {
        if (def == null)
            return false;

        if (def.conditions == null || def.conditions.Count == 0)
            return true;

        for (int i = 0; i < def.conditions.Count; i++)
        {
            UnlockCondition condition = def.conditions[i];

            if (!IsConditionMet(condition))
                return false;
        }

        return true;
    }

    private bool IsConditionMet(UnlockCondition condition)
    {
        if (condition == null)
            return true;

        switch (condition.type)
        {
            case UnlockConditionType.KillCountByEnemyId:
                return GetKillCount(condition.id) >= condition.intValue;

            case UnlockConditionType.ReachLevel:
                return GetHighestLevel() >= condition.intValue;

            case UnlockConditionType.SurviveSeconds:
                return surviveSecondsThisRun >= condition.floatValue;

            case UnlockConditionType.OverTimeEntered:
                return overTimeEnteredThisRun || HasEnteredOverTimeEver();

            case UnlockConditionType.ClearMap:
                return IsMapCleared(condition.id);

            case UnlockConditionType.None:
            default:
                return true;
        }
    }

    private void Unlock(UnlockDefinition def)
    {
        if (def == null)
            return;

        if (string.IsNullOrEmpty(def.id))
            return;

        if (unlockedIds.Contains(def.id))
            return;

        unlockedIds.Add(def.id);

        PlayerPrefs.SetInt(KEY_UNLOCKED_PREFIX + def.id, 1);
        PlayerPrefs.Save();

        OnUnlocked?.Invoke(def);

        if (verboseLog)
            Debug.Log($"UnlockManager: 해금 완료 / id={def.id}, name={def.displayName}", this);
    }

    private void LoadData()
    {
        unlockedIds.Clear();
        killCounts.Clear();

        if (allDefinitions == null)
            return;

        for (int i = 0; i < allDefinitions.Length; i++)
        {
            UnlockDefinition def = allDefinitions[i];

            if (def == null)
                continue;

            if (string.IsNullOrEmpty(def.id))
                continue;

            if (PlayerPrefs.GetInt(KEY_UNLOCKED_PREFIX + def.id, 0) == 1)
                unlockedIds.Add(def.id);

            if (def.conditions == null)
                continue;

            for (int j = 0; j < def.conditions.Count; j++)
            {
                UnlockCondition condition = def.conditions[j];

                if (condition == null)
                    continue;

                if (condition.type == UnlockConditionType.KillCountByEnemyId)
                {
                    if (string.IsNullOrEmpty(condition.id))
                        continue;

                    if (!killCounts.ContainsKey(condition.id))
                    {
                        killCounts[condition.id] =
                            PlayerPrefs.GetInt(KEY_KILL_PREFIX + condition.id, 0);
                    }
                }
            }
        }
    }

    [ContextMenu("Debug Log Unlock State")]
    public void DebugLogUnlockState()
    {
        Debug.Log(
            $"UnlockManager State\n" +
            $"SurviveThisRun={surviveSecondsThisRun}\n" +
            $"OverTimeThisRun={overTimeEnteredThisRun}\n" +
            $"OverTimeEver={HasEnteredOverTimeEver()}",
            this
        );

        if (allDefinitions == null)
            return;

        for (int i = 0; i < allDefinitions.Length; i++)
        {
            UnlockDefinition def = allDefinitions[i];

            if (def == null)
                continue;

            Debug.Log(
                $"Unlock Def: id={def.id}, unlocked={IsUnlocked(def.id)}, conditionsMet={IsAllConditionsMet(def)}",
                this
            );
        }
    }

    [ContextMenu("Clear All Unlock Save Data For Test")]
    public void ClearAllUnlockSaveDataForTest()
    {
        if (allDefinitions != null)
        {
            for (int i = 0; i < allDefinitions.Length; i++)
            {
                UnlockDefinition def = allDefinitions[i];

                if (def == null)
                    continue;

                if (!string.IsNullOrEmpty(def.id))
                    PlayerPrefs.DeleteKey(KEY_UNLOCKED_PREFIX + def.id);

                if (def.conditions == null)
                    continue;

                for (int j = 0; j < def.conditions.Count; j++)
                {
                    UnlockCondition condition = def.conditions[j];

                    if (condition == null)
                        continue;

                    if (condition.type == UnlockConditionType.KillCountByEnemyId &&
                        !string.IsNullOrEmpty(condition.id))
                    {
                        PlayerPrefs.DeleteKey(KEY_KILL_PREFIX + condition.id);
                    }

                    if (condition.type == UnlockConditionType.ClearMap &&
                        !string.IsNullOrEmpty(condition.id))
                    {
                        PlayerPrefs.DeleteKey(KEY_CLEAR_PREFIX + condition.id);
                    }
                }
            }
        }

        PlayerPrefs.DeleteKey(KEY_OVERTIME_EVER);
        PlayerPrefs.DeleteKey(KEY_HIGHEST_LEVEL);
        PlayerPrefs.Save();

        ResetRunState();
        LoadData();
        EvaluateAll();
        OnStateChanged?.Invoke();
    }
}