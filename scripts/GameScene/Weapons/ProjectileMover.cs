using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class ProjectileMover : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private Rigidbody2D rb;

    [Tooltip("회전시킬 시각 오브젝트. 비워두면 자기 자신을 회전합니다.")]
    [SerializeField] private Transform visualRoot;

    [Header("Move")]
    [SerializeField] private float speed = 10f;

    [Header("Visual Rotation")]
    [SerializeField] private bool rotateToMoveDirection = true;
    [SerializeField] private float visualRotationOffset = 0f;
    [SerializeField] private bool useLaunchRotationOffset = true;

    private Vector2 lastDirection = Vector2.right;

    private void Awake()
    {
        ResolveRigidbody();
        ResetRigidbody();
    }

    private void OnEnable()
    {
        ResolveRigidbody();
        ResetRigidbody();
    }

    public void Launch(Vector2 dir, float newSpeed)
    {
        Launch(dir, newSpeed, visualRotationOffset);
    }

    public void Launch(Vector2 dir, float newSpeed, float rotationOffset)
    {
        speed = Mathf.Max(0f, newSpeed);

        if (useLaunchRotationOffset)
            visualRotationOffset = rotationOffset;

        ResolveRigidbody();

        Vector2 moveDir = ResolveDirection(dir);
        lastDirection = moveDir;

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.angularVelocity = 0f;
            rb.velocity = moveDir * speed;
        }

        ApplyVisualRotation(moveDir);
    }

    public void SetVisualRotationOffset(float offset)
    {
        visualRotationOffset = offset;
        ApplyVisualRotation(lastDirection);
    }

    public void SetRotateToMoveDirection(bool value)
    {
        rotateToMoveDirection = value;
        ApplyVisualRotation(lastDirection);
    }

    private void ResolveRigidbody()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();
    }

    private void ResetRigidbody()
    {
        if (rb == null)
            return;

        rb.gravityScale = 0f;
        rb.angularVelocity = 0f;
        rb.velocity = Vector2.zero;
    }

    private Vector2 ResolveDirection(Vector2 dir)
    {
        if (dir.sqrMagnitude > 0.0001f)
            return dir.normalized;

        if (lastDirection.sqrMagnitude > 0.0001f)
            return lastDirection.normalized;

        return Vector2.right;
    }

    private void ApplyVisualRotation(Vector2 dir)
    {
        if (!rotateToMoveDirection)
            return;

        if (dir.sqrMagnitude < 0.0001f)
            return;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        Transform target = visualRoot != null ? visualRoot : transform;
        target.rotation = Quaternion.Euler(0f, 0f, angle + visualRotationOffset);
    }

    private void OnDisable()
    {
        ResetRigidbody();

        if (visualRoot != null)
            visualRoot.localRotation = Quaternion.identity;
    }
}