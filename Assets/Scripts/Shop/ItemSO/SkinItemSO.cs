// SkinItemSO.cs
using UnityEngine;

[CreateAssetMenu(fileName = "SkinItem", menuName = "Store/Items/Skin")]
public class SkinItemSO : BaseItemSO
{
    [Header("Skin Data")]
    public Color PrimaryColor = Color.white;

    [HideInInspector] public bool Owned;

    public override PurchaseType PurchaseMode => PurchaseType.Single;

    public override ItemSaveData GetSaveData()
    {
        return new SkinSaveData { ItemId = Id, Owned = Owned };
    }

    public override void LoadSaveData(ItemSaveData data)
    {
        if (data is SkinSaveData s) Owned = s.Owned;
    }
}
