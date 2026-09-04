using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 관련 공통 유틸리티
/// - GameManager를 통해 플레이어 Transform/위치를 안전하게 가져오는 기능 모음
/// </summary>
public static class PlayerUtility
{
    /// <summary>
    /// GameManager를 통해 Player Transform을 가져온다.
    /// - 없으면 null 반환
    /// </summary>
    public static Transform TryGetPlayerTransform()
    {
        if (GameManager.Instance == null)
            return null;

        return GameManager.Instance.PlayerTransform;
    }

    /// <summary>
    /// 2D 기준 플레이어 위치를 얻는다.
    /// - 성공 시 true, 실패 시 false 반환
    /// </summary>
    public static bool TryGetPlayerPosition2D(out Vector2 playerPos)
    {
        playerPos = default;

        var playerTf = TryGetPlayerTransform();
        if (playerTf == null)
            return false;

        playerPos = playerTf.position;
        return true;
    }
}
