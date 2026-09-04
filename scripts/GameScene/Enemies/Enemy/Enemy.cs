using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
public class Enemy : MonoBehaviour
{
    [Header("Enemy Type")]
    [SerializeField] private EnemyType enemyType = EnemyType.Type0;

    [Header("Enemy Id")]
    [SerializeField] private string enemyId = "enemy";

    [Header("Move")]
    [SerializeField] private float speed = 2f;

    [Header("Contact Damage")]
    [SerializeField] private float contactDamage = 5f;
    [SerializeField] private float contactDamageInterval = 0.5f;

    [Header("KnockBack")]
    [SerializeField] private float knockBackNormal = 1f;
    [SerializeField] private float knockBackBoss = 0f;

    [Header("Sorting")]
    [SerializeField] private int normalSortingOrder = 2;
    [SerializeField] private int bossSortingOrder = 5;
    [SerializeField] private int deadSortingOrder = 1;

    [Header("Animator")]
    [SerializeField] private RuntimeAnimatorController[] animatorCon;

    [Header("Drop")]
    [SerializeField] private DropManager dropManager;

    private Rigidbody2D rigid;
    private Collider2D coll;
    private SpriteRenderer spriter;
    private Animator anim;

    private Rigidbody2D target;

    private EnemyHealth enemyHealth;
    private BossHealth bossHealth;

    private WaitForFixedUpdate wait;

    private bool isLive;
    private bool specialRegistered;
    private float nextContactDamageTime;

    private bool IsBoss => enemyType == EnemyType.Boss;

    private bool IsSpecialTarget =>
        enemyType == EnemyType.Type2 ||
        enemyType == EnemyType.Boss;

    private void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        coll = GetComponent<Collider2D>();
        spriter = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();

        enemyHealth = GetComponent<EnemyHealth>();
        bossHealth = GetComponent<BossHealth>();

        wait = new WaitForFixedUpdate();

        if (dropManager == null)
            dropManager = FindObjectOfType<DropManager>(true);

