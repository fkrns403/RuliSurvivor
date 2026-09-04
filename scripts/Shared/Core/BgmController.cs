using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 씬 전환 시 자동으로 BGM을 바꿔주는 컨트롤러.
/// - AudioManager는 DontDestroyOnLoad이므로 계속 유지된다.
/// - 이 스크립트는 씬 이름을 보고 해당 씬의 BGM으로 교체 후 재생한다.
///
/// 주의
/// - AudioManager.bgmClip 변수만 바꾸면 실제 AudioSource.clip이 바뀌지 않는다.
/// - 따라서 AudioManager.SetBgmClip(clip)을 사용해야 한다.
/// </summary>
public class BgmController : MonoBehaviour
{
    [Header("씬 이름 기준 BGM")]
    [SerializeField] private string titleSceneName = "Title";
    [SerializeField] private AudioClip titleBgm;

    [SerializeField] private string gameSceneName = "Game";
    [SerializeField] private AudioClip gameBgm;

    private string lastScene;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        // 시작 씬도 한 번 처리
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (AudioManager.instance == null) return;

        if (scene.name == lastScene) return;
        lastScene = scene.name;

        if (scene.name == titleSceneName && titleBgm != null)
        {
            AudioManager.instance.SetBgmClip(titleBgm, play: true);
        }
        else if (scene.name == gameSceneName && gameBgm != null)
        {
            AudioManager.instance.SetBgmClip(gameBgm, play: true);
        }

        // 설정값 재적용(볼륨 꼬임 방지)
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.Apply();
    }
}
