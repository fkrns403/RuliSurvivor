using UnityEngine;

/// <summary>
/// 전역 저장 보조 매니저.
/// 
/// 배치 위치:
/// - Title 씬 Managers 아래.
/// - DontDestroyOnLoad로 Loading, Game 씬까지 유지한다.
/// 
/// 역할:
/// - PlayerPrefs 저장/로드 보조
/// - 공통 저장 함수 제공
/// - 테스트용 저장 초기화 보조
/// 
/// 주의:
/// - GameManager, Player, Boss, Canvas 같은 씬 오브젝트를 참조하지 않는다.
/// - 씬 이동 시 사라지는 오브젝트를 멤버 변수로 들고 있지 않는다.
/// - 실제 언락 판정은 UnlockManager가 담당한다.
/// - 보스 진행도는 BossUnlock이 담당한다.
/// </summary>
[DisallowMultipleComponent]
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [Header("Debug")]
    [SerializeField] private bool verboseLog = true;

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

    public void SaveInt(string key, int value)
    {
        if (string.IsNullOrEmpty(key))
            return;

        PlayerPrefs.SetInt(key, value);
        PlayerPrefs.Save();

        Log($"Int 저장 / key={key}, value={value}");
    }

    public int LoadInt(string key, int defaultValue = 0)
    {
        if (string.IsNullOrEmpty(key))
            return defaultValue;

        return PlayerPrefs.GetInt(key, defaultValue);
    }

    public void SaveFloat(string key, float value)
    {
        if (string.IsNullOrEmpty(key))
            return;

        PlayerPrefs.SetFloat(key, value);
        PlayerPrefs.Save();

        Log($"Float 저장 / key={key}, value={value}");
    }

    public float LoadFloat(string key, float defaultValue = 0f)
    {
        if (string.IsNullOrEmpty(key))
            return defaultValue;

        return PlayerPrefs.GetFloat(key, defaultValue);
    }

    public void SaveString(string key, string value)
    {
        if (string.IsNullOrEmpty(key))
            return;

        PlayerPrefs.SetString(key, value);
        PlayerPrefs.Save();

        Log($"String 저장 / key={key}, value={value}");
    }

    public string LoadString(string key, string defaultValue = "")
    {
        if (string.IsNullOrEmpty(key))
            return defaultValue;

        return PlayerPrefs.GetString(key, defaultValue);
    }

    public bool HasKey(string key)
    {
        if (string.IsNullOrEmpty(key))
            return false;

        return PlayerPrefs.HasKey(key);
    }

    public void DeleteKey(string key)
    {
        if (string.IsNullOrEmpty(key))
            return;

        PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();

        Log($"Key 삭제 / key={key}");
    }

    /// <summary>
    /// 개발 테스트용 전체 PlayerPrefs 초기화.
    /// 출시 빌드에서는 버튼으로 노출하지 않는 것을 권장한다.
    /// </summary>
    public void ClearAllSaveDataForTest()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        Log("전체 저장 데이터 초기화");
    }

    private void Log(string message)
    {
        if (!verboseLog)
            return;

        Debug.Log($"SaveManager: {message}", this);
    }
}