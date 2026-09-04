using UnityEngine;

/// <summary>
/// 산탄형 자동 조준 원거리 무기.
/// 
/// 사용 예:
/// - 산탄 지팡이
/// - 샷건형 마법탄
/// 
/// 역할:
/// - 가장 가까운 적을 향해 여러 발을 부채꼴로 발사한다.
/// - ItemData의 데미지와 개수를 반영한다.
/// 
/// ItemData 의미:
/// - baseDamage + damages = 공격력 배율
/// - baseCount + counts = 최종 산탄 수
/// - ranges = 현재는 탐지 사거리보다는 데이터 표시/후속 확장용
/// </summary>
[DisallowMultipleComponent]
public class ShotgunRangeWeapon : MonoBehaviour, IWeaponRuntime
{
    [Header("References")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private TargetScanner2D scanner;

    [Header("Projectile")]
    [SerializeField] private int projectileIndex = 0;
    [SerializeField] private float bulletSpeed = 15f;
    [SerializeField] private int bulletPierce = 0;

    [Header("Shotgun")]
    [SerializeField] private int fallbackPelletCount = 1;
    [SerializeField] private int maxPelletCount = 5;
    [SerializeField] private float spreadAngle = 25f;
    [SerializeField] private bool useRandomJitter = true;
    [SerializeField] private float randomJitterAngle = 3f;

    [Header("Attack")]
    [SerializeField] private float baseAttackInterval = 1.2f;
    [SerializeField] private float intervalDecreasePerLevel = 0.04f;
    [SerializeField] private float minAttackInterval = 0.45f;

    [Header("Fallback Stats")]
    [SerializeField] private float fallbackBaseDamage = 5f;

    [Header("Pose")]
    [SerializeField] private bool rotateToTarget = true;
    [SerializeField] private float weaponDistanceFromOwner = 0.5f;

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

        if (firePoint == null)
            firePoint = transform;

        if (owner != null)
        {
            statSystem = owner.GetComponent<PlayerStatSystem>();

            if (scanner == null)
                scanner = owner.GetComponent<TargetScanner2D>();
        }
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

        if (timer < GetAttackInterval())
            return;

        if (target == null)
            return;

        timer = 0f;
        FireShotgun(target);
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

    private void FireShotgun(Transform target)
    {
        if (BulletPoolManager.Instance == null)
        {
            Debug.LogWarning("ShotgunRangeWeapon: BulletPoolManager.Instance가 없습니다.");
            return;
        }

        Vector3 origin = firePoint != null ? firePoint.position : transform.position;

        Vector2 baseDir = target.position - origin;
        if (baseDir.sqrMagnitude < 0.0001f)
            baseDir = Vector2.right;

        baseDir.Normalize();

        int pelletCount = GetPelletCount();
        float damage = GetDamageByLevel(level);

        for (int i = 0; i < pelletCount; i++)
        {
            float angle = GetPelletAngle(i, pelletCount);

            if (useRandomJitter)
                angle += Random.Range(-randomJitterAngle, randomJitterAngle);

            Vector2 shotDir = Rotate(baseDir, angle);

            GameObject bulletObj = BulletPoolManager.Instance.GetBullet(projectileIndex);
            if (bulletObj == null)
                continue;

            bulletObj.transform.position = origin;
            bulletObj.transform.rotation = Quaternion.FromToRotation(Vector3.up, shotDir);

            Bullet bullet = bulletObj.GetComponent<Bullet>();
            if (bullet != null)
                bullet.Init(damage, bulletPierce, shotDir, bulletSpeed);
        }
    }

    private int GetPelletCount()
    {
        if (itemData != null)
            return Mathf.Clamp(itemData.GetCountAtLevel(level), 1, maxPelletCount);

        return Mathf.Clamp(fallbackPelletCount, 1, maxPelletCount);
    }

    private float GetDamageByLevel(int currentLevel)
    {
        if (itemData != null)
            return Mathf.Max(0.01f, itemData.GetDamageAtLevel(currentLevel));

        return fallbackBaseDamage + (currentLevel - 1);
    }

    private float GetAttackInterval()
    {
        float interval = baseAttackInterval - (level - 1) * intervalDecreasePerLevel;
        interval = Mathf.Max(minAttackInterval, interval);

        float attackSpeedMul = 1f;

        if (statSystem != null)
            attackSpeedMul = Mathf.Max(0.01f, statSystem.GetAttackSpeedMultiplier());

        return interval / attackSpeedMul;
    }

    private float GetPelletAngle(int index, int count)
    {
        if (count <= 1)
            return 0f;

        float t = index / (float)(count - 1);
        return Mathf.Lerp(-spreadAngle * 0.5f, spreadAngle * 0.5f, t);
    }

    private Vector2 Rotate(Vector2 dir, float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);

        return new Vector2(
            dir.x * cos - dir.y * sin,
            dir.x * sin + dir.y * cos
        ).normalized;
    }
}