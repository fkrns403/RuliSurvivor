using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 근접무기 / 회전무기 / 지속형 히트박스용 데미지 처리기
/// 
/// 특징:
/// - IDamageable 대상에게 데미지 적용
/// - 소유자 자신과 소유자의 자식 오브젝트는 공격하지 않음
/// - hitCooldown으로 동일 대상 재타격 간격 제어 가능
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class DamageDealer : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private float damage = 5f;

    [Header("Target Filter")]
    [SerializeField] private LayerMask targetMask = ~0;

    [Header("Hit Rule")]
    [Tooltip("0이면 한 번 닿았을 때 1회만, 0보다 크면 Stay에서 재타격 가능")]
    [SerializeField] private float hitCooldown = 0f;

    [Tooltip("맞히자마자 자신을 끌지 여부. 장착 무기는 false 권장")]
    [SerializeField] private bool despawnOnHit = false;

    [Header("Owner")]
    [SerializeField] private Transform ownerRoot;

    private readonly Dictionary<int, float> lastHitTimes = new Dictionary<int, float>();

    public void SetDamage(float value)
    {
        damage = Mathf.Max(0.01f, value);
    }

    public void SetHitCooldown(float value)
    {
        hitCooldown = Mathf.Max(0f, value);
    }

    public void SetDespawnOnHit(bool value)
    {
        despawnOnHit = value;
    }

    public void SetOwner(Transform owner)
    {
        ownerRoot = owner;
    }

    private void OnEnable()
    {
        lastHitTimes.Clear();
    }

    private void OnDisable()
    {
        lastHitTimes.Clear();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryHit(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (hitCooldown > 0f)
            TryHit(other);
    }

    private void TryHit(Collider2D other)
    {
        if (other == null)
            return;

        if (((1 << other.gameObject.layer) & targetMask.value) == 0)
            return;

        if (IsOwnerOrOwnerChild(other.transform))
            return;

        IDamageable dmg = other.GetComponent<IDamageable>();

        if (dmg == null)
            dmg = other.GetComponentInParent<IDamageable>();

        if (dmg == null)
            return;

        Component dmgComponent = dmg as Component;

        if (dmgComponent != null)
        {
            if (IsOwnerOrOwnerChild(dmgComponent.transform))
                return;
        }

        if (dmg.IsInvincible)
            return;

        int id = GetDamageableId(dmg, other);

        if (hitCooldown > 0f)
        {
            if (lastHitTimes.TryGetValue(id, out float last))
            {
                if (Time.time - last < hitCooldown)
                    return;
            }

            lastHitTimes[id] = Time.time;
        }
        else
        {
            if (lastHitTimes.ContainsKey(id))
                return;

            lastHitTimes[id] = Time.time;
        }

        dmg.TakeDamage(damage);

        if (despawnOnHit)
            gameObject.SetActive(false);
    }

    private bool IsOwnerOrOwnerChild(Transform target)
    {
        if (ownerRoot == null || target == null)
            return false;

        if (target == ownerRoot)
            return true;

        if (target.IsChildOf(ownerRoot))
            return true;

        if (ownerRoot.IsChildOf(target))
            return true;

        return false;
    }

    private int GetDamageableId(IDamageable dmg, Collider2D fallbackCollider)
    {
        Component dmgComponent = dmg as Component;

        if (dmgComponent != null)
            return dmgComponent.GetInstanceID();

        return fallbackCollider.GetInstanceID();
    }
}