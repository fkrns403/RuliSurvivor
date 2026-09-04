using UnityEngine;

/// <summary>
/// 보스/특수 패턴 탄환.
/// 
/// 수정 핵심:
/// - GetComponentInParent<IDamageable>() 사용 금지.
/// - Trigger Collider는 전부 무시한다.
/// - PlayerHealth가 직접 붙은 플레이어 본체 Collider에만 데미지를 준다.
/// 
/// 이유:
/// - RepositionArea, MagnetArea, SkillArea 같은 자식 Trigger가 맞으면
///   부모의 PlayerHealth를 찾아 지속 데미지가 들어가는 문제가 생긴다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class BulletSpecial : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private float damage = 10f;

    [Header("Fallback Lifetime")]
    [SerializeField] private float lifeTime = 15f;

    private float timer;
    private Rigidbody2D rb;
    private Collider2D col;
    private PooledAutoRelease autoRelease;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        autoRelease = GetComponent<PooledAutoRelease>();
    }

    private void OnEnable()
    {
        timer = 0f;

        if (col != null)
            col.enabled = true;

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;

            // 탄환은 연출상 회전 고정을 풀어둔다.
            rb.constraints = RigidbodyConstraints2D.None;
        }
    }

    private void Update()
    {
        GameManager gm = GameManager.Instance;

        if (gm != null && gm.isPaused)
            return;

        if (autoRelease != null)
            return;

        timer += Time.deltaTime;

        if (timer >= lifeTime)
            ReturnToPool();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDamagePlayer(other);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryDamagePlayer(collision.collider);
    }

    private void TryDamagePlayer(Collider2D other)
    {
        if (other == null)
            return;

        // RepositionArea / MagnetArea / SkillArea 방지
        if (other.isTrigger)
            return;

        // 플레이어 본체 Collider에 PlayerHealth가 직접 붙어 있어야만 데미지
        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();

        if (playerHealth == null)
            return;

        if (playerHealth.IsInvincible)
            return;

        playerHealth.TakeDamage(damage);
        ReturnToPool();
    }

    private void ReturnToPool()
    {
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        if (SpecialBulletPoolManager.Instance != null)
            SpecialBulletPoolManager.Instance.ReleaseSpecial(gameObject);
        else
            gameObject.SetActive(false);
    }
}