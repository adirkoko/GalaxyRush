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

    private void Start()
    {
        FindModules();
        ApplyDefaultStats();
        HookMovementEvents();

        if (defaultSkinPrefab != null)
            ReplaceSkin(defaultSkinPrefab);
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

    /// <summary>
    /// Applies default movement stats if available.
    /// </summary>
    private void ApplyDefaultStats()
    {
        if (movement != null && defaultStats != null)
            movement.ApplyStats(defaultStats);
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
