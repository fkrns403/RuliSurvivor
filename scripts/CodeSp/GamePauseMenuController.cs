using UnityEngine;
using UnityEngine.SceneManagement;

public class GamePauseMenuController : MonoBehaviour
{
    [SerializeField] private GameObject pausePanelRoot; // 2번 이미지의 “일시정지” 패널
    [SerializeField] private ConfirmExitPopup exitPopup; // “게임을 종료하시겠습니까?” 확인 팝업
    [SerializeField] private string titleSceneName = "Title";

    private bool isPaused;

    private void Awake()
    {
        if (pausePanelRoot != null)
            pausePanelRoot.SetActive(false);

        if (exitPopup != null)
        {
            exitPopup.Bind(
                onCancel: null,
                onConfirm: QuitToTitleOrExit
            );
        }
    }

    public void TogglePause()
    {
        if (isPaused) Resume();
        else Pause();
    }

    public void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;

        if (pausePanelRoot != null)
            pausePanelRoot.SetActive(true);
    }

    public void Resume()
    {
        // Exit 확인 팝업이 열려있으면 먼저 닫기
        if (exitPopup != null && exitPopup.IsOpen)
            exitPopup.Close();

        isPaused = false;
        Time.timeScale = 1f;

        if (pausePanelRoot != null)
            pausePanelRoot.SetActive(false);
    }

    public void OpenExitPopup()
    {
        // 일시정지 상태에서만 열리는게 자연스러움
        if (!isPaused) Pause();
        if (exitPopup != null) exitPopup.Open();
    }

    public void GoToTitle()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(titleSceneName);
    }

    private void QuitToTitleOrExit()
    {
        // Game 씬에서 Exit는 보통 타이틀로 돌아가게 하는 게 안전
        GoToTitle();
        // 진짜 앱 종료를 원하면 아래로 교체
        /*
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
        */
    }
}
