using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles all spacecraft movement logic including input,
/// physics forces, boosting, braking, and stabilization.
/// </summary>
public class SpacecraftMovement : MonoBehaviour
{
    // Movement stats (ScriptableObject applied from manager)
    private SpacecraftMovementStats stats;

    // Rigidbody2D used for physics simulation
    private Rigidbody2D rb;

    // Current input direction from keyboard
    private Vector2 targetDir;

    private bool hasInput, boosting, braking, stabilizing;  // Input/state flags
    private float throttle;     // Throttle level (0-1) rising/falling smoothly
    private float boostEnergy;  // Current available boost energy (for Afterburner mode)
    private float nextDashTime; // Next time dash can be triggered (for Dash mode cooldown)
    private bool boostLocked;   // true when boost is empty until threshold is reached

    // Events for visuals
    public event EventHandler OnThrustForward;
    public event EventHandler OnRotateLeft;
    public event EventHandler OnRotateRight;
    public event EventHandler OnNoThrust;
    public event EventHandler OnBoostStart;
    public event EventHandler OnBoostEnd;

    private void Awake()
    {
        // Use parent Rigidbody2D so physics is handled on root object
        rb = GetComponentInParent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError("SpacecraftMovement: No Rigidbody2D found in parent! Disabling.");
            enabled = false;
            return;
        }

