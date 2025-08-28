using UnityEngine;

/// <summary>
/// Controls thruster particle effects in response to spacecraft movement events.
/// Supports boost mode where thrusters flare up (larger, faster, denser particles),
/// and restores original values when boost ends.
/// </summary>
public class SpacecraftVisuals : MonoBehaviour
{
    [Header("Thruster Particle Systems")]
    [SerializeField] private ParticleSystem LeftThrusterParticleSystem;    // Left thruster
    [SerializeField] private ParticleSystem CenterThrusterParticleSystem;  // Center thruster
    [SerializeField] private ParticleSystem RightThrusterParticleSystem;   // Right thruster

    private SpacecraftManager manager; // Reference to the spacecraft manager

    /// <summary>
    /// Data structure for storing the default particle system values
    /// (so we can restore them after boost ends).
    /// </summary>
    [System.Serializable]
    private struct ThrusterDefaults
    {
        public ParticleSystem.MinMaxCurve rate;
        public ParticleSystem.MinMaxCurve size;
        public ParticleSystem.MinMaxCurve speed;
    }

    // Default values for each thruster
    private ThrusterDefaults leftDefaults;
    private ThrusterDefaults centerDefaults;
    private ThrusterDefaults rightDefaults;

    /// <summary>
    /// Initialize references, cache default thruster values,
    /// and subscribe to spacecraft manager events.
    /// </summary>
    private void Start()
    {
        manager = GetComponentInParent<SpacecraftManager>();
        CacheDefaults();
        SubscribeToEvents();
    }

    /// <summary>
    /// Subscribes to movement and boost events from the manager.
    /// </summary>
    private void SubscribeToEvents()
    {
        if (manager != null)
        {
            // Movement events to control which thrusters fire
            manager.OnThrustForward += (s, e) => EnableThrusters(true, true, true);
            manager.OnRotateLeft += (s, e) => EnableThrusters(false, false, true);
            manager.OnRotateRight += (s, e) => EnableThrusters(true, false, false);
            manager.OnNoThrust += (s, e) => EnableThrusters(false, false, false);

            // Boost events to flare up thrusters, then reset
            manager.OnBoostStart += (s, e) => BoostThrusters();
            manager.OnBoostEnd += (s, e) => ResetThrusters();
        }
        else
        {
            Debug.LogError("SpacecraftVisuals: No SpacecraftManager found in parent!");
        }
    }

    /// <summary>
    /// Reads and stores the default particle system settings from the inspector
    /// so they can be restored later.
    /// </summary>
    private void CacheDefaults()
    {
        leftDefaults = GetDefaults(LeftThrusterParticleSystem);
        centerDefaults = GetDefaults(CenterThrusterParticleSystem);
        rightDefaults = GetDefaults(RightThrusterParticleSystem);
    }

    /// <summary>
    /// Helper method to capture the default emission rate, particle size,
    /// and particle speed values from a given thruster particle system.
    /// </summary>
    /// <param name="ps">The particle system from which to read default values.</param>
    /// <returns>A <see cref="ThrusterDefaults"/> struct containing the captured values.</returns>
    private ThrusterDefaults GetDefaults(ParticleSystem ps)
    {
        var main = ps.main;
        var emission = ps.emission;

        return new ThrusterDefaults
        {
            rate = emission.rateOverTime,
            size = main.startSize,
            speed = main.startSpeed
        };
    }


    /// <summary>
    /// Enables or disables emission of a specific thruster particle system.
    /// </summary>
    /// <param name="particleSystem">The thruster particle system to modify.</param>
    /// <param name="isEnabled">True to enable emission, false to disable.</param>
    private void SetThrusterEmission(ParticleSystem particleSystem, bool isEnabled)
    {
        if (particleSystem == null) return;
        ParticleSystem.EmissionModule emissionModule = particleSystem.emission;
        emissionModule.enabled = isEnabled;
    }

    /// <summary>
    /// Enables or disables multiple thrusters at once based on the provided flags.
    /// </summary>
    /// <param name="left">If true, enables the left thruster; otherwise disables it.</param>
    /// <param name="center">If true, enables the center thruster; otherwise disables it.</param>
    /// <param name="right">If true, enables the right thruster; otherwise disables it.</param>
    private void EnableThrusters(bool left, bool center, bool right)
    {
        SetThrusterEmission(LeftThrusterParticleSystem, left);
        SetThrusterEmission(CenterThrusterParticleSystem, center);
        SetThrusterEmission(RightThrusterParticleSystem, right);
    }


    /// <summary>
    /// Applies boosted settings to all thrusters
    /// (higher emission, larger particles, faster speed).
    /// </summary>
    private void BoostThrusters()
    {
        AmplifyThruster(LeftThrusterParticleSystem, leftDefaults, 2f, 2f, 3f);
        AmplifyThruster(CenterThrusterParticleSystem, centerDefaults, 2f, 2f, 3f);
        AmplifyThruster(RightThrusterParticleSystem, rightDefaults, 2f, 2f, 3f);
    }

    /// <summary>
    /// Restores all thrusters to their cached default values
    /// (values from the inspector at startup).
    /// </summary>
    private void ResetThrusters()
    {
        ApplyDefaults(LeftThrusterParticleSystem, leftDefaults);
        ApplyDefaults(CenterThrusterParticleSystem, centerDefaults);
        ApplyDefaults(RightThrusterParticleSystem, rightDefaults);
    }

    /// <summary>
    /// Amplifies one thruster's visual intensity by multiplying its default
    /// emission rate, particle size, and speed with provided multipliers.
    /// </summary>
    /// <param name="ps">The thruster particle system to modify.</param>
    /// <param name="defaults">The cached default values to scale from.</param>
    /// <param name="rateMul">Multiplier for emission rate.</param>
    /// <param name="sizeMul">Multiplier for particle size.</param>
    /// <param name="speedMul">Multiplier for particle speed.</param>
    private void AmplifyThruster(ParticleSystem ps, ThrusterDefaults defaults, float rateMul, float sizeMul, float speedMul)
    {
        if (ps == null) return;

        var emission = ps.emission;
        emission.rateOverTime = defaults.rate.constant * rateMul;

        var main = ps.main;
        main.startSize = defaults.size.constant * sizeMul;
        main.startSpeed = defaults.speed.constant * speedMul;
    }



    /// <summary>
    /// Restores one thruster's visual settings (emission rate, size, and speed)
    /// using previously cached default values.
    /// </summary>
    /// <param name="ps">The thruster particle system to restore.</param>
    /// <param name="defaults">The cached default values to reapply.</param>
    private void ApplyDefaults(ParticleSystem ps, ThrusterDefaults defaults)
    {
        if (ps == null) return;

        var emission = ps.emission;
        emission.rateOverTime = defaults.rate;

        var main = ps.main;
        main.startSize = defaults.size;
        main.startSpeed = defaults.speed;
    }

}