        ConfigureRigidbody();
    }

    private void OnEnable()
    {
        CancelInvoke(nameof(DisableSelf));

        ConfigureRigidbody();
        ResetRuntimeState();
        BindTarget();

        BindHealthEvents();
        RegisterSpecialTarget();
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(DisableSelf));

        UnbindHealthEvents();
        UnregisterSpecialTarget();
    }

    private void ResetRuntimeState()
    {
        isLive = true;
        specialRegistered = false;
        nextContactDamageTime = 0f;

        if (coll != null)
            coll.enabled = true;

        if (rigid != null)
        {
            rigid.simulated = true;
            rigid.velocity = Vector2.zero;
            rigid.angularVelocity = 0f;
        }

        if (spriter != null)
        {
            spriter.enabled = true;
            spriter.color = Color.white;
            spriter.sortingOrder = IsBoss ? bossSortingOrder : normalSortingOrder;
        }

        if (anim != null)
        {
            anim.enabled = true;

            anim.ResetTrigger("Hit");
            anim.SetBool("Dead", false);

            /*
             * 풀링 재사용 핵심:
             * 죽음 애니메이션 프레임에 멈춰 있던 Animator 상태를
             * 기본 상태로 강제 복구한다.
             */
            anim.Rebind();
            anim.Update(0f);

            anim.SetBool("Dead", false);
        }
    }

    private void ConfigureRigidbody()
    {
        if (rigid == null)
            return;

        rigid.gravityScale = 0f;
        rigid.freezeRotation = true;
    }

    private void FixedUpdate()
    {
        if (!isLive)
            return;

        GameManager gm = GameManager.Instance;

        if (gm == null || !gm.isLive || gm.isPaused)
            return;

        if (target == null)
        {
            BindTarget();

            if (target == null)
                return;
        }

        Vector2 dir = target.position - rigid.position;
        Vector2 next = dir.normalized * speed * Time.fixedDeltaTime;

        rigid.MovePosition(rigid.position + next);
        rigid.velocity = Vector2.zero;
    }

    private void LateUpdate()
    {
        if (!isLive || target == null || spriter == null)
            return;

        spriter.flipX = target.position.x < rigid.position.x;
    }

    public void Setup(SpawnData data)
    {
        if (data == null)
            return;

        if (anim != null &&
            animatorCon != null &&
            data.spriteType >= 0 &&
            data.spriteType < animatorCon.Length)
        {
            anim.runtimeAnimatorController = animatorCon[data.spriteType];

            anim.Rebind();
            anim.Update(0f);
            anim.SetBool("Dead", false);
        }

        speed = data.speed;

        if (enemyHealth != null)
            enemyHealth.SetMaxHp(data.health);
    }

    public void Setup(int hp, float moveSpeed)
    {
        speed = moveSpeed;

        if (anim != null)
        {
            anim.SetBool("Dead", false);
            anim.Update(0f);
        }

        if (enemyHealth != null)
            enemyHealth.SetMaxHp(hp);
    }

    public void ApplyKnockBack()
    {
        if (!isLive)
            return;

        StartCoroutine(KnockBackRoutine());
    }

    private IEnumerator KnockBackRoutine()
    {
        yield return wait;

        if (GameManager.Instance == null)
            yield break;

        if (GameManager.Instance.PlayerTransform == null)
            yield break;

        if (rigid == null)
            yield break;

        Vector3 playerPos = GameManager.Instance.PlayerTransform.position;
        Vector3 dirVec = transform.position - playerPos;

        float power = IsBoss ? knockBackBoss : knockBackNormal;

        if (power > 0f)
            rigid.AddForce(dirVec.normalized * power, ForceMode2D.Impulse);
    }

    private void BindTarget()
    {
        if (GameManager.Instance != null &&
            GameManager.Instance.PlayerTransform != null)
        {
            target = GameManager.Instance.PlayerTransform.GetComponent<Rigidbody2D>();
            return;
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

        if (playerObj != null)
            target = playerObj.GetComponent<Rigidbody2D>();
    }

    private void BindHealthEvents()
    {
        UnbindHealthEvents();

        if (enemyHealth != null)
            enemyHealth.Died += OnEnemyHealthDied;

        if (bossHealth != null)
            bossHealth.Died += OnBossHealthDied;
    }

    private void UnbindHealthEvents()
    {
        if (enemyHealth != null)
            enemyHealth.Died -= OnEnemyHealthDied;

        if (bossHealth != null)
            bossHealth.Died -= OnBossHealthDied;
    }

    private void RegisterSpecialTarget()
    {
        if (!IsSpecialTarget)
            return;

        if (specialRegistered)
            return;

        if (SpecialAttackManager.Instance != null)
        {
            SpecialAttackManager.Instance.NotifyEliteSpawned(transform, enemyType);
            specialRegistered = true;
        }
    }

    private void UnregisterSpecialTarget()
    {
        if (!specialRegistered)
            return;

        if (SpecialAttackManager.Instance != null)
            SpecialAttackManager.Instance.NotifyEliteDied(transform, enemyType);

        specialRegistered = false;
    }

    private void OnEnemyHealthDied(EnemyHealth health)
    {
        Vector3 dropPos = GetDropPosition();

        HandleDeathCommon();

        GameManager.Instance?.AddKill(1);

        if (UnlockManager.Instance != null)
        {
            UnlockManager.Instance.RegisterKill("all");
            UnlockManager.Instance.RegisterKill(enemyId);
        }

        if (dropManager != null)
            dropManager.DropItems(dropPos, enemyType);

        FinishDeath();
    }

    private void OnBossHealthDied(BossHealth health)
    {
        Vector3 dropPos = GetDropPosition();

        HandleDeathCommon();

        GameManager.Instance?.AddKill(1);

        if (dropManager != null)
            dropManager.DropItems(dropPos, enemyType);

        if (UnlockManager.Instance != null)
        {
            UnlockManager.Instance.RegisterKill("all");
            UnlockManager.Instance.RegisterKill("boss");

            if (health != null && !string.IsNullOrEmpty(health.BossId))
                UnlockManager.Instance.RegisterKill(health.BossId);
        }

        if (health != null && !string.IsNullOrEmpty(health.BossId))
            BossUnlock.SaveBossCleared(health.BossId);

        bool handledByProgressiveSpawner = false;

        if (health != null)
            handledByProgressiveSpawner = ProgressiveBossSpawner.TryHandleBossDeath(health);

        if (!handledByProgressiveSpawner)
        {
            GameFlow flow = FindObjectOfType<GameFlow>(true);

            if (flow != null)
                flow.Win();
            else
                GameManager.Instance?.GameVictory();
        }

        FinishDeath();
    }

    private Vector3 GetDropPosition()
    {
        if (coll != null)
        {
            Vector3 p = coll.bounds.center;
            p.z = 0f;
            return p;
        }

        if (rigid != null)
        {
            Vector3 p = rigid.position;
            p.z = 0f;
            return p;
        }

        Vector3 fallback = transform.position;
        fallback.z = 0f;
        return fallback;
    }

    private void HandleDeathCommon()
    {
        isLive = false;

        if (coll != null)
            coll.enabled = false;

        if (rigid != null)
        {
            rigid.velocity = Vector2.zero;
            rigid.angularVelocity = 0f;
            rigid.simulated = false;
        }

        if (spriter != null)
            spriter.sortingOrder = deadSortingOrder;

        if (anim != null)
            anim.SetBool("Dead", true);

        if (AudioManager.instance != null)
            AudioManager.instance.PlaySfx(AudioManager.Sfx.Dead);

        UnregisterSpecialTarget();
    }

    private void FinishDeath()
    {
        CancelInvoke(nameof(DisableSelf));
        Invoke(nameof(DisableSelf), 0.5f);
    }

    private void DisableSelf()
    {
        gameObject.SetActive(false);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryDamagePlayer(collision.collider);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryDamagePlayer(other);
    }

    private void TryDamagePlayer(Collider2D other)
    {
        if (!isLive)
            return;

        if (Time.time < nextContactDamageTime)
            return;

        if (other == null)
            return;

        if (other.isTrigger)
            return;

        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();

        if (playerHealth == null)
            return;

        if (playerHealth.IsInvincible)
            return;

        playerHealth.TakeDamage(contactDamage);

        nextContactDamageTime =
            Time.time + Mathf.Max(0.05f, contactDamageInterval);
    }

    public void OverrideTarget(Transform newTarget)
    {
        if (newTarget == null)
            return;

        Rigidbody2D rb = newTarget.GetComponent<Rigidbody2D>();

        if (rb == null)
            return;

        target = rb;
    }

    public void ClearTargetOverride()
    {
        BindTarget();
    }

    public EnemyType GetEnemyType()
    {
        return enemyType;
    }

    public string GetEnemyId()
    {
        return enemyId;
    }
}