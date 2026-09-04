using UnityEngine;

/// <summary>
/// 특수 공격 패턴 타입 정의.
/// 
/// 주의:
/// - SideRushAttack은 기존 구조에 없던 타입이므로 제거한다.
/// - enum 순서가 밀리면 Inspector에 저장된 패턴 타입이 꼬일 수 있다.
/// </summary>
public enum SpecialAttackType
{
    Grid,       // 가로/세로 격자 탄막
    Radial,     // 원형 방사 탄막
    Fire,       // 지점 폭발
    FireRing    // 확장 링 공격
}