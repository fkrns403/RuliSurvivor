using UnityEngine;

/// <summary>
/// 경험치 아이템
/// - 플레이어가 먹으면 GameManager.AddExp로 경험치가 올라감
/// - 자석이 활성화되면 플레이어 쪽으로 이동
/// 
/// 개선 사항:
/// - OnEnable에서 상태를 초기화하여 재사용에 안전하게 한다.
/// - 플레이어 참조를 필요할 때 다시 찾는다.
/// - DropManager가 SetData()로 경험치 데이터를 주입할 수 있다.
/// </summary>
public class ExpItem : MonoBehaviour
{
    [Header("Exp Data")]
    [SerializeField] private ExpItemData data;

    [Header("Magnet Move")]
    [SerializeField] private float magnetSpeed = 8f;

    private Transform player;
    private bool isMagnet;

    /// <summary>
    /// 현재 연결된 경험치 데이터 읽기 전용 접근자
    /// </summary>
    public ExpItemData Data => data;

    private void OnEnable()
    {
        isMagnet = false;
        player = null;
    }

    private void Update()
    {
        EnsurePlayerReference();

        if (!isMagnet) return;
        if (player == null) return;

        transform.position = Vector3.MoveTowards(
            transform.position,
            player.position,
            magnetSpeed * Time.deltaTime
        );
    }

    /// <summary>
    /// 외부에서 경험치 데이터를 주입할 때 사용
    /// </summary>
    public void SetData(ExpItemData newData)
    {
        data = newData;
    }

    public void ActivateMagnet()
    {
        isMagnet = true;
        EnsurePlayerReference();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (GameManager.Instance != null && data != null)
        {
            GameManager.Instance.AddExp(data.expAmount);
        }

        Destroy(gameObject);
    }

    /// <summary>
    /// 플레이어 참조를 보장
    /// - GameManager.PlayerTransform 우선
    /// - 없으면 태그 탐색 fallback
    /// </summary>
    private void EnsurePlayerReference()
    {
        if (player != null)
            return;

        if (GameManager.Instance != null && GameManager.Instance.PlayerTransform != null)
        {
            player = GameManager.Instance.PlayerTransform;
            return;
        }

        GameObject p = GameObject.FindWithTag("Player");
        if (p != null)
            player = p.transform;
    }
}