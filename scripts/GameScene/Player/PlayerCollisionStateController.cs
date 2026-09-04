using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어의 일시 상태 연출과 충돌 상태를 통합 관리하는 컴포넌트.
/// 
/// 담당 기능:
/// - 대쉬 중 충돌 제거
/// - 피격 무적 중 충돌 제거
/// - 무적 점멸
/// - 잔상 효과 시작/중지
/// 
/// 왜 필요한가:
/// - 구버전 player_nyarubi는 대쉬/무적/드링크 상태마다 직접
///   Collider.isTrigger, SpriteRenderer.enabled, AfterImage를 제어했다.
/// - 현버전은 PlayerDash, PlayerHealth, 아이템 효과가 분리되어 있으므로
///   각 스크립트가 Collider를 서로 켰다 껐다 하면 상태가 꼬일 수 있다.
/// - 예를 들어 대쉬가 끝나면서 Collider를 false로 돌렸는데,
///   아직 피격 무적이 남아 있으면 다시 피해를 받을 수 있다.
/// - 따라서 여러 상태 요청을 이 컴포넌트가 모아서 최종 상태를 결정한다.
/// </summary>
[DisallowMultipleComponent]
public class PlayerCollisionStateController : MonoBehaviour
{
    private class StateInfo
    {
        public float endTime;
        public bool useTrigger;
        public bool useBlink;
        public bool useAfterImage;
    }

    [Header("References")]
    [Tooltip("플레이어 본체 SpriteRenderer")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Tooltip("플레이어 본체 Collider만 넣는다. RepositionArea, MagnetArea 같은 자식 Trigger는 넣지 않는다.")]
    [SerializeField] private Collider2D[] bodyColliders;

    [Tooltip("대쉬/무적 잔상 효과")]
    [SerializeField] private AfterImage1 afterImage;

    [Header("Blink")]
    [SerializeField] private float blinkInterval = 0.1f;

    private readonly Dictionary<string, StateInfo> states = new Dictionary<string, StateInfo>();
    private readonly List<string> removeBuffer = new List<string>();

    private bool[] originalTriggerStates;
    private bool originalSpriteEnabled = true;

    private float blinkTimer;
    private bool blinkVisible = true;
    private bool afterImageRunning;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (afterImage == null)
            afterImage = GetComponent<AfterImage1>();

        if (bodyColliders == null || bodyColliders.Length == 0)
            bodyColliders = GetComponents<Collider2D>();

        originalTriggerStates = new bool[bodyColliders.Length];

        for (int i = 0; i < bodyColliders.Length; i++)
        {
            if (bodyColliders[i] != null)
                originalTriggerStates[i] = bodyColliders[i].isTrigger;
        }

        if (spriteRenderer != null)
            originalSpriteEnabled = spriteRenderer.enabled;
    }

    private void OnDisable()
    {
        ClearAllStates();
    }

    private void Update()
    {
        RemoveExpiredStates();
        ApplyCurrentState();
    }

    /// <summary>
    /// 외부 상태를 등록한다.
    /// 
    /// key:
    /// - "Dash"
    /// - "HitInvincible"
    /// - "ReviveInvincible"
    /// - "ItemInvincible"
    /// 처럼 상태별로 고유 문자열을 사용한다.
    /// 
    /// duration:
    /// - 이 상태가 유지될 시간.
    /// 
    /// useTrigger:
    /// - true면 플레이어 본체 Collider를 Trigger로 만들어 접촉 충돌을 끊는다.
    /// 
    /// useBlink:
    /// - true면 SpriteRenderer를 점멸시킨다.
    /// 
    /// useAfterImage:
    /// - true면 AfterImage1 잔상을 켠다.
    /// </summary>
    public void BeginState(
        string key,
        float duration,
        bool useTrigger,
        bool useBlink,
        bool useAfterImage)
    {
        if (string.IsNullOrEmpty(key))
            return;

        float safeDuration = Mathf.Max(0.01f, duration);

        states[key] = new StateInfo
        {
            endTime = Time.time + safeDuration,
            useTrigger = useTrigger,
            useBlink = useBlink,
            useAfterImage = useAfterImage
        };

        ApplyCurrentState();
    }

    public void EndState(string key)
    {
        if (string.IsNullOrEmpty(key))
            return;

        if (states.ContainsKey(key))
            states.Remove(key);

        ApplyCurrentState();
    }

    public void ClearAllStates()
    {
        states.Clear();

        RestoreColliders();

        if (spriteRenderer != null)
            spriteRenderer.enabled = originalSpriteEnabled;

        StopAfterImageIfNeeded();

        blinkTimer = 0f;
        blinkVisible = true;
    }

    private void RemoveExpiredStates()
    {
        removeBuffer.Clear();

        foreach (KeyValuePair<string, StateInfo> pair in states)
        {
            if (Time.time >= pair.Value.endTime)
                removeBuffer.Add(pair.Key);
        }

        for (int i = 0; i < removeBuffer.Count; i++)
            states.Remove(removeBuffer[i]);
    }

    private void ApplyCurrentState()
    {
        bool needTrigger = false;
        bool needBlink = false;
        bool needAfterImage = false;

        foreach (KeyValuePair<string, StateInfo> pair in states)
        {
            StateInfo state = pair.Value;

            if (state.useTrigger)
                needTrigger = true;

            if (state.useBlink)
                needBlink = true;

            if (state.useAfterImage)
                needAfterImage = true;
        }

        ApplyColliderTrigger(needTrigger);
        ApplyBlink(needBlink);
        ApplyAfterImage(needAfterImage);
    }

    private void ApplyColliderTrigger(bool trigger)
    {
        if (bodyColliders == null)
            return;

        for (int i = 0; i < bodyColliders.Length; i++)
        {
            Collider2D col = bodyColliders[i];

            if (col == null)
                continue;

            if (trigger)
                col.isTrigger = true;
            else if (originalTriggerStates != null && i < originalTriggerStates.Length)
                col.isTrigger = originalTriggerStates[i];
        }
    }

    private void RestoreColliders()
    {
        ApplyColliderTrigger(false);
    }

    private void ApplyBlink(bool blink)
    {
        if (spriteRenderer == null)
            return;

        if (!blink)
        {
            spriteRenderer.enabled = originalSpriteEnabled;
            blinkTimer = 0f;
            blinkVisible = true;
            return;
        }

        blinkTimer += Time.deltaTime;

        if (blinkTimer < blinkInterval)
            return;

        blinkTimer = 0f;
        blinkVisible = !blinkVisible;
        spriteRenderer.enabled = blinkVisible;
    }

    private void ApplyAfterImage(bool enable)
    {
        if (enable)
            StartAfterImageIfNeeded();
        else
            StopAfterImageIfNeeded();
    }

    private void StartAfterImageIfNeeded()
    {
        if (afterImage == null)
            return;

        if (afterImageRunning)
            return;

        afterImage.StartGhostEffect();
        afterImageRunning = true;
    }

    private void StopAfterImageIfNeeded()
    {
        if (afterImage == null)
            return;

        if (!afterImageRunning)
            return;

        afterImage.StopGhostEffect();
        afterImageRunning = false;
    }
}