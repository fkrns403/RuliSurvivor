using System;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float health = 100f;

    [Header("Invincible")]
    [SerializeField] private bool forceInvincible;
    [SerializeField] private float hitInvincibleTime = 0.5f;

    [Header("Revive")]
    [SerializeField] private float reviveHealthRatio = 1f;
    [SerializeField] private float reviveInvincibleTime = 3f;

    [Header("References")]
    [SerializeField] private PlayerCollisionStateController collisionState;
    [SerializeField] private HitFeedback2D hitFeedback;
    [SerializeField] private Animator animator;

    private float invincibleUntil;
    private bool dead;
    private PlayerLives lives;

    public event Action<float, float> HealthChanged;

    public bool IsInvincible => forceInvincible || Time.time < invincibleUntil;
    public bool IsDead => dead;
    public float CurrentHealth => health;
    public float MaxHealth => maxHealth;

    private void Awake()
    {
        lives = GetComponent<PlayerLives>();

        if (collisionState == null)
            collisionState = GetComponent<PlayerCollisionStateController>();

        if (hitFeedback == null)
            hitFeedback = GetComponent<HitFeedback2D>();

        if (animator == null)
            animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        dead = false;
        health = maxHealth;
        invincibleUntil = 0f;

        if (collisionState != null)
            collisionState.ClearAllStates();

        SyncToGameManager();
        HealthChanged?.Invoke(health, maxHealth);

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

        health = Mathf.Max(0f, health - amount);
        SyncToGameManager();

        invincibleUntil = Time.time + Mathf.Max(0.01f, hitInvincibleTime);

        if (collisionState != null)
        {
            collisionState.BeginState(
                "HitInvincible",
                hitInvincibleTime,
                useTrigger: true,
                useBlink: true,
                useAfterImage: true
            );
        }

        if (hitFeedback != null)
            hitFeedback.PlayHit();

        SetAnimatorTriggerIfExists("Hit");

        if (AudioManager.instance != null)
            AudioManager.instance.PlaySfx(AudioManager.Sfx.Hit);

        HealthChanged?.Invoke(health, maxHealth);

        if (health <= 0f)
            DieOrRevive();
    }

    private void DieOrRevive()
    {
        if (dead)
            return;

        if (lives == null)
            lives = GetComponent<PlayerLives>();

        if (lives != null && lives.ConsumeLife(reviveInvincibleTime))
        {
            Revive();
            return;
        }

        dead = true;

        SetAnimatorBoolIfExists("Dead", true);
        SetAnimatorTriggerIfExists("Dead");

        if (GameManager.Instance != null)
            GameManager.Instance.HandlePlayerDeath();
    }

    private void Revive()
    {
        float restored = Mathf.Max(1f, maxHealth * Mathf.Clamp01(reviveHealthRatio));

        health = restored;
        dead = false;

        AddTimedInvincibility(reviveInvincibleTime);

        SetAnimatorBoolIfExists("Dead", false);

        SyncToGameManager();
        HealthChanged?.Invoke(health, maxHealth);
    }

    public void Heal(float amount)
    {
        if (dead)
            return;

        if (amount <= 0f)
            return;

        health = Mathf.Min(maxHealth, health + amount);

        SyncToGameManager();
        HealthChanged?.Invoke(health, maxHealth);
    }

    public void HealFull()
    {
        if (dead)
            return;

        health = maxHealth;

        SyncToGameManager();
        HealthChanged?.Invoke(health, maxHealth);
    }

    public void FullHeal()
    {
        HealFull();
    }

    public void HealToFull()
    {
        HealFull();
    }

    public void AddTimedInvincibility(float duration)
    {
        float safeDuration = Mathf.Max(0.01f, duration);
        invincibleUntil = Mathf.Max(invincibleUntil, Time.time + safeDuration);

        if (collisionState != null)
        {
            collisionState.BeginState(
                "ExternalInvincible",
                safeDuration,
                useTrigger: true,
                useBlink: true,
                useAfterImage: true
            );
        }
    }

    public void SetTemporaryInvincible(float duration)
    {
        AddTimedInvincibility(duration);
    }

    public void StartInvincible(float duration)
    {
        AddTimedInvincibility(duration);
    }

    public void ActivateInvincible(float duration)
    {
        AddTimedInvincibility(duration);
    }

    public void SetMaxHealth(float value, bool refill)
    {
        maxHealth = Mathf.Max(1f, value);

        if (refill)
            health = maxHealth;
        else
            health = Mathf.Clamp(health, 0f, maxHealth);

        SyncToGameManager();
        HealthChanged?.Invoke(health, maxHealth);
    }

    private void SyncToGameManager()
    {
        GameManager gm = GameManager.Instance;

        if (gm == null)
            return;

        gm.maxHealth = maxHealth;
        gm.health = health;
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