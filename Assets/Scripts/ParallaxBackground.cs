using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ParallaxBackground : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Camera to track. Defaults to main camera if empty.")]
    [SerializeField] private Transform cameraTransform;

    [Header("Parallax")]
    [Tooltip("0 = static, 1 = follow camera. Typical < 1.")]
    [SerializeField] private Vector2 parallaxFactor = new Vector2(0.5f, 0.5f);

    [Header("Pixel Art")]
    [Tooltip("Snap to pixel grid for crisp movement.")]
    [SerializeField] private bool pixelSnap = false;
    [Tooltip("Override PPU for snap. 0 = use sprite PPU.")]
    [SerializeField] private int pixelsPerUnitOverride = 0;

    private SpriteRenderer sr;
    private Vector3 camStartPos;    // Camera position at start
    private Vector3 anchorPos;      // Background anchor for tile shifts
    private float tileSizeX;        // Sprite width in world units
    private float tileSizeY;        // Sprite height in world units
    private float unitsPerPixel;    // World units per pixel for snap

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();

        if (cameraTransform == null)
        {
            var cam = Camera.main;
            if (cam != null) cameraTransform = cam.transform;
        }

        if (cameraTransform == null)
        {
            Debug.LogError($"{name}: No camera assigned and no main camera found.");
            enabled = false;
        }
    }

    private void Start()
    {
        if (sr.sprite == null)
        {
            Debug.LogError($"{name}: SpriteRenderer.sprite is null. Assign a sprite.");
            enabled = false;
            return;
        }

        camStartPos = cameraTransform.position;
        anchorPos = transform.position;

        // Use the sprite bounds for true world size (not the whole texture/atlas)
        Vector2 worldSize = sr.sprite.bounds.size;
        tileSizeX = worldSize.x;
        tileSizeY = worldSize.y;

        float ppu = pixelsPerUnitOverride > 0
            ? (float)pixelsPerUnitOverride
            : sr.sprite.pixelsPerUnit;

        unitsPerPixel = 1f / ppu;
    }

    private void LateUpdate()
    {
        Vector3 camDelta = cameraTransform.position - camStartPos;

        // Predicted parallax position before tiling
        Vector3 pos = anchorPos + new Vector3(
            camDelta.x * parallaxFactor.x,
            camDelta.y * parallaxFactor.y,
            0f
        );

        // Horizontal tiling by whole-tile steps (no modulo jitter)
        float xDiff = cameraTransform.position.x - pos.x;
        if (Mathf.Abs(xDiff) >= tileSizeX)
        {
            float steps = Mathf.Floor(Mathf.Abs(xDiff) / tileSizeX);
            float shift = Mathf.Sign(xDiff) * steps * tileSizeX;
            anchorPos.x += shift;
            pos.x += shift; // Apply instantly to avoid a visible jump
        }

        // Vertical tiling by whole-tile steps
        float yDiff = cameraTransform.position.y - pos.y;
        if (Mathf.Abs(yDiff) >= tileSizeY)
        {
            float steps = Mathf.Floor(Mathf.Abs(yDiff) / tileSizeY);
            float shift = Mathf.Sign(yDiff) * steps * tileSizeY;
            anchorPos.y += shift;
            pos.y += shift;
        }

        // Optional pixel snapping for sharp motion
        if (pixelSnap)
        {
            pos.x = Mathf.Round(pos.x / unitsPerPixel) * unitsPerPixel;
            pos.y = Mathf.Round(pos.y / unitsPerPixel) * unitsPerPixel;
        }

        transform.position = pos;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        parallaxFactor.x = Mathf.Clamp01(parallaxFactor.x);
        parallaxFactor.y = Mathf.Clamp01(parallaxFactor.y);
        if (pixelsPerUnitOverride < 0) pixelsPerUnitOverride = 0;
    }
#endif
}
