using UnityEngine;

/// <summary>
/// AudioManager (Singleton)
///
/// 역할
/// 1) BGM(1채널) 재생/정지 및 볼륨 적용
/// 2) SFX(멀티 채널) 재생 및 볼륨 적용
/// 3) master/bgm/sfx 3단 볼륨을 지원(SettingsManager에서 호출)
/// 4) DontDestroyOnLoad로 씬 전환에도 유지
///
/// 호환성(중요)
/// - 프로젝트의 기존 스크립트들이 AudioManager.instance(소문자)를 호출하는 경우가 많다.
/// - AudioManager가 Instance(대문자)만 제공하면 컴파일 에러가 발생한다.
/// - 따라서 Instance(대문자)와 instance(소문자)를 동시에 제공한다.
///
/// BGM 교체(중요)
/// - BgmController가 bgmClip 변수만 바꾸면 실제 AudioSource(bgmplayer.clip)가 바뀌지 않는다.
/// - 그래서 SetBgmClip(clip)로 bgmplayer.clip까지 같이 바꾸도록 만든다.
/// </summary>
public class AudioManager : MonoBehaviour
{
    // 신규 스타일 싱글톤
    public static AudioManager Instance { get; private set; }

    // 구버전 호환 싱글톤
    public static AudioManager instance;

    [Header("BGM")]
    [Tooltip("배경음 클립. PlayBGM(true)로 재생한다.")]
    public AudioClip bgmClip;

    [Range(0f, 1f)]
    [Tooltip("BGM 볼륨(0~1). masterVolume와 곱해져 최종 BGM 볼륨이 된다.")]
    public float bgmVolume = 1f;

    private AudioSource bgmplayer;
    private AudioHighPassFilter bgmEffect;

    [Header("SFX")]
    [Tooltip("효과음 클립 배열. enum Sfx의 정수값을 인덱스로 사용한다.")]
    public AudioClip[] sfxClips;

    [Range(0f, 1f)]
    [Tooltip("SFX 볼륨(0~1). masterVolume와 곱해져 최종 SFX 볼륨이 된다.")]
    public float sfxVolume = 1f;

    [Tooltip("동시에 재생 가능한 SFX 채널 수")]
    public int channels = 8;

    private AudioSource[] sfxplayers;
    private int channelIndex;

    [Header("Master")]
    [Range(0f, 1f)]
    [Tooltip("마스터 볼륨(0~1). BGM/SFX에 공통으로 곱해지는 값.")]
    public float masterVolume = 1f;

    /// <summary>
    /// SFX 인덱스 enum
    /// - enum 값이 sfxClips 배열 인덱스로 사용된다는 전제이다.
    /// - 중간 값을 건너뛴 경우(LevelUp=3, Range=7) 그 사이 인덱스도 배열에 채워져 있어야 안전하다.
    /// </summary>
    public enum Sfx
    {
        Dead = 0,
        Hit = 1,
        LevelUp = 3,
        Lose = 4,
        Melee = 5,
        Range = 7,
        Select = 8,
        Win = 9
    }

    private void Awake()
    {
        // 싱글톤 중복 생성 방지
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        instance = this;

        DontDestroyOnLoad(gameObject);

        Init();
        ApplyVolumes();
    }

    /// <summary>
    /// 오디오 재생용 오브젝트/컴포넌트 초기화
    /// </summary>
    private void Init()
    {
        // BGM Player 생성
        GameObject bgmObject = new GameObject("BGMPlayer");
        bgmObject.transform.SetParent(transform);

        bgmplayer = bgmObject.AddComponent<AudioSource>();
        bgmplayer.playOnAwake = false;
        bgmplayer.loop = true;
        bgmplayer.clip = bgmClip;

        // 카메라에 HighPassFilter가 있으면 참조(없으면 null 유지)
        // 씬 전환 시 Camera.main이 바뀌면 필터 참조가 끊길 수 있다.
        var cam = Camera.main;
        if (cam != null)
            bgmEffect = cam.GetComponent<AudioHighPassFilter>();

        // SFX Player 멀티 채널 생성
        GameObject sfxObject = new GameObject("SFXPlayer");
        sfxObject.transform.SetParent(transform);

        int count = Mathf.Max(1, channels);
        sfxplayers = new AudioSource[count];

        for (int i = 0; i < sfxplayers.Length; i++)
        {
            sfxplayers[i] = sfxObject.AddComponent<AudioSource>();
            sfxplayers[i].playOnAwake = false;
            sfxplayers[i].bypassListenerEffects = true;
        }
    }

