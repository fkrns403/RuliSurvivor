using UnityEngine;

/// <summary>
/// 플레이어 컨트롤 총괄.
/// 
/// 프리팹 런타임 생성 대응 버전.
/// 
/// 역할:
/// - PlayerInputSource에서 이동 입력을 읽는다.
/// - MovementMotor로 실제 이동한다.
/// - SpriteFacing으로 좌우 방향을 바꾼다.
/// - AnimatorDriver로 이동 애니메이션 값을 전달한다.
/// 
/// 중요:
/// - 이 스크립트는 플레이어 프리팹 루트에 붙어 있어야 한다.
/// - Rigidbody2D, PlayerInputSource, MovementMotor가 같은 루트에 있어야 한다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerInputSource))]
[RequireComponent(typeof(MovementMotor))]
public class PlayerController : MonoBehaviour
{
    [Header("Speed")]
    [SerializeField] private float normalSpeed = 3f;
    [SerializeField] private float currentSpeed = 3f;
    [SerializeField] private bool normalizeDiagonal = true;

    [Header("Game State Check")]
    [SerializeField] private bool stopWhenGamePaused = true;

    [Header("Debug")]
    [SerializeField] private bool verboseLog = false;

    private Rigidbody2D rb;
    private PlayerInputSource input;
    private MovementMotor motor;
    private SpriteFacing facing;
    private AnimatorDriver animDriver;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    public float Speed
    {
        get => currentSpeed;
        set => currentSpeed = Mathf.Max(0f, value);
    }

    private void Awake()
    {
        ResolveComponents();
        InitializeComponents();
    }

    private void OnEnable()
    {
        currentSpeed = normalSpeed;

        ResolveComponents();
        InitializeComponents();
    }

    private void ResolveComponents()
    {
        rb = GetComponent<Rigidbody2D>();
        input = GetComponent<PlayerInputSource>();

        motor = GetComponent<MovementMotor>();
        if (motor == null)
            motor = gameObject.AddComponent<MovementMotor>();

        facing = GetComponent<SpriteFacing>();
        if (facing == null)
            facing = gameObject.AddComponent<SpriteFacing>();

        animDriver = GetComponent<AnimatorDriver>();
        if (animDriver == null)
            animDriver = gameObject.AddComponent<AnimatorDriver>();

        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void InitializeComponents()
    {
        if (motor != null)
            motor.Initialize(rb);

        if (facing != null)
            facing.Initialize(spriteRenderer);

        if (animDriver != null)
            animDriver.Initialize(animator);
    }

    private void FixedUpdate()
    {
        if (input == null)
            return;

        if (stopWhenGamePaused && IsGameStopped())
            return;

        Vector2 move = input.MoveInput;

        if (normalizeDiagonal && move.sqrMagnitude > 1f)
            move.Normalize();

        if (verboseLog && move.sqrMagnitude > 0.0001f)
            Debug.Log($"PlayerController Move = {move}", this);

        motor.Move(move, currentSpeed, Time.fixedDeltaTime);
    }

    private void LateUpdate()
    {
        if (input == null)
            return;

        Vector2 move = input.MoveInput;

        if (animDriver != null)
            animDriver.UpdateSpeedParam(move);

        if (facing != null)
            facing.UpdateFacing(move);
    }

    private bool IsGameStopped()
    {
        GameManager gm = GameManager.Instance;

        if (gm == null)
            return false;

        return !gm.isLive || gm.isPaused;
    }
}