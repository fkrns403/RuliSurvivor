using System.Collections;
using UnityEngine;

/// <summary>
/// 플레이어 자석 효과 컨트롤러
/// - MagnetItem을 먹으면 일정 시간 동안 주변 ExpItem이 플레이어로 끌려오게 만든다.
/// - 주변 ExpItem을 탐색하여 ExpItem.ActivateMagnet()을 호출한다.
/// </summary>
public class PlayerMagnet : MonoBehaviour
{
    [Header("Magnet Range")]
    [Tooltip("자석이 활성화될 때 ExpItem을 탐색할 반경(월드 단위)")]
    [SerializeField] private float magnetRange = 10f;

    [Header("Scan Interval")]
    [Tooltip("몇 초마다 주변 ExpItem을 탐색할지(너무 낮으면 비용 증가)")]
    [SerializeField] private float scanInterval = 0.15f;

    private Coroutine _magnetRoutine;
    private float _magnetEndTime;

    /// <summary>
    /// 자석 활성화 (MagnetItem이 호출)
    /// </summary>
    public void ActivateMagnet(float duration)
    {
        if (duration <= 0f) return;

        _magnetEndTime = Mathf.Max(_magnetEndTime, Time.time + duration);

        // 이미 루틴이 돌고 있으면 종료시간만 연장
        if (_magnetRoutine == null)
            _magnetRoutine = StartCoroutine(Co_Magnet());
    }

    private IEnumerator Co_Magnet()
    {
        var wait = new WaitForSeconds(scanInterval);

        while (Time.time < _magnetEndTime)
        {
            // 반경 내 ExpItem들을 전부 자석 활성화
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, magnetRange);

            for (int i = 0; i < hits.Length; i++)
            {
                var expItem = hits[i].GetComponent<ExpItem>();
                if (expItem != null)
                    expItem.ActivateMagnet();
            }

            yield return wait;
        }

        _magnetRoutine = null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, magnetRange);
    }
}
