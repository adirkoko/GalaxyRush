using UnityEngine;

public abstract class BaseItemSO : ScriptableObject
{
    [Header("Base Data")]
    public string Id;
    public string DisplayName;
    public int Price;
    public Sprite Icon;
    public StoreCategory Category = StoreCategory.None;
    public virtual bool CanBeEquipped => PurchaseMode == PurchaseType.Single;

    public abstract PurchaseType PurchaseMode { get; }

    // Per-type save/load
    public abstract ItemSaveData GetSaveData();
    public abstract void LoadSaveData(ItemSaveData data);
}
