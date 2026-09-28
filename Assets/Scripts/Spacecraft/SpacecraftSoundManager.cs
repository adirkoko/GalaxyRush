using System;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SpacecraftSoundManager : MonoBehaviour
{
    [Header("Audio Clips")]
    [SerializeField] private AudioClip engineLoop;
    [SerializeField] private AudioClip idleHum;

    [Header("Base Settings")]
    [SerializeField, Range(0f, 1f)] private float baseVolume = 0.6f;
    [SerializeField, Range(0.1f, 3f)] private float basePitch = 1.0f;
    [SerializeField, Range(0f, 1f)] private float idleHumVolume = 0.2f;

    [Header("Boost Multipliers")]
    [SerializeField] private float boostVolumeMul = 2.0f;
    [SerializeField] private float boostPitchMul = 1.4f;

    [Header("Rotate Multipliers")]
    [SerializeField] private float rotateVolumeMul = 0.4f;
    [SerializeField] private float rotatePitchMul = 0.8f;

    private AudioSource engineSource;
    private AudioSource idleSource;
    private SpacecraftManager manager;

    private enum EngineState { Idle, Forward, Rotate }
    private EngineState engineState = EngineState.Idle;
    private bool isBoosting; // Boost is tracked separately so it survives thrust/rotate changes

    private void Awake()
    {
        // Use the required AudioSource instead of adding a second one
        engineSource = GetComponent<AudioSource>();
        engineSource.loop = true;
        engineSource.playOnAwake = false;
        engineSource.clip = engineLoop;
    }

    private void Start()
    {
        manager = GetComponentInParent<SpacecraftManager>();
        if (manager == null)
        {
            Debug.LogError("SpacecraftSoundManager: No SpacecraftManager found in parent!");
            enabled = false;
            return;
        }

        SubscribeToEvents();

        if (idleHum != null)
        {
            idleSource = gameObject.AddComponent<AudioSource>();
            idleSource.clip = idleHum;
            idleSource.loop = true;
            idleSource.volume = idleHumVolume;
            idleSource.Play();
        }
    }

    private void OnDestroy()
    {
        if (manager == null) return;

        manager.OnThrustForward -= HandleThrustForward;
        manager.OnRotateLeft -= HandleRotate;
        manager.OnRotateRight -= HandleRotate;
        manager.OnNoThrust -= HandleNoThrust;
        manager.OnBoostStart -= HandleBoostStart;
        manager.OnBoostEnd -= HandleBoostEnd;
    }

    private void SubscribeToEvents()
    {
        manager.OnThrustForward += HandleThrustForward;
        manager.OnRotateLeft += HandleRotate;
        manager.OnRotateRight += HandleRotate;
        manager.OnNoThrust += HandleNoThrust;
        manager.OnBoostStart += HandleBoostStart;
        manager.OnBoostEnd += HandleBoostEnd;
    }

    private void HandleThrustForward(object sender, EventArgs e) => SetEngineState(EngineState.Forward);
    private void HandleRotate(object sender, EventArgs e) => SetEngineState(EngineState.Rotate);
    private void HandleNoThrust(object sender, EventArgs e) => SetEngineState(EngineState.Idle);

    private void HandleBoostStart(object sender, EventArgs e)
    {
        isBoosting = true;
        RefreshEngineSound();
    }

    private void HandleBoostEnd(object sender, EventArgs e)
    {
        isBoosting = false;
        RefreshEngineSound();
    }

    private void SetEngineState(EngineState state)
    {
        // Thruster events arrive every physics tick; only react to actual changes
        if (engineState == state) return;
        engineState = state;
        RefreshEngineSound();
    }

    /// <summary>
    /// Applies volume/pitch for the current engine state and boost flag.
    /// Boost only amplifies the engine while it is already running.
    /// </summary>
    private void RefreshEngineSound()
    {
        if (engineLoop == null) return;

        if (engineState == EngineState.Idle)
        {
            if (engineSource.isPlaying) engineSource.Stop();
            return;
        }

        float volume = baseVolume;
        float pitch = basePitch;

        if (engineState == EngineState.Rotate)
        {
            volume *= rotateVolumeMul;
            pitch *= rotatePitchMul;
        }

        if (isBoosting)
        {
            volume *= boostVolumeMul;
            pitch *= boostPitchMul;
        }

        engineSource.volume = volume;
        engineSource.pitch = pitch;
        if (!engineSource.isPlaying) engineSource.Play();
    }
}
