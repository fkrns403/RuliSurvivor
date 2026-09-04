using UnityEngine;

/// <summary>
/// 데미지 배열 값을 어떻게 해석할지 결정한다.
/// 
/// FinalValue:
/// - damages 배열 값을 최종 데미지로 사용한다.
/// - 예: 5, 7, 10
/// 
/// Multiplier:
/// - baseDamage * damages 배열 값으로 최종 데미지를 계산한다.
/// - 예: baseDamage 5, damages 1.5이면 최종 데미지 7.5
/// </summary>
public enum ItemDamageValueMode
{
    FinalValue,
    Multiplier
}

/// <summary>
/// 개수 배열 값을 어떻게 해석할지 결정한다.
/// 
/// FinalValue:
/// - counts 배열 값을 최종 개수로 사용한다.
/// - 예: 2, 3, 4
/// 
/// Additive:
/// - baseCount + counts 배열 값으로 최종 개수를 계산한다.
/// - 예: baseCount 2, counts 1이면 최종 개수 3
/// </summary>
public enum ItemCountValueMode
{
    FinalValue,
    Additive
}

/// <summary>
/// 아이템 데이터.
/// 
/// 역할:
/// - 레벨업 UI에 표시할 이름, 설명, 아이콘을 보관한다.
/// - 무기/장비의 성장 데이터를 보관한다.
/// - 데미지, 개수, 사거리/범위의 레벨별 성장값을 제공한다.
/// 
/// 핵심 설계:
/// - 데미지는 최종값 또는 배율 방식 중 선택 가능하다.
/// - 개수는 최종 개수 또는 증가량 방식 중 선택 가능하다.
/// - 기존 설명처럼 "투사체 n개 증가"를 쓰려면 Count Value Mode를 Additive로 둔다.
/// </summary>
[CreateAssetMenu(fileName = "Item", menuName = "Scriptable Object/ItemData")]
public class ItemData : ScriptableObject
{
    [Header("# Main Info")]
    public ItemType ItemType;

    [Tooltip("구버전 호환용. 현 구조에서는 대부분 비워둔다.")]
    public GameObject prefab;

    public int itemId;
    public string itemName;

    [TextArea]
    public string itemDesc;

    public Sprite itemIcon;

    [Header("# Damage Data")]
    [Tooltip("기본 데미지. Multiplier 모드에서는 이 값에 damages 배열 값을 곱한다.")]
    public float baseDamage = 1f;

    [Tooltip("damages 배열 해석 방식")]
    public ItemDamageValueMode damageValueMode = ItemDamageValueMode.Multiplier;

    [Tooltip("레벨별 데미지 값. Multiplier 모드에서는 배율로 사용한다.")]
    public float[] damages;

    [Header("# Count Data")]
    [Tooltip("기본 개수. Additive 모드에서는 이 값에 counts 배열 값을 더한다.")]
    public int baseCount = 1;

    [Tooltip("counts 배열 해석 방식")]
    public ItemCountValueMode countValueMode = ItemCountValueMode.Additive;

    [Tooltip("레벨별 개수 값. Additive 모드에서는 증가량으로 사용한다.")]
    public int[] counts;

    [Header("# Range / Distance")]
    [Tooltip("범위/사거리 기본값")]
    public float baseRange = 1f;

    [Tooltip("레벨별 범위/사거리 최종값. 비어 있으면 baseRange를 사용한다.")]
    public float[] ranges;

    [Header("# Exp")]
    [Tooltip("경험치 즉시 지급형 아이템일 때 사용")]
    public int expAmount;

    [Header("# Over Level")]
    [Tooltip("기본 성장 구간 이후에도 계속 성장할 수 있는지 여부")]
    public bool allowOverLevel = false;

    [Tooltip("최대 레벨 제한. 0이면 무제한")]
    public int maxLevelCap = 0;

    [Tooltip("오버레벨 1회당 추가 데미지. Multiplier 모드에서는 배율에 더해진다.")]
    public float overLevelDamageBonus = 0.2f;

    [Tooltip("오버레벨 1회당 추가 개수. Additive 모드에서는 증가량에 더해진다.")]
    public int overLevelCountBonus = 0;

    [Tooltip("오버레벨 1회당 범위/사거리 증가량")]
    public float overLevelRangeBonus = 0f;

    [Header("# Legacy Weapon Fields")]
    [Tooltip("구버전 호환용. 현 구조에서는 대부분 비워둔다.")]
    public GameObject projectile;

    [Tooltip("구버전 호환용. 현 구조에서는 대부분 비워둔다.")]
    public GameObject projectile2;

    public int GetBaseMaxLevel()
    {
        int damageLen = damages != null ? damages.Length : 0;
        int countLen = counts != null ? counts.Length : 0;
        int rangeLen = ranges != null ? ranges.Length : 0;

        return Mathf.Max(1, damageLen, countLen, rangeLen);
    }

    public bool CanLevelUpFrom(int currentRealLevel)
    {
        if (maxLevelCap > 0 && currentRealLevel >= maxLevelCap)
            return false;

        int baseMax = GetBaseMaxLevel();

        if (currentRealLevel < baseMax)
            return true;

        return allowOverLevel;
    }

