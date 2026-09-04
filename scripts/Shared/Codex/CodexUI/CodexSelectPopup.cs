using UnityEngine;

/// <summary>
/// 도감 선택 팝업(파란 창)에 붙는 스크립트.
/// - 자기 자신(Codex)을 켜고/끄는 역할.
/// - 버튼을 누르면 CodexIn(CodexPopup)을 열어주고, 선택 팝업은 닫는다.
/// </summary>
public class CodexSelectPopup : MonoBehaviour
{
    [Header("선택 팝업 루트 (보통 Codex 자기 자신)")]
    [SerializeField] private GameObject selectRoot;

    [Header("실제 도감 페이지 팝업 (CodexIn에 붙어 있는 CodexPopup)")]
    [SerializeField] private CodexPopup codexPopup;

    private void Reset()
    {
        selectRoot = gameObject;
    }

    /// <summary>
    /// 타이틀의 '도감' 버튼에서 호출.
    /// </summary>
    public void Open()
    {
        if (selectRoot != null)
            selectRoot.SetActive(true);
    }

    /// <summary>
    /// X 버튼에서 호출. 선택 팝업만 닫는다.
    /// </summary>
    public void Close()
    {
        if (selectRoot != null)
            selectRoot.SetActive(false);
    }

    /// <summary>
    /// '캐릭터 도감' 버튼에서 호출.
    /// 선택 팝업을 닫고, CodexIn을 캐릭터 도감 상태로 연다.
    /// </summary>
    public void OpenCharacterCodex()
    {
        if (codexPopup != null)
            codexPopup.OpenWithCharacter();

        Close();
    }

    /// <summary>
    /// '몬스터 도감' 버튼에서 호출.
    /// 선택 팝업을 닫고, CodexIn을 몬스터 도감 상태로 연다.
    /// </summary>
    public void OpenMonsterCodex()
    {
        if (codexPopup != null)
            codexPopup.OpenWithMonster();

        Close();
    }
}
