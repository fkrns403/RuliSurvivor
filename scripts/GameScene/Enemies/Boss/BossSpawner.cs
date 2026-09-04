using UnityEngine;

/// <summary>
/// 단일 보스 스포너.
/// 
/// 주의:
/// - ProgressiveBossSpawner와 같은 맵에서 동시에 사용하지 않는다.
/// - 둘 다 켜져 있으면 보스가 2마리 생성될 수 있다.
/// </summary>
[DisallowMultipleComponent]
public class BossSpawner : MonoBehaviour
{
    [Header("Boss")]
    [SerializeField] private int bossIndex = 0;

    [Header("Timing")]
    [SerializeField] private float spawnTime = 120f;

    [Header("Spawn Position")]
    [SerializeField] private bool useFixedPos = true;
    [SerializeField] private Vector3 fixedPos;

    [Header("Director")]
    [SerializeField] private BossDirector bossDirector;

    private bool spawned;

    private void OnEnable()
    {
        spawned = false;
    }

    private void Update()
    {
        if (spawned)
            return;

        GameManager gm = GameManager.Instance;

        if (gm == null || !gm.isLive || gm.isPaused)
            return;

        if (gm.gameTime < spawnTime)
            return;

        if (ProgressiveBossSpawner.Active != null)
        {
            Debug.LogWarning(
                "BossSpawner: ProgressiveBossSpawner가 활성화되어 있어 단일 보스 스폰을 중단합니다.",
                this
            );

            spawned = true;
            enabled = false;
            return;
        }

        spawned = true;
        SpawnBoss(gm);
    }

    private void SpawnBoss(GameManager gm)
    {
        if (gm.bossPoolManager == null)
        {
            Debug.LogError("BossSpawner: GameManager.bossPoolManager가 없습니다.", this);
            return;
        }

        GameObject boss = gm.bossPoolManager.GetBoss(bossIndex);

        if (boss == null)
        {
            Debug.LogError("BossSpawner: 보스를 가져오지 못했습니다.", this);
            return;
        }

        Vector3 pos = useFixedPos ? fixedPos : transform.position;
        pos.z = 0f;

        boss.transform.position = pos;
        boss.transform.rotation = Quaternion.identity;

        BossDifficulty diff =
            gm.isOverTime ? BossDifficulty.OverTime : BossDifficulty.Normal;

        if (bossDirector == null)
            bossDirector = FindObjectOfType<BossDirector>(true);

        if (bossDirector != null)
            bossDirector.StartBossSequence(boss, diff);
        else
            Debug.LogError("BossSpawner: BossDirector가 연결되어 있지 않습니다.", this);
    }
}