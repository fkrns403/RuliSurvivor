using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SpecialAttack : MonoBehaviour
{
    [Header("Patterns")]
    [SerializeField] private List<SpecialAttackPattern> patterns = new List<SpecialAttackPattern>();

    [Header("Default Warning Delay")]
    [SerializeField] private float warningDelay = 2.5f;

    [Header("Fire Ring Warning Scale")]
    [SerializeField] private float fireRingWarningStartScale = 1f;
    [SerializeField] private float fireRingWarningEndScale = 8f;

    [Header("Optional Screen Warning UI")]
    [SerializeField] private bool useWarningUI = true;
    [SerializeField] private Image warningUI;
    [SerializeField] private Text warningText;

    [Header("Warning UI Text")]
    [SerializeField] private bool useWarningText = true;
    [SerializeField] private string defaultWarningMessage = "WARNING";
    [SerializeField] private string gridWarningMessage = "GRID ATTACK";
    [SerializeField] private string radialWarningMessage = "RADIAL ATTACK";
    [SerializeField] private string fireWarningMessage = "FIRE ATTACK";
    [SerializeField] private string fireRingWarningMessage = "FIRE RING";

    [Header("Grid Warning Placement")]
    [SerializeField] private bool placeGridWarningOnDangerLine = true;

    [Header("Debug")]
    [SerializeField] private bool verboseLog = true;

    public List<SpecialAttackPattern> Patterns => patterns;

    private readonly Dictionary<SpecialAttackType, SpecialAttackPattern> patternMap =
        new Dictionary<SpecialAttackType, SpecialAttackPattern>();

    private int warningUIVersion;

    private void Awake()
    {
        RebuildPatternCache();

        if (warningUI != null)
            warningUI.gameObject.SetActive(false);
    }

    private void OnValidate()
    {
        RebuildPatternCache();

        fireRingWarningStartScale = Mathf.Max(0.01f, fireRingWarningStartScale);
        fireRingWarningEndScale = Mathf.Max(fireRingWarningStartScale, fireRingWarningEndScale);
    }

    private void OnDisable()
    {
        warningUIVersion++;

        if (warningUI != null)
            warningUI.gameObject.SetActive(false);
    }

    private void RebuildPatternCache()
    {
        patternMap.Clear();

        if (patterns == null)
            return;

        for (int i = 0; i < patterns.Count; i++)
        {
            SpecialAttackPattern pattern = patterns[i];

            if (pattern == null)
                continue;

            patternMap[pattern.attackType] = pattern;
        }
    }

    public bool HasPattern(SpecialAttackType type)
    {
        return patternMap.ContainsKey(type);
    }

    public void ExecutePattern(SpecialAttackType type, Transform target)
    {
        StartCoroutine(ExecutePatternRoutine(type, target, GetDefaultMessage(type)));
    }

    public void ExecutePattern(SpecialAttackType type, Transform target, string message)
    {
        StartCoroutine(ExecutePatternRoutine(type, target, message));
    }

    public IEnumerator ExecutePatternRoutine(SpecialAttackType type, Transform target)
    {
        yield return ExecutePatternRoutine(type, target, GetDefaultMessage(type));
    }

    public IEnumerator ExecutePatternRoutine(SpecialAttackType type, Transform target, string message)
    {
        if (target == null)
            yield break;

        if (!patternMap.TryGetValue(type, out SpecialAttackPattern pattern))
        {
            LogWarning($"패턴을 찾지 못했습니다. type={type}");
            yield break;
        }

        switch (type)
        {
            case SpecialAttackType.Grid:
                yield return ExecuteGridAttack(pattern, target.position, message);
                break;

            case SpecialAttackType.Radial:
                yield return ExecuteRadialAttack(pattern, target, message);
                break;

            case SpecialAttackType.Fire:
                yield return ExecuteFireAttack(pattern, target.position, message);
                break;

            case SpecialAttackType.FireRing:
                yield return ExecuteFireRingAttack(pattern, target, message);
                break;
        }
    }

    private IEnumerator ExecuteGridAttack(SpecialAttackPattern pattern, Vector3 origin, string message)
    {
        ShowWarningUI(message);

        int lineCount = Mathf.Max(1, pattern.lineCount);
        int centerIndex = lineCount / 2;

        float spacing = Mathf.Max(0.1f, pattern.gridSpacing);
        float spawnDistance = Mathf.Max(1f, pattern.gridSpawnDistance);

        float delay = GetWarningDelay();
        float warningLife = GetWarningLife(pattern, delay);

        List<GameObject> warnings = new List<GameObject>();

        Quaternion verticalWarningRotation = GetGridWarningRotation(pattern, true);
        Quaternion horizontalWarningRotation = GetGridWarningRotation(pattern, false);

        if (pattern.warningIndex >= 0)
        {
            for (int i = 0; i < lineCount; i++)
            {
                float offset = (i - centerIndex) * spacing;

                Vector3 warningPosVertical;
                Vector3 warningPosHorizontal;

                if (placeGridWarningOnDangerLine)
                {
                    warningPosVertical = new Vector3(
                        origin.x + offset,
                        origin.y,
                        0f
                    );

                    warningPosHorizontal = new Vector3(
                        origin.x,
                        origin.y + offset,
                        0f
                    );
                }
                else
                {
                    warningPosVertical = new Vector3(
                        origin.x + offset,
                        origin.y + spawnDistance,
                        0f
                    );

                    warningPosHorizontal = new Vector3(
                        origin.x - spawnDistance,
                        origin.y + offset,
                        0f
                    );
                }

                GameObject warningV = SpawnSpecial(
                    pattern.warningIndex,
                    warningPosVertical,
                    verticalWarningRotation
                );

                if (warningV != null)
                {
                    SetupAutoRelease(warningV, warningLife);
                    warnings.Add(warningV);
                }

                GameObject warningH = SpawnSpecial(
                    pattern.warningIndex,
                    warningPosHorizontal,
                    horizontalWarningRotation
                );

                if (warningH != null)
                {
                    SetupAutoRelease(warningH, warningLife);
                    warnings.Add(warningH);
                }
            }
        }

        PlayWarningSfx();

        yield return new WaitForSeconds(delay);

        for (int i = 0; i < warnings.Count; i++)
            DespawnSpecial(warnings[i]);

        if (pattern.bulletIndex >= 0)
        {
            for (int i = 0; i < lineCount; i++)
            {
                float offset = (i - centerIndex) * spacing;

                Vector3 bulletPosVertical = new Vector3(
                    origin.x + offset,
                    origin.y + spawnDistance,
                    0f
                );

                SpawnSpecialBullet(
                    pattern,
                    bulletPosVertical,
                    Vector2.down,
                    180f
                );

                Vector3 bulletPosHorizontal = new Vector3(
                    origin.x - spawnDistance,
                    origin.y + offset,
                    0f
                );

                SpawnSpecialBullet(
                    pattern,
                    bulletPosHorizontal,
                    Vector2.right,
                    -90f
                );
            }
        }

        PlayAttackSfx();
    }

    private Quaternion GetGridWarningRotation(SpecialAttackPattern pattern, bool verticalLine)
    {
        if (pattern.warningSpriteAxis == SpecialWarningSpriteAxis.Vertical)
        {
            if (verticalLine)
                return Quaternion.identity;

            return Quaternion.Euler(0f, 0f, 90f);
        }

        if (verticalLine)
            return Quaternion.Euler(0f, 0f, 90f);

        return Quaternion.identity;
    }

    private IEnumerator ExecuteRadialAttack(SpecialAttackPattern pattern, Transform target, string message)
    {
        if (target == null)
            yield break;

        ShowWarningUI(message);

        int bulletCount = Mathf.Max(1, pattern.lineCount);
        float angleStep = 360f / bulletCount;
        float radiusOffset = 0.5f;

        float delay = GetWarningDelay();
        float warningLife = GetWarningLife(pattern, delay);

        GameObject[] warnings = new GameObject[bulletCount];

        if (pattern.warningIndex >= 0)
        {
            for (int i = 0; i < bulletCount; i++)
            {
                float angle = i * angleStep;

                GameObject warning = SpawnSpecial(
                    pattern.warningIndex,
                    target.position,
                    Quaternion.Euler(0f, 0f, angle)
                );

                if (warning != null)
                {
                    FollowTarget follow = warning.GetComponent<FollowTarget>();

                    if (follow != null)
                        follow.target = target;

                    SetupAutoRelease(warning, warningLife);
                    warnings[i] = warning;
                }
            }
        }

        PlayWarningSfx();

        yield return new WaitForSeconds(delay);

        for (int i = 0; i < warnings.Length; i++)
            DespawnSpecial(warnings[i]);

        if (target == null)
            yield break;

        if (pattern.bulletIndex >= 0)
        {
            for (int i = 0; i < bulletCount; i++)
            {
                float angle = i * angleStep;
                Vector2 dir = AngleToDir(angle);
                Vector3 spawnPos = target.position + (Vector3)(dir.normalized * radiusOffset);

                SpawnSpecialBullet(
                    pattern,
                    spawnPos,
                    dir,
                    Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f
                );
            }
        }

        PlayAttackSfx();
    }

    private IEnumerator ExecuteFireAttack(SpecialAttackPattern pattern, Vector3 centerPos, string message)
    {
        ShowWarningUI(message);

        float delay = GetWarningDelay();
        float warningLife = GetWarningLife(pattern, delay);

        GameObject warning = null;

        if (pattern.warningCircleIndex >= 0)
        {
            warning = SpawnSpecial(
                pattern.warningCircleIndex,
                centerPos,
                Quaternion.identity
            );

            if (warning != null)
                SetupAutoRelease(warning, warningLife);
        }

        PlayWarningSfx();

        yield return new WaitForSeconds(delay);

        DespawnSpecial(warning);

        if (pattern.fireBurstIndex >= 0)
        {
            GameObject fire = SpawnSpecial(
                pattern.fireBurstIndex,
                centerPos,
                Quaternion.identity
            );

            if (fire != null)
                SetupAutoRelease(fire, Mathf.Max(0.05f, pattern.effectLifeTime));
        }

        PlayAttackSfx();
    }

    private IEnumerator ExecuteFireRingAttack(SpecialAttackPattern pattern, Transform target, string message)
    {
        if (target == null)
            yield break;

        ShowWarningUI(message);

        float delay = GetWarningDelay();
        float warningLife = GetWarningLife(pattern, delay);

        GameObject warning = null;

        if (pattern.warningCircleIndex >= 0)
        {
            warning = SpawnSpecial(
                pattern.warningCircleIndex,
                target.position,
                Quaternion.identity
            );

            if (warning != null)
            {
                FollowTarget follow = warning.GetComponent<FollowTarget>();

                if (follow != null)
                    follow.target = target;

                StartCoroutine(ScaleWarningRoutine(warning.transform, delay));
                SetupAutoRelease(warning, warningLife + delay + 0.1f);
            }
        }

        PlayWarningSfx();

        yield return new WaitForSeconds(delay * 2f);

        ResetWarningScale(warning);
        DespawnSpecial(warning);

        if (target == null)
            yield break;

        if (pattern.fireRingIndex >= 0)
        {
            GameObject ring = SpawnSpecial(
                pattern.fireRingIndex,
                target.position,
                Quaternion.identity
            );

            if (ring != null)
                SetupAutoRelease(ring, Mathf.Max(0.05f, pattern.effectLifeTime));
        }

        PlayAttackSfx();
    }

    private IEnumerator ScaleWarningRoutine(Transform warningTransform, float duration)
    {
        if (warningTransform == null)
            yield break;

        warningTransform.localScale = Vector3.one * fireRingWarningStartScale;

        Vector3 startScale = Vector3.one * fireRingWarningStartScale;
        Vector3 endScale = Vector3.one * fireRingWarningEndScale;

        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.01f, duration);

        while (elapsed < safeDuration)
        {
            if (warningTransform == null)
                yield break;

            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / safeDuration);
            warningTransform.localScale = Vector3.Lerp(startScale, endScale, t);

            yield return null;
        }

        if (warningTransform != null)
            warningTransform.localScale = Vector3.one;
    }

    private void SpawnSpecialBullet(
        SpecialAttackPattern pattern,
        Vector3 position,
        Vector2 direction,
        float rotationZ)
    {
        if (pattern.bulletIndex < 0)
            return;

        GameObject bullet = SpawnSpecial(
            pattern.bulletIndex,
            position,
            Quaternion.Euler(0f, 0f, rotationZ)
        );

        if (bullet == null)
            return;

        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.None;
            rb.velocity = direction.normalized * pattern.bulletSpeed;
        }

        SetupAutoRelease(bullet, Mathf.Max(0.05f, pattern.bulletLifeTime));
    }

    private GameObject SpawnSpecial(int index, Vector3 position, Quaternion rotation)
    {
        if (index < 0)
            return null;

        if (SpecialBulletPoolManager.Instance == null)
        {
            LogWarning("SpecialBulletPoolManager.Instance가 없습니다.");
            return null;
        }

        GameObject obj = SpecialBulletPoolManager.Instance.GetSpecial(index);

        if (obj == null)
        {
            LogWarning($"풀에서 특수 오브젝트를 가져오지 못했습니다. index={index}");
            return null;
        }

        obj.transform.SetPositionAndRotation(position, rotation);
        obj.SetActive(true);

        ResetRigidbody(obj);

        return obj;
    }

    private void DespawnSpecial(GameObject obj)
    {
        if (obj == null)
            return;

        if (!obj.activeInHierarchy)
            return;

        if (SpecialBulletPoolManager.Instance != null)
            SpecialBulletPoolManager.Instance.ReleaseSpecial(obj);
        else
            obj.SetActive(false);
    }

    private void ResetWarningScale(GameObject warning)
    {
        if (warning == null)
            return;

        warning.transform.localScale = Vector3.one;
    }

    private void SetupAutoRelease(GameObject obj, float life)
    {
        if (obj == null)
            return;

        PooledAutoRelease autoRelease = obj.GetComponent<PooledAutoRelease>();

        if (autoRelease != null)
            autoRelease.SetLifeTime(life);
    }

    private void ResetRigidbody(GameObject obj)
    {
        if (obj == null)
            return;

        Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();

        if (rb == null)
            return;

        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.gravityScale = 0f;
    }

    private float GetWarningDelay()
    {
        return Mathf.Max(0.01f, warningDelay);
    }

    private float GetWarningLife(SpecialAttackPattern pattern, float fallback)
    {
        if (pattern != null && pattern.warningLifeTime > 0f)
            return pattern.warningLifeTime;

        return fallback;
    }

    private Vector2 AngleToDir(float angle)
    {
        float rad = angle * Mathf.Deg2Rad;

        return new Vector2(
            Mathf.Cos(rad),
            Mathf.Sin(rad)
        ).normalized;
    }

    private string GetDefaultMessage(SpecialAttackType type)
    {
        switch (type)
        {
            case SpecialAttackType.Grid:
                return gridWarningMessage;

            case SpecialAttackType.Radial:
                return radialWarningMessage;

            case SpecialAttackType.Fire:
                return fireWarningMessage;

            case SpecialAttackType.FireRing:
                return fireRingWarningMessage;

            default:
                return defaultWarningMessage;
        }
    }

    private void ShowWarningUI(string message)
    {
        if (!useWarningUI)
            return;

        if (warningUI == null)
            return;

        warningUIVersion++;
        int version = warningUIVersion;

        if (warningText != null && useWarningText)
            warningText.text = string.IsNullOrEmpty(message) ? defaultWarningMessage : message;

        warningUI.gameObject.SetActive(true);
        StartCoroutine(HideWarningUIRoutine(version, GetWarningDelay()));
    }

    private IEnumerator HideWarningUIRoutine(int version, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (version != warningUIVersion)
            yield break;

        if (warningUI != null)
            warningUI.gameObject.SetActive(false);
    }

    private void PlayWarningSfx()
    {
        if (AudioManager.instance != null)
            AudioManager.instance.PlaySfx(AudioManager.Sfx.LevelUp);
    }

    private void PlayAttackSfx()
    {
        if (AudioManager.instance != null)
            AudioManager.instance.PlaySfx(AudioManager.Sfx.Dead);
    }

    private void LogWarning(string message)
    {
        if (!verboseLog)
            return;

        Debug.LogWarning($"SpecialAttack: {message}", this);
    }
}