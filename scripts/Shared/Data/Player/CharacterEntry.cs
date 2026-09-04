using UnityEngine;

/// <summary>
/// 타이틀/게임 공용 캐릭터 데이터
/// 
/// 역할:
/// - 타이틀 씬에서는 캐릭터 표시용 데이터로 사용
/// - 게임 씬에서는 플레이어 프리팹/시작 무기/고유 스킬 타입을 전달하는 용도로 사용
/// 
/// 주의:
/// - TitleManager.characters 배열 순서와
///   GameBootstrap.characters 배열 순서는 반드시 동일해야 한다.
/// </summary>
public enum CharacterPassiveType
{
    None,
    ExtraLife,      // 기본 라이프 추가(예: 1회 더 부활)
    PeriodicSkill   // n초마다 자동 발동 스킬
}

[System.Serializable]
public class CharacterEntry
{
    [Header("ID (해금/저장용)")]
    [Tooltip("UnlockDefinition.id 등과 연결할 수 있는 고유 문자열")]
    public string id;

    [Header("UI 표시")]
    [Tooltip("타이틀/도감에 표시할 이름")]
    public string displayName;

    [TextArea]
    [Tooltip("타이틀에서 보여줄 패시브 설명")]
    public string passiveDesc;

    [TextArea]
    [Tooltip("타이틀에서 보여줄 무기 설명")]
    public string weaponDesc;

    [Tooltip("캐릭터 대표 초상화")]
    public Sprite portrait;

    [Tooltip("작은 아이콘")]
    public Sprite icon;

    [Header("게임 플레이용 프리팹")]
    [Tooltip("실제로 게임 씬에서 스폰할 플레이어 프리팹")]
    public GameObject playerPrefab;

    [Header("기본 설정")]
    [Tooltip("이 캐릭터의 기본 목숨 수")]
    public int defaultLives = 0;

    [Header("패시브 설정")]
    [Tooltip("기존 패시브 구조가 있다면 여기서 사용")]
    public CharacterPassiveType passiveType = CharacterPassiveType.None;

    [Tooltip("패시브가 주기 발동형일 때 간격(초)")]
    public float passiveInterval = 0f;

    [Header("시작 무기")]
    [Tooltip("게임 시작 시 1레벨로 자동 지급할 ItemData")]
    public ItemData startingWeaponItem;

    [Header("고유 스킬 타입")]
    [Tooltip("이 캐릭터가 사용할 고유 액티브/패시브 타입")]
    public CharacterType characterType = CharacterType.None;
}