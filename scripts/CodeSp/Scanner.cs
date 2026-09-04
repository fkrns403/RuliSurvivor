using UnityEngine;

public class Scanner : MonoBehaviour
{
    public float scanRange = 10f;
    public LayerMask targetLayer;

    public Transform nearestTarget { get; private set; }

    private void FixedUpdate()
    {
        if (GameManager.Instance == null || !GameManager.Instance.isLive) return;

        var hits = Physics2D.CircleCastAll(transform.position, scanRange, Vector2.zero, 0f, targetLayer);

        float best = float.MaxValue;
        Transform result = null;

        foreach (var h in hits)
        {
            float d = Vector2.Distance(transform.position, h.transform.position);
            if (d < best)
            {
                best = d;
                result = h.transform;
            }
        }

        nearestTarget = result;
    }
}
