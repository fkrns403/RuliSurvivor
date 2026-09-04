using UnityEngine;

/// <summary>
/// Rigidbody2D 기반 이동 전용 컴포넌트.
/// 
/// 프리팹 런타임 생성 대응:
/// - Rigidbody2D가 비어 있으면 자동으로 다시 찾는다.
/// - Rigidbody2D 설정도 2D 탑다운 이동에 맞게 보정한다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class MovementMotor : MonoBehaviour
{
    private Rigidbody2D rb;

    public void Initialize(Rigidbody2D targetRb)
    {
        rb = targetRb;

        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        ConfigureRigidbody();
    }

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        ConfigureRigidbody();
    }

    private void ConfigureRigidbody()
    {
        if (rb == null)
            return;

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    public void Move(Vector2 direction, float speed, float fixedDeltaTime)
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (rb == null)
            return;

        Vector2 delta = direction * speed * fixedDeltaTime;
        rb.MovePosition(rb.position + delta);
    }
}