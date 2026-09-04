using UnityEngine;

/// <summary>
/// 고유 스킬용 일반 데미지 투사체.
/// 
/// 역할:
/// - 닿은 대상에서 IDamageable을 찾아 데미지를 준다.
/// - 적 Enemy가 있으면 넉백/피격 반응을 호출한다.
/// - 발사자 자신과 발사자의 자식 오브젝트는 공격하지 않는다.
/// 
/// 수정 핵심:
/// - ownerRoot 추가
/// - 검격 탄환이 플레이어 본인을 때리는 문제 방지
/// </summary>
public class ProjectileDamage : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private float damage = 5f;

    [Header("Hit Rule")]
    [SerializeField] private bool destroyOnHit = true;

    [Header("Owner")]
    [SerializeField] private Transform ownerRoot;

    public void SetDamage(float value)
    {
        damage = Mathf.Max(0f, value);
    }

    public void SetDestroyOnHit(bool value)
    {
        destroyOnHit = value;
    }

    public void SetOwner(Transform owner)
    {
        ownerRoot = owner;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryHit(other);
    }

    private void TryHit(Collider2D other)
    {
        if (other == null)
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

        if (!dmg.IsInvincible)
            dmg.TakeDamage(damage);

        Enemy enemy = other.GetComponentInParent<Enemy>();

        if (enemy != null)
            enemy.ApplyKnockBack();

        if (destroyOnHit)
            Destroy(gameObject);
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
}