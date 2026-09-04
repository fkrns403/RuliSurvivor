using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 무기 장착 / 관리 시스템
/// 
/// 역할:
/// - 무기 프리팹을 weaponRoot 아래에 장착한다.
/// - 같은 ItemType 무기가 이미 있으면 새로 만들지 않고 SetLevel만 호출한다.
/// - ItemData를 무기 런타임에 전달한다.
/// - 무기 해제 / 전체 초기화 지원
/// 
/// 설계 의도:
/// - PlayerItemApplier는 "무슨 타입을 장착할지"만 결정한다.
/// - WeaponSystem은 "실제 장착 / 보관 / 레벨 적용"만 담당한다.
/// - 개별 무기의 동작은 각 무기 런타임 스크립트가 담당한다.
/// </summary>
[DisallowMultipleComponent]
public class WeaponSystem : MonoBehaviour
{
    [Header("Owner")]
    [Tooltip("무기의 소유자(보통 Player Transform)")]
    [SerializeField] private Transform owner;

    [Header("Weapon Root")]
    [Tooltip("장착 무기들을 정리해서 붙여둘 부모 Transform. 비우면 자기 자신을 사용한다.")]
    [SerializeField] private Transform weaponRoot;

    /// <summary>
    /// 현재 장착된 무기를 타입별로 보관한다.
    /// </summary>
    private readonly Dictionary<ItemType, IWeaponRuntime> equippedByType
        = new Dictionary<ItemType, IWeaponRuntime>();

    private void Awake()
    {
        if (owner == null)
            owner = transform;

        if (weaponRoot == null)
            weaponRoot = transform;
    }

    /// <summary>
    /// 무기를 장착하거나, 이미 있으면 레벨만 갱신한다.
    /// 
    /// 매개변수:
    /// - type        : 장착할 무기 타입
    /// - weaponPrefab: IWeaponRuntime을 구현한 MonoBehaviour 프리팹
    /// - data        : 해당 무기의 ItemData
    /// - level       : 적용할 실제 레벨(1부터 시작)
    /// </summary>
    public bool EquipOrLevelUp(ItemType type, MonoBehaviour weaponPrefab, ItemData data, int level)
    {
        if (weaponPrefab == null)
        {
            Debug.LogWarning($"WeaponSystem: weaponPrefab is null for type={type}");
            return false;
        }

        if (data == null)
        {
            Debug.LogWarning($"WeaponSystem: ItemData is null for type={type}");
            return false;
        }

        level = Mathf.Max(1, data.ClampLevel(level));

        // 이미 장착된 무기가 있으면 새로 만들지 않고 레벨만 올린다.
        if (equippedByType.TryGetValue(type, out IWeaponRuntime existing))
        {
            if (existing != null)
            {
                existing.SetLevel(level);
                return true;
            }

            // 끊어진 참조가 남아 있으면 정리
            equippedByType.Remove(type);
        }

        // 새 무기 인스턴스 생성
        MonoBehaviour instance = Instantiate(weaponPrefab, weaponRoot);

        Transform tr = instance.transform;
        tr.localPosition = Vector3.zero;
        tr.localRotation = Quaternion.identity;
        tr.localScale = Vector3.one;

        IWeaponRuntime runtime = instance as IWeaponRuntime;
        if (runtime == null)
        {
            Debug.LogError(
                $"WeaponSystem: prefab '{weaponPrefab.name}' does not implement IWeaponRuntime."
            );
            Destroy(instance.gameObject);
            return false;
        }

        runtime.OnEquip(owner, data);
        runtime.SetLevel(level);

        equippedByType[type] = runtime;
        return true;
    }

    /// <summary>
    /// 특정 타입 무기가 장착되어 있는지 확인
    /// </summary>
    public bool HasWeapon(ItemType type)
    {
        return equippedByType.ContainsKey(type) && equippedByType[type] != null;
    }

    /// <summary>
    /// 특정 무기 해제
    /// </summary>
    public void Unequip(ItemType type)
    {
        if (!equippedByType.TryGetValue(type, out IWeaponRuntime runtime))
            return;

        if (runtime != null)
        {
            runtime.OnUnequip();

            MonoBehaviour mb = runtime as MonoBehaviour;
            if (mb != null)
                Destroy(mb.gameObject);
        }

        equippedByType.Remove(type);
    }

    /// <summary>
    /// 전체 무기 해제
    /// </summary>
    public void UnequipAll()
    {
        List<ItemType> keys = new List<ItemType>(equippedByType.Keys);

        for (int i = 0; i < keys.Count; i++)
            Unequip(keys[i]);

        equippedByType.Clear();
    }
}