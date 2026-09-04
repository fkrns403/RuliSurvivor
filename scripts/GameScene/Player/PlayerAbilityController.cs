using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerAbilityController : MonoBehaviour
{
    [Header("Character")]
    [SerializeField] private CharacterType characterType = CharacterType.None;

    [Header("Common")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private LayerMask enemyMask;

    [Header("Auto Aim")]
    [SerializeField] private bool useAutoAimToEnemy = true;
    [SerializeField] private float autoAimRadius = 30f;
    [SerializeField] private bool useMouseAimFallback = false;
    [SerializeField] private Vector2 defaultAimDirection = Vector2.right;

    [Header("Runtime Reference Safety")]
    [SerializeField] private bool forceFirePointToOwnerIfNotChild = true;

    [Header("Prefabs")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private GameObject charmImpactFxPrefab;

    [Header("Projectile Rotation Offset")]
    [SerializeField] private float fireArrowProjectileRotationOffset = 0f;
    [SerializeField] private float swordSlashProjectileRotationOffset = 0f;
    [SerializeField] private float bigBulletProjectileRotationOffset = 0f;
    [SerializeField] private float charmProjectileRotationOffset = 0f;

    [Header("Cooldown")]
    [SerializeField] private float activeCooldown = 3f;

    [Header("UI")]
    [SerializeField] private SkillCooldownUI skillCooldownUI;

    [Header("Skill Icons")]
    [SerializeField] private Sprite fireArrowIcon;
    [SerializeField] private Sprite bigBulletIcon;
    [SerializeField] private Sprite swordSlashIcon;
    [SerializeField] private Sprite charmHeartIcon;
    [SerializeField] private Sprite nineLivesIcon;

    [Header("FireArrow Bow Charge")]
    [SerializeField] private BowChargeVisual fireArrowBowVisual;
    [SerializeField] private float fireArrowChargeDuration = 0.35f;
    [SerializeField] private float fireArrowExplosionRadius = 2.8f;
    [SerializeField] private float fireArrowExplosionDamage = 28f;
    [SerializeField] private float fireArrowSpeed = 12f;

    [Header("FireArrow Skill Visual Follow")]
    [SerializeField] private bool fireArrowVisualFollowPlayer = true;
    [SerializeField] private float fireArrowVisualDistance = 0.55f;
    [SerializeField] private float fireArrowVisualRotationOffset = -90f;

    [Header("Big Bullet")]
    [SerializeField] private int bigBulletCount = 10;
    [SerializeField] private float bigBulletSpreadAngle = 20f;
    [SerializeField] private float bigBulletSpeed = 16f;
    [SerializeField] private float bigBulletDamage = 6f;
    [SerializeField] private float bigBulletLifeTime = 1.5f;
    [SerializeField] private float bigBulletScale = 3f;

    [Header("Big Bullet Skill Visual")]
    [SerializeField] private GameObject bigBulletSkillVisual;
    [SerializeField] private float bigBulletVisualPrepareTime = 0.12f;
    [SerializeField] private float bigBulletVisualHoldTime = 0.25f;
    [SerializeField] private bool rotateBigBulletVisualToAim = true;
    [SerializeField] private float bigBulletVisualDistanceFromOwner = 0.65f;

    [Header("Sword Slash Projectile")]
    [SerializeField] private int swordSlashCount = 12;
    [SerializeField] private float swordSlashSpreadAngle = 12f;
    [SerializeField] private float swordSlashSpeed = 22f;
    [SerializeField] private float swordSlashDamage = 4f;
    [SerializeField] private float swordSlashLifeTime = 0.6f;

    [Header("Sword Slash Active Skill")]
    [SerializeField] private float swordSlashSkillDuration = 1.2f;
    [SerializeField] private float swordSlashProjectileInterval = 0.25f;
    [SerializeField] private float swordSlashSwingInterval = 0.18f;
    [SerializeField] private GameObject swordSlashSkillVisual;
    [SerializeField] private DamageDealer swordSlashSkillDamageDealer;
    [SerializeField] private AfterImage1 swordSlashSkillAfterImage;
    [SerializeField] private float swordSlashSwingDistance = 0.85f;
    [SerializeField] private float swordSlashSwingAngle = 140f;
    [SerializeField] private float swordSlashSingleSwingDuration = 0.16f;
    [SerializeField] private float swordSlashMeleeDamage = 6f;
    [SerializeField] private float swordSlashMeleeHitCooldown = 0.08f;

    [Header("Charm Heart")]
    [SerializeField] private float charmProjectileSpeed = 10f;
    [SerializeField] private float charmDirectDamage = 1f;
    [SerializeField] private float charmRadius = 6f;
    [SerializeField] private float charmAreaDamage = 0f;
    [SerializeField] private float charmDuration = 6f;
    [SerializeField] private float charmLifeTime = 2f;

    private float nextActiveTime;
    private bool isCastingActive;
    private PlayerLives playerLives;

    private void Awake()
    {
        ResolveRuntimeReferences();

        playerLives = GetComponent<PlayerLives>();

        if (fireArrowBowVisual != null)
            fireArrowBowVisual.HideInstant();

        if (bigBulletSkillVisual != null)
            bigBulletSkillVisual.SetActive(false);

        SetupSwordSlashVisual();
        RefreshSkillUI();
    }

    private void OnEnable()
    {
        ResolveRuntimeReferences();

        if (fireArrowBowVisual != null)
            fireArrowBowVisual.HideInstant();

        if (bigBulletSkillVisual != null)
            bigBulletSkillVisual.SetActive(false);
    }

    private void Start()
    {
        ApplyPassive();
        RefreshSkillUI();
    }

    private void ResolveRuntimeReferences()
    {
        if (firePoint == null)
            firePoint = transform;
        else if (forceFirePointToOwnerIfNotChild && !firePoint.IsChildOf(transform))
            firePoint = transform;

        if (defaultAimDirection.sqrMagnitude < 0.0001f)
            defaultAimDirection = Vector2.right;
    }

    public void SetCharacterType(CharacterType type)
    {
        characterType = type;
        ApplyPassive();
        RefreshSkillUI();
    }

    public void BindSkillCooldownUI(SkillCooldownUI ui)
    {
        skillCooldownUI = ui;

        if (playerLives == null)
            playerLives = GetComponent<PlayerLives>();

        if (playerLives != null)
            playerLives.BindUI(skillCooldownUI);

        RefreshSkillUI();
    }

    private void Update()
    {
        GameManager gm = GameManager.Instance;

        if (gm == null || !gm.isLive || gm.isPaused)
            return;

        if (Input.GetKeyDown(KeyCode.Q))
            TryUseActive();
    }

    private void ApplyPassive()
    {
        if (characterType != CharacterType.NineLives)
            return;

        if (playerLives == null)
            playerLives = gameObject.GetComponent<PlayerLives>() ?? gameObject.AddComponent<PlayerLives>();

        playerLives.SetLives(9);
    }

    private void RefreshSkillUI()
    {
        if (skillCooldownUI == null)
            return;

        switch (characterType)
        {
            case CharacterType.FireArrow:
                skillCooldownUI.SetIcon(fireArrowIcon);
                skillCooldownUI.HideStackCount();
                break;

            case CharacterType.BigBulletSpray:
                skillCooldownUI.SetIcon(bigBulletIcon);
                skillCooldownUI.HideStackCount();
                break;

            case CharacterType.SwordSlash:
                skillCooldownUI.SetIcon(swordSlashIcon);
                skillCooldownUI.HideStackCount();
                break;

            case CharacterType.CharmHeart:
                skillCooldownUI.SetIcon(charmHeartIcon);
                skillCooldownUI.HideStackCount();
                break;

            case CharacterType.NineLives:
                skillCooldownUI.SetIcon(nineLivesIcon);

                if (playerLives == null)
                    playerLives = GetComponent<PlayerLives>();

                if (playerLives != null)
                    playerLives.BindUI(skillCooldownUI);
                else
                    skillCooldownUI.SetStackCount(9);

                break;

            default:
                skillCooldownUI.HideStackCount();
                break;
        }
    }

    private void SetupSwordSlashVisual()
    {
        if (swordSlashSkillVisual == null)
            return;

        if (swordSlashSkillDamageDealer == null)
            swordSlashSkillDamageDealer = swordSlashSkillVisual.GetComponentInChildren<DamageDealer>(true);

        if (swordSlashSkillAfterImage == null)
            swordSlashSkillAfterImage = swordSlashSkillVisual.GetComponentInChildren<AfterImage1>(true);

        if (swordSlashSkillDamageDealer != null)
        {
            swordSlashSkillDamageDealer.SetOwner(transform);
            swordSlashSkillDamageDealer.SetDamage(swordSlashMeleeDamage);
            swordSlashSkillDamageDealer.SetHitCooldown(swordSlashMeleeHitCooldown);
            swordSlashSkillDamageDealer.SetDespawnOnHit(false);
        }

        swordSlashSkillVisual.SetActive(false);
    }

    private void TryUseActive()
    {
        if (characterType == CharacterType.NineLives)
            return;

        if (isCastingActive)
            return;

        if (Time.time < nextActiveTime)
            return;

        nextActiveTime = Time.time + Mathf.Max(0.1f, activeCooldown);

        if (skillCooldownUI != null)
            skillCooldownUI.StartCooldown(activeCooldown);

        Vector2 dir = GetAimDirection();

        switch (characterType)
        {
            case CharacterType.FireArrow:
                StartCoroutine(FireArrowRoutine(dir));
                break;

            case CharacterType.BigBulletSpray:
                StartCoroutine(BigBulletSprayRoutine(dir));
                break;

            case CharacterType.SwordSlash:
                StartCoroutine(SwordSlashSkillRoutine(dir));
                break;

            case CharacterType.CharmHeart:
                CharmHeartShot(dir);
                break;
        }
    }

    private Vector3 GetFirePosition()
    {
        ResolveRuntimeReferences();

        if (firePoint != null)
            return firePoint.position;

        return transform.position;
    }

    private Vector2 GetAimDirection()
    {
        Vector3 origin = GetFirePosition();

        if (useAutoAimToEnemy)
        {
            Transform nearest = FindNearestEnemy(origin);

            if (nearest != null)
            {
                Vector2 dirToEnemy = nearest.position - origin;

                if (dirToEnemy.sqrMagnitude > 0.0001f)
                    return dirToEnemy.normalized;
            }
        }

        if (useMouseAimFallback && Camera.main != null)
        {
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorld.z = 0f;

            Vector2 mouseDir = mouseWorld - origin;

            if (mouseDir.sqrMagnitude > 0.0001f)
                return mouseDir.normalized;
        }

        return defaultAimDirection.normalized;
    }

    private Transform FindNearestEnemy(Vector3 origin)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, autoAimRadius, enemyMask);

        Transform nearest = null;
        float nearestSqrDistance = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];

            if (hit == null)
                continue;

            if (!hit.gameObject.activeInHierarchy)
                continue;

            Enemy enemy = hit.GetComponent<Enemy>();

            if (enemy == null)
                enemy = hit.GetComponentInParent<Enemy>();

            BossHealth bossHealth = hit.GetComponent<BossHealth>();

            if (bossHealth == null)
                bossHealth = hit.GetComponentInParent<BossHealth>();

            EnemyHealth enemyHealth = hit.GetComponent<EnemyHealth>();

            if (enemyHealth == null)
                enemyHealth = hit.GetComponentInParent<EnemyHealth>();

            if (enemy == null && bossHealth == null && enemyHealth == null)
                continue;

            Transform target = hit.transform;

            if (enemy != null)
                target = enemy.transform;
            else if (bossHealth != null)
                target = bossHealth.transform;
            else if (enemyHealth != null)
                target = enemyHealth.transform;

            float sqrDistance = ((Vector2)target.position - (Vector2)origin).sqrMagnitude;

            if (sqrDistance < nearestSqrDistance)
            {
                nearestSqrDistance = sqrDistance;
                nearest = target;
            }
        }

        return nearest;
    }

    private IEnumerator FireArrowRoutine(Vector2 initialDir)
    {
        isCastingActive = true;

        float duration = Mathf.Max(0.01f, fireArrowChargeDuration);
        float elapsed = 0f;

        Vector2 dir = initialDir.sqrMagnitude < 0.0001f ? defaultAimDirection.normalized : initialDir;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            dir = GetAimDirection();

            UpdateFireArrowBowVisual(dir, Mathf.Clamp01(elapsed / duration));

            yield return null;
        }

        dir = GetAimDirection();
        FireArrow(dir);

        if (fireArrowBowVisual != null)
            fireArrowBowVisual.HideInstant();

        isCastingActive = false;
    }

    private void UpdateFireArrowBowVisual(Vector2 dir, float charge01)
    {
        if (fireArrowBowVisual == null)
            return;

        if (dir.sqrMagnitude < 0.0001f)
            dir = defaultAimDirection;

        dir.Normalize();

        if (fireArrowVisualFollowPlayer)
        {
            fireArrowBowVisual.transform.position =
                GetFirePosition() + (Vector3)(dir * fireArrowVisualDistance);
        }

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        fireArrowBowVisual.transform.rotation =
            Quaternion.Euler(0f, 0f, angle + fireArrowVisualRotationOffset);

        fireArrowBowVisual.ShowByCharge(charge01);
    }

    private void FireArrow(Vector2 dir)
    {
        if (projectilePrefab == null)
            return;

        GameObject p = Instantiate(projectilePrefab, GetFirePosition(), Quaternion.identity);

        ProjectileMover mover = p.GetComponent<ProjectileMover>();

        if (mover == null)
            mover = p.AddComponent<ProjectileMover>();

        mover.Launch(dir, fireArrowSpeed, fireArrowProjectileRotationOffset);

        OnHitExplode explode = p.GetComponent<OnHitExplode>();

        if (explode == null)
            explode = p.AddComponent<OnHitExplode>();

        explode.Init(explosionPrefab, fireArrowExplosionRadius, fireArrowExplosionDamage, enemyMask);

        ProjectileDamage dmg = p.GetComponent<ProjectileDamage>();

        if (dmg != null)
            dmg.SetOwner(transform);

        ProjectileLife life = p.GetComponent<ProjectileLife>();

        if (life == null)
            life = p.AddComponent<ProjectileLife>();

        life.SetLifetime(3f);
    }

    private IEnumerator BigBulletSprayRoutine(Vector2 initialDir)
    {
        isCastingActive = true;

        Vector2 dir = initialDir.sqrMagnitude < 0.0001f ? defaultAimDirection.normalized : initialDir;

        ShowBigBulletVisual(dir);

        float elapsed = 0f;
        float prepareTime = Mathf.Max(0f, bigBulletVisualPrepareTime);

        while (elapsed < prepareTime)
        {
            elapsed += Time.deltaTime;
            dir = GetAimDirection();
            ShowBigBulletVisual(dir);
            yield return null;
        }

        dir = GetAimDirection();
        BigBulletSpray(dir);

        elapsed = 0f;
        float holdTime = Mathf.Max(0f, bigBulletVisualHoldTime);

        while (elapsed < holdTime)
        {
            elapsed += Time.deltaTime;
            dir = GetAimDirection();
            ShowBigBulletVisual(dir);
            yield return null;
        }

        HideBigBulletVisual();

        isCastingActive = false;
    }

    private void ShowBigBulletVisual(Vector2 dir)
    {
        if (bigBulletSkillVisual == null)
            return;

        if (dir.sqrMagnitude < 0.0001f)
            dir = defaultAimDirection;

        dir.Normalize();

        bigBulletSkillVisual.SetActive(true);
        bigBulletSkillVisual.transform.position =
            transform.position + (Vector3)(dir * bigBulletVisualDistanceFromOwner);

        if (rotateBigBulletVisualToAim)
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            bigBulletSkillVisual.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    private void HideBigBulletVisual()
    {
        if (bigBulletSkillVisual != null)
            bigBulletSkillVisual.SetActive(false);
    }

    private void BigBulletSpray(Vector2 dir)
    {
        if (projectilePrefab == null)
            return;

        int count = Mathf.Max(1, bigBulletCount);

        for (int i = 0; i < count; i++)
        {
            float t = count == 1 ? 0.5f : i / (float)(count - 1);
            float angle = Mathf.Lerp(-bigBulletSpreadAngle * 0.5f, bigBulletSpreadAngle * 0.5f, t);
            Vector2 shotDir = Rotate(dir, angle);

            GameObject p = Instantiate(projectilePrefab, GetFirePosition(), Quaternion.identity);
            p.transform.localScale *= Mathf.Max(0.1f, bigBulletScale);

            ProjectileMover mover = p.GetComponent<ProjectileMover>();

            if (mover == null)
                mover = p.AddComponent<ProjectileMover>();

            mover.Launch(shotDir, bigBulletSpeed, bigBulletProjectileRotationOffset);

            ProjectileDamage dmg = p.GetComponent<ProjectileDamage>();

            if (dmg == null)
                dmg = p.AddComponent<ProjectileDamage>();

            dmg.SetDamage(bigBulletDamage);
            dmg.SetOwner(transform);

            ProjectileLife life = p.GetComponent<ProjectileLife>();

            if (life == null)
                life = p.AddComponent<ProjectileLife>();

            life.SetLifetime(bigBulletLifeTime);
        }
    }

    private IEnumerator SwordSlashSkillRoutine(Vector2 initialDir)
    {
        isCastingActive = true;

        SetupSwordSlashVisual();

        float duration = Mathf.Max(0.1f, swordSlashSkillDuration);
        float elapsed = 0f;
        float nextProjectileBurstTime = 0f;
        float nextSwingTime = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            Vector2 dir = GetAimDirection();

            if (dir.sqrMagnitude < 0.0001f)
                dir = initialDir;

            if (elapsed >= nextProjectileBurstTime)
            {
                SwordSlashSpray(dir);
                nextProjectileBurstTime = elapsed + Mathf.Max(0.03f, swordSlashProjectileInterval);
            }

            if (elapsed >= nextSwingTime)
            {
                StartCoroutine(SwordSlashMeleeSwingRoutine(dir));
                nextSwingTime = elapsed + Mathf.Max(0.03f, swordSlashSwingInterval);
            }

            yield return null;
        }

        if (swordSlashSkillAfterImage != null)
            swordSlashSkillAfterImage.StopGhostEffect();

        if (swordSlashSkillVisual != null)
            swordSlashSkillVisual.SetActive(false);

        isCastingActive = false;
    }

    private IEnumerator SwordSlashMeleeSwingRoutine(Vector2 dir)
    {
        if (swordSlashSkillVisual == null)
            yield break;

        if (dir.sqrMagnitude < 0.0001f)
            dir = defaultAimDirection;

        dir.Normalize();

        swordSlashSkillVisual.SetActive(true);

        if (swordSlashSkillDamageDealer != null)
        {
            swordSlashSkillDamageDealer.SetOwner(transform);
            swordSlashSkillDamageDealer.SetDamage(swordSlashMeleeDamage);
            swordSlashSkillDamageDealer.SetHitCooldown(swordSlashMeleeHitCooldown);
            swordSlashSkillDamageDealer.SetDespawnOnHit(false);
        }

        if (swordSlashSkillAfterImage != null)
            swordSlashSkillAfterImage.StartGhostEffect();

        float baseAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        float startAngle = baseAngle - swordSlashSwingAngle * 0.5f;
        float endAngle = baseAngle + swordSlashSwingAngle * 0.5f;

        float duration = Mathf.Max(0.03f, swordSlashSingleSwingDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            float z = Mathf.Lerp(startAngle, endAngle, t);

            swordSlashSkillVisual.transform.position =
                transform.position + (Vector3)(dir * swordSlashSwingDistance);

            swordSlashSkillVisual.transform.rotation =
                Quaternion.Euler(0f, 0f, z);

            yield return null;
        }
    }

    private void SwordSlashSpray(Vector2 dir)
    {
        if (projectilePrefab == null)
            return;

        int count = Mathf.Max(1, swordSlashCount);

        for (int i = 0; i < count; i++)
        {
            float t = count == 1 ? 0.5f : i / (float)(count - 1);
            float angle = Mathf.Lerp(-swordSlashSpreadAngle * 0.5f, swordSlashSpreadAngle * 0.5f, t);
            Vector2 shotDir = Rotate(dir, angle);

            GameObject p = Instantiate(projectilePrefab, GetFirePosition(), Quaternion.identity);

            ProjectileMover mover = p.GetComponent<ProjectileMover>();

            if (mover == null)
                mover = p.AddComponent<ProjectileMover>();

            mover.Launch(shotDir, swordSlashSpeed, swordSlashProjectileRotationOffset);

            ProjectileDamage dmg = p.GetComponent<ProjectileDamage>();

            if (dmg == null)
                dmg = p.AddComponent<ProjectileDamage>();

            dmg.SetDamage(swordSlashDamage);
            dmg.SetOwner(transform);

            ProjectileLife life = p.GetComponent<ProjectileLife>();

            if (life == null)
                life = p.AddComponent<ProjectileLife>();

            life.SetLifetime(swordSlashLifeTime);
        }
    }

    private void CharmHeartShot(Vector2 dir)
    {
        if (projectilePrefab == null)
            return;

        GameObject p = Instantiate(projectilePrefab, GetFirePosition(), Quaternion.identity);

        ProjectileMover mover = p.GetComponent<ProjectileMover>();

        if (mover == null)
            mover = p.AddComponent<ProjectileMover>();

        mover.Launch(dir, charmProjectileSpeed, charmProjectileRotationOffset);

        OnHitCharm charm = p.GetComponent<OnHitCharm>();

        if (charm == null)
            charm = p.AddComponent<OnHitCharm>();

        charm.Init(charmDirectDamage, charmRadius, charmAreaDamage, charmDuration, enemyMask, charmImpactFxPrefab);

        ProjectileDamage dmg = p.GetComponent<ProjectileDamage>();

        if (dmg != null)
            dmg.SetOwner(transform);

        ProjectileLife life = p.GetComponent<ProjectileLife>();

        if (life == null)
            life = p.AddComponent<ProjectileLife>();

        life.SetLifetime(charmLifeTime);
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