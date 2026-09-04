using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 몬스터 도감에서 "카드 한 장"을 표현하는 컴포넌트.
/// - UnlockDefinition 데이터를 기준으로 잠금/해금 UI를 전환한다.
/// - 잠금 상태에서는 이름을 숨기고 "???"로 표시한다.
/// - 해금 상태에서는 실제 이름과 설명을 표시한다.
/// </summary>
public class MonsterCodexCard : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Text nameText;
    [SerializeField] private Text descriptionText;

    [Header("잠금 표시")]
    [Tooltip("몬스터가 아직 해금되지 않았을 때 표시할 이름")]
    [SerializeField] private string lockedNameText = "???";

    [Header("색상 설정")]
    [SerializeField] private Color unlockedIconColor = Color.white;
    [SerializeField] private Color lockedIconColor = Color.black;

    private UnlockDefinition definition;

    /// <summary>
    /// 카드에 사용할 UnlockDefinition을 지정하고 즉시 UI를 갱신한다.
    /// MonsterCodexPanel에서 카드 생성 직후 호출된다.
    /// </summary>
    public void Setup(UnlockDefinition def)
    {
        definition = def;
        Refresh();
    }

    /// <summary>
    /// 현재 해금 상태에 맞춰 카드 UI를 다시 그린다.
    /// </summary>
    public void Refresh()
    {
        if (definition == null)
            return;

        bool isUnlocked = false;

        if (UnlockManager.Instance != null)
            isUnlocked = UnlockManager.Instance.IsUnlocked(definition.id);

        // 이름 표시
        // 해금 전: ???
        // 해금 후: 실제 몬스터 이름
        if (nameText != null)
        {
            nameText.text = isUnlocked
                ? definition.displayName
                : lockedNameText;
        }

        // 아이콘 표시
        // 해금 전: 실루엣 색
        // 해금 후: 원래 색
        if (iconImage != null)
        {
            if (definition.icon != null)
                iconImage.sprite = definition.icon;

            iconImage.color = isUnlocked ? unlockedIconColor : lockedIconColor;
        }

        // 설명 표시
        // 해금 전: 해금 조건
        // 해금 후: 몬스터 설명
        if (descriptionText != null)
        {
            if (isUnlocked)
            {
                descriptionText.text =
                    string.IsNullOrEmpty(definition.unlockedDescription)
                        ? string.Empty
                        : definition.unlockedDescription;
            }
            else
            {
                descriptionText.text =
                    string.IsNullOrEmpty(definition.lockedDescription)
                        ? "해금 조건이 없습니다."
                        : definition.lockedDescription;
            }
        }
    }
}