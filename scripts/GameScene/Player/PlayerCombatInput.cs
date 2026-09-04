using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCombatInput : MonoBehaviour
{
    public bool AttackPressed { get; private set; }

    public void OnAttack(InputValue value)
    {
        // 버튼 눌림 순간만 true
        AttackPressed = value.isPressed;
    }

    public void ConsumeAttack()
    {
        AttackPressed = false;
    }

    private void OnDisable()
    {
        AttackPressed = false;
    }
}
