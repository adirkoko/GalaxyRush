using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Defines all movement-related stats for a spacecraft.
/// Stored as a ScriptableObject so different ships/configs can reuse different stats.
/// </summary>
[CreateAssetMenu(fileName = "ShipMovementStats", menuName = "Spacecraft/Movement Stats")]
public class SpacecraftMovementStats : ScriptableObject
{
    [Header("Speed & Acceleration")]
    [Tooltip("Maximum forward speed the spacecraft can reach.")]
    [SerializeField] private float maxSpeed = 14f;

    [Tooltip("Base forward acceleration force applied when thrusting.")]
    [SerializeField] private float accel = 12f;

    [Tooltip("Rate at which throttle increases (0–1 per second).")]
    [SerializeField] private float throttleAccel = 3.2f;

    [Tooltip("Rate at which throttle decreases when no input is given.")]
    [SerializeField] private float throttleDecel = 3.4f;

    [Header("Braking / Decel")]
    [Tooltip("Strength of braking force applied when pressing the brake key.")]
    [SerializeField] private float brakeStrength = 28f;

    [Tooltip("Gentle linear drag applied when idle or braking.")]
    [SerializeField] private float linearDampingIdle = 0.06f;

    [Header("Turning / Agility (PD torque)")]
    [Tooltip("Maximum torque applied for turning.")]
    [SerializeField] private float turnTorque = 12f;

    [Tooltip("Maximum angular speed allowed (degrees per second).")]
    [SerializeField] private float maxAngularSpeed = 360f;

    [Tooltip("Proportional gain for turning (responsiveness to angle error).")]
    [SerializeField] private float turnKp = 6f;

    [Tooltip("Derivative gain for turning (damping against angular velocity).")]
    [SerializeField] private float turnKd = 1.6f;

    [Tooltip("Small angle error range that will be ignored (deadzone).")]
    [SerializeField] private float angleDeadzone = 1.0f;

    public enum BoostMode { Afterburner, Dash }

    [Header("Boost Settings")]
    [Tooltip("Boost behavior mode: Afterburner (hold to boost) or Dash (short burst).")]
    [SerializeField] private BoostMode boostMode = BoostMode.Afterburner;

    // ---------------- Afterburner ----------------
    [Header("Afterburner (used only if BoostMode = Afterburner)")]
    [Tooltip("Multiplier applied to speed and acceleration during Afterburner.")]
    [SerializeField] private float boostMultiplier = 1.5f;

    [Tooltip("Total energy capacity available for Afterburner (in seconds at full thrust).")]
    [SerializeField] private float boostCapacity = 2.0f;

    [Tooltip("Energy consumed per second while Afterburner is active.")]
    [SerializeField] private float boostUsePerSec = 1.0f;

    [Tooltip("Energy regenerated per second when Afterburner is not active.")]
    [SerializeField] private float boostRegenPerSec = 0.6f;

    [Tooltip("Minimum fraction (0–1) of boost energy required before Afterburner can activate.")]
    [SerializeField, Range(0f, 1f)] private float boostMinFractionToActivate = 0.2f;

    // ---------------- Dash ----------------
    [Header("Dash (used only if BoostMode = Dash)")]
    [Tooltip("Total dash charge capacity. Treat as 'units of charge'.")]
    [SerializeField] private float dashChargeCapacity = 100f;

    [Tooltip("Charge regenerated per second when not dashing.")]
    [SerializeField] private float dashChargeRegenPerSec = 30f;

    [Tooltip("Minimum charge fraction (0–1) required to allow a dash.")]
    [SerializeField, Range(0f, 1f)] private float dashMinFractionToActivate = 0.2f;

    [Tooltip("Impulse at 0% charge (use > 0 to guarantee a small dash).")]
    [SerializeField] private float dashImpulseMin = 4f;

    [Tooltip("Impulse at 100% charge.")]
    [SerializeField] private float dashImpulseMax = 14f;

    [Tooltip("Visual/UI duration of 'boosting' flag after dash (seconds).")]
    [SerializeField] private float dashDuration = 0.18f;

    [Tooltip("Temporary max-speed multiplier allowed during dash to avoid clamping the burst immediately.")]
    [SerializeField] private float dashOverspeedMultiplier = 1.35f;

    [Header("Improved Control (Slip Reduction)")]
    [Tooltip("How strongly the ship resists sideways sliding (0 = none, 2–6 = stronger).")]
    [SerializeField] private float lateralGrip = 0.0f;

    [Tooltip("If true, grip is applied only when player provides input.")]
    [SerializeField] private bool gripOnlyWhenInput = true;

    [Header("High-Speed Stability")]
    [Tooltip("Factor (0–1) controlling how much steering is reduced at high speed.")]
    [SerializeField] private float highSpeedStability = 0.6f;

    [Tooltip("Clamp factor for steering at top speed (e.g. 0.6 = 60% turn rate).")]
    [SerializeField] private float steerClampAtTop = 0.6f;

    // --- Properties for code access ---
    public float MaxSpeed => maxSpeed;
    public float Accel => accel;
    public float ThrottleAccel => throttleAccel;
    public float ThrottleDecel => throttleDecel;

    public float BrakeStrength => brakeStrength;
    public float LinearDampingIdle => linearDampingIdle;

    public float TurnTorque => turnTorque;
    public float MaxAngularSpeed => maxAngularSpeed;
    public float TurnKp => turnKp;
    public float TurnKd => turnKd;
    public float AngleDeadzone => angleDeadzone;

    public BoostMode BoostType => boostMode;

    // Afterburner
    public float BoostMultiplier => boostMultiplier;
    public float BoostCapacity => boostCapacity;
    public float BoostUsePerSec => boostUsePerSec;
    public float BoostRegenPerSec => boostRegenPerSec;
    public float BoostMinFractionToActivate => boostMinFractionToActivate;

    // Dash
    public float DashChargeCapacity => dashChargeCapacity;
    public float DashChargeRegenPerSec => dashChargeRegenPerSec;
    public float DashMinFractionToActivate => dashMinFractionToActivate;
    public float DashImpulseMin => dashImpulseMin;
    public float DashImpulseMax => dashImpulseMax;
    public float DashDuration => dashDuration;
    public float DashOverspeedMultiplier => dashOverspeedMultiplier;

    public float LateralGrip => lateralGrip;
    public bool GripOnlyWhenInput => gripOnlyWhenInput;

    public float HighSpeedStability => highSpeedStability;
    public float SteerClampAtTop => steerClampAtTop;
}
