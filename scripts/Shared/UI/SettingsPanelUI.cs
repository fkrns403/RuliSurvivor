using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 설정 UI 패널.
/// - Open() 시 현재 설정(SettingsManager.Current)을 UI에 반영
/// - Apply/Save/Reset/Close 버튼 제공
/// - 슬라이더는 미리 오디오만 반영(선택)
///
/// 주의
/// - UI 값을 코드로 세팅할 때(Slider.value = ...) onValueChanged가 같이 호출될 수 있다.
/// - 그때 미리적용 로직이 실행되면 의도치 않은 Apply가 발생할 수 있다.
/// - 이를 방지하기 위해 _isBinding 플래그로 "현재 UI 바인딩 중"을 구분한다.
///
/// 추가 기능
/// - 설정창을 열 때 항상 스크롤이 맨 위에서 시작하도록 강제한다.
/// </summary>
public class SettingsPanelUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("Scroll (Optional)")]
    [Tooltip("ScrollRect가 붙어있는 오브젝트를 넣는다. (보통 PausePanel 또는 ScrollView 루트)")]
    [SerializeField] private ScrollRect scrollRect;

    [Header("Audio")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("Video")]
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private Toggle vSyncToggle;

    [Tooltip("0=30, 1=60, 2=120, 3=Unlimited")]
    [SerializeField] private TMP_Dropdown fpsDropdown;

    [Tooltip("QualitySettings.names 기반")]
    [SerializeField] private TMP_Dropdown qualityDropdown;

    [Tooltip("SettingsManager.WindowResolutions 기반(창모드용)")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;

    [Header("Buttons")]
    [SerializeField] private Button applyButton;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button closeButton;

    public bool IsOpen => root != null ? root.activeSelf : gameObject.activeSelf;

    // UI에 값을 세팅하는 중인지 여부
    // true일 때는 onValueChanged가 와도 무시하여 "바인딩 중 이벤트"를 막는다.
    private bool _isBinding;

    // 드롭다운 옵션 생성 여부
    private bool _dropdownBuilt;

    private void Awake()
    {
        // 버튼 이벤트 연결(한 번만)
        if (applyButton != null) applyButton.onClick.AddListener(Apply);
        if (saveButton != null) saveButton.onClick.AddListener(Save);
        if (resetButton != null) resetButton.onClick.AddListener(ResetToDefault);
        if (closeButton != null) closeButton.onClick.AddListener(Close);

        // 값 변경 시 오디오만 미리 반영(한 번만)
        if (masterSlider != null) masterSlider.onValueChanged.AddListener(_ => PreviewApplyAudioOnly());
        if (bgmSlider != null) bgmSlider.onValueChanged.AddListener(_ => PreviewApplyAudioOnly());
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(_ => PreviewApplyAudioOnly());

        Close();
    }

    public void Open()
    {
        if (root != null) root.SetActive(true);
        else gameObject.SetActive(true);

        BuildDropdownsOnce();
        LoadToUIFromCurrent();

        // UI 로딩이 끝난 뒤 스크롤을 맨 위로 올린다.
        ScrollToTop();
    }

    public void Close()
    {
        if (root != null) root.SetActive(false);
        else gameObject.SetActive(false);
    }

    private void BuildDropdownsOnce()
    {
        if (_dropdownBuilt) return;
        _dropdownBuilt = true;

        // Quality Dropdown
        if (qualityDropdown != null)
        {
            qualityDropdown.ClearOptions();
            var names = QualitySettings.names;
            var opts = new System.Collections.Generic.List<string>(names);
            qualityDropdown.AddOptions(opts);
        }

        // Resolution Dropdown (창모드 프리셋)
        if (resolutionDropdown != null)
        {
            resolutionDropdown.ClearOptions();
            var opts = new System.Collections.Generic.List<string>();
            foreach (var r in SettingsManager.WindowResolutions)
                opts.Add($"{r.x} x {r.y}");
            resolutionDropdown.AddOptions(opts);
        }

        // FPS Dropdown (옵션이 비어있으면 채움)
        if (fpsDropdown != null && fpsDropdown.options.Count == 0)
        {
            fpsDropdown.ClearOptions();
            fpsDropdown.AddOptions(new System.Collections.Generic.List<string>
            {
                "30",
                "60",
                "120",
                "Unlimited"
            });
        }
    }

    private void LoadToUIFromCurrent()
    {
        var sm = SettingsManager.Instance;
        if (sm == null) return;

        var s = sm.Current;

        _isBinding = true;

        if (masterSlider != null) masterSlider.value = s.master;
        if (bgmSlider != null) bgmSlider.value = s.bgm;
        if (sfxSlider != null) sfxSlider.value = s.sfx;

        if (fullscreenToggle != null) fullscreenToggle.isOn = s.fullscreen;
        if (vSyncToggle != null) vSyncToggle.isOn = s.vSync;

        if (fpsDropdown != null) fpsDropdown.value = Mathf.Clamp(s.targetFpsIndex, 0, 3);

        if (qualityDropdown != null)
        {
            int qMax = Mathf.Max(0, QualitySettings.names.Length - 1);
            qualityDropdown.value = Mathf.Clamp(s.qualityIndex, 0, qMax);
        }

        if (resolutionDropdown != null)
        {
            int rMax = Mathf.Max(0, SettingsManager.WindowResolutions.Length - 1);
            resolutionDropdown.value = Mathf.Clamp(s.windowResolutionIndex, 0, rMax);
        }

        _isBinding = false;
    }

    private GameSettings ReadFromUI()
    {
        var sm = SettingsManager.Instance;
        var baseSettings = sm != null ? sm.Current : GameSettings.Default();

        // 복사본 생성(원본 보호)
        GameSettings s = new GameSettings
        {
            master = baseSettings.master,
            bgm = baseSettings.bgm,
            sfx = baseSettings.sfx,
            fullscreen = baseSettings.fullscreen,
            windowResolutionIndex = baseSettings.windowResolutionIndex,
            qualityIndex = baseSettings.qualityIndex,
            targetFpsIndex = baseSettings.targetFpsIndex,
            vSync = baseSettings.vSync,
            screenShake = baseSettings.screenShake,
            shakeStrength = baseSettings.shakeStrength,
            damageNumbers = baseSettings.damageNumbers
        };

        if (masterSlider != null) s.master = masterSlider.value;
        if (bgmSlider != null) s.bgm = bgmSlider.value;
        if (sfxSlider != null) s.sfx = sfxSlider.value;

        if (fullscreenToggle != null) s.fullscreen = fullscreenToggle.isOn;
        if (vSyncToggle != null) s.vSync = vSyncToggle.isOn;

        if (fpsDropdown != null) s.targetFpsIndex = fpsDropdown.value;
        if (qualityDropdown != null) s.qualityIndex = qualityDropdown.value;
        if (resolutionDropdown != null) s.windowResolutionIndex = resolutionDropdown.value;

        return s;
    }

    public void Apply()
    {
        var sm = SettingsManager.Instance;
        if (sm == null) return;

        var s = ReadFromUI();
        sm.Set(s);
        sm.Apply();
    }

    public void Save()
    {
        var sm = SettingsManager.Instance;
        if (sm == null) return;

        Apply();
        sm.Save();
    }

    public void ResetToDefault()
    {
        var sm = SettingsManager.Instance;
        if (sm == null) return;

        sm.ResetToDefault();
        LoadToUIFromCurrent();

        // Reset 후에도 항상 맨 위부터 보이게 유지
        ScrollToTop();
    }

    private void PreviewApplyAudioOnly()
    {
        // UI 값을 코드로 세팅하는 중이면 미리적용하지 않는다.
        if (_isBinding) return;

        var sm = SettingsManager.Instance;
        if (sm == null) return;

        var s = sm.Current;

        float master = masterSlider != null ? masterSlider.value : s.master;
        float bgm = bgmSlider != null ? bgmSlider.value : s.bgm;
        float sfx = sfxSlider != null ? sfxSlider.value : s.sfx;

        // 매번 FindObjectOfType를 호출하지 않고 싱글톤을 직접 사용한다.
        if (AudioManager.instance != null)
            AudioManager.instance.SetVolumes(master, bgm, sfx);
    }

    /// <summary>
    /// 설정창을 열 때 항상 맨 위부터 보이도록 스크롤 위치를 강제한다.
    /// ScrollRect.VerticalNormalizedPosition: 1=맨 위, 0=맨 아래
    ///
    /// 주의
    /// - ContentSizeFitter/레이아웃 그룹이 있으면 프레임 끝에서 사이즈가 확정되는 경우가 있다.
    /// - Canvas.ForceUpdateCanvases()로 강제 갱신 후 값을 넣어야 안정적으로 동작한다.
    /// </summary>
    private void ScrollToTop()
    {
        if (scrollRect == null) return;

        // 레이아웃 계산 강제
        Canvas.ForceUpdateCanvases();

        // 맨 위로 이동
        scrollRect.verticalNormalizedPosition = 1f;

        // 한 번 더 강제 갱신(안정화)
        Canvas.ForceUpdateCanvases();
    }
}
