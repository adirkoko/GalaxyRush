using UnityEngine;

[CreateAssetMenu(fileName = "WeaponItem", menuName = "Store/Items/Weapon")]
public class WeaponItemSO : BaseItemSO
{
    [Header("Weapon Data")]
    public int Damage;
    public float FireRate;
    public string AmmoType;

    // Runtime state only, loaded from the save file
    [System.NonSerialized] public bool Owned;

    public override PurchaseType PurchaseMode => PurchaseType.Single;

    public override ItemSaveData GetSaveData()
    {
        return new WeaponSaveData { ItemId = Id, Owned = Owned };
    }

    public override void LoadSaveData(ItemSaveData data)
    {
        if (data is WeaponSaveData w) Owned = w.Owned;
    }
}
