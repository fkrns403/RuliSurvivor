using System;
using UnityEngine;
using UnityEngine.UI;

public class ConfirmPopupUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject popupRoot;

    [Header("Texts (Optional)")]
    [SerializeField] private Text titleText;
    [SerializeField] private Text messageText;

    [Header("Buttons")]
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    [Header("Optional Button")]
    [SerializeField] private Button settingsButton;

    [Header("ESC")]
    [SerializeField] private bool closeWithEscape = true;

    private Action _onConfirm;
    private Action _onCancel;

    private void Awake()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(HandleConfirm);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(HandleCancel);
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveAllListeners();
            settingsButton.onClick.AddListener(HandleSettings);
        }

        CloseImmediate();
    }

    private void Update()
    {
        if (!closeWithEscape)
            return;

        if (!IsOpen)
            return;

        if (!Input.GetKeyDown(KeyCode.Escape))
            return;

        HandleCancel();
    }

    public void Open(
        string title,
        string message,
        Action onConfirm,
        Action onCancel)
    {
        _onConfirm = onConfirm;
        _onCancel = onCancel;

        if (titleText != null)
            titleText.text = title;

        if (messageText != null)
            messageText.text = message;

        if (popupRoot != null)
            popupRoot.SetActive(true);
        else
            gameObject.SetActive(true);
    }

    public void Close()
    {
        _onConfirm = null;
        _onCancel = null;

        if (popupRoot != null)
            popupRoot.SetActive(false);
        else
            gameObject.SetActive(false);
    }

    private void CloseImmediate()
    {
        if (popupRoot != null)
            popupRoot.SetActive(false);
        else
            gameObject.SetActive(false);
    }

    private void HandleConfirm()
    {
        Action cb = _onConfirm;

        Close();

        cb?.Invoke();
    }

    private void HandleCancel()
    {
        Action cb = _onCancel;

        Close();

        cb?.Invoke();
    }

    private void HandleSettings()
    {
        Close();

        if (SettingsUIManager.Instance != null)
        {
            SettingsUIManager.Instance.OpenOptions();
        }
        else
        {
            SettingsUIManager ui =
                FindObjectOfType<SettingsUIManager>(true);

            if (ui != null)
                ui.OpenOptions();
        }
    }

    public bool IsOpen
    {
        get
        {
            if (popupRoot != null)
                return popupRoot.activeSelf;

            return gameObject.activeSelf;
        }
    }
}