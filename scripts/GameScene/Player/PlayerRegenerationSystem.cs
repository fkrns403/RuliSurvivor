using UnityEngine;

/// <summary>
/// 플레이어의 주기 회복 패시브 시스템
/// 
/// 역할:
/// - Equip_Regeneration 아이템이 선택되면
///   일정 시간마다 체력을 조금씩 회복시킨다.
/// - 레벨이 오를수록 회복량이 증가한다.
/// 
/// ItemData 해석 규칙:
/// - damages 배열 값은 "틱당 회복량"으로 사용한다.
/// - counts 배열은 현재 사용하지 않는다.
/// </summary>
[DisallowMultipleComponent]
public class PlayerRegenerationSystem : MonoBehaviour
{
    [Header("Base Settings")]
    [Tooltip("기본 회복 주기(초)")]
    [SerializeField] private float tickInterval = 4f;

    [Header("Runtime")]
    [SerializeField] private bool isActive;

    [SerializeField] private int currentLevel;

    [SerializeField] private float healPerTick;

    private ItemData itemData;
    private float timer;

    private void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null)
            return;

        if (!gm.isLive || gm.isPaused)
            return;

        if (!isActive)
            return;

        if (healPerTick <= 0f)
            return;

        timer += Time.deltaTime;
        if (timer < tickInterval)
            return;

        timer = 0f;
        ApplyHeal(healPerTick);
    }

    /// <summary>
    /// 재생 패시브 업그레이드 적용
    /// - targetLevel은 실제 레벨(1부터 시작)
    /// </summary>
    public void ApplyUpgrade(ItemData data, int targetLevel)
    {
        if (data == null)
            return;

        itemData = data;
        currentLevel = Mathf.Max(1, data.ClampLevel(targetLevel));
        healPerTick = Mathf.Max(0f, data.GetDamageAtLevel(currentLevel));

        isActive = true;
        timer = 0f;
    }

    private void ApplyHeal(float amount)
    {
        GameManager gm = GameManager.Instance;
        if (gm == null)
            return;

        gm.health = Mathf.Min(gm.maxHealth, gm.health + amount);
    }

    public void ResetSystem()
    {
        itemData = null;
        currentLevel = 0;
        healPerTick = 0f;
        isActive = false;
        timer = 0f;
    }
}