        rb.gravityScale = 0f;
        rb.linearDamping = 0.5f;    // Custom drag handled manually in code
        rb.angularDamping = 2f;     // Angular damping for stability (D component of PD controller)
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;    // Smooth physics motion
    }

    private void Update()
    {
        if (stats == null) return;

        HandleInput();
        HandleBoost(Time.deltaTime);
        HandleThrottle(Time.deltaTime);
    }

    private void FixedUpdate()
    {
        if (stats == null || rb == null) return;
        float dt = Time.fixedDeltaTime;

        ApplyTurning(dt);
        ApplyThrust(dt);
        ApplyGrip(dt);
        ApplyManualStabilize();
        ApplyBraking();
        ApplyIdleDrag();
        ClampMaxSpeed();
        FireThrusterEvents();
    }


    /// <summary>
    /// Applies a new set of movement stats from a ScriptableObject.
    /// Resets boost energy and validates parameters.
    /// </summary>
    /// <param name="newStats">The stats asset to apply</param>
    public void ApplyStats(SpacecraftMovementStats newStats)
    {
        if (newStats == null)
        {
            Debug.LogError("SpacecraftMovement: Tried to apply null stats!");
            return;
        }

        stats = newStats;
        boostEnergy = stats.BoostCapacity;

        if (stats.MaxSpeed <= 0)
            Debug.LogError("SpacecraftMovement: MaxSpeed must be > 0!");
        if (stats.BoostType == SpacecraftMovementStats.BoostMode.Dash && stats.DashImpulse <= 0)
            Debug.LogWarning("SpacecraftMovement: Dash mode selected but DashImpulse <= 0.");
    }


    private void HandleInput()
    {
        Vector2 input = GameInput.Instance.GetMoveVector();

        targetDir = input.sqrMagnitude > 0f ? input.normalized : Vector2.zero;
        hasInput = targetDir != Vector2.zero;

        braking = GameInput.Instance.IsBrakeActionPressed();
        stabilizing = GameInput.Instance.IsStabilizeActionPressed();
    }



    /// <summary>
    /// Handles boost logic depending on mode (Afterburner or Dash).
    /// Consumes energy or applies dash impulse.
    /// </summary>
    /// <param name="dt">Delta time in seconds</param>
    private void HandleBoost(float dt)
    {
        bool wasBoosting = boosting;

        if (stats.BoostType == SpacecraftMovementStats.BoostMode.Afterburner)
        {
            bool keyDown = GameInput.Instance.IsBoostActionPressed();

            // Lock boost if energy is depleted while boosting
            if (boosting && boostEnergy <= 0f)
            {
                boostLocked = true;
                boosting = false;
            }

            // Unlock boost once energy has recharged past the required threshold
            if (boostLocked && boostEnergy >= stats.BoostCapacity * stats.BoostMinFractionToActivate)
                boostLocked = false;

            // Activate only if not locked and there is energy available
            if (!boostLocked && keyDown && boostEnergy > 0f) boosting = true;
            else boosting = false;
        }
        else // Dash mode
        {
            if (GameInput.Instance.WasBoostActionPressedThisFrame() && Time.time >= nextDashTime)
            {
                // Apply dash impulse
                rb.AddForce(transform.up * stats.DashImpulse, ForceMode2D.Impulse);
                nextDashTime = Time.time + stats.DashCooldown;

                // Trigger boost start event
                boosting = true;
                OnBoostStart?.Invoke(this, EventArgs.Empty);

                // Immediately schedule boost end after dashDuration (for visuals/UI only)
                Invoke(nameof(EndDashFlag), stats.DashDuration);
            }
        }


        // Fire events only on state change
        if (boosting && !wasBoosting)
            OnBoostStart?.Invoke(this, EventArgs.Empty);

        if (!boosting && wasBoosting)
            OnBoostEnd?.Invoke(this, EventArgs.Empty);
    }


    /// <summary>
    /// Smoothly updates throttle over time depending on player input.
    /// </summary>
    /// <param name="dt">Delta time in seconds</param>
    private void HandleThrottle(float dt)
    {
        float tUp = stats.ThrottleAccel * dt;
        float tDn = stats.ThrottleDecel * dt;
        throttle = Mathf.Clamp01(throttle + (hasInput ? tUp : -tDn));
    }


    /// <summary>
    /// Ends dash state (called shortly after dash impulse to reset boost flag).
    /// </summary>
    private void EndDashFlag()
    {
        if (boosting)
        {
            boosting = false;
            OnBoostEnd?.Invoke(this, EventArgs.Empty);
        }
    }



    /// <summary>
    /// Rotates the spacecraft toward the input direction using PD torque control.
    /// </summary>
    /// <param name="dt">Delta time in seconds</param>
    private void ApplyTurning(float dt)
    {
        if (!hasInput) return;

        float currentAngle = rb.rotation;
        float targetAngle = Mathf.Atan2(targetDir.y, targetDir.x) * Mathf.Rad2Deg - 90f;
        float error = Mathf.DeltaAngle(currentAngle, targetAngle);

        if (Mathf.Abs(error) <= stats.AngleDeadzone) return;

        float speed = rb.linearVelocity.magnitude;
        float speedNorm = Mathf.Clamp01(speed / Mathf.Max(0.001f, stats.MaxSpeed));
        float steerClamp = Mathf.Lerp(1f, stats.SteerClampAtTop, speedNorm * stats.HighSpeedStability);
        float maxAngAtSpeed = stats.MaxAngularSpeed * Mathf.Max(0.2f, steerClamp);

        float desiredAngVel = Mathf.Clamp(stats.TurnKp * error, -maxAngAtSpeed, maxAngAtSpeed);
        float angVelError = desiredAngVel - rb.angularVelocity;

        float torqueCmd = Mathf.Clamp(angVelError * stats.TurnKd, -stats.TurnTorque, stats.TurnTorque);
        rb.AddTorque(torqueCmd, ForceMode2D.Force);
    }


    /// <summary>
    /// Applies forward thrust based on acceleration, throttle,
    /// input alignment, and boost multiplier.
    /// </summary>
    /// <param name="dt">Delta time in seconds</param>
    private void ApplyThrust(float dt)
    {
        Vector2 fwd = transform.up;

        // Alignment between facing and input direction
        float forwardAlign = hasInput ? Mathf.Max(0f, Vector2.Dot(fwd, targetDir)) : 0f;

        float boostMul = 1f;
        if (stats.BoostType == SpacecraftMovementStats.BoostMode.Afterburner)
        {
            if (boosting)
            {
                boostMul = stats.BoostMultiplier;
                boostEnergy = Mathf.Max(0f, boostEnergy - stats.BoostUsePerSec * dt);
            }
            else
            {
                boostEnergy = Mathf.Min(stats.BoostCapacity, boostEnergy + stats.BoostRegenPerSec * dt);
            }
        }

        float thrust = stats.Accel * throttle * forwardAlign * boostMul;
        if (thrust > 0f)
            rb.AddForce(fwd * thrust, ForceMode2D.Force);
    }


    /// <summary>
    /// Applies lateral counterforce to reduce drifting,
    /// scaled by grip settings and current speed.
    /// </summary>
    /// <param name="dt">Delta time in seconds</param>
    private void ApplyGrip(float dt)
    {
        Vector2 v = rb.linearVelocity;
        float speedMag = v.magnitude;
        bool applyGrip = (stats.LateralGrip > 0f) && (!stats.GripOnlyWhenInput || hasInput);

        if (!applyGrip || speedMag <= 1e-5f) return;

        Vector2 fwd = transform.up;
        Vector2 lateral = v - Vector2.Dot(v, fwd) * fwd;
        float speedNorm = Mathf.Clamp01(speedMag / Mathf.Max(0.001f, stats.MaxSpeed));

        // Stronger grip at higher speeds
        float grip = stats.LateralGrip * (0.5f + 0.5f * speedNorm * stats.HighSpeedStability);

        // Apply gentle slowdown when idle/braking
        rb.AddForce(-lateral * grip, ForceMode2D.Force);
    }


    /// <summary>
    /// Adds extra lateral counterforce when stabilize key is pressed.
    /// </summary>
    private void ApplyManualStabilize()
    {
        if (!stabilizing) return;

        Vector2 v = rb.linearVelocity;
        if (v.sqrMagnitude <= 1e-6f) return;

        Vector2 fwd = transform.up;
        Vector2 lateral = v - Vector2.Dot(v, fwd) * fwd;
        rb.AddForce(-lateral * (stats.LateralGrip + 4f), ForceMode2D.Force);
    }


    /// <summary>
    /// Applies braking force opposite to velocity when braking is active.
    /// </summary>
    private void ApplyBraking()
    {
        if (!braking) return;

        Vector2 v = rb.linearVelocity;
        if (v.sqrMagnitude <= 1e-6f) return;

        rb.AddForce(-v.normalized * stats.BrakeStrength, ForceMode2D.Force);
    }


    /// <summary>
    /// Applies light drag when idle or braking to gradually slow down.
    /// </summary>
    private void ApplyIdleDrag()
    {
        if (hasInput && !braking) return;

        rb.AddForce(-rb.linearVelocity * stats.LinearDampingIdle, ForceMode2D.Force);
    }


    /// <summary>
    /// Clamps spacecraft velocity so it cannot exceed configured maximum speed.
    /// Boost multiplier is considered.
    /// </summary>
    private void ClampMaxSpeed()
    {
        float top = stats.MaxSpeed * (boosting && stats.BoostType == SpacecraftMovementStats.BoostMode.Afterburner ? stats.BoostMultiplier : 1f);
        if (rb.linearVelocity.magnitude > top)
            rb.linearVelocity = rb.linearVelocity.normalized * top;
    }


    /// <summary>
    /// Returns current boost energy as a normalized value (0–1).
    /// Useful for UI displays.
    /// </summary>
    public float BoostFill01 =>
        Mathf.Approximately(stats.BoostCapacity, 0f) ? 0f : Mathf.Clamp01(boostEnergy / stats.BoostCapacity);


    /// <summary>
    /// Evaluates input and orientation to trigger thruster events
    /// </summary>
    private void FireThrusterEvents()
    {
        // Threshold values
        float rotateEventAngleThreshold = 6f;           // Degrees of error required to trigger rotation
        float lowAngularSpeedThreshold = 0f;            // Angular velocity tolerance for stable state
        float forwardAlignStraightThreshold = 0.85f;    // Dot product threshold for forward alignment

        // No directional input
        if (!hasInput)
        {
            OnNoThrust?.Invoke(this, EventArgs.Empty);
            return;
        }

        // Orientation error relative to input
        float currentAngle = rb.rotation;
        float targetAngle = Mathf.Atan2(targetDir.y, targetDir.x) * Mathf.Rad2Deg - 90f;
        float errorDeg = Mathf.DeltaAngle(currentAngle, targetAngle);

        // Forward alignment and straight condition
        float forwardAlign = Mathf.Max(0f, Vector2.Dot((Vector2)transform.up, targetDir));
        bool isStraight = Mathf.Abs(rb.angularVelocity) < lowAngularSpeedThreshold &&
                          forwardAlign >= forwardAlignStraightThreshold;

        // Moving straight with low angular velocity
        if (isStraight)
        {
            OnThrustForward?.Invoke(this, EventArgs.Empty);
            OnRotateLeft?.Invoke(this, EventArgs.Empty);
            OnRotateRight?.Invoke(this, EventArgs.Empty);
            return;
        }

        // Rotate or thrust forward
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
