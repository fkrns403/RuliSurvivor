using UnityEngine;

/// <summary>
/// 폭발형 화염 데미지.
/// 
/// 역할:
/// - Fire 패턴의 폭발 이펙트에 붙는다.
/// - 플레이어가 Trigger에 닿으면 피해를 준다.
/// - 일정 시간이 지나면 풀로 반환한다.
/// 
/// 중요한 수정 사항:
/// - 기존 코드는 Start()에서 Destroy(gameObject)를 사용했다.
/// - SpecialAttack은 이 오브젝트를 SpecialBulletPoolManager에서 꺼낼 수 있으므로,
///   Destroy 대신 ReleaseSpecial을 사용하는 편이 안전하다.
/// - 풀링 재사용 시에도 수명이 다시 초기화되도록 OnEnable에서 타이머를 초기화한다.
/// </summary>
[DisallowMultipleComponent]
public class FireBurstDamage : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private float damage = 30f;

    [Header("Life")]
    [SerializeField] private float activeTime = 1.5f;

    [Header("Pool Return")]
    [Tooltip("수명 종료 시 SpecialBulletPoolManager로 반환할지 여부")]
    [SerializeField] private bool returnToSpecialPool = true;

    private float timer;
    private bool returned;

    private void OnEnable()
    {
        timer = 0f;
        returned = false;
    }

    private void Update()
    {
        GameManager gm = GameManager.Instance;

        if (gm != null && gm.isPaused)
            return;

        timer += Time.deltaTime;

        if (timer >= Mathf.Max(0.05f, activeTime))
        {
            ReturnOrDisable();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (returned)
            return;

        if (!collision.CompareTag("Player"))
            return;

        IDamageable damageable = collision.GetComponent<IDamageable>();

        if (damageable == null)
            damageable = collision.GetComponentInParent<IDamageable>();

        if (damageable == null)
            return;

        if (damageable.IsInvincible)
            return;

        damageable.TakeDamage(damage);
    }

    private void ReturnOrDisable()
    {
        if (returned)
            return;

        returned = true;

        if (returnToSpecialPool && SpecialBulletPoolManager.Instance != null)
        {
            SpecialBulletPoolManager.Instance.ReleaseSpecial(gameObject);
            return;
        }

        gameObject.SetActive(false);
    }
}