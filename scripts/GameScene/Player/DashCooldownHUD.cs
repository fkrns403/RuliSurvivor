using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 대쉬 쿨타임 UI.
/// 
/// 역할:
/// - PlayerDash.CooldownNormalized 값을 Slider에 표시한다.
/// - 플레이어가 프리팹으로 런타임 생성되어도 GameBootstrap에서 다시 연결할 수 있다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Slider))]
public class DashCooldownHUD : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private PlayerDash playerDash;

    [Header("Auto Bind")]
    [SerializeField] private bool autoFindWhenMissing = true;

    private Slider slider;

    private void Awake()
    {
        slider = GetComponent<Slider>();

        if (playerDash == null && autoFindWhenMissing)
            playerDash = FindObjectOfType<PlayerDash>(true);

        RefreshImmediate();
    }

    private void OnEnable()
    {
        if (playerDash == null && autoFindWhenMissing)
            playerDash = FindObjectOfType<PlayerDash>(true);

        RefreshImmediate();
    }

    private void LateUpdate()
    {
        if (playerDash == null)
        {
            if (autoFindWhenMissing)
                playerDash = FindObjectOfType<PlayerDash>(true);

            if (playerDash == null)
                return;
        }

        if (slider != null)
            slider.value = playerDash.CooldownNormalized;
    }

    public void BindPlayerDash(PlayerDash dash)
    {
        playerDash = dash;
        RefreshImmediate();
    }

    private void RefreshImmediate()
    {
        if (slider == null)
            slider = GetComponent<Slider>();

        if (slider == null)
            return;

        slider.value = playerDash == null ? 1f : playerDash.CooldownNormalized;
    }
}