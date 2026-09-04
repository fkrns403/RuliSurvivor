using UnityEngine;

/// <summary>
/// 활 당김 연출 전용 컴포넌트.
/// 
/// 역할:
/// - 활 스프라이트를 Idle / Draw / MaxDraw 3단계로 바꾼다.
/// - 스킬 또는 무기에서 공통으로 사용할 수 있다.
/// - 쿨타임 중에는 활을 숨기고, 실제 사용 중에만 표시한다.
/// 
/// 사용 예:
/// - FireArrow 고유 스킬 사용 직전 활 당김 연출
/// - Fugarange 열화 무기 자동 발사 직전 활 당김 연출
/// 
/// 주의:
/// - 이 컴포넌트는 공격 판정을 하지 않는다.
/// - 실제 발사, 데미지, 폭발은 PlayerAbilityController 또는 무기 스크립트가 담당한다.
/// </summary>
[DisallowMultipleComponent]
public class BowChargeVisual : MonoBehaviour
{
    [Header("Renderer")]
    [Tooltip("활 스프라이트를 표시할 SpriteRenderer")]
    [SerializeField] private SpriteRenderer bowRenderer;

    [Header("Sprites")]
    [Tooltip("대기 상태 활 스프라이트")]
    [SerializeField] private Sprite idleSprite;

    [Tooltip("활을 당기는 중간 상태 스프라이트")]
    [SerializeField] private Sprite drawSprite;

    [Tooltip("활을 최대로 당긴 상태 스프라이트")]
    [SerializeField] private Sprite maxDrawSprite;

    [Header("Visibility")]
    [Tooltip("시작할 때 활을 숨길지 여부")]
    [SerializeField] private bool hideOnAwake = true;

    private void Awake()
    {
        if (bowRenderer == null)
            bowRenderer = GetComponentInChildren<SpriteRenderer>();

        if (hideOnAwake)
            HideInstant();
    }

    /// <summary>
    /// 활을 즉시 숨긴다.
    /// 쿨타임 중, 발사 직후, 스킬이 취소되었을 때 호출한다.
    /// </summary>
    public void HideInstant()
    {
        if (bowRenderer == null)
            return;

        bowRenderer.enabled = false;
    }

    /// <summary>
    /// 활을 표시하고 현재 당김 비율에 맞는 스프라이트를 적용한다.
    /// 
    /// charge01:
    /// - 0.0에 가까우면 Idle
    /// - 중간이면 Draw
    /// - 1.0에 가까우면 MaxDraw
    /// </summary>
    public void ShowByCharge(float charge01)
    {
        if (bowRenderer == null)
            return;

        bowRenderer.enabled = true;

        charge01 = Mathf.Clamp01(charge01);

        if (charge01 < 0.35f)
        {
            SetSprite(idleSprite);
        }
        else if (charge01 < 0.8f)
        {
            SetSprite(drawSprite);
        }
        else
        {
            SetSprite(maxDrawSprite);
        }
    }

    private void SetSprite(Sprite sprite)
    {
        if (bowRenderer == null)
            return;

        if (sprite == null)
            return;

        bowRenderer.sprite = sprite;
    }
}