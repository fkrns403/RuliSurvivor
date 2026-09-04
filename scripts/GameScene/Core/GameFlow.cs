using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class GameFlow : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private LevelUpUI levelUpUI;
    [SerializeField] private ResultUI resultUI;
    [SerializeField] private GameObject pausePanel;

    [Header("Sequence UI")]
    [SerializeField] private OvertimeLetterSequenceUI clearSequenceUI;
    [SerializeField] private ChallengeRecordToastUI challengeRecordToastUI;

    [Header("Enemy Cleaner")]
    [SerializeField] private EnemyCleaner enemyCleaner;

    [Header("Map Rule")]
    [SerializeField] private int tutorialMapIndex = 0;
    [SerializeField] private int challengeMapIndex = 2;

    [Header("Tutorial Rule")]
    [SerializeField] private bool tutorialWinOnTime = true;

    [Header("Tutorial Clear Sequence Rule")]
    [SerializeField] private bool playTutorialClearSequenceOnClear = false;
    [SerializeField] private bool playTutorialClearSequenceEveryTime = true;

    [Header("Challenge Clear Sequence Rule")]
    [SerializeField] private bool playChallengeClearSequenceEveryTime = false;

    [Header("Normal Map Clear Sequence Rule")]
    [SerializeField] private bool requireBossIdForNormalWin = true;
    [SerializeField] private bool blockNormalStage1WinRequest = true;

    [SerializeField] private bool playNormalClearSequenceEveryTime = false;
    [SerializeField] private bool playNormalStage1ClearSequence = false;
    [SerializeField] private bool playNormalStage3ClearSequence = true;

    [SerializeField] private string normalStage1ClearBossId = "normal_map_stage1";
    [SerializeField] private string normalStage3ClearBossId = "normal_map_stage3";

    [Header("Clear Sequence Text")]
    [TextArea]
    [SerializeField]
    private string[] tutorialClearLines =
    {
        "튜토리얼을 완료했습니다.",
        "기본 조작과 생존 방법을 익혔습니다.",
        "이제 본격적인 전투를 시작할 수 있습니다."
    };

    [TextArea]
    [SerializeField]
    private string[] normalClearLines =
    {
        "여기까지 와줘서 고마워요.",
        "이 게임을 만드는 데\n정말 많은 시간이 들었지만,",
        "당신이 이 마지막 순간까지\n함께해줘서 보람을 느껴요.",
        "준비되었다면,",
        "함께 끝을 향해\n나아가봅시다."
    };

    [TextArea]
    [SerializeField]
    private string[] challengeClearLines =
    {
        "첼린지 기록 확인용 연출입니다.",
        "이번 생존 기록을 확인합니다.",
        "최고 기록을 갱신했다면\n잠시 후 기록 메시지가 표시됩니다."
    };

    [Header("Retry Scene Index")]
    [SerializeField] private int retrySceneIndex = 0;

    [Header("Debug")]
    [SerializeField] private bool verboseLog = true;

    private int selectedMap;
    private bool resultShown;
    private bool winProcessing;

    private const string KEY_CHALLENGE_BEST_TIME_PREFIX = "ChallengeBestTime_";
    private const string KEY_CHALLENGE_BEST_LEVEL_PREFIX = "ChallengeBestLevel_";
    private const string KEY_CLEAR_SEQUENCE_MAP_PREFIX = "ClearSequenceSeen_Map_";
    private const string KEY_CLEAR_SEQUENCE_BOSS_PREFIX = "ClearSequenceSeen_Boss_";

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        SubscribeGameManagerEvents();
    }

    private void Start()
    {
        selectedMap = PlayerPrefs.GetInt("SelectedMapIndex", 0);
        resultShown = false;
        winProcessing = false;

        ResolveReferences();
        SubscribeGameManagerEvents();

        if (resultUI != null)
            resultUI.HideAll();

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (levelUpUI != null)
            levelUpUI.ForceHide();
    }

    private void OnDisable()
    {
        UnsubscribeGameManagerEvents();
    }

    private void Update()
    {
        GameManager gm = GameManager.Instance;

        if (gm == null)
            return;

        if (!gm.isLive)
            return;

        if (IsTutorialMap() && tutorialWinOnTime)
        {
            if (gm.gameTime >= gm.maxGameTime)
                WinByBossId(string.Empty);
        }
    }

    private void ResolveReferences()
    {
        if (levelUpUI == null)
            levelUpUI = FindObjectOfType<LevelUpUI>(true);

        if (resultUI == null)
            resultUI = FindObjectOfType<ResultUI>(true);

        if (enemyCleaner == null)
            enemyCleaner = FindObjectOfType<EnemyCleaner>(true);

        if (clearSequenceUI == null)
            clearSequenceUI = FindObjectOfType<OvertimeLetterSequenceUI>(true);

        if (challengeRecordToastUI == null)
            challengeRecordToastUI = FindObjectOfType<ChallengeRecordToastUI>(true);
    }

    private void SubscribeGameManagerEvents()
    {
        GameManager gm = GameManager.Instance;

        if (gm == null)
            return;

        gm.OnLevelUp -= HandleLevelUp;
        gm.OnLevelUp += HandleLevelUp;
    }

    private void UnsubscribeGameManagerEvents()
    {
        GameManager gm = GameManager.Instance;

        if (gm == null)
            return;

        gm.OnLevelUp -= HandleLevelUp;
    }

    private void HandleLevelUp(int newLevel)
    {
        OnLevelUp();
    }

    public void OnLevelUp()
    {
        if (resultShown || winProcessing)
            return;

        ResolveReferences();

        if (levelUpUI != null)
            levelUpUI.Show();
    }

    public void Lose()
    {
        if (resultShown)
            return;

        if (IsChallengeMap())
        {
            StartCoroutine(ChallengeLoseRoutine());
            return;
        }

        ImmediateLose();
    }

    private IEnumerator ChallengeLoseRoutine()
    {
        if (resultShown)
            yield break;

        resultShown = true;

        ResolveReferences();

        GameManager gm = GameManager.Instance;
        string recordMessage = BuildChallengeRecordMessage(gm);

        if (gm != null)
            gm.StopGame();

        if (enemyCleaner != null)
            enemyCleaner.ClearAll();

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (levelUpUI != null)
            levelUpUI.ForceHide();

        if (playChallengeClearSequenceEveryTime &&
            clearSequenceUI != null &&
            challengeClearLines != null &&
            challengeClearLines.Length > 0)
        {
            clearSequenceUI.PlayLines(challengeClearLines);
            yield return null;

            while (clearSequenceUI.IsPlaying)
                yield return null;
        }

        if (!string.IsNullOrEmpty(recordMessage) && challengeRecordToastUI != null)
        {
            challengeRecordToastUI.Play(recordMessage);
            yield return null;

            while (challengeRecordToastUI.IsPlaying)
                yield return null;
        }

        if (resultUI != null)
            resultUI.ShowLose();

        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlayBGM(false);
            AudioManager.instance.PlaySfx(AudioManager.Sfx.Lose);
        }
    }

    private void ImmediateLose()
    {
        if (resultShown)
            return;

        resultShown = true;

        GameManager gm = GameManager.Instance;

        if (gm != null)
            gm.StopGame();

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (levelUpUI != null)
            levelUpUI.ForceHide();

        if (resultUI != null)
            resultUI.ShowLose();

        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlayBGM(false);
            AudioManager.instance.PlaySfx(AudioManager.Sfx.Lose);
        }
    }

    public void Win()
    {
        WinByBossId(string.Empty);
    }

    public void WinByBossId(string bossId)
    {
        if (resultShown || winProcessing)
            return;

        if (IsChallengeMap())
        {
            LogWarning("첼린지 맵은 클리어 판정에서 제외됩니다.");
            return;
        }

        if (IsNormalMap())
        {
            if (requireBossIdForNormalWin && string.IsNullOrEmpty(bossId))
            {
                LogWarning("일반맵 빈 bossId 승리 요청 차단");
                return;
            }

            if (blockNormalStage1WinRequest && bossId == normalStage1ClearBossId)
            {
                LogWarning($"일반맵 1페이즈 승리 요청 차단 / bossId={bossId}");
                return;
            }
        }

        StartCoroutine(WinRoutine(bossId));
    }

    private IEnumerator WinRoutine(string bossId)
    {
        if (resultShown || winProcessing)
            yield break;

        winProcessing = true;

        ResolveReferences();

        GameManager gm = GameManager.Instance;

        if (gm != null)
            gm.PauseGame();
        else
            Time.timeScale = 0f;

        if (enemyCleaner != null)
            enemyCleaner.ClearAll();

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (levelUpUI != null)
            levelUpUI.ForceHide();

        string[] lines = ResolveClearSequenceLines(bossId);
        bool playedSequence = lines != null && lines.Length > 0;

        if (clearSequenceUI != null && playedSequence)
        {
            clearSequenceUI.PlayLines(lines);
            yield return null;

            while (clearSequenceUI.IsPlaying)
                yield return null;
        }

        if (playedSequence)
            SaveClearSequenceSeen(bossId);

        RegisterClearMap();

        ImmediateWinAfterSequence();
    }

    private string[] ResolveClearSequenceLines(string bossId)
    {
        if (IsTutorialMap())
        {
            if (!playTutorialClearSequenceOnClear)
                return null;

            if (playTutorialClearSequenceEveryTime)
                return tutorialClearLines;

            if (!HasSeenTutorialClearSequence())
                return tutorialClearLines;

            return null;
        }

        if (IsNormalMap())
        {
            if (string.IsNullOrEmpty(bossId))
                return null;

            bool isStage1Clear = bossId == normalStage1ClearBossId;
            bool isStage3Clear = bossId == normalStage3ClearBossId;

            if (!isStage1Clear && !isStage3Clear)
                return null;

            if (isStage1Clear && !playNormalStage1ClearSequence)
                return null;

            if (isStage3Clear && !playNormalStage3ClearSequence)
                return null;

            if (playNormalClearSequenceEveryTime)
                return normalClearLines;

            if (HasSeenBossClearSequence(bossId))
                return null;

            return normalClearLines;
        }

        return null;
    }

    private bool HasSeenTutorialClearSequence()
    {
        string mapId = GetCurrentMapIdForSave();
        return PlayerPrefs.GetInt(KEY_CLEAR_SEQUENCE_MAP_PREFIX + mapId, 0) == 1;
    }

    private bool HasSeenBossClearSequence(string bossId)
    {
        string mapId = GetCurrentMapIdForSave();
        string key = KEY_CLEAR_SEQUENCE_BOSS_PREFIX + mapId + "_" + bossId;

        return PlayerPrefs.GetInt(key, 0) == 1;
    }

    private void SaveClearSequenceSeen(string bossId)
    {
        string mapId = GetCurrentMapIdForSave();

        if (IsTutorialMap())
        {
            PlayerPrefs.SetInt(KEY_CLEAR_SEQUENCE_MAP_PREFIX + mapId, 1);
            PlayerPrefs.Save();
            return;
        }

        if (IsNormalMap() && !string.IsNullOrEmpty(bossId))
        {
            string key = KEY_CLEAR_SEQUENCE_BOSS_PREFIX + mapId + "_" + bossId;
            PlayerPrefs.SetInt(key, 1);
            PlayerPrefs.Save();
        }
    }

    private string GetCurrentMapIdForSave()
    {
        GameManager gm = GameManager.Instance;

        if (gm != null && !string.IsNullOrEmpty(gm.CurrentMapId))
            return gm.CurrentMapId;

        return "map_" + selectedMap;
    }

    private void ImmediateWinAfterSequence()
    {
        if (resultShown)
            return;

        resultShown = true;
        winProcessing = false;

        GameManager gm = GameManager.Instance;

        if (gm != null)
            gm.StopGame();
        else
            Time.timeScale = 1f;

        if (resultUI != null)
            resultUI.ShowWin();

        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlayBGM(false);
            AudioManager.instance.PlaySfx(AudioManager.Sfx.Win);
        }
    }

    private void RegisterClearMap()
    {
        if (IsChallengeMap())
            return;

        GameManager gm = GameManager.Instance;

        if (gm == null || UnlockManager.Instance == null)
            return;

        string mapId = gm.CurrentMapId;

        if (string.IsNullOrEmpty(mapId))
            return;

        UnlockManager.Instance.RegisterClearMap(mapId);

        if (verboseLog)
            Debug.Log($"GameFlow: 맵 클리어 저장 완료 / mapId={mapId}", this);
    }

    private string BuildChallengeRecordMessage(GameManager gm)
    {
        if (gm == null)
            return string.Empty;

        string mapId = string.IsNullOrEmpty(gm.CurrentMapId) ? "challenge" : gm.CurrentMapId;

        string timeKey = KEY_CHALLENGE_BEST_TIME_PREFIX + mapId;
        string levelKey = KEY_CHALLENGE_BEST_LEVEL_PREFIX + mapId;

        float currentTime = gm.gameTime;
        int currentLevel = gm.level;

        float bestTime = PlayerPrefs.GetFloat(timeKey, 0f);
        int bestLevel = PlayerPrefs.GetInt(levelKey, 0);

        bool newTimeRecord = currentTime > bestTime;
        bool newLevelRecord = currentLevel > bestLevel;

        if (!newTimeRecord && !newLevelRecord)
            return string.Empty;

        if (newTimeRecord)
            PlayerPrefs.SetFloat(timeKey, currentTime);

        if (newLevelRecord)
            PlayerPrefs.SetInt(levelKey, currentLevel);

        PlayerPrefs.Save();

        StringBuilder sb = new StringBuilder();

        sb.AppendLine("새로운 기록을 달성했습니다.");
        sb.AppendLine("당신은 이전보다 더 오래 살아남았습니다.");

        if (newTimeRecord)
        {
            sb.AppendLine("");
            sb.AppendLine("최고 생존 시간 갱신");
            sb.AppendLine($"{FormatTime(currentTime)}");
        }

        if (newLevelRecord)
        {
            sb.AppendLine("");
            sb.AppendLine("최고 레벨 갱신");
            sb.AppendLine($"Lv.{currentLevel}");
        }

        return sb.ToString();
    }

    private string FormatTime(float seconds)
    {
        int total = Mathf.FloorToInt(Mathf.Max(0f, seconds));
        int min = total / 60;
        int sec = total % 60;

        return $"{min:00}:{sec:00}";
    }

    private bool IsTutorialMap()
    {
        return selectedMap == tutorialMapIndex;
    }

    private bool IsChallengeMap()
    {
        return selectedMap == challengeMapIndex;
    }

    private bool IsNormalMap()
    {
        return !IsTutorialMap() && !IsChallengeMap();
    }

    public void OnResumeButton()
    {
        GameManager gm = GameManager.Instance;

        if (gm != null)
            gm.ResumeGame();

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (AudioManager.instance != null)
            AudioManager.instance.PlaySfx(AudioManager.Sfx.Select);
    }

    public void OnRetryButton()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(retrySceneIndex);
    }

    public void OnExitButton()
    {
        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void LogWarning(string message)
    {
        if (!verboseLog)
            return;

        Debug.LogWarning($"GameFlow: {message}", this);
    }
}