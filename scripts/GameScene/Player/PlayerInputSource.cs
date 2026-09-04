using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class PlayerInputSource : MonoBehaviour
{
    public Vector2 MoveInput { get; private set; }

    [Header("Fallback Keyboard")]
    [SerializeField] private bool useKeyboardFallback = true;

    private PlayerDash dash;
    private bool wasDashPressed;

    private void Awake()
    {
        dash = GetComponent<PlayerDash>();
    }

    private void Update()
    {
        if (!useKeyboardFallback)
            return;

        Keyboard kb = Keyboard.current;

        if (kb == null)
            return;

        Vector2 move = Vector2.zero;

        if (kb.wKey.isPressed || kb.upArrowKey.isPressed)
            move.y += 1f;

        if (kb.sKey.isPressed || kb.downArrowKey.isPressed)
            move.y -= 1f;

        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)
            move.x -= 1f;

        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed)
            move.x += 1f;

        MoveInput = move.sqrMagnitude > 1f ? move.normalized : move;

        bool dashPressed =
            kb.leftShiftKey.isPressed ||
            kb.rightShiftKey.isPressed ||
            kb.spaceKey.isPressed;

        if (dashPressed && !wasDashPressed)
        {
            if (dash == null)
                dash = GetComponent<PlayerDash>();

            if (dash != null)
                dash.TryDash();
        }

        wasDashPressed = dashPressed;
    }

    public void OnMove(InputValue value)
    {
        MoveInput = value.Get<Vector2>();
    }

    public void OnDash(InputValue value)
    {
        if (!value.isPressed)
            return;

        if (dash == null)
            dash = GetComponent<PlayerDash>();

        if (dash != null)
            dash.TryDash();
    }

    private void OnDisable()
    {
        MoveInput = Vector2.zero;
        wasDashPressed = false;
    }
}