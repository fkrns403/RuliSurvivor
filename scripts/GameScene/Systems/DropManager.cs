using UnityEngine;

public enum EnemyType
{
    Type0, // 예: Zombie
    Type1, // 예: Skeleton
    Type2, // 예: Ghost
    Boss
}
/// <summary>
/// 드롭(경험치 / 자석 등) 담당 매니저
/// 
/// 역할:
/// - 적이 죽는 순간 DropItems(position, type)를 호출받아 드랍을 생성한다.
/// - 경험치는 두 가지 모드를 지원한다.
///   1) 즉시 지급 모드
///   2) 월드 픽업 드랍 모드
/// - 자석 아이템은 ItemData의 prefab을 사용하여 드랍한다.
/// 
/// 설계 포인트:
/// - Enemy / Boss 쪽은 "무엇을 얼마나 떨어뜨릴지"만 EnemyType으로 전달한다.
/// - 실제 드랍 생성은 이 매니저 한 곳에서 담당한다.
/// - ExpItemData / ItemData는 데이터만 보관하고, Instantiate는 DropManager가 담당한다.
/// </summary>
public class DropManager : MonoBehaviour
{
    public static DropManager Instance { get; private set; }

    [Header("Exp Drop Mode")]
    [Tooltip("true이면 경험치를 월드 픽업으로 드랍하고, false이면 즉시 경험치를 지급한다.")]
    [SerializeField] private bool dropExpAsPickup = true;

    [Header("Direct Exp Values (즉시 지급 모드)")]
    [SerializeField] private int type0ExpValue = 1;
    [SerializeField] private int type1ExpValue = 2;
    [SerializeField] private int type2ExpValue = 3;
    [SerializeField] private int bossExpValue = 20;

    [Header("Exp Item Data (픽업 드랍 모드)")]
    [SerializeField] private ExpItemData[] type0ExpItems;
    [SerializeField] private ExpItemData[] type1ExpItems;
    [SerializeField] private ExpItemData[] type2ExpItems;
    [SerializeField] private ExpItemData[] bossExpItems;

    [Header("Drop Counts (픽업 드랍 모드)")]
    [SerializeField] private int type0DropCount = 1;
    [SerializeField] private int type1DropCount = 2;
    [SerializeField] private int type2DropCount = 3;
    [SerializeField] private int bossDropCount = 10;

    [Header("Magnet Item Drop")]
    [Tooltip("자석 아이템용 ItemData. prefab 필드가 연결되어 있어야 한다.")]
    [SerializeField] private ItemData magnetItemData;

    [Range(0f, 1f)]
    [SerializeField] private float magnetDropChance = 0.1f;

    [Header("Drop Scatter")]
    [Tooltip("경험치 픽업이 약간 흩어지는 반경")]
    [SerializeField] private float scatterRadius = 0.3f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    /// <summary>
    /// 적 사망 시 호출되는 진입점
    /// </summary>
    public void DropItems(Vector3 position, EnemyType type)
    {
        DropExp(position, type);
        DropMagnet(position);
    }

    /// <summary>
    /// 경험치 드랍 처리
    /// - 모드에 따라 픽업 드랍 또는 즉시 지급으로 분기
    /// </summary>
    private void DropExp(Vector3 position, EnemyType type)
    {
        if (dropExpAsPickup)
            DropExpPickups(position, type);
        else
            GiveExpDirectly(type);
    }

    /// <summary>
    /// 경험치 픽업 생성
    /// </summary>
    private void DropExpPickups(Vector3 position, EnemyType type)
    {
        ExpItemData[] itemList = GetExpItemList(type);
        int dropCount = GetDropCount(type);

        if (itemList == null || itemList.Length == 0)
            return;

        if (dropCount <= 0)
            return;

        for (int i = 0; i < dropCount; i++)
        {
            ExpItemData selectedData = GetRandomExpItem(itemList);
            if (selectedData == null)
                continue;

            if (selectedData.prefab == null)
                continue;

            Vector3 spawnPos = position + GetScatterOffset();

            GameObject pickup = Instantiate(selectedData.prefab, spawnPos, Quaternion.identity);

            ExpItem expItem = pickup.GetComponent<ExpItem>();
            if (expItem != null)
            {
                expItem.SetData(selectedData);
            }
        }
    }

    /// <summary>
    /// 즉시 경험치 지급
    /// </summary>
    private void GiveExpDirectly(EnemyType type)
    {
        GameManager gm = GameManager.Instance;
        if (gm == null)
            return;

        int amount = GetDirectExpValue(type);
        if (amount > 0)
        {
            gm.AddExp(amount);
        }
    }

    /// <summary>
    /// 자석 아이템 드랍
    /// </summary>
    private void DropMagnet(Vector3 position)
    {
        if (magnetItemData == null)
            return;

        if (magnetItemData.prefab == null)
            return;

        if (Random.value >= magnetDropChance)
            return;

        Instantiate(magnetItemData.prefab, position, Quaternion.identity);
    }

    /// <summary>
    /// EnemyType에 따른 직접 경험치 값 반환
    /// </summary>
    private int GetDirectExpValue(EnemyType type)
    {
        return type switch
        {
            EnemyType.Type0 => type0ExpValue,
            EnemyType.Type1 => type1ExpValue,
            EnemyType.Type2 => type2ExpValue,
            EnemyType.Boss => bossExpValue,
            _ => 0
        };
    }

    /// <summary>
    /// EnemyType에 맞는 ExpItemData 배열 반환
    /// </summary>
    private ExpItemData[] GetExpItemList(EnemyType type)
    {
        return type switch
        {
            EnemyType.Type0 => type0ExpItems,
            EnemyType.Type1 => type1ExpItems,
            EnemyType.Type2 => type2ExpItems,
            EnemyType.Boss => bossExpItems,
            _ => null
        };
    }

    /// <summary>
    /// EnemyType에 맞는 드랍 개수 반환
    /// </summary>
    private int GetDropCount(EnemyType type)
    {
        return type switch
        {
            EnemyType.Type0 => type0DropCount,
            EnemyType.Type1 => type1DropCount,
            EnemyType.Type2 => type2DropCount,
            EnemyType.Boss => bossDropCount,
            _ => 0
        };
    }

    /// <summary>
    /// ExpItemData 배열에서 랜덤 하나 선택
    /// </summary>
    private ExpItemData GetRandomExpItem(ExpItemData[] list)
    {
        if (list == null || list.Length == 0)
            return null;

        int index = Random.Range(0, list.Length);
        return list[index];
    }

    /// <summary>
    /// 드랍 흩뿌리기용 랜덤 오프셋 계산
    /// </summary>
    private Vector3 GetScatterOffset()
    {
        Vector2 circle = Random.insideUnitCircle * scatterRadius;
        return new Vector3(circle.x, circle.y, 0f);
    }
}