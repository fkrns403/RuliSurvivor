using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 애니메이션 파라미터 갱신 전용
/// - 이동 속도를 애니메이터 파라미터에 전달
/// - Blend Tree 전환에서 사용
/// </summary>

[RequireComponent(typeof(Animator))]
//Animator 컴포넌트 없을시 오류 발생 유도

public class AnimatorDriver : MonoBehaviour
{
    /// <summary>
    /// 애니메이터의 Speed 파라미터 이름
    /// 변경가능
    /// </summary>
    [SerializeField] private string speedParamName = "Speed";

    /// <summary>
    /// 현재 오브젝트의 Animator
    /// </summary>
    private Animator _anim;

    /// <summary>
    /// PlayerController에서 Animator를 전달받아 초기화
    /// </summary>
    public void Initialize(Animator anim)
    {
        _anim = anim;
    }

    /// <summary>
    /// 현재 이동 벡터 크기를 Speed 파라미터에 전달
    /// - 0이면 Idle, 1이면 Run 등으로 활용
    /// </summary>
    public void UpdateSpeedParam(Vector2 moveInput)
    {
        if (_anim == null) 
        { 
            return;
            //null일 경우 함수 조기 종료
        }
        _anim.SetFloat(speedParamName, moveInput.magnitude);
        // _anim.SetFloat("Speed", _move.magnitude);
    }
}
