using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 튜토리얼 팝업 내부 단계 진행 컨트롤러.
/// 
/// 구조:
/// - Step Buttons 배열에 튜토리얼 페이지 버튼들을 순서대로 넣는다.
/// - 현재 단계 버튼만 켜진다.
/// - 현재 단계 버튼을 누르면 다음 단계로 간다.
/// - 마지막 단계 이후 완료 콜백을 호출한다.
/// - Exit Button은 스킵/완료 처리한다.
/// 
/// 안전 처리:
/// - Step Buttons가 비어 있으면 즉시 완료 처리
/// - Exit Button만 있어도 스킵 가능
/// - 콜백 중복 호출 방지
/// </summary>
public class TutorialPopupController : MonoBehaviour
{
    [Header("Step Buttons")]
    [SerializeField] private Button[] stepButtons;

    [Header("Exit / Skip Button")]
    [SerializeField] private Button exitButton;

    [Header("Debug")]
    [SerializeField] private bool verboseLog = true;

    private int stepIndex;
    private System.Action onFinished;
    private bool finished;

    public void Open(System.Action finishedCallback)
    {
        onFinished = finishedCallback;
        stepIndex = 0;
        finished = false;

        gameObject.SetActive(true);

        BindButtons();

        if (stepButtons == null || stepButtons.Length == 0)
        {
            Debug.LogWarning("TutorialPopupController: Step Buttons가 비어 있어 즉시 완료 처리합니다.", this);
            Finish();
            return;
        }

        Refresh();

        Log("튜토리얼 팝업 컨트롤러 시작");
    }

    private void BindButtons()
    {
        if (stepButtons != null)
        {
            for (int i = 0; i < stepButtons.Length; i++)
            {
                if (stepButtons[i] == null)
                    continue;

                int capturedIndex = i;

                stepButtons[i].onClick.RemoveAllListeners();
                stepButtons[i].onClick.AddListener(() => OnStepClicked(capturedIndex));
            }
        }

        if (exitButton != null)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(Finish);
        }
    }

    private void OnStepClicked(int clickedIndex)
    {
        if (finished)
            return;

        if (clickedIndex != stepIndex)
            return;

        Log($"튜토리얼 단계 클릭 / step={stepIndex}");

        stepIndex++;

        if (stepButtons == null || stepIndex >= stepButtons.Length)
        {
            Finish();
            return;
        }

        Refresh();
    }

    private void Refresh()
    {
        if (stepButtons == null)
            return;

        for (int i = 0; i < stepButtons.Length; i++)
        {
            if (stepButtons[i] == null)
                continue;

            stepButtons[i].gameObject.SetActive(i == stepIndex);
        }
    }

    private void Finish()
    {
        if (finished)
            return;

        finished = true;

        Log("튜토리얼 완료");

        System.Action callback = onFinished;
        onFinished = null;

        callback?.Invoke();
    }

    private void Log(string message)
    {
        if (!verboseLog)
            return;

        Debug.Log($"TutorialPopupController: {message}", this);
    }
}