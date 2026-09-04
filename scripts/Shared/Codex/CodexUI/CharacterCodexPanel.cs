using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CharacterCodexPanel : MonoBehaviour
{
    [Header("도감 데이터 (페이지 순서대로)")]
    [SerializeField] private CharacterCodexData[] pages;

    [Header("UI 참조 - 반드시 연결")]
    [SerializeField] private Image portraitImage;
    [SerializeField] private Text nameText;
    [SerializeField] private Text descriptionText;
    [SerializeField] private Text weaponText;
    [SerializeField] private Text skillText;

    [Header("무기 아이콘(선택)")]
    [SerializeField] private Image weaponIconImage;

    [Header("잠금 상태 표시")]
    [Tooltip("잠금 캐릭터 도트를 완전히 숨기지 않고 어둡게 표시합니다.")]
    [SerializeField] private bool tintLockedPortrait = true;

    [Tooltip("해금된 캐릭터 도트 색")]
    [SerializeField] private Color unlockedPortraitColor = Color.white;

    [Tooltip("잠금 상태 캐릭터 도트 색. 알파를 1로 두면 실루엣처럼 보이고, 낮추면 흐리게 보입니다.")]
    [SerializeField] private Color lockedPortraitColor = new Color(0.15f, 0.15f, 0.15f, 1f);

    [Tooltip("잠금 상태에서 이름도 ???로 표시할지 여부")]
    [SerializeField] private bool hideLockedName = false;

    [Tooltip("잠금 상태에서 표시할 이름")]
    [SerializeField] private string lockedNameText = "???";

    [Header("페이지 표시용(선택)")]
    [SerializeField] private Text pageIndicatorText;

    [Header("전환 연출용(선택)")]
    [SerializeField] private CanvasGroup pageCanvasGroup;
    [SerializeField] private float transitionDuration = 0.2f;
    [SerializeField] private float transitionScale = 0.94f;

    private int currentIndex;
    private bool isPlayingTransition;

    private void OnEnable()
    {
        currentIndex = Mathf.Clamp(currentIndex, 0, (pages?.Length ?? 1) - 1);
        RefreshPage(forceInstant: true);

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
        RefreshPage(forceInstant: true);
    }

    public void RefreshAll()
    {
        currentIndex = Mathf.Clamp(currentIndex, 0, (pages?.Length ?? 1) - 1);
        RefreshPage(forceInstant: true);
    }

    public void NextPage()
    {
        if (pages == null || pages.Length == 0)
            return;

        if (isPlayingTransition)
            return;

        int next = currentIndex + 1;

        if (next >= pages.Length)
            next = pages.Length - 1;

        if (next != currentIndex)
            StartCoroutine(PlayTransition(next));
    }

    public void PrevPage()
    {
        if (pages == null || pages.Length == 0)
            return;

        if (isPlayingTransition)
            return;

        int next = currentIndex - 1;

        if (next < 0)
            next = 0;

        if (next != currentIndex)
            StartCoroutine(PlayTransition(next));
    }

    private IEnumerator PlayTransition(int nextIndex)
    {
        isPlayingTransition = true;

        if (pageCanvasGroup == null)
        {
            currentIndex = nextIndex;
            RefreshPage(forceInstant: true);
            isPlayingTransition = false;
            yield break;
        }

        RectTransform rt = pageCanvasGroup.GetComponent<RectTransform>();

        float t = 0f;
        Vector3 startScale = Vector3.one;
        Vector3 smallScale = Vector3.one * transitionScale;

        while (t < transitionDuration)
        {
            t += Time.unscaledDeltaTime;
            float lerp = transitionDuration <= 0f ? 1f : t / transitionDuration;

            pageCanvasGroup.alpha = Mathf.Lerp(1f, 0f, lerp);

            if (rt != null)
                rt.localScale = Vector3.Lerp(startScale, smallScale, lerp);

            yield return null;
        }

        currentIndex = nextIndex;
        RefreshPage(forceInstant: true);

        t = 0f;

        while (t < transitionDuration)
        {
            t += Time.unscaledDeltaTime;
            float lerp = transitionDuration <= 0f ? 1f : t / transitionDuration;

            pageCanvasGroup.alpha = Mathf.Lerp(0f, 1f, lerp);

            if (rt != null)
                rt.localScale = Vector3.Lerp(smallScale, startScale, lerp);

            yield return null;
        }

        pageCanvasGroup.alpha = 1f;

        if (rt != null)
            rt.localScale = Vector3.one;

        isPlayingTransition = false;
    }

    private void RefreshPage(bool forceInstant)
    {
        if (pages == null || pages.Length == 0)
        {
            ClearAllUI(forceInstant);
            return;
        }

        currentIndex = Mathf.Clamp(currentIndex, 0, pages.Length - 1);

        CharacterCodexData data = pages[currentIndex];

        if (data == null)
        {
            ClearAllUI(forceInstant);
            return;
        }

        bool isUnlocked = false;

        if (UnlockManager.Instance != null && !string.IsNullOrEmpty(data.unlockId))
            isUnlocked = UnlockManager.Instance.IsUnlocked(data.unlockId);

        if (portraitImage != null)
        {
            portraitImage.sprite = data.portrait;

            if (isUnlocked)
            {
                portraitImage.color = unlockedPortraitColor;
            }
            else
            {
                portraitImage.color =
                    tintLockedPortrait
                    ? lockedPortraitColor
                    : unlockedPortraitColor;
            }
        }

        if (nameText != null)
        {
            if (!isUnlocked && hideLockedName)
            {
                nameText.text = lockedNameText;
            }
            else
            {
                if (data.unlockDefinition != null &&
                    !string.IsNullOrEmpty(data.unlockDefinition.displayName))
                {
                    nameText.text = data.unlockDefinition.displayName;
                }
                else
                {
                    nameText.text = data.characterName;
                }
            }
        }

        if (isUnlocked)
        {
            if (descriptionText != null)
            {
                if (!string.IsNullOrEmpty(data.characterDescription))
                    descriptionText.text = data.characterDescription;
                else if (data.unlockDefinition != null &&
                         !string.IsNullOrEmpty(data.unlockDefinition.unlockedDescription))
                    descriptionText.text = data.unlockDefinition.unlockedDescription;
                else
                    descriptionText.text = string.Empty;
            }

            if (weaponText != null)
            {
                if (!string.IsNullOrEmpty(data.weaponDescription))
                    weaponText.text = data.weaponDescription;
                else
                    weaponText.text = string.Empty;
            }

            if (skillText != null)
            {
                if (!string.IsNullOrEmpty(data.skillDescription))
                    skillText.text = data.skillDescription;
                else
                    skillText.text = string.Empty;
            }
        }
        else
        {
            string lockText =
                data.unlockDefinition != null &&
                !string.IsNullOrEmpty(data.unlockDefinition.lockedDescription)
                    ? data.unlockDefinition.lockedDescription
                    : "해금 조건이 없습니다.";

            if (descriptionText != null)
                descriptionText.text = lockText;

            if (weaponText != null)
                weaponText.text = string.Empty;

            if (skillText != null)
                skillText.text = string.Empty;
        }

        if (weaponIconImage != null)
        {
            if (isUnlocked)
            {
                weaponIconImage.sprite = data.weaponIcon;
                weaponIconImage.color = Color.white;
            }
            else
            {
                weaponIconImage.sprite = null;
                weaponIconImage.color = Color.clear;
            }
        }

        if (pageIndicatorText != null)
            pageIndicatorText.text = $"{currentIndex + 1} / {pages.Length}";

        FixTransitionVisual(forceInstant);
    }

    private void ClearAllUI(bool forceInstant)
    {
        if (portraitImage != null)
        {
            portraitImage.sprite = null;
            portraitImage.color = Color.clear;
        }

        if (nameText != null)
            nameText.text = string.Empty;

        if (descriptionText != null)
            descriptionText.text = string.Empty;

        if (weaponText != null)
            weaponText.text = string.Empty;

        if (skillText != null)
            skillText.text = string.Empty;

        if (weaponIconImage != null)
        {
            weaponIconImage.sprite = null;
            weaponIconImage.color = Color.clear;
        }

        if (pageIndicatorText != null)
            pageIndicatorText.text = string.Empty;

        FixTransitionVisual(forceInstant);
    }

    private void FixTransitionVisual(bool forceInstant)
    {
        if (pageCanvasGroup != null && forceInstant)
        {
            pageCanvasGroup.alpha = 1f;

            RectTransform rt = pageCanvasGroup.GetComponent<RectTransform>();

            if (rt != null)
                rt.localScale = Vector3.one;
        }
    }
}