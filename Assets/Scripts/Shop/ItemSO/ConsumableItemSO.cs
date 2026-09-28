// ConsumableItemSO.cs
using UnityEngine;

[CreateAssetMenu(fileName = "ConsumableItem", menuName = "Store/Items/Consumable")]
public class ConsumableItemSO : BaseItemSO
{
    [Header("Consumable Data")]
    public int HealAmount;

    // Runtime state only, loaded from the save file
    [System.NonSerialized] public int Quantity;

    public override PurchaseType PurchaseMode => PurchaseType.Multiple;

    public override ItemSaveData GetSaveData()
    {
        return new ConsumableSaveData { ItemId = Id, Quantity = Quantity };
    }

    public override void LoadSaveData(ItemSaveData data)
    {
        if (data is ConsumableSaveData c) Quantity = c.Quantity;
    }
}
