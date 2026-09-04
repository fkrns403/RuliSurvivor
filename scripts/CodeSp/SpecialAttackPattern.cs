using UnityEngine;

/// <summary>
/// 경고 프리팹의 기본 방향.
/// 
/// Horizontal:
/// - 프리팹 스프라이트가 기본적으로 가로 막대 모양일 때 사용한다.
/// 
/// Vertical:
/// - 프리팹 스프라이트가 기본적으로 세로 막대 모양일 때 사용한다.
/// </summary>
public enum SpecialWarningSpriteAxis
{
    Horizontal,
    Vertical
}

/// <summary>
/// 보스/엘리트 특수 공격 패턴 데이터.
/// 
/// 주의:
/// - SpecialAttackType enum은 별도 파일에 이미 있어야 한다.
/// - 이 파일 안에 SpecialAttackType enum을 다시 선언하면 중복 정의 에러가 난다.
/// 
/// 현재 구조:
/// - SpecialBulletPoolManager가 PoolManager 인덱스로 경고/탄환/이펙트를 꺼낸다.
/// - 그래서 프리팹 직접 참조가 아니라 풀 인덱스를 사용한다.
/// </summary>
[System.Serializable]
public class SpecialAttackPattern
{
    [Header("Pattern Type")]
    public SpecialAttackType attackType;

    [Header("Common Pattern Settings")]
    [Tooltip("이 패턴의 쿨타임")]
    public float cooldown = 10f;

    [Tooltip("탄환 이동 속도")]
    public float bulletSpeed = 10f;

    [Tooltip("Grid는 줄 수, Radial은 방사 탄환 수로 사용한다.")]
    public int lineCount = 5;

    [Header("Grid Shape Settings")]
    [Tooltip("Grid 패턴의 가로/세로 줄 간격")]
    public float gridSpacing = 7f;

    [Tooltip("Grid 탄환을 중심에서 얼마나 먼 거리에서 생성할지")]
    public float gridSpawnDistance = 20f;

    [Tooltip("각 Grid 경고 줄에 배치할 경고 타일 개수")]
    public int gridWarningTileCount = 9;

    [Tooltip("경고 타일 간격. 0이면 gridSpacing을 사용한다.")]
    public float gridWarningTileSpacing = 0f;

    [Header("Warning Sprite Direction")]
    [Tooltip("경고 프리팹 스프라이트의 기본 방향. Alerttiles가 | 모양이면 Vertical로 둔다.")]
    public SpecialWarningSpriteAxis warningSpriteAxis = SpecialWarningSpriteAxis.Vertical;

    [Header("Pool Indices")]
    [Tooltip("경고 타일 풀 인덱스")]
    public int warningIndex = -1;

    [Tooltip("특수 탄환 풀 인덱스")]
    public int bulletIndex = -1;

    [Tooltip("원형 경고 풀 인덱스")]
    public int warningCircleIndex = -1;

    [Tooltip("화염 폭발 이펙트 풀 인덱스")]
    public int fireBurstIndex = -1;

    [Tooltip("화염 링 이펙트 풀 인덱스")]
    public int fireRingIndex = -1;

    [Header("Lifetime")]
    [Tooltip("경고 오브젝트 유지 시간. 0이면 SpecialAttack.warningDelay 사용")]
    public float warningLifeTime = 0f;

    [Tooltip("이펙트 유지 시간")]
    public float effectLifeTime = 2f;

    [Tooltip("탄환 유지 시간")]
    public float bulletLifeTime = 8f;
}