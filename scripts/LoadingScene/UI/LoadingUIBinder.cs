using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Loading 씬의 UI를 전역 LoadingManager에 연결한다.
/// 
/// 중요:
/// - LoadingManager는 Loading 씬에 있으면 안 된다.
/// - LoadingManager는 Title 씬의 Managers에서 넘어와야 한다.
/// - 씬 로딩 순서에 따라 이 바인더가 먼저 실행될 수 있으므로
///   짧게 기다린 뒤 바인딩한다.
/// </summary>
[DisallowMultipleComponent]
public class LoadingUIBinder : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject root;
    [SerializeField] private Slider progressSlider;
    [SerializeField] private Text progressText;
    [SerializeField] private Image loadingImage;

    [Header("Debug")]
    [SerializeField] private bool verboseLog = true;

    private IEnumerator Start()
    {
        float timeout = 2f;
        float timer = 0f;

        while (LoadingManager.Instance == null && timer < timeout)
        {
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        if (LoadingManager.Instance == null)
        {
            Debug.LogError(
                "LoadingUIBinder: LoadingManager.Instance가 없습니다. " +
                "Title 씬의 Managers 오브젝트에 PersistentManagers가 붙어 있고, " +
                "그 자식으로 LoadingManager가 있는지 확인하세요.",
                this
            );

            yield break;
        }

        if (verboseLog)
            Debug.Log("LoadingUIBinder: LoadingManager를 찾아 UI를 바인딩합니다.", this);

        LoadingManager.Instance.BindUI(root, progressSlider, progressText, loadingImage);
    }
}