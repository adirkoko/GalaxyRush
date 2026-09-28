using TMPro;
using UnityEngine;

public class SpacecraftItemMono : ItemMono
{
    [Header("Spacecraft UI")]
    [SerializeField] private TextMeshProUGUI speedText;
    [SerializeField] private TextMeshProUGUI accelText;

    // Optional frame/glow shown while this spacecraft is equipped
    [SerializeField] private GameObject equippedFx;

    public override void Setup(BaseItemSO item, CategoryStoreManager owner, int currentMoney, bool ownedOrHasQty)
    {
        base.Setup(item, owner, currentMoney, ownedOrHasQty);

        var sc = item as SpacecraftItemSO;
        if (sc != null && sc.movementStats != null)
        {
            if (speedText) speedText.text = $"Speed: {sc.movementStats.MaxSpeed:0}";
            if (accelText) accelText.text = $"Accel: {sc.movementStats.Accel:0}";
        }

        RefreshEquippedFx();
    }

    public override void UpdateVisual(int money, bool ownedOrHasQty)
    {
        base.UpdateVisual(money, ownedOrHasQty);
        RefreshEquippedFx();
    }

    private void RefreshEquippedFx()
    {
        if (!equippedFx) return;
        bool isEquipped = _item.CanBeEquipped && _owner.Store.IsEquipped(_item);
        equippedFx.SetActive(isEquipped);
    }

}
