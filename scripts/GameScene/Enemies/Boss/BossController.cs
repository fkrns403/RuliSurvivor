using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class BossController : MonoBehaviour
{
    [Header("Skill Sets")]
    [SerializeField] private BossSkillSet[] skillSets;

    [Header("Target")]
    [SerializeField] private Transform player;

    [Header("Sequential Pattern Rule")]
    [SerializeField] private bool useSequentialPattern = true;

    [Tooltip("스킬 실행 후 다음 스킬을 실행하기 전까지 대기하는 시간. 파티클 패턴 지속시간과 맞추세요.")]
    [SerializeField] private float patternEndWaitTime = 5f;

    [Tooltip("스킬 실행 직후 추가 여유 시간")]
    [SerializeField] private float patternExtraGap = 0.5f;

    [Tooltip("준비된 스킬이 없을 때 재검사 간격")]
    [SerializeField] private float retryInterval = 0.25f;

    [Header("Debug")]
    [SerializeField] private bool verboseLog = true;

    private BossSkillSet currentSet;
    private readonly Dictionary<BossSkill, float> nextReadyTime = new Dictionary<BossSkill, float>();

    private BossContext ctx;
    private bool initialized;
    private bool runningPatternLoop;
    private Coroutine patternLoopRoutine;

    public void Setup(BossDifficulty difficulty)
    {
        if (initialized)
            return;

        initialized = true;

        currentSet = FindSet(difficulty);

        if (player == null && GameManager.Instance != null)
            player = GameManager.Instance.PlayerTransform;

        ctx = new BossContext
        {
            boss = transform,
            player = player,
            gm = GameManager.Instance
        };

        nextReadyTime.Clear();

        if (currentSet != null && currentSet.skills != null)
        {
            for (int i = 0; i < currentSet.skills.Length; i++)
            {
                BossSkill skill = currentSet.skills[i];

                if (skill == null)
                    continue;

                if (!nextReadyTime.ContainsKey(skill))
                    nextReadyTime.Add(skill, 0f);
            }
        }

        if (useSequentialPattern)
        {
            if (patternLoopRoutine != null)
                StopCoroutine(patternLoopRoutine);

            patternLoopRoutine = StartCoroutine(SequentialPatternLoop());
        }
    }

    private void OnDisable()
    {
        initialized = false;
        runningPatternLoop = false;

        if (patternLoopRoutine != null)
            StopCoroutine(patternLoopRoutine);

        patternLoopRoutine = null;
        currentSet = null;
        ctx = null;
        nextReadyTime.Clear();
    }

    private void Update()
    {
        if (!initialized)
            return;

        if (useSequentialPattern)
            return;

        GameManager gm = GameManager.Instance;

        if (gm == null || !gm.isLive || gm.isPaused)
            return;

        if (currentSet == null || currentSet.skills == null || currentSet.skills.Length == 0)
            return;

        EnsureContext();

        if (ctx == null || ctx.player == null)
            return;

        TryExecuteRandomSkillImmediate();
    }

    private IEnumerator SequentialPatternLoop()
    {
        runningPatternLoop = true;

        while (runningPatternLoop)
        {
            yield return null;

            if (!initialized)
                continue;

            GameManager gm = GameManager.Instance;

            if (gm == null || !gm.isLive || gm.isPaused)
                continue;

            if (currentSet == null || currentSet.skills == null || currentSet.skills.Length == 0)
                continue;

            EnsureContext();

            if (ctx == null || ctx.player == null)
                continue;

            BossSkill skill = GetReadyRandomSkill();

            if (skill == null)
            {
                yield return new WaitForSeconds(Mathf.Max(0.05f, retryInterval));
                continue;
            }

            ExecuteSkill(skill);

            float wait = Mathf.Max(0.05f, patternEndWaitTime) + Mathf.Max(0f, patternExtraGap);

            if (verboseLog)
                Debug.Log($"BossController: 패턴 종료 대기 / skill={skill.name}, wait={wait}", this);

            yield return new WaitForSeconds(wait);
        }
    }

    private void EnsureContext()
    {
        if (ctx == null)
        {
            ctx = new BossContext
            {
                boss = transform,
                gm = GameManager.Instance
            };
        }

        ctx.boss = transform;
        ctx.gm = GameManager.Instance;

        if (ctx.player == null)
        {
            if (player == null && GameManager.Instance != null)
                player = GameManager.Instance.PlayerTransform;

            ctx.player = player;
        }
    }

    private void TryExecuteRandomSkillImmediate()
    {
        BossSkill skill = GetReadyRandomSkill();

        if (skill == null)
            return;

        ExecuteSkill(skill);
    }

    private BossSkill GetReadyRandomSkill()
    {
        if (currentSet == null || currentSet.skills == null || currentSet.skills.Length == 0)
            return null;

        List<BossSkill> readySkills = new List<BossSkill>();

        for (int i = 0; i < currentSet.skills.Length; i++)
        {
            BossSkill skill = currentSet.skills[i];

            if (skill == null)
                continue;

            if (!nextReadyTime.TryGetValue(skill, out float readyTime))
            {
                nextReadyTime[skill] = 0f;
                readyTime = 0f;
            }

            if (Time.time >= readyTime)
                readySkills.Add(skill);
        }

        if (readySkills.Count <= 0)
            return null;

        int index = Random.Range(0, readySkills.Count);
        return readySkills[index];
    }

    private void ExecuteSkill(BossSkill skill)
    {
        if (skill == null)
            return;

        EnsureContext();

        if (ctx == null || ctx.player == null)
            return;

        skill.Execute(ctx);

        nextReadyTime[skill] = Time.time + Mathf.Max(0.1f, skill.cooldown);

        if (verboseLog)
            Debug.Log($"BossController: 스킬 실행 / skill={skill.name}", this);
    }

    private BossSkillSet FindSet(BossDifficulty difficulty)
    {
        if (skillSets == null)
            return null;

        for (int i = 0; i < skillSets.Length; i++)
        {
            BossSkillSet set = skillSets[i];

            if (set != null && set.difficulty == difficulty)
                return set;
        }

        return null;
    }
}