using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 캐릭터 선택 슬롯 하나를 담당하는 스크립트.
/// 
/// 역할:
/// - UnlockManager와 UnlockDefinition을 참고해서 잠금/해금 상태를 UI에 반영한다.
/// - 해금 상태면 버튼 활성화.
/// - 잠금 상태면 버튼 비활성화.
/// 
/// 이번 수정:
/// - 기존에는 UnlockManager.IsUnlocked(unlockKey)만 확인했다.
/// - 이 경우 UnlockDefinition이 UnlockManager.allDefinitions에 빠져 있으면 조건을 만족해도 열리지 않을 수 있다.
/// - 이제 unlockDefinition이 연결되어 있으면 UnlockManager.IsUnlockedOrEvaluate(unlockDefinition)을 먼저 사용한다.
/// </summary>
[RequireComponent(typeof(Button))]
public class CharacterSelectSlot : MonoBehaviour
{
    [Header("해금 키 / 정의")]
    [Tooltip("UnlockDefinition.id 와 동일해야 한다. 예: 'nyarubi', 'linoa'")]
    [SerializeField] private string unlockKey;

    [Tooltip("이 슬롯과 연결된 UnlockDefinition 에셋")]
    [SerializeField] private UnlockDefinition unlockDefinition;

    [Header("TitleManager 연동")]
    [Tooltip("캐릭터 선택을 실제로 처리하는 TitleManager")]
    [SerializeField] private TitleManager titleManager;

    [Tooltip("TitleManager.characters 배열에서 이 슬롯이 가리킬 인덱스")]
    [SerializeField] private int characterIndex = 0;

    [Header("UI 참조")]
    [Tooltip("슬롯 전체 버튼")]
    [SerializeField] private Button button;

    [Tooltip("배경 패널 이미지")]
    [SerializeField] private Image panelImage;

    [Tooltip("캐릭터 아이콘 이미지")]
    [SerializeField] private Image iconImage;

    [Tooltip("캐릭터 이름 텍스트")]
    [SerializeField] private Text nameText;

    [Tooltip("슬롯 하단 설명 텍스트")]
    [SerializeField] private Text descriptionText;

    [Header("색상 설정")]
    [SerializeField] private Color unlockedIconColor = Color.white;
    [SerializeField] private Color lockedIconColor = Color.black;

    [SerializeField]
    private Color unlockedPanelColor =
        new Color32(0xFF, 0xA0, 0xF1, 0xFF);

    [SerializeField]
    private Color lockedPanelColor =
        new Color32(0x9F, 0x9F, 0x9F, 0xFF);

    private void Reset()
    {
        button = GetComponent<Button>();
    }

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        SyncFromDefinition();

        if (button != null)
            button.onClick.AddListener(OnClick);
    }

    private void OnEnable()
    {
        SyncFromDefinition();
        Refresh();

        if (UnlockManager.Instance != null)
            UnlockManager.Instance.OnStateChanged += OnUnlockStateChanged;
    }

    private void OnDisable()
    {
        if (UnlockManager.Instance != null)
            UnlockManager.Instance.OnStateChanged -= OnUnlockStateChanged;
    }

    private void SyncFromDefinition()
    {
        if (unlockDefinition == null)
            return;

        if (string.IsNullOrEmpty(unlockKey))
            unlockKey = unlockDefinition.id;

        if (nameText != null && !string.IsNullOrEmpty(unlockDefinition.displayName))
            nameText.text = unlockDefinition.displayName;

        if (iconImage != null && unlockDefinition.icon != null)
            iconImage.sprite = unlockDefinition.icon;
    }

    private void OnUnlockStateChanged()
    {
        Refresh();
    }

    public void Refresh()
    {
        bool isUnlocked = false;

        if (characterIndex == 0)
        {
            isUnlocked = true;
        }
        else if (UnlockManager.Instance != null)
        {
            if (unlockDefinition != null)
            {
                isUnlocked = UnlockManager.Instance.IsUnlockedOrEvaluate(unlockDefinition);
            }
            else if (!string.IsNullOrEmpty(unlockKey))
            {
                isUnlocked = UnlockManager.Instance.IsUnlocked(unlockKey);
            }
        }

        if (button != null)
            button.interactable = isUnlocked;

        if (panelImage != null)
            panelImage.color = isUnlocked ? unlockedPanelColor : lockedPanelColor;

        if (iconImage != null)
        {
            if (unlockDefinition != null && unlockDefinition.icon != null)
                iconImage.sprite = unlockDefinition.icon;

            iconImage.color = isUnlocked ? unlockedIconColor : lockedIconColor;
        }

        if (nameText != null && unlockDefinition != null &&
            !string.IsNullOrEmpty(unlockDefinition.displayName))
        {
            nameText.text = unlockDefinition.displayName;
        }

        if (descriptionText != null)
        {
            if (unlockDefinition != null)
            {
                if (isUnlocked)
                {
                    descriptionText.text =
                        string.IsNullOrEmpty(unlockDefinition.unlockedDescription)
                            ? string.Empty
                            : unlockDefinition.unlockedDescription;
                }
                else
                {
                    descriptionText.text =
                        string.IsNullOrEmpty(unlockDefinition.lockedDescription)
                            ? "개발중"
                            : unlockDefinition.lockedDescription;
                }
            }
            else
            {
                descriptionText.text = isUnlocked ? string.Empty : "개발중";
            }
        }
    }

    private void OnClick()
    {
        if (titleManager == null)
        {
            Debug.LogWarning("[CharacterSelectSlot] TitleManager 가 설정되어 있지 않습니다.", this);
            return;
        }

        if (button != null && !button.interactable)
            return;

        titleManager.SelectCharacter(characterIndex);
    }
}