    /// <summary>
    /// SettingsManager에서 호출
    /// master/bgm/sfx 분리 적용(0~1)
    /// </summary>
    public void SetVolumes(float master01, float bgm01, float sfx01)
    {
        masterVolume = Mathf.Clamp01(master01);
        bgmVolume = Mathf.Clamp01(bgm01);
        sfxVolume = Mathf.Clamp01(sfx01);
        ApplyVolumes();
    }

    /// <summary>
    /// 기존 코드 호환: 마스터만 바꾸는 경우
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        ApplyVolumes();
    }

    /// <summary>
    /// BGM 클립을 교체한다.
    /// - bgmClip 변수만 바꾸는 것이 아니라, 실제 AudioSource(bgmplayer.clip)까지 반드시 바꾼다.
    /// - 필요 시 즉시 재생(play=true).
    /// </summary>
    public void SetBgmClip(AudioClip clip, bool play = true)
    {
        bgmClip = clip;

        if (bgmplayer != null)
            bgmplayer.clip = clip;

        if (play)
            PlayBGM(true);
    }

    /// <summary>
    /// 실제 AudioSource들의 볼륨을 반영
    /// 최종 BGM = master * bgm
    /// 최종 SFX = master * sfx
    /// </summary>
    private void ApplyVolumes()
    {
        if (bgmplayer != null)
            bgmplayer.volume = masterVolume * bgmVolume;

        if (sfxplayers != null)
        {
            float v = masterVolume * sfxVolume;
            for (int i = 0; i < sfxplayers.Length; i++)
            {
                if (sfxplayers[i] != null)
                    sfxplayers[i].volume = v;
            }
        }
    }

    /// <summary>
    /// BGM 재생/정지
    /// </summary>
    public void PlayBGM(bool isPlay)
    {
        if (bgmplayer == null) return;

        if (isPlay)
        {
            if (bgmplayer.clip != null && !bgmplayer.isPlaying)
                bgmplayer.Play();
        }
        else
        {
            if (bgmplayer.isPlaying)
                bgmplayer.Stop();
        }
    }

    /// <summary>
    /// BGM 필터 효과 On/Off (카메라에 필터가 있을 때만 동작)
    /// </summary>
    public void EffctBGM(bool isPlay)
    {
        if (bgmEffect == null) return;
        bgmEffect.enabled = isPlay;
    }

    /// <summary>
    /// SFX 재생
    /// - 비는 채널을 찾아 재생한다.
    /// - Hit/Melee는 같은 계열 2개 클립이 연속 배치된 경우(랜덤 0~1) 랜덤 재생한다.
    /// </summary>
    public void PlaySfx(Sfx sfx)
    {
        if (sfxClips == null || sfxClips.Length == 0) return;

        int baseIndex = (int)sfx;
        if (baseIndex < 0 || baseIndex >= sfxClips.Length) return;

        if (sfxplayers == null || sfxplayers.Length == 0) return;

        for (int i = 0; i < sfxplayers.Length; i++)
        {
            int loopIndex = (i + channelIndex) % sfxplayers.Length;
            if (sfxplayers[loopIndex].isPlaying)
                continue;

            int ranIndex = 0;
            if ((sfx == Sfx.Hit || sfx == Sfx.Melee) && sfxClips.Length >= (baseIndex + 2))
                ranIndex = Random.Range(0, 2);

            channelIndex = loopIndex;

            int clipIndex = baseIndex + ranIndex;
            if (clipIndex < 0 || clipIndex >= sfxClips.Length)
                clipIndex = baseIndex;

            AudioClip clip = sfxClips[clipIndex];
            if (clip == null) return;

            sfxplayers[loopIndex].clip = clip;
            sfxplayers[loopIndex].Play();
            break;
        }
    }
}
