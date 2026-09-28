using TMPro;
using UnityEngine;

public class GameStatusUI : MonoBehaviour
{
    [SerializeField] private SpacecraftManager spacecraftManager;
    [SerializeField] private TextMeshProUGUI statsTextMesh;
    [SerializeField] private GameObject panelRoot;

    private bool isVisible = true;

    private void Start()
    {
        // ??= bypasses Unity's null check for unassigned serialized references
        if (spacecraftManager == null) spacecraftManager = GetComponent<SpacecraftManager>();
        if (spacecraftManager == null)
        {
            Debug.LogError($"{nameof(GameStatusUI)}: No {nameof(SpacecraftManager)} found!");
        }
    }

    private void Update()
    {
        if (isVisible)
            UpdateStateTextMesh();
    }

    private void UpdateStateTextMesh()
    {
        if (statsTextMesh == null || spacecraftManager == null) return;

        float gameTime = GameManager.instance != null ? Mathf.Round(GameManager.instance.GetGameTime()) : 0f;
        float speed = Mathf.Round(spacecraftManager.CurrentSpeed);
        float boost = Mathf.Round(spacecraftManager.BoostEnergyUnits);
        float boostMax = spacecraftManager.BoostCapacityUnits;

        statsTextMesh.text =
            $"Time: {gameTime}s\n" +
            $"Speed: {speed} m/s\n" +
            $"Boost: {boost}/{boostMax}";
    }

    public void ToggleVisibility()
    {
        isVisible = !isVisible;
        if (panelRoot != null) panelRoot.SetActive(isVisible);
    }
}
