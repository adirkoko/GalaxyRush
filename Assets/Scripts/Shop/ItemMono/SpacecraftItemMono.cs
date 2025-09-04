// SpacecraftItemMono.cs
using TMPro;
using Unity.Android.Gradle.Manifest;
using UnityEngine;

public class SpacecraftItemMono : ItemMono
{
    [Header("Spacecraft UI")]
    [SerializeField] private TextMeshProUGUI speedText;
    [SerializeField] private TextMeshProUGUI accelText;

    // אופציונלי: GO עם מסגרת/אייקון/זוהר כשמצויד
    [SerializeField] private GameObject equippedFx;

    // נקרא מתוך CategoryStoreManager.BuildUI → Setup(...)
    public override void Setup(BaseItemSO item, CategoryStoreManager owner, int currentMoney, bool ownedOrHasQty)
    {
        base.Setup(item, owner, currentMoney, ownedOrHasQty);

        var sc = item as SpacecraftItemSO;
        if (sc != null && sc.movementStats != null)
        {
            // עדכן לשמות השדות המדויקים אצלך (דוגמה: maxSpeed / acceleration)
            if (speedText) speedText.text = $"Speed: {sc.movementStats}";
            if (accelText) accelText.text = $"Accel: {sc.movementStats}";
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
