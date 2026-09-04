using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Title -> Loading -> Game,
/// Game -> Loading -> Title 흐름을 담당하는 전역 로딩 매니저.
/// 
/// 중요:
/// - 이 오브젝트는 Title 씬의 Managers 아래에 둔다.
/// - Managers 루트에 PersistentManagers를 붙여서 씬 이동 후에도 유지한다.
/// - Loading 씬에는 LoadingManager를 두지 않는다.
/// - Loading 씬 UI는 LoadingUIBinder가 매번 새로 연결한다.
/// 
/// 수정:
/// - 기존 기능 유지
/// - 로딩씬에서 최소 5~8초 머문 뒤 다음 씬 활성화
/// </summary>
[DisallowMultipleComponent]
public class LoadingManager : MonoBehaviour
{
    public static LoadingManager Instance { get; private set; }

    [Header("Scene Names")]
    [SerializeField] private string loadingSceneName = "Loading";
    [SerializeField] private string fallbackSceneName = "Title";

    [Header("Loading Time")]
    [SerializeField] private float minLoadingSeconds = 5f;
    [SerializeField] private float maxLoadingSeconds = 8f;
    [SerializeField] private bool useRandomLoadingTime = true;

    [Header("Loading UI - LoadingUIBinder가 런타임 연결")]
    [SerializeField] private GameObject root;
    [SerializeField] private Slider progressSlider;
    [SerializeField] private Text progressText;
    [SerializeField] private Image loadingImage;

    [Header("Random Loading Images")]
    [SerializeField] private Sprite[] loadingSprites;
    [SerializeField] private float imageChangeInterval = 1.5f;
    [SerializeField] private bool noImmediateRepeat = true;

    [Header("Debug")]
    [SerializeField] private bool verboseLog = true;

    private static string nextSceneName;
    private static int lastImageIndex = -1;

    private bool isLoading;
    private bool transitionRequested;

    private Coroutine loadRoutine;
    private Coroutine imageRoutine;

