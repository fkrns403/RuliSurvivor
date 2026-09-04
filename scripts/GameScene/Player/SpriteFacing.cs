using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 좌우 반전 전담 클래스
/// - SpriteRenderer의 flipX를 조절해서 캐릭터가 바라보는 방향을 바꿈
/// </summary>

[RequireComponent(typeof(SpriteRenderer))]
//SpriteRenderer 컴포넌트 없을시 오류 발생 유도

public class SpriteFacing : MonoBehaviour
{
   
    private SpriteRenderer _sr;
    /// <summary>
    /// 현재 오브젝트의 SpriteRenderer
    /// </summary>
    
   
    public void Initialize(SpriteRenderer sr)
    {
        _sr = sr;
        /// <summary>
        /// PlayerController에서 SpriteRenderer를 전달받아 초기화
        /// </summary>
    }

    
    public void UpdateFacing(Vector2 moveInput)
    {
        if (_sr == null)
        {
            return;
        }

        if (moveInput.x != 0f)
        {
            // 왼쪽이면 true, 오른쪽이면 false
            _sr.flipX = moveInput.x < 0f;
        }
    }
    /// <summary>
    /// 이동 입력 벡터에 따라 flipX를 설정
    /// - x 값이 0이 아닐 때만 방향 전환
    /// </summary>
}
