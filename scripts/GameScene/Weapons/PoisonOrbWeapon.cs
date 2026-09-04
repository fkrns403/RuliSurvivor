using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 독 하트 무기
/// 
/// 이번 수정 핵심:
/// - 공격속도 배율을 탐색 주기 / 독 틱 간격에 반영
/// </summary>
public class PoisonOrbWeapon : MonoBehaviour, IWeaponRuntime
{
    [Header("Node Prefab")]
    [SerializeField] private PoisonOrbNode orbNodePrefab;

    [Header("Search")]
    [SerializeField] private LayerMask enemyMask = ~0;
    [SerializeField] private float searchRange = 6f;
    [SerializeField] private float searchInterval = 0.12f;

    [Header("Fallback Stats")]
    [SerializeField] private int fallbackStartCount = 1;
    [SerializeField] private float fallbackBaseTickDamage = 5f;
    [SerializeField] private float tickInterval = 0.3f;
    [SerializeField] private float aoeRadius = 1.5f;
    [SerializeField] private float flySpeed = 14f;

    private Transform owner;
    private ItemData itemData;
    private int level = 1;
    private float searchTimer;

    private PlayerStatSystem playerStatSystem;

    private readonly List<PoisonOrbNode> nodes = new List<PoisonOrbNode>();
    private readonly Collider2D[] searchBuffer = new Collider2D[32];
    private readonly List<Transform> candidateTargets = new List<Transform>();

    public void OnEquip(Transform ownerTransform, ItemData data)
    {
        owner = ownerTransform;
        itemData = data;
        level = 1;
        searchTimer = 0f;

        if (owner != null)
            playerStatSystem = owner.GetComponent<PlayerStatSystem>();

        RebuildNodes(GetNodeCountByLevel(level));
        ApplyStatsToNodes();
    }

    public void OnUnequip()
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i] != null)
                Destroy(nodes[i].gameObject);
        }

        nodes.Clear();
    }

    public void SetLevel(int newLevel)
    {
        level = Mathf.Max(1, newLevel);

        if (itemData != null)
            level = itemData.ClampLevel(level);

        RebuildNodes(GetNodeCountByLevel(level));
        ApplyStatsToNodes();
    }

    private void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm != null && (!gm.isLive || gm.isPaused))
            return;

        if (owner == null)
            return;

        searchTimer += Time.deltaTime;
        if (searchTimer < GetSearchInterval())
            return;

        searchTimer = 0f;
        AssignTargetsToIdleNodes();
    }

    private int GetNodeCountByLevel(int currentLevel)
    {
        currentLevel = Mathf.Max(1, currentLevel);

        if (itemData != null)
            return Mathf.Max(1, itemData.GetCountAtLevel(currentLevel));

        return fallbackStartCount + (currentLevel - 1);
    }

    private void RebuildNodes(int targetCount)
    {
        targetCount = Mathf.Max(1, targetCount);

        while (nodes.Count < targetCount)
        {
            if (orbNodePrefab == null)
                break;

            PoisonOrbNode node = Instantiate(orbNodePrefab, transform);
            nodes.Add(node);
        }

        while (nodes.Count > targetCount)
        {
            int lastIndex = nodes.Count - 1;

            if (nodes[lastIndex] != null)
                Destroy(nodes[lastIndex].gameObject);

            nodes.RemoveAt(lastIndex);
        }

        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i] != null)
                nodes[i].Setup(owner, i, nodes.Count);
        }
    }

    private void ApplyStatsToNodes()
    {
        float damage = GetTickDamageByLevel(level);
        float radius = GetAoeRadiusByLevel(level);
        float finalTickInterval = GetTickInterval();

        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i] == null)
                continue;

            nodes[i].Setup(owner, i, nodes.Count);
            nodes[i].SetCombatStats(
                enemyMask,
                searchRange,
                flySpeed,
                damage,
                finalTickInterval,
                radius
            );
        }
    }

    private void AssignTargetsToIdleNodes()
    {
        candidateTargets.Clear();

        int hitCount = Physics2D.OverlapCircleNonAlloc(
            owner.position,
            searchRange,
            searchBuffer,
            enemyMask
        );

        if (hitCount <= 0)
            return;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D col = searchBuffer[i];
            if (col == null)
                continue;

            Transform tr = col.transform;
            if (tr == null || !tr.gameObject.activeInHierarchy)
                continue;

            candidateTargets.Add(tr);
        }

        for (int i = 0; i < nodes.Count; i++)
        {
            PoisonOrbNode node = nodes[i];
            if (node == null)
                continue;

            if (!node.IsIdle)
                continue;

            Transform target = FindUntakenNearestTarget(node.transform.position);
            if (target != null)
                node.LaunchTo(target);
        }
    }

    private Transform FindUntakenNearestTarget(Vector3 from)
    {
        Transform best = null;
        float bestDist = float.MaxValue;

        for (int i = 0; i < candidateTargets.Count; i++)
        {
            Transform tr = candidateTargets[i];
            if (tr == null || !tr.gameObject.activeInHierarchy)
                continue;

            if (IsAlreadyClaimed(tr))
                continue;

            float dist = (tr.position - from).sqrMagnitude;
            if (dist < bestDist)
            {
                bestDist = dist;
                best = tr;
            }
        }

        return best;
    }

    private bool IsAlreadyClaimed(Transform target)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i] == null)
                continue;

            if (nodes[i].CurrentTarget == target)
                return true;
        }

        return false;
    }

    private float GetTickDamageByLevel(int currentLevel)
    {
        currentLevel = Mathf.Max(1, currentLevel);

        if (itemData != null)
            return Mathf.Max(0.01f, itemData.GetDamageAtLevel(currentLevel));

        return fallbackBaseTickDamage + (currentLevel - 1) * 2f;
    }

    private float GetAoeRadiusByLevel(int currentLevel)
    {
        return aoeRadius + (currentLevel - 1) * 0.15f;
    }

    private float GetSearchInterval()
    {
        float attackSpeedMul = 1f;
        if (playerStatSystem != null)
            attackSpeedMul = Mathf.Max(0.01f, playerStatSystem.GetAttackSpeedMultiplier());

        return Mathf.Max(0.03f, searchInterval / attackSpeedMul);
    }

    private float GetTickInterval()
    {
        float attackSpeedMul = 1f;
        if (playerStatSystem != null)
            attackSpeedMul = Mathf.Max(0.01f, playerStatSystem.GetAttackSpeedMultiplier());

        return Mathf.Max(0.05f, tickInterval / attackSpeedMul);
    }
}