using UnityEngine;

public class TitleExitController : MonoBehaviour
{
    [SerializeField] private ConfirmPopupUI confirmPopup;

    public void OnClickExit()
    {
        if (confirmPopup == null)
        {
            QuitGame();
            return;
        }

        confirmPopup.Open(
            "게임 종료",
            "정말 게임을 종료하시겠습니까?",
            QuitGame,
            null
        );
    }

    private void QuitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
