using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 몬스터 도감 전체 패널.
/// - ScrollView Content 아래에 MonsterCodexCard 프리팹을 필요 개수만큼 생성.
/// - UnlockDefinition 배열에서 targetType == Monster 인 것만 대상으로 삼는다.
/// - UnlockManager.OnStateChanged 를 구독해서, 해금 상태 변경 시 카드들을 리프레시한다.
/// </summary>
public class MonsterCodexPanel : MonoBehaviour
{
    [Header("스크롤뷰 Content")]
    [SerializeField] private Transform contentRoot;

    [Header("몬스터 카드 프리팹")]
    [SerializeField] private MonsterCodexCard cardPrefab;

    [Header("도감에 표시할 UnlockDefinition 목록(타입: Monster 만 넣기)")]
    [SerializeField] private UnlockDefinition[] monsterDefinitions;

    private readonly List<MonsterCodexCard> cards = new List<MonsterCodexCard>();
    private bool isBuilt = false;

    private void OnEnable()
    {
        // 처음 켜질 때 한 번 빌드
        BuildOrRefresh();

        if (UnlockManager.Instance != null)
            UnlockManager.Instance.OnStateChanged += OnUnlockStateChanged;
    }

    private void OnDisable()
    {
        if (UnlockManager.Instance != null)
            UnlockManager.Instance.OnStateChanged -= OnUnlockStateChanged;
    }

    private void OnUnlockStateChanged()
    {
        RefreshAllCards();
    }

    /// <summary>
    /// 처음에는 카드들을 생성하고, 이후에는 단순 Refresh만 수행한다.
    /// </summary>
    public void BuildOrRefresh()
    {
        if (!isBuilt)
        {
            BuildCards();
            isBuilt = true;
        }
        else
        {
            RefreshAllCards();
        }
    }

    private void BuildCards()
    {
        if (contentRoot == null || cardPrefab == null || monsterDefinitions == null)
            return;

        // 기존 자식 제거
        for (int i = contentRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(contentRoot.GetChild(i).gameObject);
        }
        cards.Clear();

        // Monster 타입만 사용
        foreach (var def in monsterDefinitions)
        {
            if (def == null) continue;
            if (def.targetType != UnlockTargetType.Monster) continue;

            var card = Instantiate(cardPrefab, contentRoot);
            card.Setup(def);
            cards.Add(card);
        }
    }

    private void RefreshAllCards()
    {
        foreach (var c in cards)
        {
            if (c != null)
                c.Refresh();
        }
    }
}
