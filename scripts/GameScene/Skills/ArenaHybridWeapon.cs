using System.Collections;
using UnityEngine;

/// <summary>
/// Arena 전용 하이브리드 무기.
/// 
/// 역할:
/// - 일정 주기마다 적 방향으로 부채꼴 탄환을 발사한다.
/// - 가까운 적이 있으면 별도의 근접 베기 오브젝트를 잠깐 활성화해 공격한다.
/// - 구버전 Arenweapon의 "원거리 탄환 + 근접 베기" 구조를 현버전 WeaponSystem에 맞춘 형태다.
/// 
/// 사용 조건:
/// - 플레이어에는 TargetScanner2D가 있어야 한다.
/// - 씬에는 BulletPoolManager가 있어야 한다.
/// - meleeVisual에는 Collider2D + DamageDealer가 있어야 한다.
/// </summary>
[DisallowMultipleComponent]
public class ArenaHybridWeapon : MonoBehaviour, IWeaponRuntime
{
    [Header("References")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private TargetScanner2D scanner;
    [SerializeField] private GameObject meleeVisual;
    [SerializeField] private DamageDealer meleeDamageDealer;
    [SerializeField] private AfterImage1 afterImage;

    [Header("Projectile")]
    [SerializeField] private int projectileIndex = 0;
    [SerializeField] private float bulletSpeed = 15f;

    [Header("Ranged Attack")]
    [SerializeField] private float rangedInterval = 5f;
    [SerializeField] private int baseBulletCount = 3;
    [SerializeField] private int maxBulletCount = 7;
    [SerializeField] private float spreadAngle = 30f;

    [Header("Melee Attack")]
    [SerializeField] private float meleeInterval = 3f;
    [SerializeField] private float meleeRange = 5f;
    [SerializeField] private float meleeDistanceFromOwner = 0.65f;
    [SerializeField] private float meleeSwingDuration = 0.22f;
    [SerializeField] private float meleeSwingAngle = 120f;

    [Header("Fallback Stats")]
    [SerializeField] private float fallbackBaseDamage = 10f;
    [SerializeField] private int fallbackPierce = 1;

    private Transform owner;
    private ItemData itemData;
    private PlayerStatSystem statSystem;

    private int level = 1;
    private float rangedTimer;
    private float meleeTimer;
    private bool isMeleeSwinging;

    public void OnEquip(Transform ownerTransform, ItemData data)
    {
        owner = ownerTransform;
        itemData = data;
        level = 1;
        rangedTimer = 0f;
        meleeTimer = 0f;
        isMeleeSwinging = false;

        if (firePoint == null)
            firePoint = transform;

        if (scanner == null && owner != null)
            scanner = owner.GetComponent<TargetScanner2D>();

        if (owner != null)
            statSystem = owner.GetComponent<PlayerStatSystem>();

        if (meleeDamageDealer == null && meleeVisual != null)
            meleeDamageDealer = meleeVisual.GetComponentInChildren<DamageDealer>();

        if (afterImage == null && meleeVisual != null)
            afterImage = meleeVisual.GetComponentInChildren<AfterImage1>();

        if (meleeDamageDealer != null)
        {
            meleeDamageDealer.SetOwner(owner);
            meleeDamageDealer.SetDamage(GetDamageByLevel(level));
            meleeDamageDealer.SetHitCooldown(0.05f);
            meleeDamageDealer.SetDespawnOnHit(false);
        }

        if (meleeVisual != null)
            meleeVisual.SetActive(false);
    }

    public void OnUnequip()
    {
        StopAllCoroutines();

        if (meleeVisual != null)
            meleeVisual.SetActive(false);
    }

    public void SetLevel(int newLevel)
    {
        level = Mathf.Max(1, newLevel);

        if (itemData != null)
            level = itemData.ClampLevel(level);

        if (meleeDamageDealer != null)
            meleeDamageDealer.SetDamage(GetDamageByLevel(level));
    }

    private void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm != null && (!gm.isLive || gm.isPaused))
            return;

        if (owner == null)
            return;

        transform.position = owner.position;

        Transform target = GetTarget();
        if (target == null)
            return;

        rangedTimer += Time.deltaTime;
        meleeTimer += Time.deltaTime;

        if (rangedTimer >= GetRangedInterval())
        {
            rangedTimer = 0f;
            FireFanBullets(target);
        }

