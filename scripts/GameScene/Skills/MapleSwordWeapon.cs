using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class MapleSwordWeapon : MonoBehaviour, IWeaponRuntime
{
    [Header("References")]
    [SerializeField] private GameObject swordVisual;
    [SerializeField] private DamageDealer damageDealer;
    [SerializeField] private AfterImage1 afterImage;
    [SerializeField] private Transform firePoint;
    [SerializeField] private TargetScanner2D scanner;

    [Header("Projectile")]
    [SerializeField] private int projectileIndex = 0;
    [SerializeField] private float bulletSpeed = 16f;

    [Tooltip("한 번 휘두를 때 발사되는 검격 탄환 수")]
    [SerializeField] private int projectileCount = 3;

    [Tooltip("여러 발을 발사할 때 퍼지는 각도")]
    [SerializeField] private float projectileSpreadAngle = 12f;

    [Tooltip("검격 탄환 크기 배율. 기존보다 작게 보이도록 0.5 권장")]
    [SerializeField] private float projectileScaleMultiplier = 0.5f;

    [Tooltip("발사 수가 늘어난 만큼 개별 탄환 데미지를 나누어 밸런스를 유지합니다.")]
    [SerializeField] private bool divideDamageByProjectileCount = true;

    [Header("Swing")]
    [SerializeField] private float attackInterval = 3f;
    [SerializeField] private float swingDuration = 0.6f;
    [SerializeField] private float attackDistance = 0.75f;
    [SerializeField] private float swingAngle = 120f;
    [SerializeField] private float projectileFireRatio = 0.5f;

    [Header("Fallback Stats")]
    [SerializeField] private float fallbackBaseDamage = 12f;
    [SerializeField] private int fallbackPierce = 1;

    private Transform owner;
    private ItemData itemData;
    private PlayerStatSystem statSystem;

    private int level = 1;
    private float timer;
    private bool isSwinging;

    public void OnEquip(Transform ownerTransform, ItemData data)
    {
        owner = ownerTransform;
        itemData = data;
        level = 1;
        timer = 0f;
        isSwinging = false;

        if (scanner == null && owner != null)
            scanner = owner.GetComponent<TargetScanner2D>();

        if (statSystem == null && owner != null)
            statSystem = owner.GetComponent<PlayerStatSystem>();

        if (damageDealer == null && swordVisual != null)
            damageDealer = swordVisual.GetComponentInChildren<DamageDealer>();

        if (afterImage == null && swordVisual != null)
            afterImage = swordVisual.GetComponentInChildren<AfterImage1>();

        if (firePoint == null)
            firePoint = transform;

        if (damageDealer != null)
        {
            damageDealer.SetOwner(owner);
            damageDealer.SetDamage(GetDamageByLevel(level));
            damageDealer.SetHitCooldown(0.1f);
            damageDealer.SetDespawnOnHit(false);
        }

        if (swordVisual != null)
            swordVisual.SetActive(false);
    }

    public void OnUnequip()
    {
        StopAllCoroutines();

        if (swordVisual != null)
            swordVisual.SetActive(false);

        isSwinging = false;
    }

    public void SetLevel(int newLevel)
    {
        level = Mathf.Max(1, newLevel);

        if (itemData != null)
            level = itemData.ClampLevel(level);

        if (damageDealer != null)
            damageDealer.SetDamage(GetDamageByLevel(level));
    }

    private void Update()
    {
        GameManager gm = GameManager.Instance;

        if (gm != null && (!gm.isLive || gm.isPaused))
            return;

        if (owner == null)
            return;

        transform.position = owner.position;

        if (isSwinging)
            return;

        timer += Time.deltaTime;

        if (timer < GetAttackInterval())
            return;

        Transform target = GetTarget();

        if (target == null)
            return;

        timer = 0f;
        StartCoroutine(SwingRoutine(target));
    }

    private Transform GetTarget()
    {
        if (scanner != null && scanner.NearestTarget != null)
            return scanner.NearestTarget;

        return null;
    }

    private IEnumerator SwingRoutine(Transform target)
    {
        isSwinging = true;

        if (swordVisual == null)
        {
            isSwinging = false;
            yield break;
        }

        Vector3 dir = target.position - owner.position;
        dir.z = 0f;

        if (dir.sqrMagnitude < 0.0001f)
            dir = Vector3.right;

        dir.Normalize();

        swordVisual.SetActive(true);

        if (afterImage != null)
            afterImage.StartGhostEffect();

        float baseAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        float startAngle = baseAngle - swingAngle * 0.5f;
        float endAngle = baseAngle + swingAngle * 0.5f;

        bool projectileFired = false;
        float elapsed = 0f;

        while (elapsed < swingDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / swingDuration);
            float z = Mathf.Lerp(startAngle, endAngle, t);

            swordVisual.transform.position = owner.position + dir * attackDistance;
            swordVisual.transform.rotation = Quaternion.Euler(0f, 0f, z);

            if (!projectileFired && t >= projectileFireRatio)
            {
                projectileFired = true;
                FireProjectiles(dir);
            }

            yield return null;
        }

        if (afterImage != null)
            afterImage.StopGhostEffect();

        swordVisual.SetActive(false);
        isSwinging = false;
    }

    private void FireProjectiles(Vector3 dir)
    {
        int count = Mathf.Max(1, projectileCount);

        for (int i = 0; i < count; i++)
        {
            float angle = GetProjectileAngleOffset(i, count);
            Vector3 shotDir = RotateDirection(dir, angle);

            FireSingleProjectile(shotDir, count);
        }
    }

    private void FireSingleProjectile(Vector3 dir, int totalProjectileCount)
    {
        if (BulletPoolManager.Instance == null)
        {
            Debug.LogWarning("MapleSwordWeapon: BulletPoolManager가 없습니다.");
            return;
        }

        GameObject bulletObj = BulletPoolManager.Instance.GetBullet(projectileIndex);

        if (bulletObj == null)
            return;

        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;

        bulletObj.transform.position = spawnPos;
        bulletObj.transform.rotation = Quaternion.FromToRotation(Vector3.up, dir);
        bulletObj.transform.localScale = Vector3.one * Mathf.Max(0.05f, projectileScaleMultiplier);

        Bullet bullet = bulletObj.GetComponent<Bullet>();

        if (bullet != null)
        {
            float damage = GetDamageByLevel(level);

            if (divideDamageByProjectileCount)
                damage /= Mathf.Max(1, totalProjectileCount);

            bullet.Init(
                damage,
                GetPierceByLevel(level),
                dir,
                bulletSpeed
            );
        }
    }

    private float GetProjectileAngleOffset(int index, int count)
    {
        if (count <= 1)
            return 0f;

        float t = index / (float)(count - 1);
        return Mathf.Lerp(-projectileSpreadAngle * 0.5f, projectileSpreadAngle * 0.5f, t);
    }

    private Vector3 RotateDirection(Vector3 dir, float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);

        Vector3 result = new Vector3(
            dir.x * cos - dir.y * sin,
            dir.x * sin + dir.y * cos,
            0f
        );

        if (result.sqrMagnitude < 0.0001f)
            result = Vector3.right;

        return result.normalized;
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

    private float GetAttackInterval()
    {
        float mul = 1f;

        if (statSystem != null)
            mul = Mathf.Max(0.01f, statSystem.GetAttackSpeedMultiplier());

        return Mathf.Max(0.1f, attackInterval / mul);
    }
}