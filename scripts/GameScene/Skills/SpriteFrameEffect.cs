using UnityEngine;

/// <summary>
/// 스프라이트 교체식 이펙트.
/// 
/// 역할:
/// - 여러 장의 Sprite를 순서대로 교체해서 간단한 프레임 애니메이션을 만든다.
/// - 하트 적중 이펙트, 낙뢰 이펙트처럼 짧게 재생되고 사라지는 효과에 사용한다.
/// - Animator Controller 없이 코드만으로 재생한다.
/// 
/// 사용 예:
/// - CharmImpactFX: 하트가 커지면서 매혹 범위를 보여주는 이펙트
/// - LightningStrikeFX: 낙뢰 스프라이트를 빠르게 교체하는 이펙트
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteFrameEffect : MonoBehaviour
{
    [Header("Renderer")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Frames")]
    [Tooltip("순서대로 재생할 스프라이트 배열")]
    [SerializeField] private Sprite[] frames;

    [Tooltip("초당 프레임 수")]
    [SerializeField] private float framesPerSecond = 18f;

    [Header("Scale Animation")]
    [Tooltip("재생 시작 크기")]
    [SerializeField] private Vector3 startScale = Vector3.one;

    [Tooltip("재생 종료 크기")]
    [SerializeField] private Vector3 endScale = Vector3.one;

    [Header("Life")]
    [Tooltip("재생이 끝나면 오브젝트를 제거할지 여부")]
    [SerializeField] private bool destroyOnFinish = true;

    private int frameIndex;
    private float timer;
    private float frameInterval;
    private float totalDuration;
    private float elapsed;
    private bool isPlaying;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        frameInterval = 1f / Mathf.Max(1f, framesPerSecond);

        int frameCount = frames != null ? frames.Length : 0;
        totalDuration = frameCount * frameInterval;
    }

    private void OnEnable()
    {
        Play();
    }

    /// <summary>
    /// 이펙트를 처음부터 재생한다.
    /// </summary>
    public void Play()
    {
        frameIndex = 0;
        timer = 0f;
        elapsed = 0f;
        isPlaying = true;

        transform.localScale = startScale;

        if (frames != null && frames.Length > 0 && spriteRenderer != null)
            spriteRenderer.sprite = frames[0];
    }

    private void Update()
    {
        if (!isPlaying)
            return;

        if (frames == null || frames.Length == 0)
        {
            Finish();
            return;
        }

        elapsed += Time.deltaTime;

        if (totalDuration > 0f)
        {
            float scaleT = Mathf.Clamp01(elapsed / totalDuration);
            transform.localScale = Vector3.Lerp(startScale, endScale, scaleT);
        }

        timer += Time.deltaTime;

        if (timer < frameInterval)
            return;

        timer = 0f;
        frameIndex++;

        if (frameIndex >= frames.Length)
        {
            Finish();
            return;
        }

        if (spriteRenderer != null)
            spriteRenderer.sprite = frames[frameIndex];
    }

    private void Finish()
    {
        isPlaying = false;

        if (destroyOnFinish)
            Destroy(gameObject);
    }
}