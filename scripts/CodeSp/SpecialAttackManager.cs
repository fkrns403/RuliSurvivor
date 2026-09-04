using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class SpecialAttackManager : MonoBehaviour
{
    public static SpecialAttackManager Instance { get; private set; }

    private const string KEY_SELECTED_MAP_INDEX = "SelectedMapIndex";

    [Header("Reference")]
    [SerializeField] private SpecialAttack specialAttack;

    [Header("Tutorial Rule")]
    [SerializeField] private bool blockType2PatternsOnTutorial = true;
    [SerializeField] private int tutorialMapIndex = 0;
    [SerializeField] private string tutorialMapId = "tutorial";

    [Header("Type2 Grid Loop")]
    [SerializeField] private bool enableType2GridLoop = true;
    [SerializeField] private float type2GridInitialDelay = 2f;
    [SerializeField] private float type2GridInterval = 4f;

    [Header("Boss Pattern Loop")]
    [SerializeField] private bool enableBossPatternLoop = true;
    [SerializeField] private float bossPatternStartDelay = 1f;

    [SerializeField]
    private BossPatternStep[] bossPatternSteps =
    {
        new BossPatternStep { attackType = SpecialAttackType.Radial, turnCount = 2, turnInterval = 1.2f },
        new BossPatternStep { attackType = SpecialAttackType.Grid, turnCount = 1, turnInterval = 1.5f },
        new BossPatternStep { attackType = SpecialAttackType.FireRing, turnCount = 1, turnInterval = 2f }
    };

    [Header("Boss Pattern Target Rule")]
    [SerializeField] private bool radialUsesBossCenter = true;
    [SerializeField] private bool gridUsesPlayerCenter = true;
    [SerializeField] private bool fireUsesPlayerCenter = true;
    [SerializeField] private bool fireRingUsesPlayerCenter = true;

    [Header("Warning Message")]
    [SerializeField] private string type2GridMessage = "TYPE 2 GRID";
    [SerializeField] private string bossRadialMessage = "BOSS RADIAL";
    [SerializeField] private string bossGridMessage = "DANGER GRID";
    [SerializeField] private string bossFireMessage = "FIRE ZONE";
    [SerializeField] private string bossFireRingMessage = "FIRE RING";

    [Header("Debug")]
    [SerializeField] private bool verboseLog = true;

    [System.Serializable]
    public class BossPatternStep
    {
        public SpecialAttackType attackType = SpecialAttackType.Radial;
        public int turnCount = 1;
        public float turnInterval = 1.5f;
    }

    private readonly HashSet<Transform> aliveType2Targets = new HashSet<Transform>();
    private readonly HashSet<Transform> aliveBosses = new HashSet<Transform>();
    private readonly List<Transform> tempToRemove = new List<Transform>();

    private Coroutine mainLoopRoutine;
    private Transform currentBoss;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        ResolveReferences();
    }

    private void Start()
    {
        ResetRuntimeState();

        if (mainLoopRoutine != null)
            StopCoroutine(mainLoopRoutine);

        mainLoopRoutine = StartCoroutine(MainPatternLoopRoutine());
    }

    private void OnEnable()
    {
        ResolveReferences();
    }

    private void OnDisable()
    {
        if (mainLoopRoutine != null)
            StopCoroutine(mainLoopRoutine);

        mainLoopRoutine = null;

        aliveType2Targets.Clear();
        aliveBosses.Clear();
        currentBoss = null;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void ResolveReferences()
    {
        if (specialAttack == null)
            specialAttack = GetComponent<SpecialAttack>();

        if (specialAttack == null)
            specialAttack = FindObjectOfType<SpecialAttack>(true);
    }

    private void ResetRuntimeState()
    {
        aliveType2Targets.Clear();
        aliveBosses.Clear();
        currentBoss = null;
    }

    private IEnumerator MainPatternLoopRoutine()
    {
        while (true)
        {
            yield return null;

            if (!CanRun())
                continue;

            CleanupInvalidTargets();
            currentBoss = GetAnyAliveBoss();

            if (currentBoss != null && enableBossPatternLoop)
            {
                yield return StartCoroutine(BossPatternLoopRoutine(currentBoss));
                continue;
            }

            if (enableType2GridLoop && HasAliveType2Target())
            {
                yield return StartCoroutine(Type2GridLoopRoutine());
                continue;
            }
        }
    }

    private IEnumerator Type2GridLoopRoutine()
    {
        if (type2GridInitialDelay > 0f)
            yield return new WaitForSeconds(type2GridInitialDelay);

        while (CanRun())
        {
            CleanupInvalidTargets();

            if (GetAnyAliveBoss() != null)
                yield break;

            if (!HasAliveType2Target())
                yield break;

            Transform player = GetPlayer();

            if (player != null && specialAttack != null && specialAttack.HasPattern(SpecialAttackType.Grid))
            {
                Log("Type2 Grid 실행");
                yield return StartCoroutine(
                    specialAttack.ExecutePatternRoutine(
                        SpecialAttackType.Grid,
                        player,
                        type2GridMessage
                    )
                );
            }

            yield return new WaitForSeconds(Mathf.Max(0.05f, type2GridInterval));
        }
    }

    private IEnumerator BossPatternLoopRoutine(Transform boss)
    {
        if (bossPatternStartDelay > 0f)
            yield return new WaitForSeconds(bossPatternStartDelay);

        while (CanRun())
        {
            CleanupInvalidTargets();

            if (!IsValidTarget(boss))
                yield break;

            if (GetAnyAliveBoss() != boss)
                yield break;

            if (bossPatternSteps == null || bossPatternSteps.Length == 0)
            {
                yield return StartCoroutine(ExecuteBossPatternOnce(SpecialAttackType.Radial, boss, bossRadialMessage));
                yield return new WaitForSeconds(1.5f);
                continue;
            }

            for (int i = 0; i < bossPatternSteps.Length; i++)
            {
                BossPatternStep step = bossPatternSteps[i];

                if (step == null)
                    continue;

                int turnCount = Mathf.Max(1, step.turnCount);

                for (int turn = 0; turn < turnCount; turn++)
                {
                    if (!CanRun())
                        yield break;

                    if (!IsValidTarget(boss))
                        yield break;

                    yield return StartCoroutine(
                        ExecuteBossPatternOnce(
                            step.attackType,
                            boss,
                            GetBossPatternMessage(step.attackType)
                        )
                    );

                    yield return new WaitForSeconds(Mathf.Max(0.05f, step.turnInterval));
                }
            }
        }
    }

    private IEnumerator ExecuteBossPatternOnce(SpecialAttackType type, Transform boss, string message)
    {
        if (specialAttack == null)
            yield break;

        if (!specialAttack.HasPattern(type))
            yield break;

        Transform target = ResolvePatternTarget(type, boss);

        if (target == null)
            yield break;

        Log($"Boss Pattern 실행 / type={type}, target={target.name}");

        yield return StartCoroutine(
            specialAttack.ExecutePatternRoutine(type, target, message)
        );
    }

    private Transform ResolvePatternTarget(SpecialAttackType type, Transform boss)
    {
        Transform player = GetPlayer();

        switch (type)
        {
            case SpecialAttackType.Radial:
                return radialUsesBossCenter ? boss : player;

            case SpecialAttackType.Grid:
                return gridUsesPlayerCenter ? player : boss;

            case SpecialAttackType.Fire:
                return fireUsesPlayerCenter ? player : boss;

            case SpecialAttackType.FireRing:
                return fireRingUsesPlayerCenter ? player : boss;

            default:
                return boss;
        }
    }

    private string GetBossPatternMessage(SpecialAttackType type)
    {
        switch (type)
        {
            case SpecialAttackType.Radial:
                return bossRadialMessage;

            case SpecialAttackType.Grid:
                return bossGridMessage;

            case SpecialAttackType.Fire:
                return bossFireMessage;

            case SpecialAttackType.FireRing:
                return bossFireRingMessage;

            default:
                return "WARNING";
        }
    }

    public void NotifyEliteSpawned(Transform target, EnemyType type)
    {
        if (target == null)
            return;

        if (type == EnemyType.Type2)
        {
            if (blockType2PatternsOnTutorial && IsTutorialMap())
            {
                Log($"튜토리얼 맵 Type2 특수 패턴 차단 / target={target.name}");
                return;
            }

            aliveType2Targets.Add(target);
            Log($"Type2 특수 대상 등록 / target={target.name}");
            return;
        }

        if (type == EnemyType.Boss)
        {
            aliveBosses.Add(target);
            Log($"Boss 특수 대상 등록 / target={target.name}");
            return;
        }
    }

    public void NotifyEliteSpawned(Transform target)
    {
        if (target == null)
            return;

        Enemy enemy = target.GetComponent<Enemy>();

        if (enemy == null)
            enemy = target.GetComponentInParent<Enemy>();

        if (enemy != null)
        {
            NotifyEliteSpawned(target, enemy.GetEnemyType());
            return;
        }

        BossHealth bossHealth = target.GetComponent<BossHealth>();

        if (bossHealth == null)
            bossHealth = target.GetComponentInParent<BossHealth>();

        if (bossHealth != null)
        {
            NotifyEliteSpawned(target, EnemyType.Boss);
            return;
        }

        Log($"타입 확인 실패. 등록하지 않음 / target={target.name}");
    }

    public void NotifyEliteDied(Transform target, EnemyType type)
    {
        if (target == null)
            return;

        RemoveTarget(target);

        Log($"특수 대상 제거 / target={target.name}, type={type}");
    }

    public void NotifyEliteDied(Transform target)
    {
        if (target == null)
            return;

        RemoveTarget(target);

        Log($"특수 대상 제거 / target={target.name}");
    }

    private void RemoveTarget(Transform target)
    {
        if (target == null)
            return;

        aliveType2Targets.Remove(target);
        aliveBosses.Remove(target);

        if (currentBoss == target)
            currentBoss = null;
    }

    private void CleanupInvalidTargets()
    {
        tempToRemove.Clear();

        foreach (Transform target in aliveType2Targets)
        {
            if (!IsValidTarget(target))
                tempToRemove.Add(target);
        }

        for (int i = 0; i < tempToRemove.Count; i++)
            aliveType2Targets.Remove(tempToRemove[i]);

        tempToRemove.Clear();

        foreach (Transform boss in aliveBosses)
        {
            if (!IsValidTarget(boss))
                tempToRemove.Add(boss);
        }

        for (int i = 0; i < tempToRemove.Count; i++)
            aliveBosses.Remove(tempToRemove[i]);
    }

    private bool IsValidTarget(Transform target)
    {
        if (target == null)
            return false;

        if (!target.gameObject.activeInHierarchy)
            return false;

        BossHealth bossHealth = target.GetComponent<BossHealth>();

        if (bossHealth == null)
            bossHealth = target.GetComponentInParent<BossHealth>();

        if (bossHealth != null && bossHealth.IsDead)
            return false;

        return true;
    }

    private bool HasAliveType2Target()
    {
        foreach (Transform target in aliveType2Targets)
        {
            if (IsValidTarget(target))
                return true;
        }

        return false;
    }

    private Transform GetAnyAliveBoss()
    {
        foreach (Transform boss in aliveBosses)
        {
            if (IsValidTarget(boss))
                return boss;
        }

        return null;
    }

    private Transform GetPlayer()
    {
        GameManager gm = GameManager.Instance;

        if (gm == null)
            return null;

        return gm.PlayerTransform;
    }

    private bool CanRun()
    {
        ResolveReferences();

        GameManager gm = GameManager.Instance;

        if (gm == null)
            return false;

        if (!gm.isLive || gm.isPaused)
            return false;

        if (specialAttack == null)
            return false;

        if (gm.PlayerTransform == null)
            return false;

        return true;
    }

    private bool IsTutorialMap()
    {
        int selectedMapIndex = PlayerPrefs.GetInt(KEY_SELECTED_MAP_INDEX, -1);

        if (selectedMapIndex == tutorialMapIndex)
            return true;

        GameManager gm = GameManager.Instance;

        if (gm != null &&
            !string.IsNullOrEmpty(tutorialMapId) &&
            !string.IsNullOrEmpty(gm.CurrentMapId) &&
            gm.CurrentMapId == tutorialMapId)
        {
            return true;
        }

        return false;
    }

    private void Log(string message)
    {
        if (!verboseLog)
            return;

        Debug.Log($"SpecialAttackManager: {message}", this);
    }
}