using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject pausePanelRoot;
    [SerializeField] private ConfirmPopupUI confirmPopup;

    [Header("Scene")]
    [SerializeField] private string titleSceneName = "Title";

    [Header("ESC Behavior")]
    [SerializeField] private bool escTogglesPause = true;

    private bool paused;

    private void Awake()
    {
        if (pausePanelRoot != null) pausePanelRoot.SetActive(false);
        if (confirmPopup != null) confirmPopup.Close();

        ResumeInternal();
    }

    private void Update()
    {
        if (!escTogglesPause) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (confirmPopup != null && confirmPopup.IsOpen)
            {
                confirmPopup.Close();
                return;
            }

            if (!paused) Pause();
            else Resume();
        }
    }

    public void Pause()
    {
        paused = true;

        if (pausePanelRoot != null) pausePanelRoot.SetActive(true);

        Time.timeScale = 0f;
        AudioListener.pause = false;
    }

    public void Resume()
    {
        ResumeInternal();
    }

    private void ResumeInternal()
    {
        paused = false;

        if (pausePanelRoot != null) pausePanelRoot.SetActive(false);

        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

    public void OnClickResume()
    {
        Resume();
    }

    public void OnClickMainMenu()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (!string.IsNullOrEmpty(titleSceneName))
            SceneManager.LoadScene(titleSceneName);
    }

    public void OnClickExitGame()
    {
        if (confirmPopup == null)
        {
            QuitGame();
            return;
        }

        confirmPopup.Open(
            "게임 종료",
            "진행 중인 게임을 종료하시겠습니까?",
            QuitGame,
            null
        );
    }

    private void QuitGame()
    {
        Time.timeScale = 1f;

        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
