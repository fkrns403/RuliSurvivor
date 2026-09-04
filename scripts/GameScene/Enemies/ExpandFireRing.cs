using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 보스 화염 링 / 장판 피해 처리.
/// 
/// 수정 핵심:
/// - GetComponentInParent<IDamageable>() 사용 금지.
/// - Trigger Collider는 무시한다.
/// - PlayerHealth가 직접 붙은 플레이어 본체 Collider만 피해 대상으로 본다.
/// - 같은 플레이어에게 너무 빠르게 반복 피해가 들어가지 않도록 hitCooldown을 둔다.
/// 
/// 사용 예:
/// - FireRing 이펙트
/// - FlameRingEffectAttack
/// - FlameRingEffectBigAttack
/// </summary>
[DisallowMultipleComponent]
public class ExpandFireRing : MonoBehaviour
{
    [Header("Scale")]
    [SerializeField] private bool expandOverTime = true;
    [SerializeField] private float startScale = 1f;
    [SerializeField] private float endScale = 6f;
    [SerializeField] private float expandDuration = 1.5f;

    [Header("Damage")]
    [SerializeField] private float damage = 10f;

    [Tooltip("같은 플레이어에게 반복 피해가 들어가는 최소 간격")]
    [SerializeField] private float hitCooldown = 0.5f;

    [Header("Life")]
    [SerializeField] private float lifeTime = 2f;

    private float timer;
    private readonly Dictionary<PlayerHealth, float> lastHitTimes = new Dictionary<PlayerHealth, float>();

    private void OnEnable()
    {
        timer = 0f;
        lastHitTimes.Clear();

        if (expandOverTime)
            transform.localScale = Vector3.one * startScale;
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (expandOverTime)
        {
            float t = expandDuration <= 0f ? 1f : Mathf.Clamp01(timer / expandDuration);
            float scale = Mathf.Lerp(startScale, endScale, t);
            transform.localScale = Vector3.one * scale;
        }

        if (timer >= lifeTime)
            ReturnToPool();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDamagePlayer(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryDamagePlayer(other);
    }

    private void TryDamagePlayer(Collider2D other)
    {
        if (other == null)
            return;

        // 보조 Area Trigger 방지
        if (other.isTrigger)
            return;

        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();

        if (playerHealth == null)
            return;

        if (playerHealth.IsInvincible)
            return;

        if (lastHitTimes.TryGetValue(playerHealth, out float lastTime))
        {
            if (Time.time - lastTime < hitCooldown)
                return;
        }

        lastHitTimes[playerHealth] = Time.time;
        playerHealth.TakeDamage(damage);
    }

    private void ReturnToPool()
    {
        if (SpecialBulletPoolManager.Instance != null)
            SpecialBulletPoolManager.Instance.ReleaseSpecial(gameObject);
        else
            gameObject.SetActive(false);
    }
}