    public static bool IsLoadingNow
    {
        get
        {
            return Instance != null &&
                   (Instance.isLoading || Instance.transitionRequested);
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (GetComponentInParent<PersistentManagers>() == null)
        {
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }

        Log("Instance 등록 완료");
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static void LoadScene(string targetSceneName)
    {
        if (Instance == null)
        {
            Instance = FindObjectOfType<LoadingManager>(true);

            if (Instance == null)
            {
                Debug.LogError(
                    "LoadingManager.LoadScene 실패: LoadingManager.Instance가 없습니다. " +
                    "Title 씬의 Managers 오브젝트가 DontDestroyOnLoad로 유지되는지 확인하세요."
                );
                return;
            }
        }

        Instance.RequestLoadScene(targetSceneName);
    }

    private void RequestLoadScene(string targetSceneName)
    {
        if (isLoading || transitionRequested)
        {
            Log("이미 로딩 중이므로 중복 요청을 무시합니다.");
            return;
        }

        transitionRequested = true;

        nextSceneName = string.IsNullOrEmpty(targetSceneName)
            ? fallbackSceneName
            : targetSceneName;

        Time.timeScale = 1f;
        AudioListener.pause = false;

        ClearUIReferences();

        Log($"Loading 씬으로 이동 시작 / target={nextSceneName}");

        SceneManager.LoadScene(loadingSceneName);
    }

    public void BindUI(GameObject uiRoot, Slider slider, Text text, Image image)
    {
        root = uiRoot;
        progressSlider = slider;
        progressText = text;
        loadingImage = image;

        if (root != null)
            root.SetActive(true);

        if (progressSlider != null)
        {
            progressSlider.minValue = 0f;
            progressSlider.maxValue = 1f;
            progressSlider.value = 0f;
        }

        if (progressText != null)
            progressText.text = "0%";

        ApplyRandomImageImmediately();

        Log("Loading UI 바인딩 완료");

        if (loadRoutine != null)
            StopCoroutine(loadRoutine);

        loadRoutine = StartCoroutine(CoLoadTargetScene());
    }

    private IEnumerator CoLoadTargetScene()
    {
        isLoading = true;

        if (string.IsNullOrEmpty(nextSceneName))
            nextSceneName = fallbackSceneName;

        StartImageRoutine();

        yield return null;

        float minDisplayTime = GetLoadingWaitSeconds();
        float elapsed = 0f;

        AsyncOperation op = SceneManager.LoadSceneAsync(nextSceneName);

        if (op == null)
        {
            Debug.LogError(
                $"LoadingManager: 씬 로드 실패 / scene={nextSceneName}. Build Settings를 확인하세요.",
                this
            );

            ResetState();
            yield break;
        }

        op.allowSceneActivation = false;

        while (true)
        {
            elapsed += Time.unscaledDeltaTime;

            float timeProgress =
                Mathf.Clamp01(elapsed / Mathf.Max(0.01f, minDisplayTime));

            float loadProgress =
                Mathf.Clamp01(op.progress / 0.9f);

            float progress =
                Mathf.Clamp01(Mathf.Min(timeProgress, loadProgress));

            SetProgress(progress);

            bool loadingReady = op.progress >= 0.9f;
            bool timeReady = elapsed >= minDisplayTime;

            if (loadingReady && timeReady)
                break;

            yield return null;
        }

        SetProgress(1f);

        yield return new WaitForSecondsRealtime(0.15f);

        op.allowSceneActivation = true;

        while (!op.isDone)
            yield return null;

        ResetState();
    }

    private void SetProgress(float value)
    {
        value = Mathf.Clamp01(value);

        if (progressSlider != null)
        {
            progressSlider.minValue = 0f;
            progressSlider.maxValue = 1f;
            progressSlider.value = value;
        }

        if (progressText != null)
        {
            int percent = Mathf.FloorToInt(value * 100f);
            progressText.text = $"{percent}%";
        }
    }

    private float GetLoadingWaitSeconds()
    {
        float min = Mathf.Max(0f, minLoadingSeconds);
        float max = Mathf.Max(min, maxLoadingSeconds);

        if (useRandomLoadingTime)
            return Random.Range(min, max);

        return min;
    }

    private void ApplyRandomImageImmediately()
    {
        if (loadingImage == null)
            return;

        if (loadingSprites == null || loadingSprites.Length == 0)
            return;

        int index = PickImageIndex();

        loadingImage.sprite = loadingSprites[index];
        lastImageIndex = index;
    }

    private void StartImageRoutine()
    {
        StopImageRoutine();

        if (loadingImage == null)
            return;

        if (loadingSprites == null || loadingSprites.Length <= 1)
            return;

        imageRoutine = StartCoroutine(CoChangeImages());
    }

    private IEnumerator CoChangeImages()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(
                Mathf.Max(0.1f, imageChangeInterval)
            );

            if (loadingImage == null)
                yield break;

            int index = PickImageIndex();

            loadingImage.sprite = loadingSprites[index];
            lastImageIndex = index;
        }
    }

    private int PickImageIndex()
    {
        if (loadingSprites == null || loadingSprites.Length == 0)
            return 0;

        if (loadingSprites.Length == 1)
            return 0;

        int index = Random.Range(0, loadingSprites.Length);

        if (noImmediateRepeat)
        {
            int guard = 0;

            while (index == lastImageIndex && guard < 20)
            {
                index = Random.Range(0, loadingSprites.Length);
                guard++;
            }
        }

        return index;
    }

    private void StopImageRoutine()
    {
        if (imageRoutine != null)
        {
            StopCoroutine(imageRoutine);
            imageRoutine = null;
        }
    }

    private void ClearUIReferences()
    {
        root = null;
        progressSlider = null;
        progressText = null;
        loadingImage = null;
    }

    private void ResetState()
    {
        StopImageRoutine();

        isLoading = false;
        transitionRequested = false;
        loadRoutine = null;

        ClearUIReferences();

        Log("로딩 상태 초기화 완료");
    }

    private void Log(string message)
    {
        if (!verboseLog)
            return;

        Debug.Log($"LoadingManager: {message}", this);
    }
}