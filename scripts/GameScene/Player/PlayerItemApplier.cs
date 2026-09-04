using UnityEngine;

[DisallowMultipleComponent]
public class PlayerItemApplier : MonoBehaviour
{
    [Header("Systems")]
    [SerializeField] private WeaponSystem weaponSystem;
    [SerializeField] private PlayerStatSystem statSystem;
    [SerializeField] private PlayerRegenerationSystem regenerationSystem;

    [Header("Current Weapon Prefabs")]
    [SerializeField] private MonoBehaviour weaponMeleePrefab;
    [SerializeField] private MonoBehaviour weaponOrbitPrefab;
    [SerializeField] private MonoBehaviour weaponRangePrefab;
    [SerializeField] private MonoBehaviour weaponShotgunPrefab;
    [SerializeField] private MonoBehaviour weaponArenaPrefab;
    [SerializeField] private MonoBehaviour weaponFugaPrefab;
    [SerializeField] private MonoBehaviour weaponMacePrefab;
    [SerializeField] private MonoBehaviour weaponMapleSwordPrefab;
    [SerializeField] private MonoBehaviour weaponDrainPrefab;
    [SerializeField] private MonoBehaviour weaponPiercePrefab;
    [SerializeField] private MonoBehaviour weaponPoisonOrbPrefab;
    [SerializeField] private MonoBehaviour weaponLightningPrefab;

    [Header("Instant Item Settings")]
    [SerializeField] private float magnetDuration = 5f;

    [Header("Equip Heal / Tension Up")]
    [SerializeField] private bool healItemFullHeal = true;
    [SerializeField] private float defaultHealAmount = 30f;
    [SerializeField] private float healItemInvincibleSeconds = 3f;
    [SerializeField] private float healItemAttackSpeedMultiplier = 2f;
    [SerializeField] private float healItemAttackSpeedDuration = 3f;

    [Header("Debug")]
    [SerializeField] private bool verboseLog = true;

    private PlayerMagnet playerMagnet;
    private PlayerHealth playerHealth;

    private void Awake()
    {
        ResolveSystems();
    }

    private void ResolveSystems()
    {
        if (weaponSystem == null)
            weaponSystem = GetComponent<WeaponSystem>();

        if (statSystem == null)
            statSystem = GetComponent<PlayerStatSystem>();

        if (regenerationSystem == null)
            regenerationSystem = GetComponent<PlayerRegenerationSystem>();

        if (playerMagnet == null)
            playerMagnet = GetComponent<PlayerMagnet>();

        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();
    }

    public bool Apply(ItemData data, int currentLevel)
    {
        ResolveSystems();

        if (data == null)
        {
            LogWarning("적용할 ItemData가 null입니다.");
            return false;
        }

        int safeCurrentLevel = Mathf.Max(0, currentLevel);
        int targetLevel = safeCurrentLevel + 1;

        switch (data.ItemType)
        {
            case ItemType.ExpSmall:
            case ItemType.ExpMedium:
            case ItemType.ExpLarge:
                return GiveExpFromItem(data);

            case ItemType.Magnet:
                return ActivateMagnetEffect();

            case ItemType.Equip_Heal:
                return ApplyHealItem(data);

            case ItemType.Weapon_Melee:
                return ApplyWeapon(ItemType.Weapon_Melee, weaponMeleePrefab, data, targetLevel);

            case ItemType.Weapon_Orbit:
                return ApplyWeapon(ItemType.Weapon_Orbit, weaponOrbitPrefab, data, targetLevel);

            case ItemType.Weapon_Range:
                return ApplyWeapon(ItemType.Weapon_Range, weaponRangePrefab, data, targetLevel);

            case ItemType.Weapon_Shotgun:
                return ApplyWeapon(ItemType.Weapon_Shotgun, weaponShotgunPrefab, data, targetLevel);

            case ItemType.Weapon_Arena:
                return ApplyWeapon(ItemType.Weapon_Arena, weaponArenaPrefab, data, targetLevel);

            case ItemType.Weapon_Fuga:
                return ApplyWeapon(ItemType.Weapon_Fuga, weaponFugaPrefab, data, targetLevel);

            case ItemType.Weapon_Mace:
                return ApplyWeapon(ItemType.Weapon_Mace, weaponMacePrefab, data, targetLevel);

            case ItemType.Weapon_MapleSword:
                return ApplyWeapon(ItemType.Weapon_MapleSword, weaponMapleSwordPrefab, data, targetLevel);

            case ItemType.Weapon_Drain:
                return ApplyWeapon(ItemType.Weapon_Drain, weaponDrainPrefab, data, targetLevel);

            case ItemType.Weapon_Pierce:
                return ApplyWeapon(ItemType.Weapon_Pierce, weaponPiercePrefab, data, targetLevel);

            case ItemType.Weapon_PoisonOrb:
                return ApplyWeapon(ItemType.Weapon_PoisonOrb, weaponPoisonOrbPrefab, data, targetLevel);

            case ItemType.Weapon_Lightning:
                return ApplyWeapon(ItemType.Weapon_Lightning, weaponLightningPrefab, data, targetLevel);

            case ItemType.Equip_Glove:
            case ItemType.Equip_Shoe:
            case ItemType.Equip_Jewel:
            case ItemType.Equip_Breadbone:
                return ApplyStatEquip(data, safeCurrentLevel);

            case ItemType.Equip_Regeneration:
                return ApplyRegenerationEquip(data, targetLevel);

            default:
                LogWarning($"처리되지 않은 ItemType입니다. ItemType={data.ItemType}, ItemName={data.itemName}");
                return false;
        }
    }

