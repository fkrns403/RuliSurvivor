using System;
using UnityEngine;

/// <summary>
/// 캐릭터 도감에 한 페이지로 표시할 데이터.
/// UnlockDefinition과 별도로, 도감에서 보여줄 상세 설명을 모두 묶어둔다.
//// </summary>
[Serializable]
public class CharacterCodexData
{
    [Header("해금과 연결할 ID / 정의")]
    [Tooltip("UnlockDefinition.id 와 동일해야 한다.")]
    public string unlockId;

    [Tooltip("UI에 사용할 UnlockDefinition (아이콘, 이름, 잠금/해금 설명)")]
    public UnlockDefinition unlockDefinition;

    [Header("도감에 표시할 캐릭터 정보")]
    public Sprite portrait;              // 캐릭터 이미지
    public string characterName;
    [TextArea] public string characterDescription;   // 기본 설명
    [TextArea] public string weaponDescription;      // 무기 설명 텍스트
    [TextArea] public string skillDescription;       // 스킬/패시브 설명 텍스트

    [Header("도감용 무기 이미지")]
    [Tooltip("이 캐릭터 도감 페이지에 보여줄 무기 아이콘/이미지")]
    public Sprite weaponIcon;            // ★ 캐릭터별 무기 스프라이트
}
