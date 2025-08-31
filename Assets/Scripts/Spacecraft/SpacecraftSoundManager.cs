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

    [Header("Boost Multipliers")]
    [SerializeField] private float boostVolumeMul = 2.0f;
    [SerializeField] private float boostPitchMul = 1.4f;

    [Header("Rotate Multipliers")]
    [SerializeField] private float rotateVolumeMul = 0.4f;
    [SerializeField] private float rotatePitchMul = 0.8f;

    private AudioSource engineSource;
    private SpacecraftManager manager;

    private enum EngineState { Idle, Forward, Rotate, Boost }
    private EngineState currentState = EngineState.Idle;
    private EngineState prevState = EngineState.Idle;

    private void Awake()
    {
        engineSource = gameObject.AddComponent<AudioSource>();
        engineSource.loop = true;
        engineSource.playOnAwake = false;
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

        if (engineLoop != null)
        {
            engineSource.clip = engineLoop;
            engineSource.volume = baseVolume;
            engineSource.pitch = basePitch;
            engineSource.Stop();
        }

        if (idleHum != null)
        {
            AudioSource idle = gameObject.AddComponent<AudioSource>();
            idle.clip = idleHum;
            idle.loop = true;
            idle.volume = 0.2f;
            idle.Play();
        }
    }

    private void SubscribeToEvents()
    {
        manager.OnThrustForward += (s, e) => ApplyForwardSound();
        manager.OnRotateLeft += (s, e) => ApplyRotateSound();
        manager.OnRotateRight += (s, e) => ApplyRotateSound();
        manager.OnNoThrust += (s, e) => StopEngineLoop();

        manager.OnBoostStart += (s, e) => ApplyBoostSound();
        manager.OnBoostEnd += (s, e) => EndBoostSound();
    }

    private void ApplyForwardSound()
    {
        if (currentState == EngineState.Boost) return; // אל תדרוס בוסט
        currentState = EngineState.Forward;

        engineSource.volume = baseVolume;
        engineSource.pitch = basePitch;
        if (!engineSource.isPlaying && engineLoop != null)
            engineSource.Play();
    }

    private void ApplyRotateSound()
    {
        if (currentState == EngineState.Boost) return; // אל תדרוס בוסט
        currentState = EngineState.Rotate;

        engineSource.volume = baseVolume * rotateVolumeMul;
        engineSource.pitch = basePitch * rotatePitchMul;
        if (!engineSource.isPlaying && engineLoop != null)
            engineSource.Play();
    }

    private void StopEngineLoop()
    {
        if (engineSource.isPlaying)
            engineSource.Stop();
        currentState = EngineState.Idle;
    }

    private void ApplyBoostSound()
    {
        if (currentState != EngineState.Boost)
            prevState = currentState; // Save previous state

        currentState = EngineState.Boost;

        // Only amplify if engine was already active
        if (prevState == EngineState.Forward || prevState == EngineState.Rotate)
        {
            engineSource.volume = baseVolume * boostVolumeMul;
            engineSource.pitch = basePitch * boostPitchMul;

            if (!engineSource.isPlaying && engineLoop != null)
                engineSource.Play();
        }
    }


    private void EndBoostSound()
    {
        currentState = prevState;

        switch (prevState)
        {
            case EngineState.Forward:
                ApplyForwardSound();
                break;
            case EngineState.Rotate:
                ApplyRotateSound();
                break;
            case EngineState.Idle:
                break;
        }
    }

}
