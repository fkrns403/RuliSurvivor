using UnityEngine;

/// <summary>
/// 장착형 근접무기
/// 
/// 이번 수정 핵심:
/// - 공격속도 배율을 DamageDealer의 hitCooldown에 반영
/// </summary>
public class EquippedMeleeWeapon : MonoBehaviour, IWeaponRuntime
{
    [SerializeField] private float fallbackBaseDamage = 10f;
    [SerializeField] private float hitCooldown = 0.2f;

    private Transform owner;
    private ItemData itemData;
    private PlayerStatSystem playerStatSystem;

    public void OnEquip(Transform ownerTransform, ItemData data)
    {
        owner = ownerTransform;
        itemData = data;

        if (owner != null)
            playerStatSystem = owner.GetComponent<PlayerStatSystem>();

        DamageDealer dealer = GetComponentInChildren<DamageDealer>();
        if (dealer != null)
        {
            dealer.SetOwner(owner);
            dealer.SetDamage(GetDamageByLevel(1));
            dealer.SetHitCooldown(GetFinalHitCooldown());
            dealer.SetDespawnOnHit(false);
        }
    }

    public void OnUnequip()
    {
    }

    public void SetLevel(int level)
    {
        int finalLevel = Mathf.Max(1, level);

        if (itemData != null)
            finalLevel = itemData.ClampLevel(finalLevel);

        DamageDealer dealer = GetComponentInChildren<DamageDealer>();
        if (dealer != null)
        {
            dealer.SetDamage(GetDamageByLevel(finalLevel));
            dealer.SetHitCooldown(GetFinalHitCooldown());
        }
    }

    private float GetDamageByLevel(int currentLevel)
    {
        currentLevel = Mathf.Max(1, currentLevel);

        if (itemData != null)
            return Mathf.Max(0.01f, itemData.GetDamageAtLevel(currentLevel));

        return fallbackBaseDamage + (currentLevel - 1) * 3f;
    }

    private float GetFinalHitCooldown()
    {
        float attackSpeedMul = 1f;
        if (playerStatSystem != null)
            attackSpeedMul = Mathf.Max(0.01f, playerStatSystem.GetAttackSpeedMultiplier());

        return Mathf.Max(0.01f, hitCooldown / attackSpeedMul);
    }

    private void LateUpdate()
    {
        if (owner == null)
            return;

        transform.position = owner.position;
    }
}