    public int ClampLevel(int realLevel)
    {
        realLevel = Mathf.Max(1, realLevel);

        if (maxLevelCap > 0)
            return Mathf.Min(realLevel, maxLevelCap);

        return realLevel;
    }

    /// <summary>
    /// 실제 최종 데미지를 반환한다.
    /// </summary>
    public float GetDamageAtLevel(int realLevel)
    {
        realLevel = Mathf.Max(1, realLevel);

        int baseMax = GetBaseMaxLevel();
        float raw = GetRawDamageAtLevel(Mathf.Min(realLevel, baseMax));

        if (realLevel > baseMax && allowOverLevel)
        {
            int overLevel = realLevel - baseMax;
            raw += overLevel * overLevelDamageBonus;
        }

        if (damageValueMode == ItemDamageValueMode.Multiplier)
            return baseDamage * raw;

        return raw;
    }

    /// <summary>
    /// 설명 표시용 데미지 값.
    /// Multiplier 모드에서는 배율값을 그대로 반환한다.
    /// 예: 1.5
    /// </summary>
    public float GetDamageDisplayValueAtLevel(int realLevel)
    {
        realLevel = Mathf.Max(1, realLevel);

        int baseMax = GetBaseMaxLevel();
        float raw = GetRawDamageAtLevel(Mathf.Min(realLevel, baseMax));

        if (realLevel > baseMax && allowOverLevel)
        {
            int overLevel = realLevel - baseMax;
            raw += overLevel * overLevelDamageBonus;
        }

        return raw;
    }

    /// <summary>
    /// 설명 표시용 데미지 증가율.
    /// Multiplier 1.5이면 50을 반환한다.
    /// </summary>
    public float GetDamagePercentIncreaseAtLevel(int realLevel)
    {
        float display = GetDamageDisplayValueAtLevel(realLevel);

        if (damageValueMode == ItemDamageValueMode.Multiplier)
            return (display - 1f) * 100f;

        if (baseDamage <= 0f)
            return 0f;

        return ((display / baseDamage) - 1f) * 100f;
    }

    /// <summary>
    /// 실제 최종 개수를 반환한다.
    /// 무기 스크립트는 이 값을 사용하면 된다.
    /// </summary>
    public int GetCountAtLevel(int realLevel)
    {
        realLevel = Mathf.Max(1, realLevel);

        int baseMax = GetBaseMaxLevel();
        int raw = GetRawCountAtLevel(Mathf.Min(realLevel, baseMax));

        if (realLevel > baseMax && allowOverLevel)
        {
            int overLevel = realLevel - baseMax;
            raw += overLevel * overLevelCountBonus;
        }

        if (countValueMode == ItemCountValueMode.Additive)
            return baseCount + raw;

        return raw;
    }

    /// <summary>
    /// 설명 표시용 개수 값.
    /// Additive 모드에서는 증가량을 반환한다.
    /// 예: baseCount 2, counts 1이면 1 반환
    /// </summary>
    public int GetCountDisplayValueAtLevel(int realLevel)
    {
        realLevel = Mathf.Max(1, realLevel);

        int baseMax = GetBaseMaxLevel();
        int raw = GetRawCountAtLevel(Mathf.Min(realLevel, baseMax));

        if (realLevel > baseMax && allowOverLevel)
        {
            int overLevel = realLevel - baseMax;
            raw += overLevel * overLevelCountBonus;
        }

        return raw;
    }

    /// <summary>
    /// 실제 범위/사거리 값을 반환한다.
    /// </summary>
    public float GetRangeAtLevel(int realLevel)
    {
        realLevel = Mathf.Max(1, realLevel);

        int baseMax = GetBaseMaxLevel();
        float value = GetBaseRangeAtLevel(Mathf.Min(realLevel, baseMax));

        if (realLevel > baseMax && allowOverLevel)
        {
            int overLevel = realLevel - baseMax;
            value += overLevel * overLevelRangeBonus;
        }

        return value;
    }

    private float GetRawDamageAtLevel(int realLevel)
    {
        if (damages == null || damages.Length == 0)
        {
            if (damageValueMode == ItemDamageValueMode.Multiplier)
                return 1f;

            return baseDamage;
        }

        int index = Mathf.Clamp(realLevel - 1, 0, damages.Length - 1);
        return damages[index];
    }

    private int GetRawCountAtLevel(int realLevel)
    {
        if (counts == null || counts.Length == 0)
        {
            if (countValueMode == ItemCountValueMode.Additive)
                return 0;

            return baseCount;
        }

        int index = Mathf.Clamp(realLevel - 1, 0, counts.Length - 1);
        return counts[index];
    }

    private float GetBaseRangeAtLevel(int realLevel)
    {
        if (ranges == null || ranges.Length == 0)
            return baseRange;

        int index = Mathf.Clamp(realLevel - 1, 0, ranges.Length - 1);
        return ranges[index];
    }
}