using UnityEngine;

/// <summary>
/// 경고 스프라이트(장판/레이저 예고선 등)를 깜빡이게 함
/// - 풀링 재사용 시에도 알파 상태가 항상 초기화되도록 구성
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class WarningFlasher : MonoBehaviour
{
    [Tooltip("알파가 오르내리는 속도")]
    [SerializeField] private float flashSpeed = 1f;

    [Tooltip("최대 알파(투명도)")]
    [Range(0f, 1f)]
    [SerializeField] private float maxAlpha = 0.7f;

    private SpriteRenderer sr;
    private float alpha;
    private bool fadingIn;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        if (sr == null)
            sr = GetComponent<SpriteRenderer>();

        alpha = 0f;
        fadingIn = true;

        if (sr != null)
        {
            Color color = sr.color;
            color.a = 0f;
            sr.color = color;
        }
    }

    private void Update()
    {
        if (sr == null) return;

        float delta = flashSpeed * Time.deltaTime;
        alpha += fadingIn ? delta : -delta;

        if (alpha >= maxAlpha)
        {
            alpha = maxAlpha;
            fadingIn = false;
        }
        else if (alpha <= 0f)
        {
            alpha = 0f;
            fadingIn = true;
        }

        Color color = sr.color;
        color.a = alpha;
        sr.color = color;
    }
}