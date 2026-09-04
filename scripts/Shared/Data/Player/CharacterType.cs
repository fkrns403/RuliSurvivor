/// <summary>
/// 캐릭터별 고유 능력 타입
/// 
/// 용도:
/// - CharacterEntry에서 어떤 캐릭터인지 지정
/// - GameBootstrap이 PlayerAbilityController에 이 값을 넘겨줌
/// - PlayerAbilityController가 이 값을 보고 고유 액티브/패시브를 적용함
/// </summary>
public enum CharacterType
{
    None,

    /// <summary>
    /// 목숨/부활 계열 패시브
    /// </summary>
    NineLives,

    /// <summary>
    /// 적중 시 폭발하는 화살
    /// </summary>
    FireArrow,

    /// <summary>
    /// 대형 탄환 난사
    /// </summary>
    BigBulletSpray,

    /// <summary>
    /// 검격 난사
    /// </summary>
    SwordSlash,

    /// <summary>
    /// 적을 매혹 상태로 바꾸는 하트 탄
    /// </summary>
    CharmHeart
}