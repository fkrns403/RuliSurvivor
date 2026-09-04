using System;
using UnityEngine;

/// <summary>
/// 보스 체력 컴포넌트.
/// 
/// 역할:
/// - 보스의 현재 체력 / 최대 체력 / 무적 / 사망 판정을 담당한다.
/// - UI는 직접 담당하지 않는다.
/// - BossHealthBarUI가 이 값을 읽어 보스 체력바를 표시한다.
/// 
/// 수정 핵심:
/// - CurrentHp / MaxHp 프로퍼티 추가
/// - OnHpChanged 이벤트 추가
/// - 보스 UI가 GameManager.health를 잘못 읽지 않도록 BossHealth 전용 값을 제공
/// </summary>
[DisallowMultipleComponent]
public class BossHealth : MonoBehaviour, IDamageable
{
    [Header("Boss Id")]
    [SerializeField] private string bossId = "boss_01";
    public string BossId => bossId;

    [Header("HP")]
    [SerializeField] private float maxHp = 300f;

    [Header("Invincible")]
    [SerializeField] private bool invincible;
    [SerializeField] private float invincibleTimeAfterHit = 0.1f;

    private float hp;
    private float invincibleEndTime;
    private bool dead;

    public event Action<BossHealth> Died;
    public event Action<BossHealth, float, float> HpChanged;

    public bool IsInvincible => invincible || Time.time < invincibleEndTime;

    public float CurrentHp => hp;
    public float MaxHp => maxHp;
    public bool IsDead => dead;

    private void OnEnable()
    {
        hp = Mathf.Max(1f, maxHp);
        invincibleEndTime = 0f;
        dead = false;

        HpChanged?.Invoke(this, hp, maxHp);
    }

    public void TakeDamage(float amount)
    {
        if (amount <= 0f)
            return;

        if (dead)
            return;

        if (IsInvincible)
            return;

        hp = Mathf.Max(0f, hp - amount);

        if (invincibleTimeAfterHit > 0f)
            invincibleEndTime = Time.time + invincibleTimeAfterHit;

        HpChanged?.Invoke(this, hp, maxHp);

        if (hp <= 0f)
            Die();
    }

    public void SetInvincible(bool value)
    {
        invincible = value;
    }

    public void SetMaxHp(float value, bool refill = true)
    {
        maxHp = Mathf.Max(1f, value);

        if (refill)
            hp = maxHp;
        else
            hp = Mathf.Clamp(hp, 0f, maxHp);

        HpChanged?.Invoke(this, hp, maxHp);
    }

    private void Die()
    {
        if (dead)
            return;

        dead = true;
        Died?.Invoke(this);
    }
}