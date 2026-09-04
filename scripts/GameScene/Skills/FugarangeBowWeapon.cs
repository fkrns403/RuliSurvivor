using UnityEngine;

/// <summary>
/// Fugarange 전용 열화 활 무기.
/// 
/// 역할:
/// - FireArrow 스킬의 열화판 무기처럼 사용할 수 있다.
/// - 쿨타임 중에는 활 스프라이트를 숨긴다.
/// - 발사 직전 chargeDuration 동안만 활을 표시하고 Idle -> Draw -> MaxDraw 순서로 당김 연출을 한다.
/// - 공격 타이밍이 되면 BulletPoolManager에서 탄환을 꺼내 가장 가까운 적에게 발사한다.
/// 
/// 중요한 설계:
/// - 이 무기는 CharacterType을 검사하지 않는다.
/// - 따라서 FireArrow 주인이 아니어도 LevelUpUI에서 이 ItemData를 얻으면 장착 가능하다.
/// - 캐릭터 고유 스킬은 PlayerAbilityController가 담당하고,
///   이 무기는 WeaponSystem을 통해 장착되는 일반 열화 무기다.
/// </summary>
[DisallowMultipleComponent]
public class FugarangeBowWeapon : MonoBehaviour, IWeaponRuntime
{
    [Header("References")]
    [Tooltip("활 당김 스프라이트 연출 컴포넌트")]
    [SerializeField] private BowChargeVisual bowVisual;

    [Tooltip("탄환이 생성될 위치")]
    [SerializeField] private Transform firePoint;

    [Tooltip("타겟 탐색기. 비워두면 owner에서 TargetScanner2D를 자동으로 찾는다.")]
    [SerializeField] private TargetScanner2D scanner;

    [Header("Projectile")]
    [Tooltip("BulletPoolManager에서 사용할 탄환 인덱스")]
    [SerializeField] private int projectileIndex = 0;

    [Header("Fallback Stats")]
    [Tooltip("ItemData가 없을 때 사용할 기본 피해량")]
    [SerializeField] private float fallbackBaseDamage = 8f;

    [Tooltip("ItemData가 없을 때 사용할 기본 관통 수")]
    [SerializeField] private int fallbackBasePierce = 1;

    [Tooltip("기본 공격 간격")]
    [SerializeField] private float baseAttackInterval = 1.5f;

    [Tooltip("발사 직전 활을 당기는 시간")]
    [SerializeField] private float chargeDuration = 0.35f;

    [Tooltip("탄환 이동 속도")]
    [SerializeField] private float bulletSpeed = 15f;

    [Header("Pose")]
    [Tooltip("타겟 방향으로 무기를 회전시킬지 여부")]
    [SerializeField] private bool rotateToTarget = true;

    [Tooltip("플레이어 중심에서 무기가 떨어질 거리")]
    [SerializeField] private float weaponDistanceFromOwner = 0.55f;

    private Transform owner;
    private ItemData itemData;
    private PlayerStatSystem statSystem;

    private int level = 1;
    private float timer;

    public void OnEquip(Transform ownerTransform, ItemData data)
    {
        owner = ownerTransform;
        itemData = data;
        level = 1;
        timer = 0f;

        if (bowVisual == null)
            bowVisual = GetComponentInChildren<BowChargeVisual>();

        if (firePoint == null)
            firePoint = transform;

        if (scanner == null && owner != null)
            scanner = owner.GetComponent<TargetScanner2D>();

        if (owner != null)
            statSystem = owner.GetComponent<PlayerStatSystem>();

        if (bowVisual != null)
            bowVisual.HideInstant();
    }

    public void OnUnequip()
    {
        if (bowVisual != null)
            bowVisual.HideInstant();
    }

    public void SetLevel(int newLevel)
    {
        level = Mathf.Max(1, newLevel);

        if (itemData != null)
            level = itemData.ClampLevel(level);
    }

    private void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm != null && (!gm.isLive || gm.isPaused))
            return;

        if (owner == null)
            return;

        Transform target = GetTarget();

        UpdateWeaponPose(target);

        float interval = GetAttackInterval();

        timer += Time.deltaTime;

        float remaining = interval - timer;

        if (remaining > chargeDuration)
        {
            if (bowVisual != null)
                bowVisual.HideInstant();

            return;
        }

        if (target != null && bowVisual != null)
        {
            float charge01 = 1f - Mathf.Clamp01(remaining / Mathf.Max(0.01f, chargeDuration));
            bowVisual.ShowByCharge(charge01);
        }

        if (timer < interval)
            return;

        timer = 0f;

        if (target != null)
            Fire(target);

        if (bowVisual != null)
            bowVisual.HideInstant();
    }

    private Transform GetTarget()
    {
        if (scanner != null && scanner.NearestTarget != null)
            return scanner.NearestTarget;

        return null;
    }

    private void UpdateWeaponPose(Transform target)
    {
        if (owner == null)
            return;

        if (target == null)
        {
            transform.position = owner.position;
            return;
        }

        Vector3 dir = target.position - owner.position;
        dir.z = 0f;

        if (dir.sqrMagnitude < 0.0001f)
            dir = Vector3.right;

        dir.Normalize();

        transform.position = owner.position + dir * weaponDistanceFromOwner;

        if (rotateToTarget)
        {
            float z = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0f, 0f, z);
        }
    }

    private void Fire(Transform target)
    {
        if (BulletPoolManager.Instance == null)
        {
            Debug.LogWarning("FugarangeBowWeapon: BulletPoolManager가 없습니다.");
            return;
        }

        GameObject bulletObj = BulletPoolManager.Instance.GetBullet(projectileIndex);
        if (bulletObj == null)
            return;

        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;

        Vector3 dir = target.position - spawnPos;
        dir.z = 0f;

        if (dir.sqrMagnitude < 0.0001f)
            dir = Vector3.right;

        dir.Normalize();

        bulletObj.transform.position = spawnPos;
        bulletObj.transform.rotation = Quaternion.FromToRotation(Vector3.up, dir);

        Bullet bullet = bulletObj.GetComponent<Bullet>();
        if (bullet == null)
        {
            Debug.LogWarning("FugarangeBowWeapon: 탄환 프리팹에 Bullet 컴포넌트가 없습니다.");
            return;
        }

        bullet.Init(
            GetDamageByLevel(level),
            GetPierceByLevel(level),
            dir,
            bulletSpeed
        );
    }

    private float GetDamageByLevel(int currentLevel)
    {
        currentLevel = Mathf.Max(1, currentLevel);

        if (itemData != null)
            return Mathf.Max(0.01f, itemData.GetDamageAtLevel(currentLevel));

        return fallbackBaseDamage + (currentLevel - 1) * 2f;
    }

    private int GetPierceByLevel(int currentLevel)
    {
        currentLevel = Mathf.Max(1, currentLevel);

        if (itemData != null)
            return Mathf.Max(0, itemData.GetCountAtLevel(currentLevel));

        return fallbackBasePierce;
    }

    private float GetAttackInterval()
    {
        float interval = baseAttackInterval - (level - 1) * 0.06f;
        interval = Mathf.Clamp(interval, 0.45f, 10f);

        float attackSpeedMul = 1f;
        if (statSystem != null)
            attackSpeedMul = Mathf.Max(0.01f, statSystem.GetAttackSpeedMultiplier());

        return interval / attackSpeedMul;
    }
}