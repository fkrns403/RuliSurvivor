using System.Collections;
using UnityEngine;

/// <summary>
/// 플레이어 대쉬.
/// 
/// 구버전 player_nyarubi 방식에 가깝게 수정한 버전.
/// 
/// 핵심:
/// - Rigidbody velocity로 밀지 않는다.
/// - 대쉬 중 PlayerController.Speed만 dashSpeed로 바꾼다.
/// - 이동 자체는 기존 PlayerController / MovementMotor가 계속 담당한다.
/// - 그래서 대쉬 중에도 입력 방향 변경이 가능하다.
/// - 대쉬 중 무적 / Trigger / 잔상은 유지한다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerDash : MonoBehaviour
{
    [Header("Dash")]
    [SerializeField] private float dashSpeed = 18f;
    [SerializeField] private float dashDuration = 0.4f;
    [SerializeField] private float dashCooldown = 8f;

    [Header("References")]
    [SerializeField] private PlayerInputSource input;
    [SerializeField] private PlayerCollisionStateController collisionState;
    [SerializeField] private PlayerController playerController;

    private Rigidbody2D rb;

    private bool isDashing;
    private float nextDashTime;
    private float speedBeforeDash;

    private Vector2 lastMoveDir = Vector2.right;

    public bool IsDashing => isDashing;

    public float CooldownNormalized
    {
        get
        {
            if (dashCooldown <= 0f)
                return 1f;

            float remain = Mathf.Max(0f, nextDashTime - Time.time);
            return Mathf.Clamp01(1f - remain / dashCooldown);
        }
    }

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();

        isDashing = false;
        nextDashTime = 0f;
    }

    private void OnDisable()
    {
        StopAllCoroutines();

        RestoreSpeed();

        if (collisionState != null)
            collisionState.EndState("Dash");

        if (rb != null)
            rb.velocity = Vector2.zero;

        isDashing = false;
    }

    private void ResolveReferences()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (input == null)
            input = GetComponent<PlayerInputSource>();

        if (collisionState == null)
            collisionState = GetComponent<PlayerCollisionStateController>();

        if (playerController == null)
            playerController = GetComponent<PlayerController>();
    }

    private void Update()
    {
        if (input == null)
            return;

        Vector2 move = input.MoveInput;

        if (move.sqrMagnitude > 0.001f)
            lastMoveDir = move.normalized;
    }

    public void TryDash()
    {
        if (isDashing)
            return;

        if (Time.time < nextDashTime)
            return;

        GameManager gm = GameManager.Instance;

        if (gm != null && (!gm.isLive || gm.isPaused))
            return;

        Vector2 dir = ResolveDashDirection();

        if (dir.sqrMagnitude < 0.001f)
            return;

        StartCoroutine(DashRoutine());
    }

    private Vector2 ResolveDashDirection()
    {
        if (input != null && input.MoveInput.sqrMagnitude > 0.001f)
            return input.MoveInput.normalized;

        if (lastMoveDir.sqrMagnitude > 0.001f)
            return lastMoveDir.normalized;

        return Vector2.right;
    }

    private IEnumerator DashRoutine()
    {
        isDashing = true;
        nextDashTime = Time.time + Mathf.Max(0.01f, dashCooldown);

        ResolveReferences();

        if (playerController != null)
        {
            speedBeforeDash = playerController.Speed;
            playerController.Speed = Mathf.Max(0.01f, dashSpeed);
        }

        if (collisionState != null)
        {
            collisionState.BeginState(
                "Dash",
                dashDuration,
                useTrigger: true,
                useBlink: false,
                useAfterImage: true
            );
        }

        if (AudioManager.instance != null)
            AudioManager.instance.PlaySfx(AudioManager.Sfx.Dead);

        float timer = 0f;
        float safeDuration = Mathf.Max(0.01f, dashDuration);

        while (timer < safeDuration)
        {
            GameManager gm = GameManager.Instance;

            if (gm != null && (!gm.isLive || gm.isPaused))
            {
                yield return null;
                continue;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        RestoreSpeed();

        if (collisionState != null)
            collisionState.EndState("Dash");

        if (rb != null)
            rb.velocity = Vector2.zero;

        isDashing = false;
    }

    private void RestoreSpeed()
    {
        if (playerController == null)
            playerController = GetComponent<PlayerController>();

        if (playerController != null && speedBeforeDash > 0f)
            playerController.Speed = speedBeforeDash;
    }
}