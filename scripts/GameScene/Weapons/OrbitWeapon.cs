using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 회전 무기.
/// 
/// 사용 예:
/// - BL책
/// - 플레이어 주변을 도는 책/탄환/마법구체류
/// 
/// 역할:
/// - 플레이어 주변에 여러 개의 히트박스를 회전시킨다.
/// - ItemData의 데미지, 개수, 범위를 반영한다.
/// 
/// ItemData 의미:
/// - baseDamage + damages = 공격력 배율 계산
/// - baseCount + counts = 최종 회전 투사체 개수
/// - ranges = 회전 반경
/// </summary>
public class OrbitWeapon : MonoBehaviour, IWeaponRuntime
{
    [Header("Orbit")]
    [SerializeField] private float fallbackRadius = 1.5f;
    [SerializeField] private float rotateSpeedDeg = 180f;

    [Header("Hitbox Prefab")]
    [SerializeField] private GameObject orbitHitboxPrefab;

    [Header("Fallback Stats")]
    [SerializeField] private float fallbackDamage = 5f;
    [SerializeField] private int fallbackCount = 1;
    [SerializeField] private float hitCooldown = 0.2f;

    private Transform owner;
    private ItemData itemData;
    private int level = 1;
    private float angleOffset;

    private readonly List<GameObject> hitboxes = new List<GameObject>();

    public void OnEquip(Transform ownerTransform, ItemData data)
    {
        owner = ownerTransform;
        itemData = data;
        level = 1;
        angleOffset = 0f;

        Rebuild(GetCountByLevel(level));
        ApplyDamage(GetDamageByLevel(level));
    }

    public void OnUnequip()
    {
        for (int i = 0; i < hitboxes.Count; i++)
        {
            if (hitboxes[i] != null)
                Destroy(hitboxes[i]);
        }

        hitboxes.Clear();
    }

    public void SetLevel(int newLevel)
    {
        level = Mathf.Max(1, newLevel);

        if (itemData != null)
            level = itemData.ClampLevel(level);

        Rebuild(GetCountByLevel(level));
        ApplyDamage(GetDamageByLevel(level));
    }

    private void LateUpdate()
    {
        if (owner == null)
            return;

        transform.position = owner.position;
        angleOffset += rotateSpeedDeg * Time.deltaTime;

        int n = hitboxes.Count;
        if (n <= 0)
            return;

        float radius = GetRadiusByLevel(level);
        float step = 360f / n;

        for (int i = 0; i < n; i++)
        {
            GameObject hb = hitboxes[i];
            if (hb == null)
                continue;

            float a = (angleOffset + step * i) * Mathf.Deg2Rad;

            hb.transform.localPosition = new Vector3(
                Mathf.Cos(a) * radius,
                Mathf.Sin(a) * radius,
                0f
            );
        }
    }

    private void Rebuild(int count)
    {
        count = Mathf.Max(1, count);

        for (int i = 0; i < hitboxes.Count; i++)
        {
            if (hitboxes[i] != null)
                Destroy(hitboxes[i]);
        }

        hitboxes.Clear();

        if (orbitHitboxPrefab == null)
            return;

        for (int i = 0; i < count; i++)
        {
            GameObject hb = Instantiate(orbitHitboxPrefab, transform);
            hb.name = $"OrbitHitbox_{i}";

            DamageDealer dealer = hb.GetComponentInChildren<DamageDealer>();
            if (dealer != null)
            {
                dealer.SetOwner(owner);
                dealer.SetDamage(GetDamageByLevel(level));
                dealer.SetHitCooldown(hitCooldown);
                dealer.SetDespawnOnHit(false);
            }

            hitboxes.Add(hb);
        }
    }

    private void ApplyDamage(float damage)
    {
        for (int i = 0; i < hitboxes.Count; i++)
        {
            GameObject hb = hitboxes[i];
            if (hb == null)
                continue;

            DamageDealer dealer = hb.GetComponentInChildren<DamageDealer>();
            if (dealer != null)
                dealer.SetDamage(damage);
        }
    }

    private float GetDamageByLevel(int currentLevel)
    {
        if (itemData != null)
            return Mathf.Max(0.01f, itemData.GetDamageAtLevel(currentLevel));

        return Mathf.Max(0.01f, fallbackDamage);
    }

    private int GetCountByLevel(int currentLevel)
    {
        if (itemData != null)
            return Mathf.Max(1, itemData.GetCountAtLevel(currentLevel));

        return Mathf.Max(1, fallbackCount);
    }

    private float GetRadiusByLevel(int currentLevel)
    {
        if (itemData != null)
            return Mathf.Max(0.1f, itemData.GetRangeAtLevel(currentLevel));

        return Mathf.Max(0.1f, fallbackRadius);
    }
}