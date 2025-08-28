using UnityEngine;

/// <summary>
/// Creates a parallax scrolling effect with infinite tiling for backgrounds.
/// </summary>
public class ParallaxBackground : MonoBehaviour
{
    // Camera reference (defaults to main camera if null)
    [SerializeField] private Transform cameraTransform;

    // Movement multiplier (X/Y)
    [SerializeField] private Vector2 parallaxFactor = new Vector2(0.5f, 0.5f);

    private Vector3 lastCameraPosition; // Camera position from previous frame (for delta calculation)
    private float textureUnitSizeX;     // Width of the sprite in world units
    private float textureUnitSizeY;     // Height of the sprite in world units


    private void Start()
    {
        InitializeCamera();
        CalculateTextureSize();
        lastCameraPosition = cameraTransform.position;
    }

    private void LateUpdate()
    {
        ApplyParallax();
        HandleHorizontalLooping();
        HandleVerticalLooping();
    }

    /// <summary>
    /// Sets the camera transform if not assigned.
    /// </summary>
    private void InitializeCamera()
    {
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;
    }

    /// <summary>
    /// Calculates the size of the sprite in world units.
    /// </summary>
    private void CalculateTextureSize()
    {
        Sprite sprite = GetComponent<SpriteRenderer>().sprite;
        Texture2D texture = sprite.texture;

        textureUnitSizeX = texture.width / sprite.pixelsPerUnit;
        textureUnitSizeY = texture.height / sprite.pixelsPerUnit;
    }

    /// <summary>
    /// Applies parallax movement based on camera delta.
    /// </summary>
    private void ApplyParallax()
    {
        Vector3 deltaMovement = cameraTransform.position - lastCameraPosition;
        transform.position += new Vector3(
            deltaMovement.x * parallaxFactor.x,
            deltaMovement.y * parallaxFactor.y,
            0
        );

        lastCameraPosition = cameraTransform.position;
    }

    /// <summary>
    /// Repositions background horizontally for seamless tiling.
    /// </summary>
    private void HandleHorizontalLooping()
    {
        if (Mathf.Abs(cameraTransform.position.x - transform.position.x) >= textureUnitSizeX)
        {
            float offsetX = (cameraTransform.position.x - transform.position.x) % textureUnitSizeX;
            transform.position = new Vector3(cameraTransform.position.x + offsetX, transform.position.y, transform.position.z);
        }
    }

    /// <summary>
    /// Repositions background vertically for seamless tiling.
    /// </summary>
    private void HandleVerticalLooping()
    {
        if (Mathf.Abs(cameraTransform.position.y - transform.position.y) >= textureUnitSizeY)
        {
            float offsetY = (cameraTransform.position.y - transform.position.y) % textureUnitSizeY;
            transform.position = new Vector3(transform.position.x, cameraTransform.position.y + offsetY, transform.position.z);
        }
    }
}
