using UnityEngine;

/// <summary>
/// Applies steering, thrust, grip, braking, and drag forces to a spacecraft Rigidbody2D.
/// Encapsulates movement-related physics logic.
/// </summary>
public sealed class SteeringAndForces
{
    private const float SPEED_NORM_EPSILON = 0.001f;
    private const float GRIP_EPSILON = 1e-5f;
    private const float SQR_MAGNITUDE_EPSILON = 1e-6f;

    private bool straightLock;

    /// <summary>
    /// Applies turning torque to align the spacecraft toward a target direction.
    /// Includes hysteresis and dead-zone to avoid oscillation.
    /// </summary>
    /// <param name="rb">Rigidbody2D reference.</param>
    /// <param name="stats">Movement stats (turning parameters).</param>
    /// <param name="hasInput">Whether input is active.</param>
    /// <param name="targetDir">Normalized input direction.</param>
    /// <param name="dt">Delta time (seconds).</param>
    public void ApplyTurning(Rigidbody2D rb, SpacecraftMovementStats stats, bool hasInput, Vector2 targetDir, float dt)
    {
        if (!hasInput) { straightLock = false; return; }

        float currentAngle = rb.rotation;
        float targetAngle = Mathf.Atan2(targetDir.y, targetDir.x) * Mathf.Rad2Deg - 90f;
        float error = Mathf.DeltaAngle(currentAngle, targetAngle);

        Vector2 fwd = rb.transform.up;
        float forwardAlign = Mathf.Max(0f, Vector2.Dot(fwd, targetDir));

        // Hysteresis thresholds for entering/exiting "straight lock"
        const float enterAlign = 0.995f;        // alignment threshold (~5.7° cone)
        const float enterErrorFactor = 1.5f;    // angle error multiplier
        float enterError = Mathf.Max(enterErrorFactor, stats.AngleDeadzone * enterErrorFactor);
        const float enterAngVel = 5f;           // max angular velocity (deg/s)

        const float exitAlign = 0.985f;         // alignment threshold (~10° cone)
        const float exitError = 3.0f;           // max allowed angle error (deg)
        const float exitAngVel = 12f;           // max allowed angular velocity (deg/s)

        if (!straightLock)
        {
            if (forwardAlign >= enterAlign &&
                Mathf.Abs(error) <= enterError &&
                Mathf.Abs(rb.angularVelocity) <= enterAngVel)
            {
                straightLock = true;
                return; // perfectly aligned → skip torque
            }
        }
        else
        {
            // Exit lock if alignment is lost beyond thresholds
            if (forwardAlign < exitAlign ||
                Mathf.Abs(error) > exitError ||
                Mathf.Abs(rb.angularVelocity) > exitAngVel)
            {
                straightLock = false;
            }
            else
            {
                return; // stay in lock → no torque
            }
        }

        // Skip if within dead-zone
        if (Mathf.Abs(error) <= stats.AngleDeadzone) return;

        // PD control for angular velocity
        const float minSteerClampFactor = 0.2f;
        float speed = rb.linearVelocity.magnitude;
        float speedNorm = Mathf.Clamp01(speed / Mathf.Max(SPEED_NORM_EPSILON, stats.MaxSpeed));
        float steerClamp = Mathf.Lerp(1f, stats.SteerClampAtTop, speedNorm * stats.HighSpeedStability);
        float maxAngAtSpeed = stats.MaxAngularSpeed * Mathf.Max(minSteerClampFactor, steerClamp);

        float desiredAngVel = Mathf.Clamp(stats.TurnKp * error, -maxAngAtSpeed, maxAngAtSpeed);
        float angVelError = desiredAngVel - rb.angularVelocity;

        float torqueCmd = Mathf.Clamp(angVelError * stats.TurnKd, -stats.TurnTorque, stats.TurnTorque);
        rb.AddTorque(torqueCmd, ForceMode2D.Force);
    }

    /// <summary>
    /// Applies forward thrust based on throttle, alignment, and boost state.
    /// </summary>
    /// <param name="rb">Rigidbody2D reference.</param>
    /// <param name="stats">Movement stats (acceleration and boost).</param>
    /// <param name="hasInput">Whether input is active.</param>
    /// <param name="targetDir">Normalized input direction.</param>
    /// <param name="throttle">Throttle factor [0..1].</param>
    /// <param name="boosting">Whether boost is active.</param>
    public void ApplyThrust(Rigidbody2D rb, SpacecraftMovementStats stats, bool hasInput, Vector2 targetDir, float throttle, bool boosting)
    {
        Vector2 fwd = rb.transform.up;
        float forwardAlign = hasInput ? Mathf.Max(0f, Vector2.Dot(fwd, targetDir)) : 0f;

        float boostMul = 1f;
        if (stats.BoostType == SpacecraftMovementStats.BoostMode.Afterburner && boosting)
            boostMul = stats.BoostMultiplier;

        float thrust = stats.Accel * throttle * forwardAlign * boostMul;
        if (thrust > 0f)
            rb.AddForce(fwd * thrust, ForceMode2D.Force);
    }

