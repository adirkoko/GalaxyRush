using UnityEngine;

[CreateAssetMenu(fileName = "SpacecraftItem", menuName = "Store/Items/Spacecraft")]
public class SpacecraftItemSO : BaseItemSO
{
    [Header("Loadout")]
    public SpacecraftMovementStats movementStats; // אותו טייפ שהיה לך
    public GameObject skinPrefab;                 // ויזואל

    [HideInInspector] public bool Owned;

    public override PurchaseType PurchaseMode => PurchaseType.Single;

    // רצוי שהקטגוריה תהיה מקובעת כאן:
    private void Reset()
    {
        Category = StoreCategory.Spacecrafts;
    }

    private void OnValidate()
    {
        // שמירה על תאימות לישן: יצירת Id אוטומטי אם חסר
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
