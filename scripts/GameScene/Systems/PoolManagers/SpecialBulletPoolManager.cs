using UnityEngine;

[DisallowMultipleComponent]
public class SpecialBulletPoolManager : MonoBehaviour
{
    public static SpecialBulletPoolManager Instance { get; private set; }

    [SerializeField] private PoolManager pool;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (pool == null) pool = GetComponent<PoolManager>();
        if (pool == null) Debug.LogError("SpecialBulletPoolManager: 같은 오브젝트에 PoolManager가 필요합니다.");
    }

    public GameObject GetSpecial(int index) => pool != null ? pool.Get(index) : null;
    public void ReleaseSpecial(GameObject obj) { if (pool != null) pool.Release(obj); }
}