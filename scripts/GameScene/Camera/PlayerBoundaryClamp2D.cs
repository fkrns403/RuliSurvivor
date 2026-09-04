using UnityEngine;

/// <summary>
/// 플레이어가 맵 경계 밖으로 나가지 못하게 제한.
/// 
/// 기준:
/// - 플레이어는 맵 끝까지 거의 갈 수 있게 한다.
/// - 위쪽도 너무 강하게 막지 않는다.
/// - 회전 아이템/무기 연출은 카메라 topViewOffset으로 보완한다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerBoundaryClamp2D : MonoBehaviour
{
    [Header("Boundary")]
    [SerializeField] private MapBoundary mapBoundary;

    [Header("Padding")]
    [SerializeField] private float leftPadding = 0.4f;
    [SerializeField] private float rightPadding = 0.4f;
    [SerializeField] private float bottomPadding = 0.4f;
    [SerializeField] private float topPadding = 0.4f;

    [Header("Auto Find")]
    [SerializeField] private bool autoFindBoundary = true;
    [SerializeField] private float findInterval = 0.25f;

    private Rigidbody2D rb;
    private float nextFindTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        FindBoundary();
    }

    private void OnEnable()
    {
        FindBoundary();
    }

    private void FixedUpdate()
    {
        ClampNow();
    }

    private void LateUpdate()
    {
        ClampNow();
    }

    private void FindBoundary()
    {
        if (!autoFindBoundary)
            return;

        if (GameManager.Instance != null &&
            GameManager.Instance.CurrentMapBoundary != null)
        {
            mapBoundary = GameManager.Instance.CurrentMapBoundary;
            return;
        }

        mapBoundary = FindObjectOfType<MapBoundary>(true);
    }

    private void ClampNow()
    {
        if (mapBoundary == null)
        {
            if (autoFindBoundary && Time.unscaledTime >= nextFindTime)
            {
                nextFindTime = Time.unscaledTime + findInterval;
                FindBoundary();
            }

            return;
        }

        Vector3 current = transform.position;

        Vector3 clamped = mapBoundary.ClampPosition(
            current,
            leftPadding,
            rightPadding,
            bottomPadding,
            topPadding
        );

        if ((current - clamped).sqrMagnitude < 0.000001f)
            return;

        transform.position = clamped;

        if (rb != null)
        {
            rb.position = clamped;
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }
}