using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Speedometer : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private RectTransform background;
    [SerializeField] private RectTransform needle;
    [SerializeField] private TMP_Text speedText;
    [SerializeField] private Image backgroundImage; // Tinted by speed

    [Header("Target")]
    [SerializeField] private GameObject targetObject;
    [SerializeField] private float maxSpeed = 200f;

    [Header("Needle Settings")]
    [SerializeField] private float minNeedleAngle = -130f;
    [SerializeField] private float maxNeedleAngle = 130f;
    [SerializeField] private AnimationCurve speedCurve = AnimationCurve.Linear(0, 0, 1, 1);
    [SerializeField] private float needleSmoothing = 12f;

    [Header("Color Settings")]
    [SerializeField] private Gradient speedColors; // e.g. green -> yellow -> red
    [SerializeField] private float colorSmoothing = 5f;

    private Rigidbody2D targetRb2D;
    private float currentAngle;
    private float lastSpeed;
    private Color currentColor;

    private void Awake()
    {
        if (targetObject != null)
        {
            targetRb2D = targetObject.GetComponent<Rigidbody2D>();
            if (targetRb2D == null)
                Debug.LogError($"Speedometer: Target {targetObject.name} has no Rigidbody2D!");
        }
        else
        {
            Debug.LogWarning("Speedometer: targetObject not assigned.");
        }

        // Start from the zero-speed color instead of transparent black
        if (speedColors != null) currentColor = speedColors.Evaluate(0f);
    }

    private void FixedUpdate()
    {
        if (targetRb2D == null) return;
        lastSpeed = targetRb2D.linearVelocity.magnitude; // Unity units per second
    }

    private void Update()
    {
        UpdateNeedle(lastSpeed);
        UpdateText(lastSpeed);
        UpdateColor(lastSpeed);
    }

    private void UpdateNeedle(float speed)
    {
        if (needle == null || maxSpeed <= 0f) return;

        float t = Mathf.Clamp01(speed / maxSpeed);
        float curvedT = speedCurve.Evaluate(t);
        float targetAngle = Mathf.Lerp(minNeedleAngle, maxNeedleAngle, curvedT);

        currentAngle = Mathf.Lerp(currentAngle, targetAngle, 1f - Mathf.Exp(-needleSmoothing * Time.unscaledDeltaTime));
        needle.localRotation = Quaternion.Euler(0, 0, currentAngle);
    }

    private void UpdateText(float speed)
    {
        if (speedText != null)
            speedText.text = $"{Mathf.RoundToInt(speed)}";
    }

    private void UpdateColor(float speed)
    {
        if (backgroundImage == null || speedColors == null) return;

        float t = maxSpeed > 0f ? Mathf.Clamp01(speed / maxSpeed) : 0f;
        Color targetColor = speedColors.Evaluate(t);

        currentColor = Color.Lerp(currentColor, targetColor, 1f - Mathf.Exp(-colorSmoothing * Time.deltaTime));
        backgroundImage.color = currentColor;
    }
}
