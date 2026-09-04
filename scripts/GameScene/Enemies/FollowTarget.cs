using UnityEngine;

/// <summary>
/// 경고 오브젝트를 타겟에 붙여서 따라가게 함
/// - 풀링 재사용 시 target이 남지 않도록 OnDisable에서 정리
/// </summary>
public class FollowTarget : MonoBehaviour
{
    public Transform target;

    [Header("Smooth Follow (선택)")]
    [SerializeField] private bool smoothFollow = false;

    [SerializeField] private float followSpeed = 10f;

    private void LateUpdate()
    {
        if (target == null) return;

        if (!smoothFollow)
        {
            transform.position = target.position;
            return;
        }

        transform.position = Vector3.Lerp(
            transform.position,
            target.position,
            followSpeed * Time.deltaTime
        );
    }

    private void OnDisable()
    {
        target = null;
    }
}