using UnityEngine;
using UnityEngine.SceneManagement;

public class EscapePopupOpener : MonoBehaviour
{
    [Header("Title")]
    [SerializeField] private bool enableInTitleScene = true;
    [SerializeField] private string titleSceneName = "Title";
    [SerializeField] private TitleExitController titleExitController;
    [SerializeField] private ConfirmPopupUI titleConfirmPopup;

    [Header("Toggle Options")]
    [SerializeField] private bool allowCloseWithEscWhenPopupOpen = true;
    [SerializeField] private float inputCooldownSeconds = 0.1f;

    private float _cooldownUntil;

    private void Update()
    {
        if (Time.unscaledTime < _cooldownUntil)
            return;

        if (!Input.GetKeyDown(KeyCode.Escape))
            return;

        // 타이틀 씬에서만 동작
        if (!enableInTitleScene)
            return;

        if (SceneManager.GetActiveScene().name != titleSceneName)
            return;

        // 이미 팝업이 열려 있으면 ESC로 닫기(원하면)
        if (titleConfirmPopup != null && titleConfirmPopup.IsOpen)
        {
            if (allowCloseWithEscWhenPopupOpen)
            {
                titleConfirmPopup.Close();
                _cooldownUntil = Time.unscaledTime + inputCooldownSeconds;
            }
            return;
        }

        // ESC로 열 때도 "버튼 클릭과 동일한 경로"로 열기
        if (titleExitController != null)
        {
            titleExitController.OnClickExit();
            _cooldownUntil = Time.unscaledTime + inputCooldownSeconds;
            return;
        }

        // 백업: 컨트롤러가 없으면 팝업이라도 열기(단, 이 경우 confirm 콜백이 없으니 종료는 안 됨)
        if (titleConfirmPopup != null)
        {
            titleConfirmPopup.Open("게임 종료", "정말 게임을 종료하시겠습니까?", null, null);
            _cooldownUntil = Time.unscaledTime + inputCooldownSeconds;
        }
    }
}
