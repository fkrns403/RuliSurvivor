using UnityEngine;

/// <summary>
/// 보스 탄막 패턴용 공격 애니메이션 컨트롤러.
/// 
/// 사용 방식:
/// - 보스 프리팹에 붙인다.
/// - Animator에 "Attack" Trigger 파라미터를 만든다.
/// - 2페이즈 / 3페이즈 보스에서만 공격 애니메이션을 허용한다.
/// - 탄막 발사 직전에 PlayAttackAnimation()을 호출한다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
public class BossAttackAnimationController : MonoBehaviour
{
    [Header("Animator")]
    [SerializeField] private Animator animator;

    [Tooltip("공격 애니메이션 Trigger 이름")]
    [SerializeField] private string attackTriggerName = "Attack";

    [Header("Phase Rule")]
    [Tooltip("이 보스가 몇 페이즈인지 직접 지정합니다. 1이면 공격 애니메이션 차단, 2/3이면 허용.")]
    [SerializeField] private int bossPhase = 1;

    [Tooltip("2페이즈에서 공격 애니메이션 허용")]
    [SerializeField] private bool allowPhase2 = true;

    [Tooltip("3페이즈에서 공격 애니메이션 허용")]
    [SerializeField] private bool allowPhase3 = true;

    [Header("Cooldown")]
    [Tooltip("탄막이 너무 자주 발사될 때 애니메이션 트리거가 과하게 반복되지 않도록 막는 시간")]
    [SerializeField] private float animationCooldown = 0.35f;

    [Header("Debug")]
    [SerializeField] private bool verboseLog = false;

    private float nextAnimationTime;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        nextAnimationTime = 0f;

        if (animator != null)
            animator.ResetTrigger(attackTriggerName);
    }

    public void SetBossPhase(int phase)
    {
        bossPhase = Mathf.Clamp(phase, 1, 3);
    }

    public void PlayAttackAnimation()
    {
        if (animator == null)
            return;

        if (!CanPlayByPhase())
            return;

        if (Time.time < nextAnimationTime)
            return;

        nextAnimationTime =
            Time.time + Mathf.Max(0.01f, animationCooldown);

        animator.ResetTrigger(attackTriggerName);
        animator.SetTrigger(attackTriggerName);

        if (verboseLog)
            Debug.Log($"BossAttackAnimationController: Attack Animation Play / phase={bossPhase}", this);
    }

    private bool CanPlayByPhase()
    {
        if (bossPhase == 2 && allowPhase2)
            return true;

        if (bossPhase == 3 && allowPhase3)
            return true;

        return false;
    }
}