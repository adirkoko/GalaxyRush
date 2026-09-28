using UnityEngine;

[CreateAssetMenu(fileName = "SpacecraftItem", menuName = "Store/Items/Spacecraft")]
public class SpacecraftItemSO : BaseItemSO
{
    [Header("Loadout")]
    public SpacecraftMovementStats movementStats;
    public GameObject skinPrefab;

    // Runtime state only, loaded from the save file
    [System.NonSerialized] public bool Owned;

    public override PurchaseType PurchaseMode => PurchaseType.Single;

    private void Reset()
    {
        Category = StoreCategory.Spacecrafts;
    }

    private void OnValidate()
    {
        // Auto-generate an Id if missing
        if (string.IsNullOrWhiteSpace(Id))
            Id = name.Replace(" ", "_").ToLowerInvariant();
        if (Category == StoreCategory.None)
            Category = StoreCategory.Spacecrafts;
    }

    public override ItemSaveData GetSaveData()
    {
        return new SpacecraftSaveData { ItemId = Id, Owned = Owned };
    }

    public override void LoadSaveData(ItemSaveData data)
    {
        if (data is SpacecraftSaveData s) Owned = s.Owned;
    }
}
