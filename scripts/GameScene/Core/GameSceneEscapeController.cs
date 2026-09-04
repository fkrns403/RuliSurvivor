using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Game 씬 전용 ESC / Pause / 종료 / 메인 메뉴 이동 컨트롤러.
/// 
/// 역할:
/// - ESC로 PausePanel 열기/닫기
/// - 메인 메뉴 이동 확인 팝업
/// - 게임 종료 확인 팝업
/// - ConfirmPopup이 열려 있으면 ESC 입력을 뒤 패널로 넘기지 않음
/// - Game -> Loading -> Title 이동 처리
/// </summary>
[DisallowMultipleComponent]
public class GameSceneEscapeController : MonoBehaviour
{
    [Header("Pause Panel")]
    [SerializeField] private GameObject pausePanelRoot;

    [Header("Confirm Popup")]
    [SerializeField] private ConfirmPopupUI confirmPopup;

    [Header("Scene")]
    [SerializeField] private string titleSceneName = "Title";

    [Header("Input")]
    [SerializeField] private bool useEscapeKey = true;
    [SerializeField] private float inputCooldownSeconds = 0.1f;

    private float inputCooldownUntil;
    private bool sceneTransitioning;

    private void Awake()
    {
        if (pausePanelRoot != null)
            pausePanelRoot.SetActive(false);
    }

    private void Update()
    {
        if (sceneTransitioning)
            return;

        if (!useEscapeKey)
            return;

        if (Time.unscaledTime < inputCooldownUntil)
            return;

        if (!Input.GetKeyDown(KeyCode.Escape))
            return;

        HandleEscapeKey();

        inputCooldownUntil =
            Time.unscaledTime + Mathf.Max(0f, inputCooldownSeconds);
    }

    private void HandleEscapeKey()
    {
        /*
         * ConfirmPopup이 열려 있으면
         * ESC 입력은 ConfirmPopupUI가 직접 처리한다.
         * 
         * 여기서는 추가 입력 처리 금지.
         * 뒤의 PausePanel이 같이 반응하지 않게 막는다.
         */
        if (confirmPopup != null && confirmPopup.IsOpen)
            return;

        if (IsPausePanelOpen())
        {
            OnClickResume();
            return;
        }

        OpenPausePanel();
    }

    public void OpenPausePanel()
    {
        if (sceneTransitioning)
            return;

        GameManager gm = GameManager.Instance;

        if (gm != null)
            gm.PauseGame();
        else
            Time.timeScale = 0f;

        if (pausePanelRoot != null)
            pausePanelRoot.SetActive(true);
    }

    public void OnClickResume()
    {
        if (sceneTransitioning)
            return;

        if (confirmPopup != null && confirmPopup.IsOpen)
            confirmPopup.Close();

        if (pausePanelRoot != null)
            pausePanelRoot.SetActive(false);

        ResumeGameSafely();
        PlaySelectSfx();
    }

    public void PauseAndOpenMainMenuPopup()
    {
        if (sceneTransitioning)
            return;

        GameManager gm = GameManager.Instance;

        if (gm != null)
            gm.PauseGame();
        else
            Time.timeScale = 0f;

        if (pausePanelRoot != null)
            pausePanelRoot.SetActive(false);

        if (confirmPopup == null)
        {
            GoToTitle();
            return;
        }

        confirmPopup.Open(
            "메인 메뉴",
            "타이틀 화면으로 이동하시겠습니까?",
            GoToTitle,
            ReopenPausePanelOnly
        );

        PlaySelectSfx();
    }

    public void PauseAndOpenExitPopup()
    {
        if (sceneTransitioning)
            return;

        GameManager gm = GameManager.Instance;

        if (gm != null)
            gm.PauseGame();
        else
            Time.timeScale = 0f;

        if (pausePanelRoot != null)
            pausePanelRoot.SetActive(false);

        if (confirmPopup == null)
        {
            QuitGame();
            return;
        }

        confirmPopup.Open(
            "게임 종료",
            "게임을 종료하시겠습니까?",
            QuitGame,
            ReopenPausePanelOnly
        );

        PlaySelectSfx();
    }

    private void GoToTitle()
    {
        if (sceneTransitioning)
            return;

        sceneTransitioning = true;

        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (pausePanelRoot != null)
            pausePanelRoot.SetActive(false);

        if (confirmPopup != null && confirmPopup.IsOpen)
            confirmPopup.Close();

        GameManager gm = GameManager.Instance;

        if (gm != null)
            gm.StopGame();

        if (LoadingManager.Instance != null)
        {
            LoadingManager.LoadScene(titleSceneName);
            return;
        }

        Debug.LogError(
            "GameSceneEscapeController: LoadingManager.Instance가 없어 직접 Title 씬으로 이동합니다. " +
            "Managers/PersistentManagers 세팅을 확인하세요.",
            this
        );

        SceneManager.LoadScene(titleSceneName);
    }

    private void QuitGame()
    {
        if (sceneTransitioning)
            return;

        sceneTransitioning = true;

        Time.timeScale = 1f;
        AudioListener.pause = false;

        GameManager gm = GameManager.Instance;

        if (gm != null)
            gm.StopGame();

        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void ReopenPausePanelOnly()
    {
        if (sceneTransitioning)
            return;

        GameManager gm = GameManager.Instance;

        if (gm != null)
            gm.PauseGame();
        else
            Time.timeScale = 0f;

        if (pausePanelRoot != null)
            pausePanelRoot.SetActive(true);
    }

    private void ResumeGameSafely()
    {
        GameManager gm = GameManager.Instance;

        if (gm != null)
        {
            gm.ResumeGame();
            return;
        }

        Time.timeScale = 1f;
    }

    private bool IsPausePanelOpen()
    {
        return pausePanelRoot != null && pausePanelRoot.activeSelf;
    }

    private void PlaySelectSfx()
    {
        if (AudioManager.instance != null)
            AudioManager.instance.PlaySfx(AudioManager.Sfx.Select);
    }
}