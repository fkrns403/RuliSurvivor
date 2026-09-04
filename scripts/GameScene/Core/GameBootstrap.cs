using System.Collections;
using UnityEngine;

/// <summary>
/// Game 씬 진입 시 선택된 맵과 플레이어를 생성하고,
/// 런타임 UI까지 자동 연결하는 부트스트랩.
/// </summary>
[DisallowMultipleComponent]
public class GameBootstrap : MonoBehaviour
{
    private const string KEY_CHAR = "SelectedCharacterIndex";
    private const string KEY_MAP = "SelectedMapIndex";

    [Header("TitleManager와 같은 순서로 넣어야 하는 캐릭터 목록")]
    [SerializeField] private CharacterEntry[] characters;

    [Header("TitleManager와 같은 순서로 넣어야 하는 맵 목록")]
    [SerializeField] private MapEntry[] maps;

    [Header("플레이어 기본 스폰 위치")]
    [SerializeField] private Vector3 playerSpawnPos = Vector3.zero;

    private GameObject spawnedMap;
    private GameObject spawnedPlayer;
    private MapEntry selectedMapEntry;

    private void Start()
    {
        Time.timeScale = 1f;

        GameManager gm = GameManager.Instance;

        if (gm == null)
        {
            Debug.LogError("GameBootstrap: GameManager.Instance가 없습니다.", this);
            return;
        }

        if (characters == null || characters.Length == 0)
        {
            Debug.LogError("GameBootstrap: characters 배열이 비어 있습니다.", this);
            return;
        }

        if (maps == null || maps.Length == 0)
        {
            Debug.LogError("GameBootstrap: maps 배열이 비어 있습니다.", this);
            return;
        }

        ResolveSelectedIndices(out int charIndex, out int mapIndex);

        selectedMapEntry = maps[mapIndex];

        SpawnMap(mapIndex);
        SpawnPlayer(charIndex);

        gm.StartGame();

        StartCoroutine(DelayedCameraBoundaryBind());
    }

    private void ResolveSelectedIndices(out int charIndex, out int mapIndex)
    {
        if (GameSessionData.HasSelection)
        {
            charIndex = GameSessionData.SelectedCharacterIndex;
            mapIndex = GameSessionData.SelectedMapIndex;
        }
        else
        {
            charIndex = PlayerPrefs.GetInt(KEY_CHAR, 0);
            mapIndex = PlayerPrefs.GetInt(KEY_MAP, 0);
        }

        charIndex = Mathf.Clamp(charIndex, 0, characters.Length - 1);
        mapIndex = Mathf.Clamp(mapIndex, 0, maps.Length - 1);

        PlayerPrefs.SetInt(KEY_CHAR, charIndex);
        PlayerPrefs.SetInt(KEY_MAP, mapIndex);
        PlayerPrefs.Save();
    }

    private void SpawnMap(int mapIndex)
    {
        MapEntry mapEntry = maps[mapIndex];

        if (mapEntry == null || mapEntry.mapPrefab == null)
        {
            Debug.LogError($"GameBootstrap: mapPrefab이 비어 있습니다. mapIndex={mapIndex}", this);
            return;
        }

        spawnedMap = Instantiate(mapEntry.mapPrefab);

        GameManager gm = GameManager.Instance;

        if (gm != null)
        {
            gm.SetCurrentMapId(mapEntry.id);
            gm.maxGameTime = Mathf.Max(1f, mapEntry.maxGameTime);
        }

        MapBoundary boundary = spawnedMap.GetComponentInChildren<MapBoundary>(true);

        if (boundary != null && gm != null)
            gm.SetMapBoundary(boundary);
        else
            Debug.LogWarning("GameBootstrap: 맵 프리팹에서 MapBoundary를 찾지 못했습니다.", this);
    }

    private void SpawnPlayer(int charIndex)
    {
        CharacterEntry characterEntry = characters[charIndex];

        if (characterEntry == null || characterEntry.playerPrefab == null)
        {
            Debug.LogError($"GameBootstrap: playerPrefab이 비어 있습니다. charIndex={charIndex}", this);
            return;
        }

        GameManager gm = GameManager.Instance;

        if (gm == null)
            return;

        gm.SetPlayerPrefab(characterEntry.playerPrefab);

        Vector3 spawnPos = ResolveSpawnPosition();

        Transform playerTransform = gm.SpawnPlayerAt(spawnPos);
        spawnedPlayer = playerTransform != null ? playerTransform.gameObject : null;

        if (spawnedPlayer == null)
        {
            Debug.LogError("GameBootstrap: 플레이어 생성에 실패했습니다.", this);
            return;
        }

        if (!spawnedPlayer.CompareTag("Player"))
            spawnedPlayer.tag = "Player";

        EnsurePlayerRuntimeComponents(spawnedPlayer);
        ApplyCharacterSetup(characterEntry);
        BindPlayerSpawner();
        BindRuntimePlayerUI();
    }

    private Vector3 ResolveSpawnPosition()
    {
        Transform point = PlayerSpawnPoint.FindDefault();

        if (point != null)
            return point.position;

        return playerSpawnPos;
    }

