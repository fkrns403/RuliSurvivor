using UnityEngine;

public class CodexPopup : MonoBehaviour
{
    [Header("팝업 루트 (CodexIn 전체)")]
    [SerializeField] private GameObject popupRoot;

    [Header("도감 루트 오브젝트(탭 단위)")]
    [SerializeField] private GameObject characterCodexRoot; // CodexIn/CharacterCodex
    [SerializeField] private GameObject monsterCodexRoot;   // CodexIn/MonsterCodex

    [Header("도감 패널 스크립트(각 루트 내부에 붙어 있음)")]
    [SerializeField] private CharacterCodexPanel characterCodexPanel;
    [SerializeField] private MonsterCodexPanel monsterCodexPanel;

    private void Awake()
    {
        // 시작 시 겹침 방지: 전부 OFF
        if (popupRoot != null) popupRoot.SetActive(false);
        if (characterCodexRoot != null) characterCodexRoot.SetActive(false);
        if (monsterCodexRoot != null) monsterCodexRoot.SetActive(false);
    }

    public void Open() => OpenWithCharacter();

    public void OpenWithCharacter()
    {
        if (popupRoot != null) popupRoot.SetActive(true);
        ShowCharacterCodex();
    }

    public void OpenWithMonster()
    {
        if (popupRoot != null) popupRoot.SetActive(true);
        ShowMonsterCodex();
    }

    public void Close()
    {
        // 닫을 때도 루트 꺼서 다음 오픈 때 상태 꼬임 방지
        if (characterCodexRoot != null) characterCodexRoot.SetActive(false);
        if (monsterCodexRoot != null) monsterCodexRoot.SetActive(false);
        if (popupRoot != null) popupRoot.SetActive(false);
    }

    public void ShowCharacterCodex()
    {
        // 루트 토글로 겹침 완전 차단
        if (characterCodexRoot != null) characterCodexRoot.SetActive(true);
        if (monsterCodexRoot != null) monsterCodexRoot.SetActive(false);

        // 데이터/화면 갱신
        if (characterCodexPanel != null)
            characterCodexPanel.RefreshAll();
    }

    public void ShowMonsterCodex()
    {
        if (monsterCodexRoot != null) monsterCodexRoot.SetActive(true);
        if (characterCodexRoot != null) characterCodexRoot.SetActive(false);

        if (monsterCodexPanel != null)
            monsterCodexPanel.BuildOrRefresh();
    }
}
