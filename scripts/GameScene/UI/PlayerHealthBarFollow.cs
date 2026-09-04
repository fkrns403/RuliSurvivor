using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 플레이어 체력바 전용 추적 UI.
/// 
/// 사용 위치:
/// - Canvas / Hud / PlayerHealthBar 오브젝트에 붙인다.
/// - 보스 체력바에는 절대 붙이지 않는다.
/// 
/// 역할:
/// - 런타임에 생성된 플레이어를 자동으로 찾는다.
/// - PlayerHealth의 체력 값을 Slider에 반영한다.
/// - 플레이어의 월드 위치를 화면 좌표로 변환해서 체력바 위치를 갱신한다.
/// 
/// 왜 필요한가:
/// - 구버전 Followerhealth는 FixedUpdate에서 플레이어 위치를 따라갔다.
/// - 현버전은 Cinemachine, CameraHardClamp2D, 런타임 플레이어 생성, GameManager.PlayerTransform 구조를 사용한다.
/// - 따라서 FixedUpdate보다 LateUpdate에서 카메라 보정 이후 위치를 따라가는 편이 안정적이다.
/// 
/// 주의:
/// - BossHealthBarUI와 절대 같이 쓰지 않는다.
/// - BossHealthBar에는 BossHealthBarUI만 붙인다.
/// - PlayerHealthBar에는 이 PlayerHealthBarFollow만 붙인다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Slider))]
public class PlayerHealthBarFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private PlayerHealth playerHealth;

    [Header("Camera")]
    [SerializeField] private Camera targetCamera;

    [Header("Offset")]
    [Tooltip("플레이어 기준 화면상 체력바 오프셋. Y를 음수로 두면 캐릭터 아래에 표시된다.")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, -0.75f, 0f);

    [Header("Auto Bind")]
    [SerializeField] private bool autoFindPlayer = true;
    [SerializeField] private float searchInterval = 0.25f;

    [Header("Visibility")]
    [SerializeField] private bool hideWhenNoPlayer = true;
    [SerializeField] private bool hideWhenDead = true;

    [Header("Optional")]
    [SerializeField] private CanvasGroup canvasGroup;

    private Slider slider;
    private RectTransform rect;
    private float searchTimer;

    private void Awake()
    {
        slider = GetComponent<Slider>();
        rect = GetComponent<RectTransform>();

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (targetCamera == null)
            targetCamera = Camera.main;

        TryBindPlayer();

        RefreshHealth();
    }

    private void OnEnable()
    {
        TryBindPlayer();
        RefreshHealth();
    }

    private void OnDisable()
    {
        UnbindHealthEvent();
    }

    private void LateUpdate()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (target == null || playerHealth == null)
        {
            if (autoFindPlayer)
            {
                searchTimer += Time.unscaledDeltaTime;

                if (searchTimer >= searchInterval)
                {
                    searchTimer = 0f;
                    TryBindPlayer();
                }
            }

            if (target == null || playerHealth == null)
            {
                if (hideWhenNoPlayer)
                    SetVisible(false);

                return;
            }
        }

        UpdatePosition();
        RefreshHealth();
    }

    private void TryBindPlayer()
    {
        Transform found = null;

        if (GameManager.Instance != null && GameManager.Instance.PlayerTransform != null)
            found = GameManager.Instance.PlayerTransform;

        if (found == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

            if (playerObj != null)
                found = playerObj.transform;
        }

        if (found == null)
            return;

        Bind(found);
    }

    public void Bind(Transform newTarget)
    {
        if (newTarget == null)
            return;

        UnbindHealthEvent();

        target = newTarget;
        playerHealth = target.GetComponent<PlayerHealth>();

        if (playerHealth != null)
            playerHealth.HealthChanged += OnHealthChanged;

        RefreshHealth();
        SetVisible(true);
    }

    private void UnbindHealthEvent()
    {
        if (playerHealth != null)
            playerHealth.HealthChanged -= OnHealthChanged;
    }

    private void UpdatePosition()
    {
        if (rect == null || target == null || targetCamera == null)
            return;

        Vector3 worldPos = target.position + worldOffset;
        Vector3 screenPos = targetCamera.WorldToScreenPoint(worldPos);

        rect.position = screenPos;
    }

    private void OnHealthChanged(float current, float max)
    {
        SetHealthValue(current, max);
    }

    private void RefreshHealth()
    {
        if (playerHealth == null)
            return;

        SetHealthValue(playerHealth.CurrentHealth, playerHealth.MaxHealth);
    }

    private void SetHealthValue(float current, float max)
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

        SetVisible(true);
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