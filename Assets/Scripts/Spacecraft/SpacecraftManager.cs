using System;
using UnityEngine;

/// <summary>
/// Central manager for a spacecraft prefab.
/// Orchestrates movement + skin modules and exposes unified events
/// so other systems can subscribe without direct coupling.
/// </summary>
public class SpacecraftManager : MonoBehaviour
{
    [Header("Default Configs")]
    [SerializeField] private SpacecraftMovementStats defaultStats;  // Default movement stats to apply at spawn
    [SerializeField] private GameObject defaultSkinPrefab;          // Default skin prefab to instantiate


    [Header("Data Sources")]
    [Tooltip("Spacecrafts SO list to resolve ID -> SO on load")]
    [SerializeField] private ItemSOList spacecraftList; // Must be Category = Spacecrafts

    [Header("Live Store Hook (optional)")]
    [Tooltip("If there is a StoreManager in the scene and you want to respond to Equip during gameplay")]
    [SerializeField] private StoreManager store; // Optional
    [SerializeField] private bool listenToStoreLive = false;

    private bool _configured; 
    public bool IsConfigured => _configured;

    private SpacecraftMovement movement;    // Handles physics and control logic
    private SpacecraftSkin skin;            // Current active skin instance
    private Rigidbody2D rb;                 // Cached rigidbody reference for speed queries

    // Forwarded events from SpacecraftMovement so visuals and other systems
    // can subscribe to one hub (the manager)
    public event EventHandler OnThrustForward;
    public event EventHandler OnRotateLeft;
    public event EventHandler OnRotateRight;
    public event EventHandler OnNoThrust;
    public event EventHandler OnBoostStart;
    public event EventHandler OnBoostEnd;

    private void Awake()
    {
        FindModules();
        HookMovementEvents();
    }

    private void Start()
    {
        // If not configured before Start (e.g. by a spawner), load from the save
        if (!_configured)
        {
            if (!TryConfigureFromEquippedSave())
                ApplyFallbackDefaults();
        }
    }

    private void OnEnable()
    {
        if (listenToStoreLive && store != null)
            store.OnEquippedChanged += HandleEquippedChanged;
    }

    private void OnDisable()
    {
        if (store != null)
            store.OnEquippedChanged -= HandleEquippedChanged;
    }

    public void ConfigureFromItem(SpacecraftItemSO item)
    {
        if (item == null) return;

        if (item.movementStats != null)
            ApplyMovementStats(item.movementStats);

        if (item.skinPrefab != null)
            ReplaceSkin(item.skinPrefab);

        _configured = true;
    }

