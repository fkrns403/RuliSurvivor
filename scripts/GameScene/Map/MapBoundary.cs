using UnityEngine;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
public class MapBoundary : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private TilemapRenderer tilemapRenderer;

    [Tooltip("직접 맞춘 경계 콜라이더. 있으면 가장 우선 사용합니다.")]
    [SerializeField] private Collider2D boundaryCollider;

    private Bounds worldBounds;

    public Bounds WorldBounds => worldBounds;

    public float MinX => worldBounds.min.x;
    public float MaxX => worldBounds.max.x;
    public float MinY => worldBounds.min.y;
    public float MaxY => worldBounds.max.y;

    public BoxCollider2D BoundaryCollider
    {
        get
        {
            AutoFindSources();
            return boundaryCollider as BoxCollider2D;
        }
    }

    public Collider2D BoundaryCollider2D
    {
        get
        {
            AutoFindSources();
            return boundaryCollider;
        }
    }

    private void Awake()
    {
        AutoFindSources();
        Recalculate();
    }

    private void OnEnable()
    {
        AutoFindSources();
        Recalculate();
    }

    public void RefreshBounds()
    {
        Recalculate();
    }

    private void AutoFindSources()
    {
        if (tilemap == null)
            tilemap = GetComponent<Tilemap>();

        if (tilemapRenderer == null)
            tilemapRenderer = GetComponent<TilemapRenderer>();

        if (boundaryCollider == null)
        {
            boundaryCollider = GetComponent<Collider2D>();

            if (boundaryCollider == null)
                boundaryCollider = GetComponentInChildren<Collider2D>(true);
        }
    }

    public void Recalculate()
    {
        AutoFindSources();

        if (boundaryCollider != null)
        {
            worldBounds = boundaryCollider.bounds;
            return;
        }

        if (tilemapRenderer != null)
        {
            worldBounds = tilemapRenderer.bounds;
            return;
        }

        if (tilemap != null)
        {
            Bounds localBounds = tilemap.localBounds;

            Vector3 worldCenter =
                tilemap.transform.TransformPoint(localBounds.center);

            Vector3 worldSize =
                Vector3.Scale(localBounds.size, tilemap.transform.lossyScale);

            worldBounds = new Bounds(worldCenter, worldSize);
            return;
        }

        worldBounds = new Bounds(transform.position, Vector3.zero);
    }

    public Vector3 ClampPosition(Vector3 position, Vector2 padding)
    {
        Recalculate();

        float minX = MinX + padding.x;
        float maxX = MaxX - padding.x;
        float minY = MinY + padding.y;
        float maxY = MaxY - padding.y;

        if (minX > maxX)
        {
            float centerX = worldBounds.center.x;
            minX = centerX;
            maxX = centerX;
        }

        if (minY > maxY)
        {
            float centerY = worldBounds.center.y;
            minY = centerY;
            maxY = centerY;
        }

        float x = Mathf.Clamp(position.x, minX, maxX);
        float y = Mathf.Clamp(position.y, minY, maxY);

        return new Vector3(x, y, position.z);
    }

    public Vector3 ClampPosition(
        Vector3 position,
        float leftPadding,
        float rightPadding,
        float bottomPadding,
        float topPadding)
    {
        Recalculate();

        float minX = MinX + leftPadding;
        float maxX = MaxX - rightPadding;
        float minY = MinY + bottomPadding;
        float maxY = MaxY - topPadding;

        if (minX > maxX)
        {
            float centerX = worldBounds.center.x;
            minX = centerX;
            maxX = centerX;
        }

        if (minY > maxY)
        {
            float centerY = worldBounds.center.y;
            minY = centerY;
            maxY = centerY;
        }

        float x = Mathf.Clamp(position.x, minX, maxX);
        float y = Mathf.Clamp(position.y, minY, maxY);

        return new Vector3(x, y, position.z);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        AutoFindSources();
        Recalculate();

        Gizmos.color = new Color(0f, 1f, 0f, 0.5f);
        Gizmos.DrawWireCube(worldBounds.center, worldBounds.size);
    }
#endif
}