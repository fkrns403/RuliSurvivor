using UnityEngine;

/// <summary>
/// 게임 설정 저장/로드/적용을 담당.
/// - PlayerPrefs(JSON)로 저장
/// - Audio / Quality / Fullscreen / WindowResolution / FPS / VSync 적용
///
/// 창모드 해상도
/// - WindowResolutions 배열의 index를 GameSettings.windowResolutionIndex로 저장한다.
/// - fullscreen이 false일 때만 SetResolution을 적용한다.
///
/// 전체화면 해상도
/// - 기본은 Screen.currentResolution을 사용하여 모니터 해상도에 맞춘다.
/// - 필요하면 전체화면도 드롭다운으로 선택하도록 확장할 수 있다.
/// </summary>
public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    public GameSettings Current { get; private set; } = GameSettings.Default();

    private const string KEY = "GAME_SETTINGS_JSON";

    public static readonly Vector2Int[] WindowResolutions =
    {
        new Vector2Int(1280, 720),
        new Vector2Int(1600, 900),
        new Vector2Int(1920, 1080),
        new Vector2Int(2560, 1440),
    };

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Load();
        Apply();
    }

    public void Set(GameSettings newSettings)
    {
        Current = newSettings;
    }

    public void Apply()
    {
        // Audio
        if (AudioManager.instance != null)
            AudioManager.instance.SetVolumes(Current.master, Current.bgm, Current.sfx);

        // Quality
        int qCount = QualitySettings.names.Length;
        if (qCount > 0)
        {
            int qi = Mathf.Clamp(Current.qualityIndex, 0, qCount - 1);
            QualitySettings.SetQualityLevel(qi, true);
        }

        // Fullscreen / Windowed
        Screen.fullScreen = Current.fullscreen;

        if (Current.fullscreen)
        {
            // 전체화면은 기본적으로 모니터 해상도에 맞추는 방식
            // 원하는 설계가 "전체화면도 특정 해상도 선택"이면 여기 로직을 확장해야 한다.
            Resolution r = Screen.currentResolution;
            Screen.SetResolution(r.width, r.height, true);
        }
        else
        {
            // 창모드 해상도 프리셋 적용
            int idx = Mathf.Clamp(Current.windowResolutionIndex, 0, WindowResolutions.Length - 1);
            var res = WindowResolutions[idx];
            Screen.SetResolution(res.x, res.y, false);
        }

        // VSync / FPS
        QualitySettings.vSyncCount = Current.vSync ? 1 : 0;

        int fps = Current.targetFpsIndex switch
        {
            0 => 30,
            1 => 60,
            2 => 120,
            _ => -1
        };

        // VSync가 켜져 있으면 targetFrameRate는 무시되는 경우가 많다.
        Application.targetFrameRate = Current.vSync ? -1 : fps;
    }

    public void Save()
    {
        string json = JsonUtility.ToJson(Current);
        PlayerPrefs.SetString(KEY, json);
        PlayerPrefs.Save();
    }

    public void Load()
    {
        if (!PlayerPrefs.HasKey(KEY))
        {
            Current = GameSettings.Default();
            return;
        }

        string json = PlayerPrefs.GetString(KEY);
        Current = string.IsNullOrEmpty(json)
            ? GameSettings.Default()
            : (JsonUtility.FromJson<GameSettings>(json) ?? GameSettings.Default());
    }

    public void ResetToDefault()
    {
        Current = GameSettings.Default();
        Apply();
        Save();
    }
}
