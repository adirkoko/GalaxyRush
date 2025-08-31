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
        spacecraftManager ??= GetComponent<SpacecraftManager>();
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

        float gameTime = Mathf.Round(GameManager.instance.GetGameTime());
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
        panelRoot.SetActive(isVisible);
    }
}
