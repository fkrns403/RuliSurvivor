using UnityEngine;

/// <summary>
/// 자석 아이템
/// - 플레이어와 충돌하면 PlayerMagnet을 찾아 일정 시간 자석 효과를 발동한다.
/// </summary>
public class MagnetItem : MonoBehaviour
{
    [Header("Magnet Duration")]
    [Tooltip("자석 효과 지속 시간(초)")]
    public float magnetDuration = 5f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        PlayerMagnet pm = other.GetComponent<PlayerMagnet>();
        if (pm != null)
        {
            pm.ActivateMagnet(magnetDuration);
        }

        Destroy(gameObject);
    }
}
