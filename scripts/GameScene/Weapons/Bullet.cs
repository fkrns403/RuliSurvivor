using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 발사체.
/// 
/// 담당:
/// - 이동 방향으로 Rigidbody2D 발사
/// - 필요 시 스프라이트 회전 보정
/// - IDamageable 대상에게 피해 적용
/// - 적 넉백 적용
/// - pierce 소진, Area 이탈, 수명 종료 시 풀 반환
/// 
/// 회전 보정:
/// - 일반 탄환 기본값은 0.
/// - 불화살/검격 프리팹만 Visual Rotation Offset을 90 또는 -90으로 조정한다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class Bullet : MonoBehaviour
{
    [Header("Damage")]
    public float damage;

    [Tooltip("-100이면 무한 관통")]
    public int pierce;

    [Header("Move")]
    [SerializeField] private float defaultLifeTime = 8f;

    [Header("Visual Rotation")]
    [SerializeField] private bool rotateToMoveDirection = true;

    [Tooltip("일반탄은 0. 불화살/검격이 옆으로 누우면 90 또는 -90으로 조정.")]
    [SerializeField] private float visualRotationOffset = 0f;

    [Header("Hit Filter")]
    [SerializeField] private LayerMask hitMask = ~0;

    [SerializeField] private bool preventMultiHitSameTarget = true;

    [Header("Area Tag")]
    [SerializeField] private string areaTag = "Area";
    [SerializeField] private bool acceptLegacyLowercaseAreaTag = true;

    [Header("Hit Feedback")]
    [SerializeField] private bool applyEnemyKnockback = true;

    private Rigidbody2D rb;
    private float lifeTimer;
    private float lifeTime;

    private readonly HashSet<int> hitIds = new HashSet<int>();

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        hitIds.Clear();

        lifeTimer = 0f;
        lifeTime = Mathf.Max(0.1f, defaultLifeTime);

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.gravityScale = 0f;
        }
    }

    private void Update()
    {
        GameManager gm = GameManager.Instance;

        if (gm != null && gm.isPaused)
            return;

        lifeTimer += Time.deltaTime;

        if (lifeTimer >= lifeTime)
            ReturnToPool();
    }

    public void Init(float dmg, int per, Vector3 dir, float speed = 15f)
    {
        Init(dmg, per, dir, speed, visualRotationOffset);
    }

    public void Init(float dmg, int per, Vector3 dir, float speed, float rotationOffset)
    {
        damage = Mathf.Max(0f, dmg);
        pierce = per;
        visualRotationOffset = rotationOffset;

        lifeTimer = 0f;
        lifeTime = Mathf.Max(0.1f, defaultLifeTime);

        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        Vector2 moveDir = dir.sqrMagnitude > 0.0001f
            ? ((Vector2)dir).normalized
            : Vector2.right;

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.angularVelocity = 0f;
            rb.velocity = moveDir * speed;
        }

        ApplyVisualRotation(moveDir);
    }

    public void SetVisualRotationOffset(float offset)
    {
        visualRotationOffset = offset;

        if (rb != null && rb.velocity.sqrMagnitude > 0.0001f)
            ApplyVisualRotation(rb.velocity.normalized);
    }

    public void SetRotateToMoveDirection(bool value)
    {
        rotateToMoveDirection = value;
    }

    private void ApplyVisualRotation(Vector2 dir)
    {
        if (!rotateToMoveDirection)
            return;

        if (dir.sqrMagnitude < 0.0001f)
            return;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle + visualRotationOffset);
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (col == null)
            return;

        if (!IsInHitMask(col.gameObject))
            return;

        IDamageable damageable = col.GetComponent<IDamageable>();

        if (damageable == null)
            damageable = col.GetComponentInParent<IDamageable>();

        if (damageable == null)
            return;

        if (preventMultiHitSameTarget)
        {
            int id = damageable.GetHashCode();

            if (hitIds.Contains(id))
                return;

            hitIds.Add(id);
        }

        if (!damageable.IsInvincible)
        {
            damageable.TakeDamage(damage);

            if (applyEnemyKnockback)
                TryApplyEnemyKnockback(damageable);
        }

        if (pierce == -100)
            return;

        pierce--;

        if (pierce < 0)
            ReturnToPool();
    }

    private void TryApplyEnemyKnockback(IDamageable damageable)
    {
        Component component = damageable as Component;

        if (component == null)
            return;

        Enemy enemy = component.GetComponentInParent<Enemy>();

        if (enemy == null)
            return;

        enemy.ApplyKnockBack();
    }

    private void OnTriggerExit2D(Collider2D col)
    {
        if (col == null)
            return;

        if (!IsAreaTrigger(col))
            return;

        if (pierce == -100)
            return;

        ReturnToPool();
    }

    private bool IsInHitMask(GameObject obj)
    {
        if (obj == null)
            return false;

        int bit = 1 << obj.layer;
        return (hitMask.value & bit) != 0;
    }

    private bool IsAreaTrigger(Collider2D col)
    {
        if (col == null)
            return false;

        if (!string.IsNullOrEmpty(areaTag) && col.CompareTag(areaTag))
            return true;

        if (acceptLegacyLowercaseAreaTag && col.gameObject.tag == "area")
            return true;

        return false;
    }

    private void ReturnToPool()
    {
        if (!gameObject.activeInHierarchy)
            return;

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        if (BulletPoolManager.Instance != null)
        {
            BulletPoolManager.Instance.ReleaseBullet(gameObject);
            return;
        }

        gameObject.SetActive(false);
    }
}