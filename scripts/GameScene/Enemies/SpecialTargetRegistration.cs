using UnityEngine;

/// <summary>
/// Type2 / Boss 적을 SpecialAttackManager에 등록 / 해제하는 보조 컴포넌트
/// 
/// 사용 이유:
/// - 기존 Enemy / Boss 코드 전체를 크게 수정하지 않고
///   프리팹에 이 컴포넌트만 붙여서 특수패턴 대상 등록을 처리할 수 있다.
/// 
/// 사용 대상:
/// - Type2 적 프리팹
/// - Boss 프리팹
/// </summary>
public class SpecialTargetRegistration : MonoBehaviour
{
    [SerializeField] private EnemyType registeredType = EnemyType.Type2;

    private void OnEnable()
    {
        if (SpecialAttackManager.Instance != null)
            SpecialAttackManager.Instance.NotifyEliteSpawned(transform, registeredType);
    }

    private void OnDisable()
    {
        if (SpecialAttackManager.Instance != null)
            SpecialAttackManager.Instance.NotifyEliteDied(transform, registeredType);
    }
}