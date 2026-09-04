using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 보스 전용 체력바 UI.
/// 
/// 설계:
/// - 생성/파괴하지 않는다.
/// - Canvas/Hud 아래에 한 번만 배치한다.
/// - 보스가 생성되면 BossHealth를 바인딩한다.
/// - 보스가 죽거나 비활성화되면 체력바만 숨긴다.
/// - 다음 페이즈 보스가 나오면 다시 같은 체력바를 재사용한다.
/// 
/// 위치 처리:
/// - 보스 월드 위치를 화면 좌표로 변환해서 체력바 위치를 갱신한다.
/// - 보스가 화면 안에 있든 밖에 있든, 보스의 실제 위치 기준으로 따라간다.
/// - 화면 밖이면 체력바도 화면 밖 좌표로 이동한다.
/// 
/// 주의:
/// - 이 스크립트는 보스 체력바 전용이다.
/// - PlayerHealthBarFollow와 같이 붙이면 안 된다.
/// - 보스 체력바 오브젝트 자체는 비활성화하지 않는 것을 권장한다.
/// - 숨김은 CanvasGroup alpha로 처리한다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Slider))]
public class BossHealthBarUI : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private BossHealth bossHealth;
    [SerializeField] private Transform target;

    [Header("Camera")]
    [SerializeField] private Camera targetCamera;

    [Header("Follow Offset")]
    [Tooltip("보스 기준 월드 오프셋. Y를 양수로 두면 보스 머리 위에 표시된다.")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.5f, 0f);

    [Header("Auto Bind")]
    [SerializeField] private bool autoFindBoss = true;

    [Tooltip("보스를 자동 탐색하는 간격")]
    [SerializeField] private float searchInterval = 0.25f;

    [Header("Visibility")]
    [SerializeField] private bool hideWhenNoBoss = true;
    [SerializeField] private bool hideWhenBossDead = true;

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

        HideVisualOnly();
    }

    private void OnEnable()
    {
        TryAutoBind();
        Refresh();
    }

    private void OnDisable()
    {
        UnbindBoss();
    }

    private void LateUpdate()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (bossHealth == null || target == null)
        {
            if (autoFindBoss)
            {
                searchTimer += Time.unscaledDeltaTime;

                if (searchTimer >= searchInterval)
                {
                    searchTimer = 0f;
                    TryAutoBind();
                }
            }

            if (bossHealth == null || target == null)
            {
                if (hideWhenNoBoss)
                    HideVisualOnly();

                return;
            }
        }

        if (!bossHealth.gameObject.activeInHierarchy)
        {
            BindBoss(null);
            return;
        }

        if (bossHealth.IsDead)
        {
            if (hideWhenBossDead)
                HideVisualOnly();

            BindBoss(null);
            return;
        }

        UpdatePosition();
        Refresh();
    }

    /// <summary>
    /// 보스 생성 직후 외부에서 직접 연결할 때 사용한다.
    /// ProgressiveBossSpawner 또는 BossDirector에서 호출 가능하다.
    /// </summary>
    public void BindBoss(BossHealth newBoss)
    {
        UnbindBoss();

        bossHealth = newBoss;
        target = bossHealth != null ? bossHealth.transform : null;

        if (bossHealth == null)
        {
            if (hideWhenNoBoss)
                HideVisualOnly();

            return;
        }

        bossHealth.HpChanged += OnBossHpChanged;
        bossHealth.Died += OnBossDied;

        ShowVisualOnly();
        UpdatePosition();
        Refresh();
    }

    private void TryAutoBind()
    {
        if (!autoFindBoss)
            return;

        if (bossHealth != null)
            return;

        BossHealth[] bosses = FindObjectsOfType<BossHealth>(true);

        for (int i = 0; i < bosses.Length; i++)
        {
            BossHealth boss = bosses[i];

            if (boss == null)
                continue;

            if (!boss.gameObject.activeInHierarchy)
                continue;

            if (boss.IsDead)
                continue;

            BindBoss(boss);
            return;
        }

        if (hideWhenNoBoss)
            HideVisualOnly();
    }

    private void UnbindBoss()
    {
        if (bossHealth == null)
            return;

        bossHealth.HpChanged -= OnBossHpChanged;
        bossHealth.Died -= OnBossDied;
    }

    private void UpdatePosition()
    {
        if (rect == null || target == null || targetCamera == null)
            return;

        Vector3 worldPos = target.position + worldOffset;
        Vector3 screenPos = targetCamera.WorldToScreenPoint(worldPos);

        rect.position = screenPos;
    }

    private void OnBossHpChanged(BossHealth boss, float current, float max)
    {
        SetHealthValue(current, max);
    }

    private void OnBossDied(BossHealth boss)
    {
        if (hideWhenBossDead)
            HideVisualOnly();

        BindBoss(null);
    }

    private void Refresh()
    {
        if (bossHealth == null)
            return;

        SetHealthValue(bossHealth.CurrentHp, bossHealth.MaxHp);
    }

    private void SetHealthValue(float current, float max)
    {
        if (slider == null)
            return;

        float safeMax = Mathf.Max(1f, max);
        slider.value = Mathf.Clamp01(current / safeMax);

        if (hideWhenBossDead && current <= 0f)
        {
            HideVisualOnly();
            return;
        }

        ShowVisualOnly();
    }

    private void ShowVisualOnly()
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void HideVisualOnly()
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }
}