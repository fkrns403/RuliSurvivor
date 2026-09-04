using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class OvertimeLetterSequenceUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject letterRoot;
    [SerializeField] private CanvasGroup letterCanvasGroup;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button continueButton;

    [Header("Typing")]
    [SerializeField] private float typeInterval = 0.03f;

    [TextArea]
    [SerializeField]
    private string[] defaultLines =
    {
        "여기까지 와줘서 고마워요.",
        "이 게임을 만드는 데\n정말 많은 시간이 들었지만,",
        "당신이 이 마지막 순간까지\n함께해줘서 보람을 느껴요.",
        "준비되었다면,",
        "함께 끝을 향해\n나아가봅시다."
    };

    private readonly Queue<string> queue = new Queue<string>();

    private bool isPlaying;
    private bool canContinue;

    public bool IsPlaying => isPlaying;

    private void Awake()
    {
        if (letterRoot == null)
            letterRoot = gameObject;

        if (letterCanvasGroup == null && letterRoot != null)
            letterCanvasGroup = letterRoot.GetComponent<CanvasGroup>();

        if (letterCanvasGroup == null && letterRoot != null)
            letterCanvasGroup = letterRoot.AddComponent<CanvasGroup>();

        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(OnClickContinue);
        }

        if (messageText != null)
            messageText.text = string.Empty;

        HideVisual();
    }

    public void StartSequence()
    {
        PlayLines(defaultLines);
    }

    public void PlayLines(string[] lines)
    {
        if (isPlaying)
            return;

        StartCoroutine(PlayRoutine(lines));
    }

    private IEnumerator PlayRoutine(string[] lines)
    {
        isPlaying = true;
        canContinue = false;

        ShowVisual();

        queue.Clear();

        if (lines != null)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                if (!string.IsNullOrEmpty(lines[i]))
                    queue.Enqueue(lines[i]);
            }
        }

        yield return ShowNextLineRoutine();
    }

    private IEnumerator ShowNextLineRoutine()
    {
        if (queue.Count <= 0)
        {
            EndSequence();
            yield break;
        }

        string line = queue.Dequeue();

        if (messageText != null)
            messageText.text = string.Empty;

        canContinue = false;

        float interval = Mathf.Max(0.001f, typeInterval);

        for (int i = 0; i < line.Length; i++)
        {
            if (messageText != null)
                messageText.text += line[i];

            yield return new WaitForSecondsRealtime(interval);
        }

        canContinue = true;
    }

    private void OnClickContinue()
    {
        if (!isPlaying)
            return;

        if (!canContinue)
            return;

        canContinue = false;

        StartCoroutine(ShowNextLineRoutine());
    }

    private void EndSequence()
    {
        HideVisual();

        isPlaying = false;
        canContinue = false;
    }

    private void ShowVisual()
    {
        if (letterRoot != null && letterRoot != gameObject)
            letterRoot.SetActive(true);

        if (letterCanvasGroup != null)
        {
            letterCanvasGroup.alpha = 1f;
            letterCanvasGroup.interactable = true;
            letterCanvasGroup.blocksRaycasts = true;
        }
    }

    private void HideVisual()
    {
        if (letterRoot != null && letterRoot != gameObject)
            letterRoot.SetActive(false);

        if (letterCanvasGroup != null)
        {
            letterCanvasGroup.alpha = 0f;
            letterCanvasGroup.interactable = false;
            letterCanvasGroup.blocksRaycasts = false;
        }
    }
}