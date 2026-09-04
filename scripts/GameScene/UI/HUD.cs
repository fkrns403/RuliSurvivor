using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class HUD : MonoBehaviour
{
    public enum InfoType
    {
        Exp,
        Level,
        Kill,
        Time,
        Health
    }

    [Header("What to display")]
    [SerializeField] private InfoType type;

    [Header("Time Color")]
    [SerializeField] private bool changeTimeColorOnOverTime = true;
    [SerializeField] private Color normalTimeColor = Color.white;
    [SerializeField] private Color overtimeColor = Color.red;

    [Header("Safety")]
    [SerializeField] private bool disableIfAttachedToWorldTarget = true;
    [SerializeField] private bool warnIfWorldSpaceCanvas = true;

    private Text text;
    private Slider slider;
    private bool disabledBySafety;

    private void Awake()
    {
        text = GetComponent<Text>();
        slider = GetComponent<Slider>();

        SafetyCheck();
    }

    private void OnEnable()
    {
        SafetyCheck();
    }

    private void SafetyCheck()
    {
        disabledBySafety = false;

        if (disableIfAttachedToWorldTarget)
        {
            BossHealth boss = GetComponentInParent<BossHealth>();
            Enemy enemy = GetComponentInParent<Enemy>();

            if (boss != null || enemy != null)
            {
                disabledBySafety = true;

                Debug.LogWarning(
                    "HUD: 이 컴포넌트는 플레이어 화면 HUD 전용입니다. " +
                    "Boss/Enemy 아래 체력바에는 BossHealthBarUI 또는 EnemyHealthBarUI를 사용하세요.",
                    this
                );

                enabled = false;
                return;
            }
        }

        if (warnIfWorldSpaceCanvas)
        {
            Canvas canvas = GetComponentInParent<Canvas>();

            if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
            {
                Debug.LogWarning(
                    "HUD: World Space Canvas 아래에 있습니다. " +
                    "플레이어 HUD는 보통 Screen Space Canvas에 두는 것이 안전합니다.",
                    this
                );
            }
        }
    }

    private void LateUpdate()
    {
        if (disabledBySafety)
            return;

        GameManager gm = GameManager.Instance;

        if (gm == null)
            return;

        switch (type)
        {
            case InfoType.Exp:
                UpdateExp(gm);
                break;

            case InfoType.Level:
                UpdateLevel(gm);
                break;

            case InfoType.Kill:
                UpdateKill(gm);
                break;

            case InfoType.Time:
                UpdateTime(gm);
                break;

            case InfoType.Health:
                UpdateHealth(gm);
                break;
        }
    }

    private void UpdateExp(GameManager gm)
    {
        if (slider == null)
            return;

        float required = Mathf.Max(1f, gm.GetRequiredExpForNextLevel());
        float current = Mathf.Max(0f, gm.exp);

        slider.value = Mathf.Clamp01(current / required);
    }

    private void UpdateLevel(GameManager gm)
    {
        if (text == null)
            return;

        text.text = $"Lv.{Mathf.Max(1, gm.level)}";
    }

    private void UpdateKill(GameManager gm)
    {
        if (text == null)
            return;

        text.text = gm.killCount.ToString();
    }

    private void UpdateTime(GameManager gm)
    {
        if (text == null)
            return;

        if (gm.isOverTime)
        {
            float overtime = Mathf.Max(0f, gm.gameTime - gm.maxGameTime);
            int min = Mathf.FloorToInt(overtime / 60f);
            int sec = Mathf.FloorToInt(overtime % 60f);

            text.text = $"OT {min:D2}:{sec:D2}";

            if (changeTimeColorOnOverTime)
                text.color = overtimeColor;

            return;
        }

        int remain = Mathf.CeilToInt(gm.maxGameTime - gm.gameTime);
        remain = Mathf.Max(0, remain);

        int rMin = remain / 60;
        int rSec = remain % 60;

        text.text = $"{rMin:D2}:{rSec:D2}";

        if (changeTimeColorOnOverTime)
            text.color = normalTimeColor;
    }

    private void UpdateHealth(GameManager gm)
    {
        if (slider == null)
            return;

        float maxHp = Mathf.Max(1f, gm.maxHealth);
        float currentHp = Mathf.Clamp(gm.health, 0f, maxHp);

        slider.value = currentHp / maxHp;
    }
}