using UnityEngine;

/// <summary>
/// 보스 진행도 저장 전용 전역 매니저.
/// 
/// 배치 위치:
/// - Title 씬 Managers 아래.
/// - DontDestroyOnLoad로 Loading, Game 씬까지 유지.
/// 
/// 담당:
/// - 특정 보스 처치 여부 저장
/// - 1단계 보스 최초 클리어 여부 저장
/// - ProgressiveBossSpawner의 연속 보스 진행 여부 판단
/// 
/// 씬 이동 구조 주의:
/// - Game 씬 오브젝트 참조를 들고 있지 않는다.
/// - 문자열 key와 PlayerPrefs만 사용한다.
/// </summary>
[DisallowMultipleComponent]
public class BossUnlock : MonoBehaviour
{
    public static BossUnlock Instance { get; private set; }

    [Header("Debug")]
    [SerializeField] private bool verboseLog = true;

    private const string KEY_BOSS_CLEARED_PREFIX = "BOSS_CLEARED_";
    private const string KEY_FIRST_STAGE_CLEARED_PREFIX = "BOSS_FIRST_STAGE_CLEARED_";

    private static BossUnlock StaticInstance => Instance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public static void SaveBossCleared(string bossId)
    {
        if (string.IsNullOrEmpty(bossId))
            return;

        PlayerPrefs.SetInt(KEY_BOSS_CLEARED_PREFIX + bossId, 1);
        PlayerPrefs.Save();

        if (StaticInstance != null && StaticInstance.verboseLog)
            Debug.Log($"BossUnlock: 보스 클리어 저장 / bossId={bossId}", StaticInstance);
    }

    public static bool IsBossCleared(string bossId)
    {
        if (string.IsNullOrEmpty(bossId))
            return false;

        return PlayerPrefs.GetInt(KEY_BOSS_CLEARED_PREFIX + bossId, 0) == 1;
    }

    public static void SaveFirstStageCleared(string progressKey)
    {
        if (string.IsNullOrEmpty(progressKey))
            return;

        PlayerPrefs.SetInt(KEY_FIRST_STAGE_CLEARED_PREFIX + progressKey, 1);
        PlayerPrefs.Save();

        if (StaticInstance != null && StaticInstance.verboseLog)
            Debug.Log($"BossUnlock: 1단계 클리어 저장 / key={progressKey}", StaticInstance);
    }

    public static bool IsFirstStageCleared(string progressKey)
    {
        if (string.IsNullOrEmpty(progressKey))
            return false;

        return PlayerPrefs.GetInt(KEY_FIRST_STAGE_CLEARED_PREFIX + progressKey, 0) == 1;
    }

    public static void ClearBossClearedForTest(string bossId)
    {
        if (string.IsNullOrEmpty(bossId))
            return;

        PlayerPrefs.DeleteKey(KEY_BOSS_CLEARED_PREFIX + bossId);
        PlayerPrefs.Save();
    }

    public static void ClearFirstStageClearedForTest(string progressKey)
    {
        if (string.IsNullOrEmpty(progressKey))
            return;

        PlayerPrefs.DeleteKey(KEY_FIRST_STAGE_CLEARED_PREFIX + progressKey);
        PlayerPrefs.Save();
    }
}