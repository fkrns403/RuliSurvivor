using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Title 씬에서 생성된 전역 매니저 루트를 씬 전환 후에도 유지한다.
/// 
/// 유지 대상:
/// - LoadingManager
/// - AudioManager
/// - SettingsManager
/// - UnlockManager
/// - BgmController
/// 
/// 유지하면 안 되는 대상:
/// - GameManager
/// - GameBootstrap
/// - PoolManager
/// - DropManager
/// - GameFlow
/// 
/// 사용 위치:
/// - Title 씬의 Managers 오브젝트에 붙인다.
/// </summary>
[DisallowMultipleComponent]
public class PersistentManagers : MonoBehaviour
{
    private static PersistentManagers instance;

    [Header("Debug")]
    [SerializeField] private bool verboseLog = true;

    public static bool Exists => instance != null;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            if (verboseLog)
                Debug.Log("PersistentManagers: 중복 Managers가 발견되어 새 오브젝트를 제거합니다.", this);

            Destroy(gameObject);
            return;
        }

        instance = this;

        // DontDestroyOnLoad는 루트 오브젝트에 적용하는 것이 가장 안전하다.
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;

        if (verboseLog)
            Debug.Log("PersistentManagers: Managers 루트가 DontDestroyOnLoad로 등록되었습니다.", this);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            instance = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!verboseLog)
            return;

        Debug.Log($"PersistentManagers: 씬 이동 후 유지 중 / scene={scene.name}", this);
    }
}