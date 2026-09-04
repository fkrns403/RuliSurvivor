using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 인덱스 기반 풀 매니저
/// - prefabs[index] 프리팹을 풀링한다.
/// - PooledObject가 붙어있으면 OnSpawned/OnDespawned를 자동 호출한다.
/// </summary>
public class PoolManager : MonoBehaviour
{
    [Header("Pooled Prefabs (index-based)")]
    public GameObject[] prefabs;

    private List<GameObject>[] pools;

    private void Awake()
    {
        if (prefabs == null) prefabs = new GameObject[0];

        pools = new List<GameObject>[prefabs.Length];
        for (int i = 0; i < pools.Length; i++)
            pools[i] = new List<GameObject>();
    }

    public int MaxIndex => (prefabs != null && prefabs.Length > 0) ? prefabs.Length - 1 : 0;

    /// <summary>
    /// index에 해당하는 프리팹 풀에서 오브젝트를 꺼낸다.
    /// </summary>
    public GameObject Get(int index)
    {
        if (prefabs == null || prefabs.Length == 0) return null;

        index = Mathf.Clamp(index, 0, prefabs.Length - 1);

        // 1) 비활성 오브젝트 재사용
        var pool = pools[index];
        for (int i = 0; i < pool.Count; i++)
        {
            var item = pool[i];
            if (item == null) continue;

            if (!item.activeSelf)
            {
                item.SetActive(true);

                var po = item.GetComponent<PooledObject>();
                if (po != null) po.OnSpawned();

                return item;
            }
        }

        // 2) 없으면 새로 생성
        var prefab = prefabs[index];
        if (prefab == null) return null;

        var created = Instantiate(prefab, transform);
        pool.Add(created);

        var pooled = created.GetComponent<PooledObject>();
        if (pooled != null)
        {
            pooled.SetPoolIndex(index);
            pooled.OnSpawned();
        }

        return created;
    }

    /// <summary>
    /// 오브젝트를 풀로 반환(비활성화).
    /// </summary>
    public void Release(GameObject obj)
    {
        if (obj == null) return;

        var po = obj.GetComponent<PooledObject>();
        if (po != null) po.OnDespawned();

        obj.SetActive(false);
        obj.transform.SetParent(transform);
    }

    /// <summary>
    /// 풀 전체 비활성화(씬 전환/게임 종료 때)
    /// </summary>
    public void DeactivateAll()
    {
        if (pools == null) return;

        for (int i = 0; i < pools.Length; i++)
        {
            var pool = pools[i];
            for (int j = 0; j < pool.Count; j++)
            {
                var obj = pool[j];
                if (obj == null) continue;

                var po = obj.GetComponent<PooledObject>();
                if (po != null) po.OnDespawned();

                obj.SetActive(false);
                obj.transform.SetParent(transform);
            }
        }
    }
}