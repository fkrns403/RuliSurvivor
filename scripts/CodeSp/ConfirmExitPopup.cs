using UnityEngine;
using UnityEngine.UI;

public class ConfirmExitPopup : MonoBehaviour
{
    [Header("Popup Root (패널 최상위)")]
    [SerializeField] private GameObject popupRoot;

    [Header("Buttons")]
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button confirmButton;

    // 외부에서 주입되는 동작
    private System.Action onCancel;
    private System.Action onConfirm;

    public bool IsOpen => popupRoot != null && popupRoot.activeSelf;

    private void Awake()
    {
        if (popupRoot != null)
            popupRoot.SetActive(false);

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(HandleCancel);
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(HandleConfirm);
        }
    }

    public void Bind(System.Action onCancel, System.Action onConfirm)
    {
        this.onCancel = onCancel;
        this.onConfirm = onConfirm;
    }

    public void Open()
    {
        if (popupRoot != null)
            popupRoot.SetActive(true);
    }

    public void Close()
    {
        if (popupRoot != null)
            popupRoot.SetActive(false);
    }

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    private void HandleCancel()
    {
        Close();
        onCancel?.Invoke();
    }

    private void HandleConfirm()
    {
        // 일단 닫고 실행(씬 전환/종료 시 UI 꼬임 방지)
        Close();
        onConfirm?.Invoke();
    }
}
