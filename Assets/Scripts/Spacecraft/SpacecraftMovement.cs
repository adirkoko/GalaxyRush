using System;
using UnityEngine;

/// <summary>
/// Controls spacecraft movement, thrust, steering, boost, and related input handling.
/// Uses <see cref="BoostController"/> for boost logic and <see cref="SteeringAndForces"/> for force application.
/// </summary>
public class SpacecraftMovement : MonoBehaviour
{
    private const float MIN_BOOST_CAPACITY_EPSILON = 0.001f;

    private SpacecraftMovementStats stats;
    private Rigidbody2D rb;

    private Vector2 targetDir;
    private bool hasInput, boosting, braking, stabilizing;
    private float throttle;

    private BoostController boost;
    private SteeringAndForces forces = new SteeringAndForces();

    #region Events

    /// <summary> Invoked when forward thrust should be applied. </summary>
    public event EventHandler OnThrustForward;

    /// <summary> Invoked when a left rotation should be applied. </summary>
    public event EventHandler OnRotateLeft;

    /// <summary> Invoked when a right rotation should be applied. </summary>
    public event EventHandler OnRotateRight;

    /// <summary> Invoked when there is no thrust input. </summary>
    public event EventHandler OnNoThrust;

    /// <summary> Invoked when boost starts. </summary>
    public event EventHandler OnBoostStart;

    /// <summary> Invoked when boost ends. </summary>
    public event EventHandler OnBoostEnd;

    /// <summary> Invoked when a dash boost is fired (charge, impulse, speed before dash). </summary>
    public event Action<float, float, float> OnDashFired;

    /// <summary> Invoked when a boost or dash activation is denied. </summary>
    public event Action<SpacecraftMovementStats.BoostMode, float, float> OnBoostDenied;

    #endregion

    #region Properties

    /// <summary>
    /// The current active boost mode.
    /// </summary>
    public SpacecraftMovementStats.BoostMode CurrentBoostMode =>
        stats != null ? stats.BoostType : SpacecraftMovementStats.BoostMode.Afterburner;

    /// <summary>
    /// Whether the spacecraft is currently boosting.
    /// </summary>
    public bool IsBoosting => boosting;

    /// <summary>
    /// Current available boost energy (in units).
    /// </summary>
    public float BoostEnergyUnits => boost?.EnergyUnits ?? 0f;

    /// <summary>
    /// Maximum boost capacity (in units).
    /// </summary>
    public float BoostCapacityUnits => boost?.CapacityUnits ?? 0f;

    /// <summary>
    /// Normalized boost fill ratio [0..1].
    /// </summary>
    public float BoostFill01 =>
        stats == null ? 0f : Mathf.Clamp01(BoostEnergyUnits / Mathf.Max(MIN_BOOST_CAPACITY_EPSILON, BoostCapacityUnits));

    #endregion

    private void Awake()
    {
        rb = GetComponentInParent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError("SpacecraftMovement: No Rigidbody2D found in parent! Disabling.");
            enabled = false;
            return;
        }