    private void EnsurePlayerRuntimeComponents(GameObject player)
    {
        if (player == null)
            return;

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();

        if (rb == null)
            rb = player.AddComponent<Rigidbody2D>();

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.simulated = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        if (player.GetComponent<Collider2D>() == null)
        {
            CapsuleCollider2D col = player.AddComponent<CapsuleCollider2D>();
            col.isTrigger = false;
            col.size = new Vector2(0.6f, 0.9f);
            col.direction = CapsuleDirection2D.Vertical;
        }

        if (player.GetComponent<PlayerInputSource>() == null)
            player.AddComponent<PlayerInputSource>();

        if (player.GetComponent<MovementMotor>() == null)
            player.AddComponent<MovementMotor>();

        if (player.GetComponent<SpriteFacing>() == null)
            player.AddComponent<SpriteFacing>();

        if (player.GetComponent<AnimatorDriver>() == null)
            player.AddComponent<AnimatorDriver>();

        if (player.GetComponent<PlayerController>() == null)
            player.AddComponent<PlayerController>();

        if (player.GetComponent<PlayerHealth>() == null)
            player.AddComponent<PlayerHealth>();

        if (player.GetComponent<PlayerStatSystem>() == null)
            player.AddComponent<PlayerStatSystem>();

        if (player.GetComponent<PlayerMagnet>() == null)
            player.AddComponent<PlayerMagnet>();

        if (player.GetComponent<TargetScanner2D>() == null)
            player.AddComponent<TargetScanner2D>();

        if (player.GetComponent<WeaponSystem>() == null)
            player.AddComponent<WeaponSystem>();

        if (player.GetComponent<PlayerItemApplier>() == null)
            player.AddComponent<PlayerItemApplier>();

        if (player.GetComponent<PlayerAbilityController>() == null)
            player.AddComponent<PlayerAbilityController>();

        if (player.GetComponent<PlayerDash>() == null)
            player.AddComponent<PlayerDash>();
    }

    private void ApplyCharacterSetup(CharacterEntry entry)
    {
        if (spawnedPlayer == null || entry == null)
            return;

        ApplyStartingWeapon(entry);
        ApplyCharacterAbility(entry);
        ApplyLives(entry);
    }

    private void ApplyStartingWeapon(CharacterEntry entry)
    {
        if (entry.startingWeaponItem == null)
            return;

        PlayerItemApplier applier = spawnedPlayer.GetComponent<PlayerItemApplier>();

        if (applier == null)
        {
            Debug.LogWarning("GameBootstrap: PlayerItemApplier가 없어 시작 무기를 적용할 수 없습니다.", this);
            return;
        }

        bool applied = applier.ApplyStartingWeapon(entry.startingWeaponItem);

        if (!applied)
        {
            Debug.LogWarning($"GameBootstrap: 시작 무기 적용 실패 - {entry.startingWeaponItem.itemName}", this);
            return;
        }

        LevelUpUI levelUpUI = FindObjectOfType<LevelUpUI>(true);

        if (levelUpUI != null)
            levelUpUI.SyncItemLevel(entry.startingWeaponItem, 1);
    }

    private void ApplyCharacterAbility(CharacterEntry entry)
    {
        PlayerAbilityController ability = spawnedPlayer.GetComponent<PlayerAbilityController>();

        if (ability != null)
            ability.SetCharacterType(entry.characterType);
    }

    private void ApplyLives(CharacterEntry entry)
    {
        PlayerLives lives = spawnedPlayer.GetComponent<PlayerLives>();

        if (lives == null)
            return;

        if (entry.characterType == CharacterType.NineLives)
            lives.SetLives(Mathf.Max(0, entry.defaultLives));
        else
            lives.SetLives(0);
    }

    private void BindPlayerSpawner()
    {
        if (spawnedPlayer == null || selectedMapEntry == null)
            return;

        if (selectedMapEntry.spawnDataSet == null)
        {
            Debug.LogWarning($"GameBootstrap: spawnDataSet이 비어 있습니다. map id={selectedMapEntry.id}", this);
            return;
        }

        Spawner playerSpawner = spawnedPlayer.GetComponentInChildren<Spawner>(true);

        if (playerSpawner == null)
        {
            Debug.LogWarning("GameBootstrap: 플레이어 프리팹 내부에서 Spawner를 찾지 못했습니다.", this);
            return;
        }

        playerSpawner.SetSpawnData(selectedMapEntry.spawnDataSet);
    }

    private void BindRuntimePlayerUI()
    {
        if (spawnedPlayer == null)
            return;

        SkillCooldownUI skillUI = FindObjectOfType<SkillCooldownUI>(true);

        PlayerAbilityController ability = spawnedPlayer.GetComponent<PlayerAbilityController>();

        if (ability != null)
            ability.BindSkillCooldownUI(skillUI);

        PlayerLives lives = spawnedPlayer.GetComponent<PlayerLives>();

        if (lives != null)
            lives.BindUI(skillUI);

        DashCooldownHUD dashHUD = FindObjectOfType<DashCooldownHUD>(true);
        PlayerDash dash = spawnedPlayer.GetComponent<PlayerDash>();

        if (dashHUD != null)
            dashHUD.BindPlayerDash(dash);
    }

    private IEnumerator DelayedCameraBoundaryBind()
    {
        yield return null;
        yield return null;

        GameManager gm = GameManager.Instance;

        if (gm != null)
            gm.UpdateCameraConfiner();
    }

    private void OnDestroy()
    {
        GameManager gm = GameManager.Instance;

        if (gm != null && spawnedPlayer != null && gm.IsRegisteredPlayer(spawnedPlayer))
            gm.UnregisterPlayer(spawnedPlayer);

        spawnedMap = null;
        spawnedPlayer = null;
        selectedMapEntry = null;
    }
}