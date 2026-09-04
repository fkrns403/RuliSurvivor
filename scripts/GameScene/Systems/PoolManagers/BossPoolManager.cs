using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 보스 전용 풀 매니저.
/// 
/// 역할:
/// - 보스 프리팹을 인덱스별로 풀링한다.
/// - 비활성 보스가 있으면 재사용한다.
/// - 없으면 새로 생성한다.
/// 
/// 중복 생성 방지:
/// - 같은 bossIndex의 보스가 이미 활성화되어 있으면 새로 생성하지 않는다.
/// - 기존 활성 보스를 그대로 반환한다.
/// - ProgressiveBossSpawner 사용 중 BossSpawner가 같이 켜졌을 때 생기는
///   보스 2마리 생성 문제를 줄인다.
/// </summary>
[DisallowMultipleComponent]
public class BossPoolManager : MonoBehaviour
{
    [Header("Boss Prefabs")]
    [SerializeField] private GameObject[] bossPrefabs;

    [Header("Duplicate Safety")]
    [Tooltip("체크하면 같은 bossIndex의 활성 보스가 이미 있을 때 새 보스를 만들지 않는다.")]
    [SerializeField] private bool preventDuplicateActiveBoss = true;

    private List<GameObject>[] pools;

    public int MaxIndex
    {
        get
        {
            if (bossPrefabs == null || bossPrefabs.Length == 0)
                return 0;

            return bossPrefabs.Length - 1;
        }
    }

    private void Awake()
    {
        if (bossPrefabs == null)
            bossPrefabs = new GameObject[0];

        pools = new List<GameObject>[bossPrefabs.Length];

        for (int i = 0; i < pools.Length; i++)
            pools[i] = new List<GameObject>();
    }

    public GameObject GetBoss(int bossIndex)
    {
        if (bossPrefabs == null || bossPrefabs.Length == 0)
        {
            Debug.LogError("BossPoolManager: bossPrefabs 배열이 비어 있습니다.", this);
            return null;
        }

        bossIndex = Mathf.Clamp(bossIndex, 0, bossPrefabs.Length - 1);

        List<GameObject> pool = pools[bossIndex];

        if (preventDuplicateActiveBoss)
        {
            GameObject activeBoss = FindActiveBoss(pool);

            if (activeBoss != null)
            {
                Debug.LogWarning(
                    $"BossPoolManager: bossIndex {bossIndex} 보스가 이미 활성화되어 있어 기존 보스를 반환합니다.",
                    activeBoss
                );

                return activeBoss;
            }
        }

        for (int i = 0; i < pool.Count; i++)
        {
            GameObject boss = pool[i];

            if (boss == null)
                continue;

            if (!boss.activeSelf)
            {
                boss.transform.SetParent(null);
                boss.SetActive(true);

                ResetBossRuntimeState(boss);

                PooledObject pooled = boss.GetComponent<PooledObject>();

                if (pooled != null)
                    pooled.OnSpawned();

                return boss;
            }
        }

        GameObject prefab = bossPrefabs[bossIndex];

        if (prefab == null)
        {
            Debug.LogError($"BossPoolManager: bossPrefabs[{bossIndex}]가 null입니다.", this);
            return null;
        }

        GameObject created = Instantiate(prefab);
        created.name = prefab.name;
        created.transform.SetParent(null);
        created.SetActive(true);

        pool.Add(created);

        ResetBossRuntimeState(created);

        PooledObject po = created.GetComponent<PooledObject>();

        if (po != null)
        {
            po.SetPoolIndex(bossIndex);
            po.OnSpawned();
        }

        return created;
    }

    private GameObject FindActiveBoss(List<GameObject> pool)
    {
        if (pool == null)
            return null;

        for (int i = 0; i < pool.Count; i++)
        {
            GameObject boss = pool[i];

            if (boss == null)
                continue;

            if (!boss.activeInHierarchy)
                continue;

            BossHealth health = boss.GetComponent<BossHealth>();

            if (health != null && health.IsDead)
                continue;

            return boss;
        }

        return null;
    }

    private void ResetBossRuntimeState(GameObject boss)
    {
        if (boss == null)
            return;

        Rigidbody2D rb = boss.GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.gravityScale = 0f;
            rb.simulated = true;
        }

        Collider2D col = boss.GetComponent<Collider2D>();

        if (col != null)
            col.enabled = true;
    }

    public void Release(GameObject boss)
    {
        if (boss == null)
            return;

        PooledObject po = boss.GetComponent<PooledObject>();

        if (po != null)
            po.OnDespawned();

        Rigidbody2D rb = boss.GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        boss.SetActive(false);
        boss.transform.SetParent(transform);
    }

    public void DeactivateAll()
    {
        if (pools == null)
            return;

        for (int i = 0; i < pools.Length; i++)
        {
            List<GameObject> pool = pools[i];

            if (pool == null)
                continue;

            for (int j = 0; j < pool.Count; j++)
            {
                GameObject boss = pool[j];

                if (boss == null)
                    continue;

                if (!boss.activeSelf)
                    continue;

                Release(boss);
            }
        }
    }

    public int GetActiveBossCount()
    {
        int count = 0;

        if (pools == null)
            return count;

        for (int i = 0; i < pools.Length; i++)
        {
            List<GameObject> pool = pools[i];

            if (pool == null)
                continue;

            for (int j = 0; j < pool.Count; j++)
            {
                GameObject boss = pool[j];

                if (boss != null && boss.activeInHierarchy)
                    count++;
            }
        }

        return count;
    }
}