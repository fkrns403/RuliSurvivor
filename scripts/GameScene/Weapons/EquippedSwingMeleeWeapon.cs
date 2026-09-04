using System.Collections;
using UnityEngine;

/// <summary>
/// 장착형 검 무기
/// 
/// 이번 수정 핵심:
/// - 공격속도 배율을 스윙 주기에 반영
/// </summary>
public class EquippedSwingMeleeWeapon : MonoBehaviour, IWeaponRuntime
{
    [Header("References")]
    [SerializeField] private DamageDealer damageDealer;
    [SerializeField] private AfterImage1 afterImage;
    [SerializeField] private TargetScanner2D scanner;

    [Header("Fallback Stats")]
    [SerializeField] private float fallbackBaseDamage = 10f;
    [SerializeField] private float hitCooldown = 0.15f;

    [Header("Swing")]
    [SerializeField] private float attackInterval = 2.0f;
    [SerializeField] private float swingDuration = 0.25f;
    [SerializeField] private float attackDistance = 0.8f;
    [SerializeField] private float swingAngle = 120f;

    private Transform owner;
    private ItemData itemData;
    private int level = 1;
    private float timer;
    private bool isSwinging;

    private PlayerStatSystem playerStatSystem;

    public void OnEquip(Transform ownerTransform, ItemData data)
    {
        owner = ownerTransform;
        itemData = data;
        timer = 0f;
        isSwinging = false;
        level = 1;

        if (scanner == null && owner != null)
            scanner = owner.GetComponent<TargetScanner2D>();

        if (damageDealer == null)
            damageDealer = GetComponentInChildren<DamageDealer>();

        if (afterImage == null)
            afterImage = GetComponentInChildren<AfterImage1>();

        if (owner != null)
            playerStatSystem = owner.GetComponent<PlayerStatSystem>();

        if (damageDealer != null)
        {
            damageDealer.SetOwner(owner);
            damageDealer.SetDamage(GetDamageByLevel(level));
            damageDealer.SetHitCooldown(hitCooldown);
            damageDealer.SetDespawnOnHit(false);
        }

        gameObject.SetActive(true);
    }

    public void OnUnequip()
    {
        StopAllCoroutines();
    }

    public void SetLevel(int newLevel)
    {
        level = Mathf.Max(1, newLevel);

        if (itemData != null)
            level = itemData.ClampLevel(level);

        if (damageDealer != null)
            damageDealer.SetDamage(GetDamageByLevel(level));
    }

    private float GetDamageByLevel(int currentLevel)
    {
        currentLevel = Mathf.Max(1, currentLevel);

        if (itemData != null)
            return Mathf.Max(0.01f, itemData.GetDamageAtLevel(currentLevel));

        return fallbackBaseDamage + (currentLevel - 1) * 3f;
    }

    private void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm != null && (!gm.isLive || gm.isPaused))
            return;

        if (owner == null)
            return;

        timer += Time.deltaTime;

        if (isSwinging)
            return;

        if (timer < GetAttackInterval())
            return;

        Transform target = GetTarget();
        if (target == null)
            return;

        timer = 0f;
        StartCoroutine(SwingRoutine(target));
    }

    private Transform GetTarget()
    {
        if (scanner != null && scanner.NearestTarget != null)
            return scanner.NearestTarget;

        return null;
    }

    private IEnumerator SwingRoutine(Transform target)
    {
        isSwinging = true;

        Vector3 dir = target.position - owner.position;
        dir.z = 0f;

        if (dir.sqrMagnitude < 0.0001f)
            dir = Vector3.right;

        dir.Normalize();

        transform.position = owner.position + dir * attackDistance;

        float baseAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        float startAngle = baseAngle - swingAngle * 0.5f;
        float endAngle = baseAngle + swingAngle * 0.5f;

        if (afterImage != null)
            afterImage.StartGhostEffect();

        float elapsed = 0f;

        while (elapsed < swingDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / swingDuration);

            float z = Mathf.Lerp(startAngle, endAngle, t);
            transform.position = owner.position + dir * attackDistance;
            transform.rotation = Quaternion.Euler(0f, 0f, z);

            yield return null;
        }

        if (afterImage != null)
            afterImage.StopGhostEffect();

        isSwinging = false;
    }

    private void LateUpdate()
    {
        if (owner == null)
            return;

        if (!isSwinging)
            transform.position = owner.position;
    }

    private float GetAttackInterval()
    {
        float attackSpeedMul = 1f;
        if (playerStatSystem != null)
            attackSpeedMul = Mathf.Max(0.01f, playerStatSystem.GetAttackSpeedMultiplier());

        return Mathf.Max(0.05f, attackInterval / attackSpeedMul);
    }
}