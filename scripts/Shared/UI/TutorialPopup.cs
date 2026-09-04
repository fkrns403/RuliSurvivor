using UnityEngine;

/// <summary>
/// 튜토리얼 맵 선택 시 표시되는 팝업.
/// 
/// 역할:
/// - TitleManager가 Show(callback)을 호출하면 튜토리얼 패널을 연다.
/// - TutorialPopupController가 단계 진행을 맡는다.
/// - 완료/스킵 시 callback을 실행해서 게임 시작으로 넘긴다.
/// 
/// 수정 핵심:
/// - popupRoot가 꺼져 있어도 강제로 활성화
/// - controller 오브젝트도 강제로 활성화
/// - popupRoot의 바로 아래 자식들도 강제로 활성화
/// - activeSelf / activeInHierarchy 로그로 실제 켜졌는지 확인
/// </summary>
public class TutorialPopup : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject popupRoot;

    [Header("Controller")]
    [SerializeField] private TutorialPopupController controller;

    [Header("Debug")]
    [SerializeField] private bool verboseLog = true;

    private System.Action onFinished;
    private bool isOpen;

    private void Awake()
    {
        if (popupRoot == null)
            popupRoot = gameObject;

        if (controller == null)
            controller = GetComponentInChildren<TutorialPopupController>(true);

        CloseOnly();
    }

    public void Show(System.Action finishedCallback)
    {
        onFinished = finishedCallback;
        isOpen = true;

        if (popupRoot == null)
            popupRoot = gameObject;

        if (controller == null)
            controller = GetComponentInChildren<TutorialPopupController>(true);

        // 부모부터 강제로 활성화
        gameObject.SetActive(true);

        // 실제 패널 활성화
        popupRoot.SetActive(true);

        // 패널 안의 1단계 자식들도 일단 켜준다.
        // 이후 TutorialPopupController가 stepButtons만 단계별로 다시 정리한다.
        for (int i = 0; i < popupRoot.transform.childCount; i++)
        {
            Transform child = popupRoot.transform.GetChild(i);

            if (child != null)
                child.gameObject.SetActive(true);
        }

        // 가장 앞으로 올림
        transform.SetAsLastSibling();
        popupRoot.transform.SetAsLastSibling();

        if (controller != null)
            controller.gameObject.SetActive(true);

        Log(
            $"Show 실행 / popupRoot={popupRoot.name}, " +
            $"activeSelf={popupRoot.activeSelf}, activeInHierarchy={popupRoot.activeInHierarchy}"
        );

        if (controller == null)
        {
            Debug.LogWarning("TutorialPopup: TutorialPopupController가 없습니다. 바로 완료 처리합니다.", this);
            Finish();
            return;
        }

        controller.Open(Finish);
    }

    private void Finish()
    {
        if (!isOpen)
            return;

        isOpen = false;

        CloseOnly();

        System.Action callback = onFinished;
        onFinished = null;

        Log("완료 콜백 실행");

        callback?.Invoke();
    }

    private void CloseOnly()
    {
        if (popupRoot != null)
            popupRoot.SetActive(false);
    }

    private void Log(string message)
    {
        if (!verboseLog)
            return;

        Debug.Log($"TutorialPopup: {message}", this);
    }
}