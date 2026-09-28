using System;
using UnityEngine;

/// <summary>
/// Handles boost mechanics for spacecraft movement, including Afterburner and Dash modes.
/// Manages boost energy, activation rules, and boost-related events.
/// </summary>
public sealed class BoostController
{
    private const float MIN_BOOST_CAPACITY_EPSILON = 0.001f;

    private readonly Rigidbody2D rb;
    private SpacecraftMovementStats stats;

    private float energy;
    private bool boosting;
    private bool locked;              // Lock state for Afterburner mode
    private bool pendingDash;         // True if a dash impulse is scheduled
    private float pendingDashImpulse; // Dash impulse force to be applied

    #region Events

    /// <summary> Invoked when boost starts. </summary>
    public event Action OnBoostStart;

    /// <summary> Invoked when boost ends. </summary>
    public event Action OnBoostEnd;

    /// <summary> Invoked when a dash is fired (charge, impulse, speed before dash). </summary>
    public event Action<float, float, float> OnDashFired;

    /// <summary> Invoked when boost activation is denied (mode, required fraction, current fraction). </summary>
    public event Action<SpacecraftMovementStats.BoostMode, float, float> OnBoostDenied;

    #endregion

    /// <summary>
    /// Creates a new <see cref="BoostController"/> for a given Rigidbody2D.
    /// </summary>
    /// <param name="rbRef">Reference to the spacecraft's rigidbody.</param>
    public BoostController(Rigidbody2D rbRef) => rb = rbRef;

    /// <summary>
    /// Applies new stats and initializes boost energy.
    /// </summary>
    /// <param name="s">The stats to apply.</param>
    public void ApplyStats(SpacecraftMovementStats s)
    {
        stats = s;
        energy = (stats.BoostType == SpacecraftMovementStats.BoostMode.Afterburner)
            ? stats.BoostCapacity
            : stats.DashChargeCapacity;

        boosting = false;
        locked = false;
        pendingDash = false;
        pendingDashImpulse = 0f;
    }

    /// <summary>
    /// Whether boost is currently active.
    /// </summary>
    public bool IsBoosting => boosting;

    /// <summary>
    /// Current available boost energy (in units).
    /// </summary>
    public float EnergyUnits => energy;

    /// <summary>
    /// Maximum boost capacity (in units).
    /// </summary>
    public float CapacityUnits =>
        stats == null ? 0f :
        stats.BoostType == SpacecraftMovementStats.BoostMode.Afterburner
            ? stats.BoostCapacity
            : stats.DashChargeCapacity;

    /// <summary>
    /// Updates boost logic. Should be called every frame.
    /// </summary>
    /// <param name="dt">Delta time (seconds).</param>
    /// <param name="isBoostHeld">Delegate returning true if boost key is held.</param>
    /// <param name="wasBoostPressedThisFrame">Delegate returning true if boost key was pressed this frame.</param>
    public void Tick(float dt, Func<bool> isBoostHeld, Func<bool> wasBoostPressedThisFrame)
    {
        if (stats == null) return;

        if (stats.BoostType == SpacecraftMovementStats.BoostMode.Afterburner)
            TickAfterburner(dt, isBoostHeld, wasBoostPressedThisFrame);
        else
            TickDash(dt, wasBoostPressedThisFrame);
    }

    /// <summary>
    /// Applies deferred dash impulse, if one was triggered. Should be called from FixedUpdate.
    /// </summary>
    /// <param name="forward">The forward vector of the spacecraft.</param>
    public void FixedTick(Vector2 forward)
    {
        if (pendingDash)
        {
            rb.AddForce(forward * pendingDashImpulse, ForceMode2D.Impulse);
            pendingDash = false;
        }
    }

    /// <summary>
    /// Afterburner update logic: handles energy use, regen, and lock/unlock conditions.
    /// </summary>
    /// <param name="dt">Delta time (seconds).</param>
    /// <param name="isHeld">Delegate returning true if boost key is held.</param>
    /// <param name="wasPressed">Delegate returning true if boost key was pressed this frame.</param>
    private void TickAfterburner(float dt, Func<bool> isHeld, Func<bool> wasPressed)
    {
        bool keyDown = isHeld != null && isHeld();

        if (wasPressed != null && wasPressed())
        {
            bool deny = locked || energy <= 0f || energy < stats.BoostCapacity * stats.BoostMinFractionToActivate;
            if (deny)
            {
                float req = stats.BoostMinFractionToActivate;
                float cur = Mathf.Approximately(stats.BoostCapacity, 0f) ? 0f : (energy / stats.BoostCapacity);
                OnBoostDenied?.Invoke(SpacecraftMovementStats.BoostMode.Afterburner, req, cur);
            }
        }

        bool was = boosting;

        // Deactivate if energy depleted
        if (boosting && energy <= 0f)
        {
            locked = true;
            boosting = false;
        }

        // Unlock when minimum fraction regained
        if (locked && energy >= stats.BoostCapacity * stats.BoostMinFractionToActivate)
            locked = false;

        boosting = (!locked && keyDown && energy > 0f);

        // Energy consumption/regeneration
        if (boosting)
            energy = Mathf.Max(0f, energy - stats.BoostUsePerSec * dt);
        else
            energy = Mathf.Min(stats.BoostCapacity, energy + stats.BoostRegenPerSec * dt);

        // Fire events on state change
        if (boosting && !was) OnBoostStart?.Invoke();
        if (!boosting && was) OnBoostEnd?.Invoke();
    }

    /// <summary>
    /// Dash update logic: handles charge regen and dash triggering.
    /// </summary>
    /// <param name="dt">Delta time (seconds).</param>
    /// <param name="wasPressed">Delegate returning true if boost key was pressed this frame.</param>
    private void TickDash(float dt, Func<bool> wasPressed)
    {
        // Regenerate dash charge
        if (!boosting && energy < stats.DashChargeCapacity)
            energy = Mathf.Min(stats.DashChargeCapacity, energy + stats.DashChargeRegenPerSec * dt);

        if (wasPressed == null || !wasPressed()) return;

        float minToActivate = stats.DashChargeCapacity * stats.DashMinFractionToActivate;
        if (energy < minToActivate)
        {
            float curFrac = Mathf.Approximately(stats.DashChargeCapacity, 0f) ? 0f : (energy / stats.DashChargeCapacity);
            OnBoostDenied?.Invoke(SpacecraftMovementStats.BoostMode.Dash, stats.DashMinFractionToActivate, curFrac);
            return;
        }

        // Perform dash
        float charge01 = Mathf.Clamp01(energy / Mathf.Max(MIN_BOOST_CAPACITY_EPSILON, stats.DashChargeCapacity));
        float impulse = Mathf.Lerp(stats.DashImpulseMin, stats.DashImpulseMax, charge01);
        float speedBefore = rb.linearVelocity.magnitude;

        pendingDash = true;
        pendingDashImpulse = impulse;

        energy = 0f;
        boosting = true;
        OnBoostStart?.Invoke();
        OnDashFired?.Invoke(charge01, impulse, speedBefore);
    }

    /// <summary>
    /// Ends the dash window, signaling dash completion.
    /// </summary>
    public void EndDashWindow()
    {
        if (boosting)
        {
            boosting = false;
            OnBoostEnd?.Invoke();
        }
    }
}
