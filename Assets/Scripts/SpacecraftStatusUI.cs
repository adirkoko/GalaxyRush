using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

[DisallowMultipleComponent]
public class SpacecraftStatusUI : MonoBehaviour
{
    public enum EnergyDisplayMode { Percent, ValueOutOfMax }

    [Header("References")]
    [SerializeField] private SpacecraftManager manager;
    [SerializeField] private Gradient energyGradient;
    [SerializeField] private Slider energySlider;
    [SerializeField] private TMP_Text energyText;
    [SerializeField] private TMP_Text speedText;
    [SerializeField] private Image boostIcon;
    [SerializeField] private Sprite afterburnerSprite;
    [SerializeField] private Sprite dashSprite;

    [Header("Presentation")]
    [SerializeField, Range(0f, 30f)] private float uiSmoothing = 12f;
    [SerializeField] private string speedFormat = "{0:0.0} m/s";
    [SerializeField] private Color afterburnerColor = new Color(0f, 0.9f, 1f);
    [SerializeField] private Color dashColor = new Color(1f, 0.2f, 0.9f);
    [SerializeField] private Color boostFlashColor = Color.white;
    [SerializeField] private float boostFlashDuration = 0.12f;

    [Header("Energy Display")]
    [SerializeField] private EnergyDisplayMode energyDisplayMode = EnergyDisplayMode.ValueOutOfMax;
    [SerializeField] private string percentFormat = "{0:0}%";
    [SerializeField] private string valueFormat = "{0:0}/{1:0}"; 

    private const float DenominatorEpsilon = 0.0001f;

    private float energyUI;
    private float speedUI;
    private SpacecraftMovementStats.BoostMode? lastMode;
    private float flashTimer;
    private Color baseIconColor;

    private void Start()
    {
        if (manager == null) manager = GetComponentInParent<SpacecraftManager>();
        if (boostIcon != null) baseIconColor = boostIcon.color;
    }

    private void OnEnable()
    {
        if (manager != null)
        {
            manager.OnBoostStart += HandleBoostStart;
            manager.OnBoostEnd += HandleBoostEnd;
        }
    }

    private void OnDisable()
    {
        if (manager != null)
        {
            manager.OnBoostStart -= HandleBoostStart;
            manager.OnBoostEnd -= HandleBoostEnd;
        }
    }

    private void Update()
    {
        if (manager == null) return;

        // --- Energy ---
        float cap = Mathf.Max(DenominatorEpsilon, manager.BoostCapacityUnits);
        float cur = Mathf.Clamp(manager.BoostEnergyUnits, 0f, cap);
        float fill = cap > 0f ? cur / cap : 0f;

        energyUI = Smooth(energyUI, fill, uiSmoothing, Time.deltaTime);

        if (energySlider != null)
        {
            energySlider.value = energyUI;

            // Tint the slider fill by the energy gradient
            if (energySlider.fillRect != null)
            {
                var fillImage = energySlider.fillRect.GetComponent<Image>();
                if (fillImage != null)
                {
                    fillImage.color = energyGradient.Evaluate(energyUI);
                }
            }
        }

        if (energyText != null)
        {
            switch (energyDisplayMode)
            {
                case EnergyDisplayMode.Percent:
                    float percent = (cap > 0f) ? (cur / cap) * 100f : 0f;
                    energyText.text = string.Format(percentFormat, percent);
                    break;

                case EnergyDisplayMode.ValueOutOfMax:
                    energyText.text = string.Format(valueFormat, cur, cap);
                    break;
            }
        }

        // --- Speed ---
        speedUI = Smooth(speedUI, manager.CurrentSpeed, uiSmoothing, Time.deltaTime);
        if (speedText != null) speedText.text = string.Format(speedFormat, speedUI);

        // --- Boost icon ---
        UpdateBoostIcon(manager.CurrentBoostMode, manager.IsBoosting, Time.deltaTime);
    }

    private void UpdateBoostIcon(SpacecraftMovementStats.BoostMode mode, bool isBoosting, float dt)
    {
        if (boostIcon == null) return;

        if (lastMode != mode)
        {
            boostIcon.sprite = (mode == SpacecraftMovementStats.BoostMode.Afterburner) ? afterburnerSprite : dashSprite;
            baseIconColor = (mode == SpacecraftMovementStats.BoostMode.Afterburner) ? afterburnerColor : dashColor;
            lastMode = mode;
        }

        if (flashTimer > 0f)
        {
            flashTimer -= dt;
            float t = Mathf.Clamp01(1f - (flashTimer / Mathf.Max(DenominatorEpsilon, boostFlashDuration)));
            boostIcon.color = Color.Lerp(boostFlashColor, baseIconColor, t);
        }
        else
        {
            boostIcon.color = baseIconColor;
        }
    }

    private void HandleBoostStart(object _, EventArgs __) => flashTimer = boostFlashDuration;
    private void HandleBoostEnd(object _, EventArgs __) { /* no-op */ }

    private static float Smooth(float current, float target, float smoothing, float dt)
    {
        float k = 1f - Mathf.Exp(-Mathf.Max(0f, smoothing) * dt);
        return Mathf.Lerp(current, target, Mathf.Clamp01(k));
    }
}
