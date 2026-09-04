using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 캐릭터 고유 스킬 UI.
/// 
/// 역할:
/// - Q 스킬 아이콘 표시
/// - 쿨다운 원형 게이지 표시
/// - 쿨다운 남은 시간 표시
/// - 목숨형 캐릭터의 스택 수 표시
/// 
/// 수정 이유:
/// - 기존 SetStackCount()에서 "¡¿" 문자가 들어가 있어 X 표기가 깨졌다.
/// - HideStackCount()가 stackText 오브젝트를 꺼버리기 때문에,
///   Q 표시 텍스트와 stackText가 같은 오브젝트면 Q까지 사라질 수 있었다.
/// 
/// 사용 규칙:
/// - keyText는 "Q" 표시 전용이다.
/// - stackText는 "x3", "x9" 같은 스택 표시 전용이다.
/// - keyText와 stackText는 서로 다른 TMP_Text 오브젝트를 연결하는 것을 권장한다.
/// </summary>
[DisallowMultipleComponent]
public class SkillCooldownUI : MonoBehaviour
{
    [Header("Radial Gauge")]
    [SerializeField] private Image cooldownRadialFill;

    [Header("Icon")]
    [SerializeField] private Image dimIcon;
    [SerializeField] private Image clearIcon;

    [Header("Text")]
    [SerializeField] private TMP_Text cooldownText;

    [Tooltip("Q 표시 전용 텍스트. 스택 텍스트와 같은 오브젝트를 넣지 않는 것을 권장한다.")]
    [SerializeField] private TMP_Text keyText;

    [Tooltip("목숨/스택 표시 전용 텍스트. 예: x3")]
    [SerializeField] private TMP_Text stackText;

    [Header("Key Text")]
    [SerializeField] private bool showKeyText = true;
    [SerializeField] private string keyLabel = "Q";

    [Header("Stack Text")]
    [SerializeField] private bool hideStackWhenZero = true;
    [SerializeField] private string stackPrefix = "x";

    [Header("Alpha")]
    [SerializeField, Range(0f, 1f)] private float dimIconAlpha = 0.35f;
    [SerializeField, Range(0f, 1f)] private float clearIconAlpha = 1f;

    private float cooldownDuration = 1f;
    private float cooldownEndTime;
    private bool isCooling;

    private void Awake()
    {
        SetupImages();
        SetupKeyText();
        SetReady();
    }

    private void OnEnable()
    {
        SetupImages();
        SetupKeyText();

        if (!isCooling)
            SetReady();
    }

    private void Update()
    {
        if (!isCooling)
            return;

        float remain = cooldownEndTime - Time.time;

        if (remain <= 0f)
        {
            SetReady();
            return;
        }

        float progress = 1f - Mathf.Clamp01(remain / cooldownDuration);

        if (cooldownRadialFill != null)
            cooldownRadialFill.fillAmount = progress;

        if (clearIcon != null)
            clearIcon.fillAmount = progress;

        if (cooldownText != null)
            cooldownText.text = Mathf.CeilToInt(remain).ToString();
    }

    private void SetupImages()
    {
        SetupRadialImage(cooldownRadialFill, 1f);
        SetupRadialImage(clearIcon, 1f);

        if (dimIcon != null)
        {
            Color c = dimIcon.color;
            c.a = dimIconAlpha;
            dimIcon.color = c;
        }

        if (clearIcon != null)
        {
            Color c = clearIcon.color;
            c.a = clearIconAlpha;
            clearIcon.color = c;
        }
    }

    private void SetupRadialImage(Image image, float fillAmount)
    {
        if (image == null)
            return;

        image.type = Image.Type.Filled;
        image.fillMethod = Image.FillMethod.Radial360;
        image.fillOrigin = 2;
        image.fillClockwise = true;
        image.fillAmount = fillAmount;
    }

    private void SetupKeyText()
    {
        if (keyText == null)
            return;

        keyText.gameObject.SetActive(showKeyText);
        keyText.text = showKeyText ? keyLabel : string.Empty;
    }

    public void SetIcon(Sprite icon)
    {
        if (dimIcon != null)
        {
            dimIcon.sprite = icon;
            dimIcon.enabled = icon != null;
        }

        if (clearIcon != null)
        {
            clearIcon.sprite = icon;
            clearIcon.enabled = icon != null;
        }
    }

    public void StartCooldown(float duration)
    {
        cooldownDuration = Mathf.Max(0.1f, duration);
        cooldownEndTime = Time.time + cooldownDuration;
        isCooling = true;

        if (cooldownRadialFill != null)
            cooldownRadialFill.fillAmount = 0f;

        if (clearIcon != null)
            clearIcon.fillAmount = 0f;

        if (cooldownText != null)
        {
            cooldownText.gameObject.SetActive(true);
            cooldownText.text = Mathf.CeilToInt(cooldownDuration).ToString();
        }

        SetupKeyText();
    }

    public void SetReady()
    {
        isCooling = false;

        if (cooldownRadialFill != null)
            cooldownRadialFill.fillAmount = 1f;

        if (clearIcon != null)
            clearIcon.fillAmount = 1f;

        if (cooldownText != null)
        {
            cooldownText.text = string.Empty;
            cooldownText.gameObject.SetActive(false);
        }

        SetupKeyText();
    }

    public void SetStackCount(int count)
    {
        if (stackText == null)
            return;

        int safeCount = Mathf.Max(0, count);

        if (safeCount <= 0 && hideStackWhenZero)
        {
            HideStackCount();
            return;
        }

        stackText.gameObject.SetActive(true);
        stackText.text = stackPrefix + safeCount;

        SetupKeyText();
    }

    public void HideStackCount()
    {
        if (stackText == null)
            return;

        stackText.text = string.Empty;
        stackText.gameObject.SetActive(false);

        SetupKeyText();
    }

    public void SetKeyTextVisible(bool visible)
    {
        showKeyText = visible;
        SetupKeyText();
    }

    public void SetKeyLabel(string label)
    {
        keyLabel = string.IsNullOrEmpty(label) ? "Q" : label;
        SetupKeyText();
    }
}