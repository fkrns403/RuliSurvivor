using UnityEngine;

/// <summary>
/// 장착형 무기 런타임 인터페이스
/// 
/// 공통 규칙:
/// - OnEquip(owner, data) : 처음 장착될 때 1회 호출
/// - OnUnequip()          : 제거될 때 호출
/// - SetLevel(level)      : 무기 레벨 갱신
/// 
/// level 규칙:
/// - 실제 레벨은 1부터 시작한다.
/// - 예: 최초 획득 시 level = 1
/// </summary>
public interface IWeaponRuntime
{
    void OnEquip(Transform owner, ItemData data);
    void OnUnequip();
    void SetLevel(int level);
}