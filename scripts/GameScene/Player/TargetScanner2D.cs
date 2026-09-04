using UnityEngine;

/// <summary>
/// 2D에서 가장 가까운 적(Transform)을 찾는 스캐너
/// - 자동발사 무기(Weapon_Range)가 타겟을 필요로 하므로 제공
/// - 적 레이어를 지정해서 OverlapCircle로 탐색한다.
/// </summary>
[DisallowMultipleComponent]
public class TargetScanner2D : MonoBehaviour
{
    [Header("Scan")]
    [SerializeField] private float radius = 8f;
    [SerializeField] private LayerMask enemyLayer;

    public Transform NearestTarget { get; private set; }

    private void Update()
    {
        Scan();
    }

    private void Scan()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius, enemyLayer);
        float best = float.MaxValue;
        Transform bestTr = null;

        for (int i = 0; i < hits.Length; i++)
        {
            var h = hits[i];
            if (h == null) continue;

            float d = (h.transform.position - transform.position).sqrMagnitude;
            if (d < best)
            {
                best = d;
                bestTr = h.transform;
            }
        }

        NearestTarget = bestTr;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, radius);
    }
#endif
}
