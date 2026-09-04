using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 해금 대상 타입(캐릭터/몬스터 등)
/// </summary>
public enum UnlockTargetType
{
    Character,
    Monster,
    Etc
}

/// <summary>
/// 하나의 해금 대상(캐릭터 하나, 몬스터 하나 등)에 대한 정의
/// </summary>
[CreateAssetMenu(fileName = "UnlockDefinition", menuName = "Unlock/Definition")]
public class UnlockDefinition : ScriptableObject
{
    [Header("Unique Id (예: 'nyarubi', 'rosetta')")]
    public string id;

    [Header("대상 타입")]
    public UnlockTargetType targetType = UnlockTargetType.Character;

    [Header("UI 표시용")]
    public Sprite icon;
    public string displayName;
    [TextArea] public string lockedDescription;
    [TextArea] public string unlockedDescription;

    [Header("해금 조건(모두 만족해야 해금, AND 조건)")]
    public List<UnlockCondition> conditions = new List<UnlockCondition>();
}
