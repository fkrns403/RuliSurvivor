using UnityEngine;

/// <summary>
/// 장착형 발사무기 공용 스크립트
/// 
/// 이번 수정 핵심:
/// - PlayerStatSystem의 공격속도 배율을 실제 발사 간격에 반영
/// </summary>
public class EquippedShooterWeapon : MonoBehaviour, IWeaponRuntime
{
    [Header("References")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private TargetScanner2D scanner;

    [Header("Projectile")]
    [SerializeField] private int projectileIndex = 0;

    [Header("Fallback Stats")]
    [SerializeField] private float fallbackBaseDamage = 10f;
    [SerializeField] private int fallbackBasePierce = 1;
    [SerializeField] private float baseAttackInterval = 1.2f;
    [SerializeField] private float bulletSpeed = 15f;

    [Header("Aim")]
    [SerializeField] private bool rotateToTarget = true;
    [SerializeField] private float weaponDistanceFromOwner = 0.5f;

    private Transform owner;
    private ItemData itemData;
    private int level = 1;
    private float timer;

    private PlayerStatSystem playerStatSystem;

    public void OnEquip(Transform ownerTransform, ItemData data)
    {
        owner = ownerTransform;
        itemData = data;
        timer = 0f;
        level = 1;

        if (firePoint == null)
            firePoint = transform;

        if (scanner == null && owner != null)
            scanner = owner.GetComponent<TargetScanner2D>();

        if (owner != null)
            playerStatSystem = owner.GetComponent<PlayerStatSystem>();
    }

    public void OnUnequip()
    {
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

        timer += Time.deltaTime;
        if (timer < GetAttackIntervalByLevel(level))
            return;

        if (target == null)
            return;

        timer = 0f;
        Fire(target);
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
            Debug.LogWarning("EquippedShooterWeapon: BulletPoolManager is missing.");
            return;
        }

        GameObject bulletObj = BulletPoolManager.Instance.GetBullet(projectileIndex);
        if (bulletObj == null)
            return;

        Vector3 spawnPos = (firePoint != null) ? firePoint.position : transform.position;
        Vector3 dir = target.position - spawnPos;
        dir.z = 0f;

        if (dir.sqrMagnitude < 0.0001f)
            dir = Vector3.right;

        dir.Normalize();

        bulletObj.transform.position = spawnPos;
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
        else
        {
            Debug.LogWarning($"EquippedShooterWeapon: projectile index {projectileIndex} has no Bullet component.");
        }
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

        return fallbackBasePierce + Mathf.Max(0, currentLevel - 1);
    }

    /// <summary>
    /// 공격속도 배율 반영
    /// - 예: 배율이 2.0이면 실제 발사 간격은 절반이 된다.
    /// </summary>
    private float GetAttackIntervalByLevel(int currentLevel)
    {
        float interval = baseAttackInterval - (currentLevel - 1) * 0.08f;
        interval = Mathf.Clamp(interval, 0.2f, 10f);

        float attackSpeedMul = 1f;
        if (playerStatSystem != null)
            attackSpeedMul = Mathf.Max(0.01f, playerStatSystem.GetAttackSpeedMultiplier());

        return interval / attackSpeedMul;
    }
}