public enum ItemType
{
    // 경험치 / 즉시 효과
    ExpSmall,
    ExpMedium,
    ExpLarge,
    Magnet,

    // 무기 타입
    Weapon_Melee,        // 고정 근접 히트박스 무기
    Weapon_Orbit,        // 플레이어 주변 회전 투사체 무기
    Weapon_Range,        // 일반 원거리 / 관통 탄환 무기
    Weapon_Shotgun,      // 산탄 원거리 무기
    Weapon_Arena,        // 아렌 계열 하이브리드 무기
    Weapon_Fuga,         // 불화살 열화판 활 무기
    Weapon_Mace,         // 둔기 스윙 무기
    Weapon_MapleSword,   // 검 스윙 + 검격 발사 무기
    Weapon_Drain,        // 주변 오라 지속 피해 무기
    Weapon_Pierce,       // 거대탄 / 관통 특화 무기
    Weapon_PoisonOrb,    // 하트 오브 / 독 오브 지속 피해 무기
    Weapon_Lightning,    // 낙뢰 무기

    // 장비 / 패시브
    Equip_Glove,
    Equip_Shoe,
    Equip_Heal,
    Equip_Jewel,
    Equip_Breadbone,
    Equip_Regeneration
}