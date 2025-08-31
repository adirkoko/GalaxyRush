using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Provides a simple press/release animation for UI buttons by scaling the transform.
/// When pressed, the button shrinks to a configured scale factor,
/// and smoothly returns to its original size when released.
/// </summary>
public class UIButtonScaler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Scale Settings")]
    [Tooltip("Multiplier applied to the button’s scale when pressed (e.g. 0.9 = shrink by 10%).")]
    [SerializeField] private float pressedScale = 0.9f;

    [Tooltip("Interpolation speed for scaling animation (higher = snappier).")]
    [SerializeField] private float animationSpeed = 10f;

    private Vector3 originalScale; // Initial local scale of the button
    private Vector3 targetScale;   // Desired scale based on input state

    private void Awake()
    {
        // Cache the original scale at startup
        originalScale = transform.localScale;
        targetScale = originalScale;
    }

    private void Update()
    {
        // Smoothly interpolate towards the target scale every frame
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            Time.unscaledDeltaTime * animationSpeed
        );
    }

    /// <summary>
    /// Called when the button is pressed down (PointerDown event).
    /// Shrinks the button to the configured pressed scale.
    /// </summary>
    /// <param name="eventData">Pointer event data provided by the UI system.</param>
    public void OnPointerDown(PointerEventData eventData)
    {
        targetScale = originalScale * pressedScale;
    }

    /// <summary>
    /// Called when the button is released (PointerUp event).
    /// Restores the button back to its original scale.
    /// </summary>
    /// <param name="eventData">Pointer event data provided by the UI system.</param>
    public void OnPointerUp(PointerEventData eventData)
    {
        targetScale = originalScale;
    }
}
