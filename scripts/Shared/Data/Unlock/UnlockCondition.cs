using System;
using UnityEngine;

/// <summary>
/// 해금 조건 종류
/// </summary>
public enum UnlockConditionType
{
    None,
    KillCountByEnemyId,  // 특정 enemyId 를 N마리 처치
    ClearMap,            // 특정 맵을 1회 이상 클리어
    ReachLevel,          // 플레이어 레벨 N 도달
    SurviveSeconds,      // 한 판에서 N초 이상 생존
    OverTimeEntered      // 오버타임에 한 번이라도 진입
}

/// <summary>
/// 개별 해금 조건 하나
/// </summary>
[Serializable]
public class UnlockCondition
{
    [Tooltip("조건 종류")]
    public UnlockConditionType type = UnlockConditionType.None;

    [Tooltip("enemyId 또는 mapId 등 (KillCountByEnemyId, ClearMap 에서 사용)")]
    public string id;

    [Tooltip("정수 값: 필요 킬 수, 필요 레벨 등")]
    public int intValue;

    [Tooltip("실수 값: 생존 시간(초) 등")]
    public float floatValue;

#if UNITY_EDITOR
    public override string ToString()
    {
        switch (type)
        {
            case UnlockConditionType.KillCountByEnemyId:
                return $"Kill {id} x{intValue}";
            case UnlockConditionType.ClearMap:
                return $"Clear map {id}";
            case UnlockConditionType.ReachLevel:
                return $"Reach level {intValue}";
            case UnlockConditionType.SurviveSeconds:
                return $"Survive {floatValue} sec";
            case UnlockConditionType.OverTimeEntered:
                return "Enter OverTime once";
            default:
                return type.ToString();
        }
    }
#endif
}
