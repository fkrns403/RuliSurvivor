using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerStatSystem : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] private float baseMoveSpeed = 3f;
    [SerializeField] private float moveSpeedMultiplier = 1f;

    [Header("Attack")]
    [SerializeField] private float attackSpeedEquipMultiplier = 1f;
    [SerializeField] private float attackSpeedTemporaryMultiplier = 1f;

    [Header("Optional")]
    [SerializeField] private float expGainMultiplier = 1f;

    private PlayerController playerController;
    private Coroutine attackSpeedBuffRoutine;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        ApplyMoveSpeedToPlayer();
    }

    public void ApplyEquip(ItemData data, int currentLevel)
    {
        if (data == null)
            return;

        float rate = GetRate(data, currentLevel);

        switch (data.ItemType)
        {
            case ItemType.Equip_Shoe:
                moveSpeedMultiplier = 1f + Mathf.Max(0f, rate);
                ApplyMoveSpeedToPlayer();
                break;

            case ItemType.Equip_Glove:
                attackSpeedEquipMultiplier = 1f + Mathf.Max(0f, rate);
                break;

            case ItemType.Equip_Jewel:
                expGainMultiplier = 1f + Mathf.Max(0f, rate);
                break;

            case ItemType.Equip_Breadbone:
                break;

            case ItemType.Equip_Heal:
                break;
        }
    }

    public void ApplyTemporaryAttackSpeedMultiplier(float multiplier, float duration)
    {
        if (attackSpeedBuffRoutine != null)
            StopCoroutine(attackSpeedBuffRoutine);

        attackSpeedBuffRoutine = StartCoroutine(
            AttackSpeedBuffRoutine(multiplier, duration)
        );
    }

    public void SetTemporaryAttackSpeedMultiplier(float multiplier, float duration)
    {
        ApplyTemporaryAttackSpeedMultiplier(multiplier, duration);
    }

    private IEnumerator AttackSpeedBuffRoutine(float multiplier, float duration)
    {
        attackSpeedTemporaryMultiplier = Mathf.Max(1f, multiplier);

        float timer = 0f;
        float safeDuration = Mathf.Max(0.1f, duration);

        while (timer < safeDuration)
        {
            GameManager gm = GameManager.Instance;

            if (gm != null && gm.isPaused)
            {
                yield return null;
                continue;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        attackSpeedTemporaryMultiplier = 1f;
        attackSpeedBuffRoutine = null;
    }

    private void ApplyMoveSpeedToPlayer()
    {
        if (playerController != null)
            playerController.Speed = baseMoveSpeed * moveSpeedMultiplier;
    }

    private float GetRate(ItemData data, int level)
    {
        if (data == null)
            return 0f;

        if (data.damages != null && data.damages.Length > 0)
        {
            int idx = Mathf.Clamp(level, 0, data.damages.Length - 1);
            return data.damages[idx];
        }

        return 0f;
    }

    public float GetAttackSpeedMultiplier()
    {
        return attackSpeedEquipMultiplier * attackSpeedTemporaryMultiplier;
    }

    public float GetExpGainMultiplier()
    {
        return Mathf.Max(0.01f, expGainMultiplier);
    }
}