using UnityEngine;

/// <summary>
/// ParticleSystem 기반 보스 전용 공격 패턴 스킬.
/// 
/// 역할:
/// - BossController가 BossSkillSet에서 이 스킬을 선택하면 Execute가 호출된다.
/// - 보스 위치 또는 플레이어 위치 기준으로 ParticleSystem 패턴 프리팹을 생성한다.
/// - 플레이어 방향 조준 가능.
/// - 생성 직후 모든 ParticleSystem을 재생한다.
/// - BossParticleDamage가 있으면 런타임 데미지 값을 주입한다.
/// - 2페이즈/3페이즈 보스라면 공격 애니메이션을 호출한다.
/// </summary>
[CreateAssetMenu(menuName = "Boss/Skill/Particle Pattern Skill")]
public class BossParticlePatternSkill : BossSkill
{
    private enum SpawnAnchor
    {
        Boss,
        Player
    }

    [Header("Pattern Prefab")]
    [SerializeField] private GameObject particlePatternPrefab;

    [Header("Spawn")]
    [SerializeField] private SpawnAnchor spawnAnchor = SpawnAnchor.Boss;
    [SerializeField] private Vector3 spawnOffset;
    [SerializeField] private bool aimAtPlayer = true;
    [SerializeField] private float rotationOffsetZ = 0f;

    [Header("Damage")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private LayerMask targetMask;
    [SerializeField] private float sameTargetHitCooldown = 0.25f;

    [Header("Attack Animation")]
    [SerializeField] private bool playBossAttackAnimation = true;

    [Header("Lifetime")]
    [SerializeField] private float lifeTime = 5f;

    [Header("Debug")]
    [SerializeField] private bool logIfMissing = true;

    public override void Execute(BossContext ctx)
    {
        if (ctx == null)
            return;

        if (particlePatternPrefab == null)
        {
            if (logIfMissing)
                Debug.LogWarning($"{name}: particlePatternPrefab이 비어 있습니다.");

            return;
        }

        Transform anchor = GetAnchor(ctx);

        if (anchor == null)
            return;

        Vector3 spawnPosition = anchor.position + spawnOffset;
        spawnPosition.z = 0f;

        Quaternion rotation = CalculateRotation(ctx);

        GameObject pattern = Instantiate(
            particlePatternPrefab,
            spawnPosition,
            rotation
        );

        BossAttackAnimationController attackAnimationController =
            ResolveAttackAnimationController(ctx);

        ParticleSystem[] particleSystems =
            pattern.GetComponentsInChildren<ParticleSystem>(true);

        for (int i = 0; i < particleSystems.Length; i++)
        {
            particleSystems[i].Play();
        }

        BossParticleDamage[] damages =
            pattern.GetComponentsInChildren<BossParticleDamage>(true);

        for (int i = 0; i < damages.Length; i++)
        {
            damages[i].Init(
                damage,
                targetMask,
                lifeTime,
                sameTargetHitCooldown,
                attackAnimationController
            );
        }

        Destroy(pattern, Mathf.Max(0.1f, lifeTime));
    }

    private BossAttackAnimationController ResolveAttackAnimationController(BossContext ctx)
    {
        if (!playBossAttackAnimation)
            return null;

        if (ctx == null || ctx.boss == null)
            return null;

        BossAttackAnimationController controller =
            ctx.boss.GetComponent<BossAttackAnimationController>();

        if (controller == null)
            controller = ctx.boss.GetComponentInParent<BossAttackAnimationController>();

        if (controller == null)
            controller = ctx.boss.GetComponentInChildren<BossAttackAnimationController>(true);

        return controller;
    }

    private Transform GetAnchor(BossContext ctx)
    {
        switch (spawnAnchor)
        {
            case SpawnAnchor.Player:
                return ctx.player;

            case SpawnAnchor.Boss:
            default:
                return ctx.boss;
        }
    }

    private Quaternion CalculateRotation(BossContext ctx)
    {
        float z = rotationOffsetZ;

        if (aimAtPlayer && ctx.boss != null && ctx.player != null)
        {
            Vector3 dir = ctx.player.position - ctx.boss.position;
            dir.z = 0f;

            if (dir.sqrMagnitude > 0.0001f)
                z += Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        }

        return Quaternion.Euler(0f, 0f, z);
    }
}