using UnityEngine;

[DisallowMultipleComponent]
public class BulletPoolManager : MonoBehaviour
{
    public static BulletPoolManager Instance { get; private set; }

    [SerializeField] private PoolManager pool;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (pool == null) pool = GetComponent<PoolManager>();
        if (pool == null) Debug.LogError("BulletPoolManager: 같은 오브젝트에 PoolManager가 필요합니다.");
    }

    public GameObject GetBullet(int index) => pool != null ? pool.Get(index) : null;
    public void ReleaseBullet(GameObject bullet) { if (pool != null) pool.Release(bullet); }
}