    /// <summary>
    /// Finds required child modules (Movement + Skin).
    /// </summary>
    private void FindModules()
    {
        movement = GetComponentInChildren<SpacecraftMovement>();
        skin = GetComponentInChildren<SpacecraftSkin>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void ApplyFallbackDefaults()
    {
        if (movement != null && defaultStats != null)
            movement.ApplyStats(defaultStats);

        if (defaultSkinPrefab != null)
            ReplaceSkin(defaultSkinPrefab);
    }

    /// <summary>
    /// Loads the equipped spacecraft from the save and configures the ship.
    /// Returns true on success.
    /// </summary>
    private bool TryConfigureFromEquippedSave()
    {
        if (!SaveLoadManager.Load<GameSaveData>(StoreManager.SAVE_FILE_NAME, out var data))
            return false;

        if (data.Equipped == null) return false;

        // Find the equipped item in the Spacecrafts category
        var eq = data.Equipped.Find(e => e.Category == StoreCategory.Spacecrafts);
        if (eq == null || string.IsNullOrEmpty(eq.ItemId)) return false;

        // Resolve ID -> SO via the list
        if (spacecraftList == null)
        {
            Debug.LogWarning("SpacecraftManager: spacecraftList is not assigned; cannot resolve equipped item.");
            return false;
        }

        var so = spacecraftList.GetItemById(eq.ItemId) as SpacecraftItemSO;
        if (so == null)
        {
            Debug.LogWarning($"SpacecraftManager: equipped spacecraft id '{eq.ItemId}' not found in ItemSOList.");
            return false;
        }

        ConfigureFromItem(so);
        return true;
    }

    private void HandleEquippedChanged(StoreCategory category, BaseItemSO item)
    {
        if (category != StoreCategory.Spacecrafts) return;

        // Live update: swap skin/stats when a different spacecraft is equipped
        var so = item as SpacecraftItemSO;
        if (so != null) ConfigureFromItem(so);
    }


    /// <summary>
    /// Subscribes to movement events and re-exposes them
    /// through this manager for other modules.
    /// </summary>
    private void HookMovementEvents()
    {
        if (movement == null) return;

        movement.OnThrustForward += (s, e) => OnThrustForward?.Invoke(this, e);
        movement.OnRotateLeft += (s, e) => OnRotateLeft?.Invoke(this, e);
        movement.OnRotateRight += (s, e) => OnRotateRight?.Invoke(this, e);
        movement.OnNoThrust += (s, e) => OnNoThrust?.Invoke(this, e);
        movement.OnBoostStart += (s, e) => OnBoostStart?.Invoke(this, e);
        movement.OnBoostEnd += (s, e) => OnBoostEnd?.Invoke(this, e);
    }

    /// <summary>
    /// Replaces the current skin with a new prefab.
    /// The prefab must include a SpacecraftSkin component.
    /// </summary>
    /// <param name="newSkinPrefab">The skin prefab to instantiate</param>
    public void ReplaceSkin(GameObject newSkinPrefab)
    {
        if (newSkinPrefab == null) return;

        if (skin != null)
        {
            Destroy(skin.gameObject);
            skin = null;
        }

        GameObject newSkinObject = Instantiate(newSkinPrefab, transform);
        skin = newSkinObject.GetComponent<SpacecraftSkin>();

        if (skin == null)
        {
            Debug.LogError($"New skin prefab {newSkinPrefab.name} has no SpacecraftSkin component!");
            return;
        }

        newSkinObject.transform.localPosition = Vector3.zero;
        newSkinObject.transform.localRotation = Quaternion.identity;
    }

    /// <summary>
    /// Applies a new set of movement stats to the movement module.
    /// </summary>
    /// <param name="stats">Stats asset to apply</param>
    public void ApplyMovementStats(SpacecraftMovementStats stats)
    {
        if (movement == null || stats == null) return;
        movement.ApplyStats(stats);
    }

    /// <summary>
    /// Current active boost mode of the movement module.
    /// Defaults to Afterburner if no movement module is present.
    /// </summary>
    public SpacecraftMovementStats.BoostMode CurrentBoostMode =>
        movement != null ? movement.CurrentBoostMode : SpacecraftMovementStats.BoostMode.Afterburner;

    /// <summary>
    /// True if the spacecraft is currently boosting.
    /// </summary>
    public bool IsBoosting => movement != null && movement.IsBoosting;

    /// <summary>
    /// Current available boost energy (units).
    /// </summary>
    public float BoostEnergyUnits => movement != null ? movement.BoostEnergyUnits : 0f;

    /// <summary>
    /// Maximum boost capacity (units).
    /// </summary>
    public float BoostCapacityUnits => movement != null ? movement.BoostCapacityUnits : 1f;

    /// <summary>
    /// Normalized boost fill ratio [0..1].
    /// </summary>
    public float BoostFill01 => movement != null ? movement.BoostFill01 : 0f;

    /// <summary>
    /// Current linear speed magnitude (m/s).
    /// </summary>
    public float CurrentSpeed => rb != null ? rb.linearVelocity.magnitude : 0f;

    // Public accessors for child modules
    public SpacecraftMovement Movement => movement;
    public SpacecraftSkin CurrentSkin => skin;
}
