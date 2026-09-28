using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpacecraftMovement))]
public class SpacecraftDataRecorder : MonoBehaviour
{
    // References
    private SpacecraftMovement movement;
    private Rigidbody2D rb;

    // -------- Session Metrics (public read-only) --------
    public float SessionTime { get; private set; }
    public float TotalDistance { get; private set; }
    public float PeakSpeed { get; private set; }
    public float TimeMoving { get; private set; }
    public float TimeIdle { get; private set; }
    public float TimeWithInput { get; private set; }
    public float TimeWithoutInput { get; private set; }

    // Generic Boost (kept for backward-compat)
    public float TimeInBoost { get; private set; }
    public int BoostActivations { get; private set; }

    // Braking
    public float TimeBraking { get; private set; }
    public int BrakeCount { get; private set; }

    // Stabilize
    public float TimeStabilizing { get; private set; }
    public int StabilizeCount { get; private set; }

    // Collisions
    public int CollisionCount { get; private set; }
    public float PeakCollisionRelativeSpeed { get; private set; }

    // Derived
    public float AverageSpeed => SessionTime > 0f ? TotalDistance / SessionTime : 0f;
    public float MovingRatio => SessionTime > 0f ? TimeMoving / SessionTime : 0f;

    // -------- New Detailed Metrics --------
    [Header("Detailed Boost Metrics")]
    // Afterburner-specific
    public float TimeInAfterburner { get; private set; }
    public int AfterburnerActivations { get; private set; }
    public float AfterburnerEnergySpent { get; private set; }       // in movement "units"
    public float DistanceDuringAfterburner { get; private set; }    // meters

    // Dash-specific
    public int DashCount { get; private set; }
    public float DashChargeSum01 { get; private set; }              // sum of charge fractions
    public float DashImpulseSum { get; private set; }               // total impulse applied
    public float DashDeltaVSum { get; private set; }                // sum of (speedAfter - speedBefore)
    public float DashDeltaVPeak { get; private set; }               // best deltaV observed
    public int DashDeniedCount { get; private set; }
    public int AfterburnerDeniedCount { get; private set; }

    // -------- Config --------
    [Header("Config")]
    [SerializeField] private float movingSpeedEpsilon = 0.2f;
    [SerializeField] private bool autoStartSession = true;
    [SerializeField] private string autoSaveJsonFileName = "";

    // -------- Internals --------
    private bool sessionActive;
    private Vector2 lastPos;
    private bool isBoosting;

    private bool brakePrev, stabilizePrev;

    // For energy tracking (Afterburner)
    private float prevBoostEnergyUnits;

    // For dash Δv collection (might queue multiple presses before next physics tick)
    private readonly Queue<float> pendingDashSpeedBefore = new Queue<float>();

