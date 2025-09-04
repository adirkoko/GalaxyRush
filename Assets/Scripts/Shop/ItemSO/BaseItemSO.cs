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

    // לקביעת התנהגות קנייה
    public abstract PurchaseType PurchaseMode { get; }

    // לשמירה/טעינה ייחודית לכל סוג
    public abstract ItemSaveData GetSaveData();
    public abstract void LoadSaveData(ItemSaveData data);
}
