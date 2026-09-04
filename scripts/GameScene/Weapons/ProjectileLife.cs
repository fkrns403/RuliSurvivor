using UnityEngine;

/// <summary>
/// 일정 시간이 지나면 자동으로 사라지는 투사체 수명 컴포넌트
/// 
/// 사용 예:
/// - 검격 난사
/// - 대형 탄환
/// - 매혹 탄
/// </summary>
public class ProjectileLife : MonoBehaviour
{
    [SerializeField] private float lifetime = 1f;
    private float endTime;

    /// <summary>
    /// 외부에서 수명 설정
    /// </summary>
    public void SetLifetime(float t)
    {
        lifetime = Mathf.Max(0.05f, t);
        endTime = Time.time + lifetime;
    }

    private void OnEnable()
    {
        endTime = Time.time + lifetime;
    }

    private void Update()
    {
        if (Time.time >= endTime)
            Destroy(gameObject);
    }
}