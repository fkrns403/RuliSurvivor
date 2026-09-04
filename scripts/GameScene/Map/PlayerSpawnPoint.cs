using UnityEngine;

public class PlayerSpawnPoint : MonoBehaviour
{
    [Header("기본 스폰 포인트로 쓸지")]
    public bool isDefault = true;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 0.8f);
    }

    public static Transform FindDefault()
    {
        var points = GameObject.FindObjectsOfType<PlayerSpawnPoint>(true);
        if (points == null || points.Length == 0) return null;

        foreach (var p in points)
            if (p != null && p.isDefault)
                return p.transform;

        return points[0].transform;
    }
}