        // Configure Rigidbody2D for top-down space-like movement
        rb.gravityScale = 0f;
        rb.linearDamping = 0f;
        rb.angularDamping = 2f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        // Setup boost controller and hook into events
        boost = new BoostController(rb);
        boost.OnBoostStart += () => { boosting = true; OnBoostStart?.Invoke(this, EventArgs.Empty); };
        boost.OnBoostEnd += () => { boosting = false; OnBoostEnd?.Invoke(this, EventArgs.Empty); };
        boost.OnDashFired += (charge01, impulse, speedBefore) =>
        {
            OnDashFired?.Invoke(charge01, impulse, speedBefore);
            ScheduleDashEndOnce();
        };
        boost.OnBoostDenied += (m, req, cur) => OnBoostDenied?.Invoke(m, req, cur);
    }

    private void Update()
    {
        if (stats == null) return;

        HandleInput();

        // Update boost logic (frame-based)
        boost.Tick(Time.deltaTime,
            isBoostHeld: () => GameInput.Instance != null && GameInput.Instance.IsBoostActionPressed(),
            wasBoostPressedThisFrame: () => GameInput.Instance != null && GameInput.Instance.WasBoostActionPressedThisFrame());

        // Smooth throttle ramp up/down
        float tUp = stats.ThrottleAccel * Time.deltaTime;
        float tDn = stats.ThrottleDecel * Time.deltaTime;
        throttle = Mathf.Clamp01(throttle + (hasInput ? tUp : -tDn));
    }

    private void FixedUpdate()
    {
        if (stats == null || rb == null) return;
        float dt = Time.fixedDeltaTime;

        // Apply deferred dash impulse if one was scheduled
        boost.FixedTick((Vector2)transform.up);

        // Apply steering and movement forces
        forces.ApplyTurning(rb, stats, hasInput, targetDir, dt);
        forces.ApplyThrust(rb, stats, hasInput, targetDir, throttle, boosting);
        forces.ApplyGrip(rb, stats, hasInput);
        forces.ApplyManualStabilize(rb, stats, stabilizing);
        forces.ApplyBraking(rb, stats, braking);
        forces.ApplyIdleDrag(rb, stats, hasInput, braking);
        forces.ClampMaxSpeed(rb, stats, boosting);

        // Emit thruster feedback events
        FireThrusterEvents();
    }

    /// <summary>
    /// Applies new movement stats to this spacecraft.
    /// </summary>
    /// <param name="newStats">The stats instance to apply (must not be null).</param>
    public void ApplyStats(SpacecraftMovementStats newStats)
    {
        if (newStats == null)
        {
            Debug.LogError("SpacecraftMovement: Tried to apply null stats!");
            return;
        }

        stats = newStats;
        if (stats.MaxSpeed <= 0f)
            Debug.LogError("SpacecraftMovement: MaxSpeed must be > 0!");

        boost.ApplyStats(stats);
        boosting = false;
        throttle = 0f;
    }

    /// <summary>
    /// Reads player input and updates internal direction and state flags.
    /// </summary>
    private void HandleInput()
    {
        if (GameInput.Instance == null)
        {
            targetDir = Vector2.zero;
            hasInput = false;
            braking = false;
            stabilizing = false;
            return;
        }

        Vector2 input = GameInput.Instance.GetMoveVector();
        targetDir = input.sqrMagnitude > 0f ? input.normalized : Vector2.zero;
        hasInput = targetDir != Vector2.zero;

        braking = GameInput.Instance.IsBrakeActionPressed();
        stabilizing = GameInput.Instance.IsStabilizeActionPressed();
    }

    /// <summary>
    /// Schedules the end of a dash window (Dash mode only).
    /// </summary>
    private void ScheduleDashEndOnce()
    {
        if (stats == null || stats.BoostType != SpacecraftMovementStats.BoostMode.Dash) return;
        CancelInvoke(nameof(EndDashWindow));
        Invoke(nameof(EndDashWindow), stats.DashDuration);
    }

    /// <summary>
    /// Ends the dash boost window.
    /// </summary>
    private void EndDashWindow() => boost.EndDashWindow();

    /// <summary>
    /// Determines and fires thrust/rotation events based on alignment and angular velocity.
    /// </summary>
    private void FireThrusterEvents()
    {
        const float rotateEventAngleThreshold = 6f;
        const float lowAngularSpeedThreshold = 5f;
        const float forwardAlignStraightThreshold = 0.85f;

        if (!hasInput)
        {
            OnNoThrust?.Invoke(this, EventArgs.Empty);
            return;
        }

        float currentAngle = rb.rotation;
        float targetAngle = Mathf.Atan2(targetDir.y, targetDir.x) * Mathf.Rad2Deg - 90f;
        float errorDeg = Mathf.DeltaAngle(currentAngle, targetAngle);

        float fwdAlign = Mathf.Max(0f, Vector2.Dot((Vector2)transform.up, targetDir));
        bool isStraight = Mathf.Abs(rb.angularVelocity) < lowAngularSpeedThreshold &&
                          fwdAlign >= forwardAlignStraightThreshold;

        if (isStraight)
        {
            OnThrustForward?.Invoke(this, EventArgs.Empty);
            return;
        }

        if (Mathf.Abs(errorDeg) > rotateEventAngleThreshold)
        {
            if (errorDeg > 0f) OnRotateLeft?.Invoke(this, EventArgs.Empty);
            else OnRotateRight?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            OnThrustForward?.Invoke(this, EventArgs.Empty);
        }
    }
}
