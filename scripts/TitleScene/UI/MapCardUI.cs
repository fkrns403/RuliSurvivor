using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 타이틀 씬 맵 카드 UI.
/// 
/// 역할:
/// - TitleManager가 생성한 맵 카드 하나를 담당한다.
/// - 맵 이미지와 이름을 표시한다.
/// - 클릭 시 TitleManager.SelectMap(index)를 호출한다.
/// 
/// 중요:
/// - 프리팹을 런타임 Instantiate로 만들기 때문에
///   Inspector의 Button OnClick()은 비어 있어도 된다.
/// - Button.onClick은 Setup()에서 코드로 자동 등록한다.
/// - IPointerClickHandler는 사용하지 않는다.
///   Button 클릭과 중복 호출되어 SelectMap이 2번 호출될 수 있기 때문이다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class MapCardUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image previewImage;
    [SerializeField] private Text nameText;
    [SerializeField] private Button button;

    [Header("Selected Mark")]
    [SerializeField] private GameObject selectedMark;

    [Header("Debug")]
    [SerializeField] private bool verboseLog = true;

    private TitleManager owner;
    private int index = -1;
    private bool clickLocked;

    private void Awake()
    {
        ResolveReferences();
    }

    private void Reset()
    {
        button = GetComponent<Button>();
        previewImage = GetComponent<Image>();
    }

    /// <summary>
    /// TitleManager가 맵카드를 생성한 직후 호출한다.
    /// </summary>
    public void Setup(TitleManager newOwner, int newIndex, MapEntry data)
    {
        owner = newOwner;
        index = newIndex;
        clickLocked = false;

        ResolveReferences();
        RegisterButtonEvent();

        if (previewImage != null)
            previewImage.sprite = data != null ? data.preview : null;

        if (nameText != null)
            nameText.text = data != null ? data.displayName : string.Empty;

        if (button != null)
            button.interactable = true;

        SetSelected(false);

        Log($"Setup 완료 / index={index}, map={(data != null ? data.displayName : "null")}");
    }

    public void SetSelected(bool selected)
    {
        if (selectedMark != null)
            selectedMark.SetActive(selected);
    }

    public void SetInteractable(bool interactable)
    {
        clickLocked = !interactable;

        if (button != null)
            button.interactable = interactable;
    }

    private void OnClick()
    {
        if (clickLocked)
            return;

        if (button != null && !button.interactable)
            return;

        if (owner == null)
            owner = FindObjectOfType<TitleManager>(true);

        if (owner == null)
        {
            Debug.LogError("MapCardUI: TitleManager를 찾지 못했습니다.", this);
            return;
        }

        if (index < 0)
            index = transform.GetSiblingIndex();

        Log($"맵 카드 클릭됨 / index={index}");

        owner.SelectMap(index);
    }

    private void ResolveReferences()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (previewImage == null)
            previewImage = GetComponent<Image>();
    }

    private void RegisterButtonEvent()
    {
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClick);
    }

    private void Log(string message)
    {
        if (!verboseLog)
            return;

        Debug.Log($"MapCardUI: {message}", this);
    }
}