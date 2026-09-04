using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 레벨업 선택 UI.
/// 
/// 역할:
/// - GameFlow가 GameManager.OnLevelUp 이벤트를 받으면 Show()를 호출한다.
/// - allItems에서 현재 레벨업 가능한 후보를 만든다.
/// - 후보 중 optionCount만큼 뽑아 Item 슬롯에 넣는다.
/// - UI가 열리면 게임을 일시정지하고, 선택 후 닫히면 재개한다.
/// 
/// 이번 수정 핵심:
/// - root가 비활성화되어 있어도 정상적으로 켜질 수 있게 처리한다.
/// - 후보가 없을 때 명확한 로그를 남긴다.
/// - 슬롯이 비활성화된 상태여도 SetActive(true) 후 SetItem한다.
/// </summary>
[DisallowMultipleComponent]
public class LevelUpUI : MonoBehaviour
{
    [Header("Root")]
    [Tooltip("레벨업 UI 전체 루트. 보통 Levelup 오브젝트 자신을 넣는다.")]
    [SerializeField] private GameObject root;

    [Header("Item Pool")]
    [Tooltip("레벨업 선택지로 등장할 전체 ItemData 목록")]
    [SerializeField] private ItemData[] allItems;

    [Header("Slots")]
    [Tooltip("화면에 보이는 고정 선택 슬롯들")]
    [SerializeField] private Item[] itemSlots;

    [Header("Option Count")]
    [SerializeField] private int optionCount = 4;

    [Header("Fallback Options")]
    [SerializeField] private bool allowFallbackOptions = true;
    [SerializeField] private ItemData[] fallbackItems;

    [Header("Debug")]
    [SerializeField] private bool verboseLog = true;

    private readonly Dictionary<ItemData, int> itemLevels = new Dictionary<ItemData, int>();
    private readonly List<ItemData> candidateBuffer = new List<ItemData>();
    private readonly List<ItemData> selectedBuffer = new List<ItemData>();

    private bool isOpen;

    private void Awake()
    {
        if (root == null)
            root = gameObject;

        AutoBindSlotsIfEmpty();
        ForceHide();
    }

    private void AutoBindSlotsIfEmpty()
    {
        if (itemSlots != null && itemSlots.Length > 0)
            return;

        itemSlots = GetComponentsInChildren<Item>(true);

        if (itemSlots == null || itemSlots.Length == 0)
            LogWarning("Item 슬롯을 자동 탐색하지 못했습니다.");
    }

    public void Show()
    {
        if (isOpen)
            return;

        if (root == null)
            root = gameObject;

        AutoBindSlotsIfEmpty();

        BuildRandomOptions();

        if (selectedBuffer.Count <= 0)
        {
            LogWarning("표시할 레벨업 후보가 없습니다. allItems / ItemData 최대레벨 / CanLevelUpFrom을 확인하세요.");
            return;
        }

        ApplyOptionsToSlots();

        isOpen = true;

        root.SetActive(true);

        GameManager gm = GameManager.Instance;

        if (gm != null)
            gm.PauseGame();
        else
            Time.timeScale = 0f;
    }

    public void Hide()
    {
        isOpen = false;

        if (root != null)
            root.SetActive(false);

        GameManager gm = GameManager.Instance;

        if (gm != null)
            gm.ResumeGame();
        else
            Time.timeScale = 1f;
    }

    public void ForceHide()
    {
        isOpen = false;

        if (root == null)
            root = gameObject;

        HideAllSlots();

        if (root != null)
            root.SetActive(false);
    }

    public int GetCurrentLevel(ItemData data)
    {
        if (data == null)
            return 0;

        if (itemLevels.TryGetValue(data, out int level))
            return Mathf.Max(0, level);

        return 0;
    }

    public void SyncItemLevel(ItemData data, int realLevel)
    {
        if (data == null)
            return;

        itemLevels[data] = Mathf.Max(0, realLevel);
    }

    public void NotifyItemSelected(ItemData data)
    {
        if (data == null)
            return;

        int current = GetCurrentLevel(data);
        itemLevels[data] = current + 1;
    }

    private void BuildRandomOptions()
    {
        candidateBuffer.Clear();
        selectedBuffer.Clear();

        BuildMainCandidates();

        if (allowFallbackOptions && candidateBuffer.Count < GetVisibleOptionCount())
            AddFallbackCandidates();

        PickRandomOptions();
    }

    private void BuildMainCandidates()
    {
        if (allItems == null || allItems.Length == 0)
        {
            LogWarning("allItems가 비어 있습니다.");
            return;
        }

        for (int i = 0; i < allItems.Length; i++)
        {
            ItemData data = allItems[i];

            if (data == null)
                continue;

            if (candidateBuffer.Contains(data))
                continue;

            int currentLevel = GetCurrentLevel(data);

            if (!data.CanLevelUpFrom(currentLevel))
                continue;

            candidateBuffer.Add(data);
        }
    }

    private void AddFallbackCandidates()
    {
        if (fallbackItems == null || fallbackItems.Length == 0)
            return;

        for (int i = 0; i < fallbackItems.Length; i++)
        {
            ItemData data = fallbackItems[i];

            if (data == null)
                continue;

            if (candidateBuffer.Contains(data))
                continue;

            int currentLevel = GetCurrentLevel(data);

            if (!data.CanLevelUpFrom(currentLevel))
                continue;

            candidateBuffer.Add(data);
        }
    }

    private void PickRandomOptions()
    {
        int count = Mathf.Min(GetVisibleOptionCount(), candidateBuffer.Count);

        for (int i = 0; i < count; i++)
        {
            int randomIndex = Random.Range(0, candidateBuffer.Count);
            ItemData picked = candidateBuffer[randomIndex];

            selectedBuffer.Add(picked);
            candidateBuffer.RemoveAt(randomIndex);
        }
    }

    private void ApplyOptionsToSlots()
    {
        HideAllSlots();

        if (itemSlots == null || itemSlots.Length == 0)
        {
            LogWarning("itemSlots가 비어 있습니다.");
            return;
        }

        int count = Mathf.Min(selectedBuffer.Count, itemSlots.Length);

        for (int i = 0; i < count; i++)
        {
            Item slot = itemSlots[i];

            if (slot == null)
                continue;

            ItemData data = selectedBuffer[i];
            int currentLevel = GetCurrentLevel(data);

            slot.gameObject.SetActive(true);
            slot.SetItem(data, currentLevel, this);
        }
    }

    private void HideAllSlots()
    {
        if (itemSlots == null)
            return;

        for (int i = 0; i < itemSlots.Length; i++)
        {
            if (itemSlots[i] == null)
                continue;

            itemSlots[i].gameObject.SetActive(false);
        }
    }

    private int GetVisibleOptionCount()
    {
        int slotCount = itemSlots != null ? itemSlots.Length : 0;

        if (slotCount <= 0)
            return 0;

        return Mathf.Clamp(optionCount, 1, slotCount);
    }

    private void LogWarning(string message)
    {
        if (!verboseLog)
            return;

        Debug.LogWarning($"LevelUpUI: {message}", this);
    }
}