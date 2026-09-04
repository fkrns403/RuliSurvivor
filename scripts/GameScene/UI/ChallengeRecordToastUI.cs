using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ChallengeRecordToastUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject root;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button continueButton;

    private bool isPlaying;
    private bool continuePressed;

    public bool IsPlaying => isPlaying;

    private void Awake()
    {
        if (root == null)
            root = gameObject;

        if (canvasGroup == null && root != null)
            canvasGroup = root.GetComponent<CanvasGroup>();

        if (canvasGroup == null && root != null)
            canvasGroup = root.AddComponent<CanvasGroup>();

        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(OnClickContinue);
        }

        Hide();
    }

    public void Play(string message)
    {
        if (string.IsNullOrEmpty(message))
            return;

        StopAllCoroutines();
        StartCoroutine(PlayRoutine(message));
    }

    private IEnumerator PlayRoutine(string message)
    {
        isPlaying = true;
        continuePressed = false;

        if (messageText != null)
            messageText.text = message;

        Show();

        while (!continuePressed)
            yield return null;

        Hide();

        isPlaying = false;
    }

    private void OnClickContinue()
    {
        continuePressed = true;
    }

    private void Show()
    {
        if (root != null)
            root.SetActive(true);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        if (continueButton != null)
            continueButton.gameObject.SetActive(true);
    }

    private void Hide()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (root != null)
            root.SetActive(false);
    }
}