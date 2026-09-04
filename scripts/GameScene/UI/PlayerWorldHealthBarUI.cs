using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 플레이어 자식으로 붙는 월드 체력바.
/// 
/// 목적:
/// - Canvas/HUD에서 WorldToScreenPoint로 플레이어를 추적하면
///   Rigidbody 이동, Cinemachine, Canvas 갱신 순서 때문에 빠른 이동 시 지연이 보일 수 있다.
/// - 이 체력바는 Player 프리팹의 자식으로 둔다.
/// - 따라서 플레이어 Transform과 같은 프레임에 같이 이동한다.
/// 
/// 추천 구조:
/// Player
/// └ PlayerHealthBarCanvas
///    └ PlayerHealthBar
///       └ Slider
/// 
/// 주의:
/// - 보스 체력바에는 이 스크립트를 사용하지 않는다.
/// - 보스 체력바는 Canvas/Hud 상단 고정 BossHealthBarUI를 사용한다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Slider))]
public class PlayerWorldHealthBarUI : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private PlayerHealth playerHealth;

    [Header("Visibility")]
    [SerializeField] private bool hideWhenFull = false;
    [SerializeField] private bool hideWhenDead = true;

    [Header("Optional")]
    [SerializeField] private CanvasGroup canvasGroup;

    private Slider slider;

    private void Awake()
    {
        slider = GetComponent<Slider>();

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (playerHealth == null)
            playerHealth = GetComponentInParent<PlayerHealth>();
    }

    private void OnEnable()
    {
        Bind(playerHealth);
    }

    private void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.HealthChanged -= OnHealthChanged;
    }

    public void Bind(PlayerHealth target)
    {
        if (playerHealth != null)
            playerHealth.HealthChanged -= OnHealthChanged;

        playerHealth = target;

        if (playerHealth == null)
        {
            SetVisible(false);
            return;
        }

        playerHealth.HealthChanged += OnHealthChanged;
        Refresh();
    }

    private void OnHealthChanged(float current, float max)
    {
        if (slider == null)
            return;

        float safeMax = Mathf.Max(1f, max);
        float value = Mathf.Clamp01(current / safeMax);

        slider.value = value;

        if (hideWhenDead && current <= 0f)
        {
            SetVisible(false);
            return;
        }

        if (hideWhenFull && value >= 0.999f)
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);
    }

    private void Refresh()
    {
        if (playerHealth == null)
        {
            SetVisible(false);
            return;
        }

        OnHealthChanged(playerHealth.CurrentHealth, playerHealth.MaxHealth);
    }

    private void SetVisible(bool visible)
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }
}