        if (!isMeleeSwinging && meleeTimer >= GetMeleeInterval())
        {
            float dist = Vector3.Distance(owner.position, target.position);

            if (dist <= meleeRange)
            {
                meleeTimer = 0f;
                StartCoroutine(MeleeSwingRoutine(target));
            }
        }
    }

    private Transform GetTarget()
    {
        if (scanner != null && scanner.NearestTarget != null)
            return scanner.NearestTarget;

        return null;
    }

    private void FireFanBullets(Transform target)
    {
        if (BulletPoolManager.Instance == null)
        {
            Debug.LogWarning("ArenaHybridWeapon: BulletPoolManager가 없습니다.");
            return;
        }

        Vector3 origin = firePoint != null ? firePoint.position : transform.position;

        Vector3 baseDir = target.position - origin;
        baseDir.z = 0f;

        if (baseDir.sqrMagnitude < 0.0001f)
            baseDir = Vector3.right;

        baseDir.Normalize();

        int bulletCount = GetBulletCountByLevel(level);
        float startAngle = -spreadAngle * (bulletCount - 1) * 0.5f;

        for (int i = 0; i < bulletCount; i++)
        {
            float angle = startAngle + spreadAngle * i;
            Vector3 dir = Quaternion.Euler(0f, 0f, angle) * baseDir;

            GameObject bulletObj = BulletPoolManager.Instance.GetBullet(projectileIndex);
            if (bulletObj == null)
                continue;

            bulletObj.transform.position = origin;
            bulletObj.transform.rotation = Quaternion.FromToRotation(Vector3.up, dir);

            Bullet bullet = bulletObj.GetComponent<Bullet>();
            if (bullet != null)
            {
                bullet.Init(
                    GetDamageByLevel(level),
                    GetPierceByLevel(level),
                    dir,
                    bulletSpeed
                );
            }
        }
    }

    private IEnumerator MeleeSwingRoutine(Transform target)
    {
        isMeleeSwinging = true;

        if (meleeVisual == null)
        {
            isMeleeSwinging = false;
            yield break;
        }

        Vector3 dir = target.position - owner.position;
        dir.z = 0f;

        if (dir.sqrMagnitude < 0.0001f)
            dir = Vector3.right;

        dir.Normalize();

        meleeVisual.SetActive(true);

        if (afterImage != null)
            afterImage.StartGhostEffect();

        float baseAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        float startAngle = baseAngle - meleeSwingAngle * 0.5f;
        float endAngle = baseAngle + meleeSwingAngle * 0.5f;

        float elapsed = 0f;

        while (elapsed < meleeSwingDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / meleeSwingDuration);
            float z = Mathf.Lerp(startAngle, endAngle, t);

            meleeVisual.transform.position = owner.position + dir * meleeDistanceFromOwner;
            meleeVisual.transform.rotation = Quaternion.Euler(0f, 0f, z);

            yield return null;
        }

        if (afterImage != null)
            afterImage.StopGhostEffect();

        meleeVisual.SetActive(false);
        isMeleeSwinging = false;
    }

    private float GetDamageByLevel(int currentLevel)
    {
        currentLevel = Mathf.Max(1, currentLevel);

        if (itemData != null)
            return Mathf.Max(0.01f, itemData.GetDamageAtLevel(currentLevel));

        return fallbackBaseDamage + (currentLevel - 1) * 3f;
    }

    private int GetPierceByLevel(int currentLevel)
    {
        currentLevel = Mathf.Max(1, currentLevel);

        if (itemData != null)
            return Mathf.Max(0, itemData.GetCountAtLevel(currentLevel));

        return fallbackPierce;
    }

    private int GetBulletCountByLevel(int currentLevel)
    {
        int count = baseBulletCount + (currentLevel / 2) * 2;
        return Mathf.Clamp(count, baseBulletCount, maxBulletCount);
    }

    private float GetRangedInterval()
    {
        float mul = 1f;

        if (statSystem != null)
            mul = Mathf.Max(0.01f, statSystem.GetAttackSpeedMultiplier());

        return Mathf.Max(0.1f, rangedInterval / mul);
    }

    private float GetMeleeInterval()
    {
        float mul = 1f;

        if (statSystem != null)
            mul = Mathf.Max(0.01f, statSystem.GetAttackSpeedMultiplier());

        return Mathf.Max(0.1f, meleeInterval / mul);
    }
}