    public bool ApplyStartingWeapon(ItemData data)
    {
        if (data == null)
        {
            LogWarning("시작 무기 ItemData가 null입니다.");
            return false;
        }

        return Apply(data, 0);
    }

    private bool ApplyWeapon(ItemType type, MonoBehaviour weaponPrefab, ItemData data, int targetLevel)
    {
        if (weaponSystem == null)
        {
            LogWarning($"WeaponSystem이 없습니다. Type={type}");
            return false;
        }

        if (weaponPrefab == null)
        {
            LogWarning($"무기 프리팹이 연결되지 않았습니다. Type={type}, ItemName={data.itemName}");
            return false;
        }

        if (!(weaponPrefab is IWeaponRuntime))
        {
            LogWarning($"무기 프리팹이 IWeaponRuntime을 구현하지 않습니다. Type={type}, Prefab={weaponPrefab.name}");
            return false;
        }

        int safeLevel = Mathf.Max(1, targetLevel);

        bool result = weaponSystem.EquipOrLevelUp(type, weaponPrefab, data, safeLevel);

        if (!result)
            LogWarning($"WeaponSystem.EquipOrLevelUp 실패. Type={type}, ItemName={data.itemName}");

        return result;
    }

    private bool ApplyStatEquip(ItemData data, int currentLevel)
    {
        if (statSystem == null)
        {
            LogWarning($"PlayerStatSystem이 없습니다. ItemName={data.itemName}");
            return false;
        }

        statSystem.ApplyEquip(data, currentLevel);
        return true;
    }

    private bool ApplyRegenerationEquip(ItemData data, int targetLevel)
    {
        if (regenerationSystem == null)
        {
            LogWarning($"PlayerRegenerationSystem이 없습니다. ItemName={data.itemName}");
            return false;
        }

        regenerationSystem.ApplyUpgrade(data, Mathf.Max(1, targetLevel));
        return true;
    }

    private bool GiveExpFromItem(ItemData data)
    {
        if (GameManager.Instance == null)
        {
            LogWarning($"GameManager.Instance가 없습니다. ItemName={data.itemName}");
            return false;
        }

        int amount = Mathf.Max(0, data.expAmount);

        if (amount <= 0)
        {
            LogWarning($"경험치 지급량이 0 이하입니다. ItemName={data.itemName}");
            return false;
        }

        GameManager.Instance.AddExp(amount);
        return true;
    }

    private bool ActivateMagnetEffect()
    {
        if (playerMagnet == null)
            playerMagnet = GetComponent<PlayerMagnet>();

        if (playerMagnet == null)
        {
            LogWarning("PlayerMagnet이 없습니다.");
            return false;
        }

        playerMagnet.ActivateMagnet(Mathf.Max(0.1f, magnetDuration));
        return true;
    }

    private bool ApplyHealItem(ItemData data)
    {
        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();

        if (playerHealth == null)
        {
            LogWarning($"PlayerHealth가 없습니다. 회복 아이템 적용 불가. ItemName={data.itemName}");
            return false;
        }

        if (healItemFullHeal)
            playerHealth.HealFull();
        else
            playerHealth.Heal(Mathf.Max(1f, defaultHealAmount));

        playerHealth.AddTimedInvincibility(Mathf.Max(0.1f, healItemInvincibleSeconds));

        if (statSystem == null)
            statSystem = GetComponent<PlayerStatSystem>();

        if (statSystem != null)
        {
            statSystem.ApplyTemporaryAttackSpeedMultiplier(
                Mathf.Max(1f, healItemAttackSpeedMultiplier),
                Mathf.Max(0.1f, healItemAttackSpeedDuration)
            );
        }

        if (AudioManager.instance != null)
            AudioManager.instance.PlaySfx(AudioManager.Sfx.LevelUp);

        return true;
    }

    private void LogWarning(string message)
    {
        if (!verboseLog)
            return;

        Debug.LogWarning($"PlayerItemApplier: {message}", this);
    }
}