using System;
using UnityEngine;

/// <summary>
/// 일반 적 체력 처리.
/// 
/// 역할:
/// - 적의 현재 체력 / 최대 체력 관리
/// - 피해 처리
/// - 피격 무적 시간 처리
/// - 피격 연출 호출
/// - Hit 애니메이션 호출
/// - 사망 시 Dead 애니메이션 상태 변경
/// - Enemy.cs에 사망 이벤트 전달
/// 
/// IDamageable 규약:
/// - bool IsInvincible { get; }
/// - void TakeDamage(float amount);
/// 
/// 따라서 IDamageable은 수정하지 않고,
/// EnemyHealth가 IsInvincible을 구현한다.
/// </summary>
[DisallowMultipleComponent]
public class EnemyHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] private float maxHp = 10f;
    [SerializeField] private float hp = 10f;

    [Header("Invincible")]
    [SerializeField] private float hitInvincibleTime = 0.05f;

    [Header("Feedback")]
    [SerializeField] private HitFeedback2D hitFeedback;
    [SerializeField] private Animator animator;

    private float invincibleUntil;
    private bool dead;

    public event Action<EnemyHealth> Died;

    public float CurrentHp => hp;
    public float MaxHp => maxHp;
    public bool IsDead => dead;

    public bool IsInvincible => Time.time < invincibleUntil;

    private void Awake()
    {
        if (hitFeedback == null)
            hitFeedback = GetComponent<HitFeedback2D>();

        if (animator == null)
            animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        hp = maxHp;
        dead = false;
        invincibleUntil = 0f;

        SetAnimatorBoolIfExists("Dead", false);
    }

    public void SetMaxHp(float value)
    {
        maxHp = Mathf.Max(1f, value);
        hp = maxHp;
        dead = false;
        invincibleUntil = 0f;

        SetAnimatorBoolIfExists("Dead", false);
    }

    public void TakeDamage(float amount)
    {
        if (dead)
            return;

        if (amount <= 0f)
            return;

        if (IsInvincible)
            return;

        hp = Mathf.Max(0f, hp - amount);
        invincibleUntil = Time.time + Mathf.Max(0f, hitInvincibleTime);

        if (hp > 0f)
        {
            if (hitFeedback != null)
                hitFeedback.PlayHit();

            SetAnimatorTriggerIfExists("Hit");

            if (AudioManager.instance != null)
                AudioManager.instance.PlaySfx(AudioManager.Sfx.Hit);

            return;
        }

        Die();
    }

    private void Die()
    {
        if (dead)
            return;

        dead = true;

        SetAnimatorBoolIfExists("Dead", true);

        Died?.Invoke(this);
    }

    private void SetAnimatorTriggerIfExists(string parameterName)
    {
        if (animator == null)
            return;

        AnimatorControllerParameter[] parameters = animator.parameters;

        for (int i = 0; i < parameters.Length; i++)
        {
            AnimatorControllerParameter p = parameters[i];

            if (p.name == parameterName &&
                p.type == AnimatorControllerParameterType.Trigger)
            {
                animator.SetTrigger(parameterName);
                return;
            }
        }
    }

    private void SetAnimatorBoolIfExists(string parameterName, bool value)
    {
        if (animator == null)
            return;

        AnimatorControllerParameter[] parameters = animator.parameters;

        for (int i = 0; i < parameters.Length; i++)
        {
            AnimatorControllerParameter p = parameters[i];

            if (p.name == parameterName &&
                p.type == AnimatorControllerParameterType.Bool)
            {
                animator.SetBool(parameterName, value);
                return;
            }
        }
    }
}