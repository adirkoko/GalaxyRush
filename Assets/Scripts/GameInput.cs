using UnityEngine;

/// <summary>
/// Centralized input manager: combines Unity Input System + Joystick Pack
/// Always use this class to query player input.
/// </summary>
public class GameInput : MonoBehaviour
{
    public static GameInput Instance { get; private set; }

    [SerializeField] private Joystick joystick; // Drag Joystick prefab here (Fixed/Floating/Dynamic)

    private InputActions inputActions;

    private void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Singleton pattern for global access
        Instance = this;
        inputActions = new InputActions();
    }

    // inputActions is null on a duplicate instance that is being destroyed in Awake
    private void OnEnable() => inputActions?.Enable();
    private void OnDisable() => inputActions?.Disable();

    private void OnDestroy()
    {
        inputActions?.Dispose();
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// Returns movement vector from keyboard/controller or joystick.
    /// Joystick overrides if active.
    /// </summary>
    public Vector2 GetMoveVector()
    {
        Vector2 input = Vector2.zero;

        // Keyboard/controller (Unity Input System)
        if (inputActions.Player.SpacecraftUp.IsPressed()) input.y += 1;
        if (inputActions.Player.SpacecraftDown.IsPressed()) input.y -= 1;
        if (inputActions.Player.SpacecraftRight.IsPressed()) input.x += 1;
        if (inputActions.Player.SpacecraftLeft.IsPressed()) input.x -= 1;

        // Joystick (if active, override)
        if (joystick != null)
        {
            Vector2 joy = new Vector2(joystick.Horizontal, joystick.Vertical);
            if (joy.sqrMagnitude > 0.01f) // filter noise
                input = joy;
        }

        input = input.sqrMagnitude > 1f ? input.normalized : input;
        return input;
    }

    public int GetHorizontalStep()
    {
        if (inputActions.Player.SpacecraftRight.triggered) return +1;
        if (inputActions.Player.SpacecraftLeft.triggered) return -1;
        return 0;
    }

    // --- Other actions from Input System ---
    public bool IsBoostActionPressed() => inputActions.Player.SpacecraftBoost.IsPressed();
    public bool WasBoostActionPressedThisFrame() => inputActions.Player.SpacecraftBoost.triggered;
    public bool IsBrakeActionPressed() => inputActions.Player.SpacecraftBrake.IsPressed();
    public bool IsStabilizeActionPressed() => inputActions.Player.SpacecraftStabilize.IsPressed();
}
