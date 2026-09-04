using UnityEngine;

/// <summary>
/// SettingsPanelUI를 열고 닫는 공용 매니저.
/// - 타이틀에서는 버튼으로 OpenOptions() 호출
/// - 게임 중에는 별도 입력으로 열고 싶으면 ToggleOptions()를 호출하도록 구성
///
/// 주의
/// - 이 스크립트에서 ESC 입력을 처리하면, 다른 UI(종료 팝업 등)과 충돌할 수 있다.
/// - 타이틀에서 설정 버튼으로만 열 계획이면 Update에서 키 입력을 제거하는 것이 안전하다.
/// </summary>
public class SettingsUIManager : MonoBehaviour
{
    public static SettingsUIManager Instance { get; private set; }

    [SerializeField] private SettingsPanelUI settingsPanel;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (settingsPanel == null)
            settingsPanel = FindObjectOfType<SettingsPanelUI>(true);
    }

    // 타이틀에서 ESC로 열지 않을 거면 Update 자체를 없애는 게 가장 안전하다.
    // private void Update()
    // {
    //     if (Input.GetKeyDown(KeyCode.Escape))
    //         ToggleOptions();
    // }

    public void ToggleOptions()
    {
        if (settingsPanel == null) return;

        if (settingsPanel.IsOpen) settingsPanel.Close();
        else settingsPanel.Open();
    }

    public void OpenOptions()
    {
        if (settingsPanel == null) return;
        settingsPanel.Open();
    }

    public void CloseOptions()
    {
        if (settingsPanel == null) return;
        settingsPanel.Close();
    }
}