    private void Awake()
    {
        movement = GetComponent<SpacecraftMovement>();
        rb = GetComponentInParent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError("SpacecraftDataRecorder: No Rigidbody2D found in parent! Disabling.");
            enabled = false;
            return;
        }
    }

    private void OnEnable()
    {
        if (movement != null)
        {
            movement.OnBoostStart += HandleBoostStart;
            movement.OnBoostEnd += HandleBoostEnd;

            // New analytics hooks
            movement.OnDashFired += HandleDashFired;
            movement.OnBoostDenied += HandleBoostDenied;
        }
    }

    private void OnDisable()
    {
        if (movement != null)
        {
            movement.OnBoostStart -= HandleBoostStart;
            movement.OnBoostEnd -= HandleBoostEnd;

            movement.OnDashFired -= HandleDashFired;
            movement.OnBoostDenied -= HandleBoostDenied;
        }

        if (!string.IsNullOrWhiteSpace(autoSaveJsonFileName) && SessionTime > 0f)
        {
            try
            {
                SaveJson(autoSaveJsonFileName);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"SpacecraftDataRecorder: Failed to save JSON. {e.Message}");
            }
        }
    }

    private void Start()
    {
        if (autoStartSession) StartSession();
    }

    public void StartSession()
    {
        ResetAll();
        sessionActive = true;
        lastPos = rb.position;

        // Initialize prev energy for Afterburner tracking
        prevBoostEnergyUnits = movement != null ? movement.BoostEnergyUnits : 0f;
    }

    public void EndSession()
    {
        sessionActive = false;
    }

    private void Update()
    {
        if (!sessionActive || rb == null || GameInput.Instance == null) return;
        float dt = Time.deltaTime;
        SessionTime += dt;

        // Input reconstruction (same as before)
        Vector2 moveVec = GameInput.Instance.GetMoveVector();
        bool hasInput = moveVec.sqrMagnitude > 0f;
        bool brakeDown = GameInput.Instance.IsBrakeActionPressed();
        bool stabilizeDown = GameInput.Instance.IsStabilizeActionPressed();

        if (hasInput) TimeWithInput += dt; else TimeWithoutInput += dt;
        if (isBoosting) TimeInBoost += dt;
        if (brakeDown) TimeBraking += dt;
        if (stabilizeDown) TimeStabilizing += dt;

        if (brakeDown && !brakePrev) BrakeCount++;
        if (stabilizeDown && !stabilizePrev) StabilizeCount++;

        brakePrev = brakeDown;
        stabilizePrev = stabilizeDown;

        // Split Afterburner time specifically
        if (movement != null &&
            movement.CurrentBoostMode == SpacecraftMovementStats.BoostMode.Afterburner &&
            movement.IsBoosting)
        {
            TimeInAfterburner += dt;
        }

        // Track Afterburner energy consumption (difference between frames)
        if (movement != null &&
            movement.CurrentBoostMode == SpacecraftMovementStats.BoostMode.Afterburner)
        {
            float cur = movement.BoostEnergyUnits;
            float delta = prevBoostEnergyUnits - cur; // positive when consuming
            if (delta > 0f && movement.IsBoosting)
                AfterburnerEnergySpent += delta;

            prevBoostEnergyUnits = cur;
        }
    }

    private void FixedUpdate()
    {
        if (!sessionActive || rb == null) return;

        // Distance / speed peaks (unchanged)
        Vector2 p = rb.position;
        float seg = Vector2.Distance(p, lastPos);
        TotalDistance += seg;

        // Distance during Afterburner only
        if (movement != null &&
            movement.CurrentBoostMode == SpacecraftMovementStats.BoostMode.Afterburner &&
            movement.IsBoosting)
        {
            DistanceDuringAfterburner += seg;
        }

        lastPos = p;

        float speed = rb.linearVelocity.magnitude;
        if (speed > PeakSpeed) PeakSpeed = speed;

        float dt = Time.fixedDeltaTime;
        if (speed > movingSpeedEpsilon) TimeMoving += dt;
        else TimeIdle += dt;

        // Dash Δv: compute using first physics tick after dash
        // For N pending dashes that happened during last frame, we process them here.
        // NOTE: This approximates each Δv with the current speed; good enough for telem.
        while (pendingDashSpeedBefore.Count > 0)
        {
            float before = pendingDashSpeedBefore.Dequeue();
            float after = rb.linearVelocity.magnitude;
            float deltaV = after - before;
            DashDeltaVSum += deltaV;
            if (deltaV > DashDeltaVPeak) DashDeltaVPeak = deltaV;
        }
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        CollisionCount++;
        float rel = col.relativeVelocity.magnitude;
        if (rel > PeakCollisionRelativeSpeed) PeakCollisionRelativeSpeed = rel;
    }

    private void HandleBoostStart(object s, EventArgs e)
    {
        isBoosting = true;
        BoostActivations++;

        // Count Afterburner activations specifically
        if (movement != null &&
            movement.CurrentBoostMode == SpacecraftMovementStats.BoostMode.Afterburner)
        {
            AfterburnerActivations++;
        }
    }

    private void HandleBoostEnd(object s, EventArgs e)
    {
        isBoosting = false;
    }

    // ----- New handlers -----

    private void HandleDashFired(float charge01, float impulse, float speedBefore)
    {
        DashCount++;
        DashChargeSum01 += Mathf.Clamp01(charge01);
        DashImpulseSum += Mathf.Max(0f, impulse);

        // queue Δv measurement for next physics step
        pendingDashSpeedBefore.Enqueue(Mathf.Max(0f, speedBefore));
    }

    private void HandleBoostDenied(SpacecraftMovementStats.BoostMode mode, float requiredFrac, float currentFrac)
    {
        if (mode == SpacecraftMovementStats.BoostMode.Dash) DashDeniedCount++;
        else AfterburnerDeniedCount++;
    }

    // ----- Utilities -----
    public void ResetAll()
    {
        SessionTime = 0f;
        TotalDistance = 0f;
        PeakSpeed = 0f;
        TimeMoving = 0f;
        TimeIdle = 0f;
        TimeWithInput = 0f;
        TimeWithoutInput = 0f;

        TimeInBoost = 0f;
        BoostActivations = 0;

        TimeBraking = 0f;
        BrakeCount = 0;

        TimeStabilizing = 0f;
        StabilizeCount = 0;

        CollisionCount = 0;
        PeakCollisionRelativeSpeed = 0f;

        // Detailed
        TimeInAfterburner = 0f;
        AfterburnerActivations = 0;
        AfterburnerEnergySpent = 0f;
        DistanceDuringAfterburner = 0f;

        DashCount = 0;
        DashChargeSum01 = 0f;
        DashImpulseSum = 0f;
        DashDeltaVSum = 0f;
        DashDeltaVPeak = 0f;
        DashDeniedCount = 0;
        AfterburnerDeniedCount = 0;

        isBoosting = false;
        brakePrev = false;
        stabilizePrev = false;

        pendingDashSpeedBefore.Clear();
        prevBoostEnergyUnits = movement != null ? movement.BoostEnergyUnits : 0f;
    }

    [Serializable]
    public struct Snapshot
    {
        // Existing summary
        public float sessionTime;
        public float totalDistance;
        public float peakSpeed;
        public float averageSpeed;
        public float timeMoving;
        public float timeIdle;
        public float timeWithInput;
        public float timeWithoutInput;

        public float timeInBoost;
        public int boostActivations;

        public float timeBraking;
        public int brakeCount;

        public float timeStabilizing;
        public int stabilizeCount;

        public int collisionCount;
        public float peakCollisionRelativeSpeed;
        public float movingRatio;

        // New detailed
        public float timeInAfterburner;
        public int afterburnerActivations;
        public float afterburnerEnergySpent;
        public float distanceDuringAfterburner;

        public int dashCount;
        public float dashChargeAvg01;
        public float dashImpulseAvg;
        public float dashDeltaVAvg;
        public float dashDeltaVPeak;
        public int dashDeniedCount;
        public int afterburnerDeniedCount;
    }

    public Snapshot GetSnapshot()
    {
        return new Snapshot
        {
            // Existing
            sessionTime = SessionTime,
            totalDistance = TotalDistance,
            peakSpeed = PeakSpeed,
            averageSpeed = AverageSpeed,
            timeMoving = TimeMoving,
            timeIdle = TimeIdle,
            timeWithInput = TimeWithInput,
            timeWithoutInput = TimeWithoutInput,

            timeInBoost = TimeInBoost,
            boostActivations = BoostActivations,

            timeBraking = TimeBraking,
            brakeCount = BrakeCount,

            timeStabilizing = TimeStabilizing,
            stabilizeCount = StabilizeCount,

            collisionCount = CollisionCount,
            peakCollisionRelativeSpeed = PeakCollisionRelativeSpeed,
            movingRatio = MovingRatio,

            // Detailed
            timeInAfterburner = TimeInAfterburner,
            afterburnerActivations = AfterburnerActivations,
            afterburnerEnergySpent = AfterburnerEnergySpent,
            distanceDuringAfterburner = DistanceDuringAfterburner,

            dashCount = DashCount,
            dashChargeAvg01 = DashCount > 0 ? DashChargeSum01 / DashCount : 0f,
            dashImpulseAvg = DashCount > 0 ? DashImpulseSum / DashCount : 0f,
            dashDeltaVAvg = DashCount > 0 ? DashDeltaVSum / DashCount : 0f,
            dashDeltaVPeak = DashDeltaVPeak,
            dashDeniedCount = DashDeniedCount,
            afterburnerDeniedCount = AfterburnerDeniedCount
        };
    }


    public string ToJson(bool prettyPrint = true) => JsonUtility.ToJson(GetSnapshot(), prettyPrint);

    public void SaveJson(string fileName)
    {
        var safeFileName = fileName.EndsWith(".json") ? fileName : fileName + ".json";

        var json = ToJson(true);
        var path = Path.Combine(Application.persistentDataPath, safeFileName);
        File.WriteAllText(path, json);
        Debug.Log($"SpacecraftDataRecorder: Saved analytics JSON to: {path}");
    }
}
