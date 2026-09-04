using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 레벨업 UI의 선택 슬롯 하나를 담당한다.
/// 
/// 역할:
/// - LevelUpUI가 넘겨준 ItemData를 화면에 표시한다.
/// - 버튼 클릭 시 PlayerItemApplier에 아이템 적용을 요청한다.
/// - 적용 성공 시 LevelUpUI에 선택 완료를 알린다.
/// - 선택 완료 후 LevelUpUI를 닫는다.
/// 
/// 수정 핵심:
/// - PlayerItemApplier 탐색을 강화한다.
/// - GameManager.PlayerTransform, Player 태그, 씬 전체 검색 순서로 찾는다.
/// - 실패 시 명확한 로그를 출력한다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class Item : MonoBehaviour
{
    [Header("Data")]
    public ItemData data;

    [Header("Runtime")]
    public int level;

    [Header("UI")]
    [SerializeField] private Image icon;
    [SerializeField] private Text textLevel;
    [SerializeField] private Text textName;
    [SerializeField] private Text textDesc;

    [Header("Debug")]
    [SerializeField] private bool verboseLog = true;

    private Button buttonCache;
    private LevelUpUI ownerUI;

    private void Awake()
    {
        buttonCache = GetComponent<Button>();

        if (icon == null)
        {
            Image[] images = GetComponentsInChildren<Image>(true);

            if (images.Length > 0)
                icon = images[images.Length - 1];
        }

        if (textLevel == null || textName == null || textDesc == null)
        {
            Text[] texts = GetComponentsInChildren<Text>(true);

            if (texts.Length >= 3)
            {
                textLevel = texts[0];
                textName = texts[1];
                textDesc = texts[2];
            }
        }

        if (buttonCache != null)
        {
            buttonCache.onClick.RemoveAllListeners();
            buttonCache.onClick.AddListener(onClick);
        }
    }

    private void OnEnable()
    {
        RefreshUI();
    }

    public void SetItem(ItemData newData, int currentLevel, LevelUpUI owner)
    {
        data = newData;
        level = Mathf.Max(0, currentLevel);
        ownerUI = owner;

        RefreshUI();
    }

    public void RefreshUI()
    {
        if (data == null)
        {
            ClearUI();

            if (buttonCache != null)
                buttonCache.interactable = false;

            return;
        }

        int nextRealLevel = GetNextRealLevel();

        if (icon != null)
            icon.sprite = data.itemIcon;

        if (textName != null)
            textName.text = data.itemName;

        if (textLevel != null)
            textLevel.text = "Lv." + nextRealLevel;

        if (textDesc != null)
            textDesc.text = BuildDescriptionSafe(nextRealLevel);

        if (buttonCache != null)
            buttonCache.interactable = data.CanLevelUpFrom(level);
    }

    private void ClearUI()
    {
        if (icon != null)
            icon.sprite = null;

        if (textName != null)
            textName.text = string.Empty;

        if (textLevel != null)
            textLevel.text = string.Empty;

        if (textDesc != null)
            textDesc.text = string.Empty;
    }

    private int GetNextRealLevel()
    {
        return Mathf.Max(1, level + 1);
    }

    private string BuildDescriptionSafe(int realLevel)
    {
        if (data == null)
            return string.Empty;

        string desc = data.itemDesc ?? string.Empty;

        try
        {
            switch (data.ItemType)
            {
                case ItemType.Weapon_Melee:
                case ItemType.Weapon_Orbit:
                case ItemType.Weapon_Range:
                case ItemType.Weapon_Shotgun:
                case ItemType.Weapon_Arena:
                case ItemType.Weapon_Fuga:
                case ItemType.Weapon_Mace:
                case ItemType.Weapon_MapleSword:
                case ItemType.Weapon_Drain:
                case ItemType.Weapon_Pierce:
                case ItemType.Weapon_PoisonOrb:
                case ItemType.Weapon_Lightning:
                    {
                        float damagePercent = data.GetDamagePercentIncreaseAtLevel(realLevel);
                        int countBonus = data.GetCountDisplayValueAtLevel(realLevel);

                        return string.Format(
                            desc,
                            Mathf.RoundToInt(damagePercent),
                            countBonus
                        );
                    }

                case ItemType.Equip_Glove:
                case ItemType.Equip_Shoe:
                case ItemType.Equip_Heal:
                case ItemType.Equip_Jewel:
                case ItemType.Equip_Breadbone:
                case ItemType.Equip_Regeneration:
                    {
                        float value = data.GetDamageDisplayValueAtLevel(realLevel);
                        int percent = Mathf.RoundToInt(value * 100f);

                        return string.Format(desc, percent);
                    }

                default:
                    return desc;
            }
        }
        catch
        {
            return desc;
        }
    }

    public void onClick()
    {
        if (!TryApplySelection())
            return;

        if (ownerUI == null)
            ownerUI = GetComponentInParent<LevelUpUI>(true);

        if (ownerUI != null)
        {
            ownerUI.NotifyItemSelected(data);
            ownerUI.Hide();
        }
    }

    private bool TryApplySelection()
    {
        if (data == null)
        {
            LogWarning("선택할 ItemData가 없습니다.");
            return false;
        }

        if (!data.CanLevelUpFrom(level))
        {
            LogWarning($"레벨업 불가 아이템입니다. item={data.itemName}, currentLevel={level}");
            return false;
        }

        PlayerItemApplier applier = ResolvePlayerApplier();

        if (applier == null)
        {
            LogWarning("PlayerItemApplier를 찾지 못했습니다. 플레이어 프리팹에 PlayerItemApplier가 있는지 확인하세요.");
            return false;
        }

        bool result = applier.Apply(data, level);

        if (!result)
            LogWarning($"PlayerItemApplier.Apply 실패. item={data.itemName}, level={level}");

        return result;
    }

    private PlayerItemApplier ResolvePlayerApplier()
    {
        if (GameManager.Instance != null && GameManager.Instance.PlayerTransform != null)
        {
            PlayerItemApplier applier = GameManager.Instance.PlayerTransform.GetComponent<PlayerItemApplier>();

            if (applier != null)
                return applier;
        }

        GameObject player = GameObject.FindWithTag("Player");

        if (player != null)
        {
            PlayerItemApplier applier = player.GetComponent<PlayerItemApplier>();

            if (applier != null)
                return applier;
        }

        PlayerItemApplier found = FindObjectOfType<PlayerItemApplier>(true);

        if (found != null)
            return found;

        return null;
    }

    private void LogWarning(string message)
    {
        if (!verboseLog)
            return;

        Debug.LogWarning($"Item: {message}", this);
    }
}