    /// <summary>
    /// Applies lateral grip to reduce sideways velocity drift.
    /// </summary>
    /// <param name="rb">Rigidbody2D reference.</param>
    /// <param name="stats">Movement stats (lateral grip).</param>
    /// <param name="hasInput">Whether input is active.</param>
    public void ApplyGrip(Rigidbody2D rb, SpacecraftMovementStats stats, bool hasInput)
    {
        Vector2 v = rb.linearVelocity;
        float speedMag = v.magnitude;
        bool applyGrip = (stats.LateralGrip > 0f) && (!stats.GripOnlyWhenInput || hasInput);
        if (!applyGrip || speedMag <= GRIP_EPSILON) return;

        Vector2 fwd = rb.transform.up;
        Vector2 lateral = v - Vector2.Dot(v, fwd) * fwd;
        float speedNorm = Mathf.Clamp01(speedMag / Mathf.Max(SPEED_NORM_EPSILON, stats.MaxSpeed));
        float grip = stats.LateralGrip * (0.5f + 0.5f * speedNorm * stats.HighSpeedStability);

        rb.AddForce(-lateral * grip, ForceMode2D.Force);
    }

    /// <summary>
    /// Applies additional lateral force to stabilize the spacecraft when player holds stabilize input.
    /// </summary>
    /// <param name="rb">Rigidbody2D reference.</param>
    /// <param name="stats">Movement stats (lateral grip).</param>
    /// <param name="stabilizing">Whether stabilize input is active.</param>
    public void ApplyManualStabilize(Rigidbody2D rb, SpacecraftMovementStats stats, bool stabilizing)
    {
        if (!stabilizing) return;
        Vector2 v = rb.linearVelocity;
        if (v.sqrMagnitude <= SQR_MAGNITUDE_EPSILON) return;

        Vector2 fwd = rb.transform.up;
        Vector2 lateral = v - Vector2.Dot(v, fwd) * fwd;
        const float manualStabilizeExtraGrip = 4f;
        rb.AddForce(-lateral * (stats.LateralGrip + manualStabilizeExtraGrip), ForceMode2D.Force);
    }

    /// <summary>
    /// Applies braking force against current velocity.
    /// </summary>
    /// <param name="rb">Rigidbody2D reference.</param>
    /// <param name="stats">Movement stats (brake strength).</param>
    /// <param name="braking">Whether brake input is active.</param>
    public void ApplyBraking(Rigidbody2D rb, SpacecraftMovementStats stats, bool braking)
    {
        if (!braking) return;
        Vector2 v = rb.linearVelocity;
        if (v.sqrMagnitude <= SQR_MAGNITUDE_EPSILON) return;

        rb.AddForce(-v.normalized * stats.BrakeStrength, ForceMode2D.Force);
    }

    /// <summary>
    /// Applies drag to gradually slow the spacecraft when idle or braking.
    /// </summary>
    /// <param name="rb">Rigidbody2D reference.</param>
    /// <param name="stats">Movement stats (idle drag).</param>
    /// <param name="hasInput">Whether input is active.</param>
    /// <param name="braking">Whether brake input is active.</param>
    public void ApplyIdleDrag(Rigidbody2D rb, SpacecraftMovementStats stats, bool hasInput, bool braking)
    {
        if (hasInput && !braking) return;
        rb.AddForce(-rb.linearVelocity * stats.LinearDampingIdle, ForceMode2D.Force);
    }

    /// <summary>
    /// Clamps the spacecraft's velocity to maximum speed (considering boost mode).
    /// </summary>
    /// <param name="rb">Rigidbody2D reference.</param>
    /// <param name="stats">Movement stats (max speed and boost multipliers).</param>
    /// <param name="boosting">Whether boost is active.</param>
    public void ClampMaxSpeed(Rigidbody2D rb, SpacecraftMovementStats stats, bool boosting)
    {
        float top = stats.MaxSpeed;

        if (stats.BoostType == SpacecraftMovementStats.BoostMode.Afterburner && boosting)
            top *= stats.BoostMultiplier;

        if (stats.BoostType == SpacecraftMovementStats.BoostMode.Dash && boosting)
            top *= Mathf.Max(1f, stats.DashOverspeedMultiplier);

        if (rb.linearVelocity.magnitude > top)
            rb.linearVelocity = rb.linearVelocity.normalized * top;
    }
}
