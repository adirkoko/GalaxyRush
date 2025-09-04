// InventoryManager.cs
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    [Header("מקורות SO (למיפוי Id -> SO)")]
    [SerializeField] private List<ItemSOList> categoryLists;

    public string EquippedWeaponId { get; private set; }
    public string SelectedSpacecraftId { get; private set; }

    // API לקריאה
    public bool IsOwned(string itemId)
    {
        var item = FindItem(itemId);
        if (item is WeaponItemSO w) return w.Owned;
        if (item is SkinItemSO s) return s.Owned;
        if (item is ConsumableItemSO c) return c.Quantity > 0;
        if (item is SpacecraftItemSO sc) return sc.Owned;
        return false;
    }

    public int GetQuantity(string itemId)
    {
        var item = FindItem(itemId);
        if (item is ConsumableItemSO c) return c.Quantity;
        // לכל השאר (Single) – 1 אם בבעלות, אחרת 0:
        return IsOwned(itemId) ? 1 : 0;
    }

    public void RegisterPurchase(BaseItemSO item)
    {
        switch (item)
        {
            case WeaponItemSO w:
                w.Owned = true;
                if (string.IsNullOrEmpty(EquippedWeaponId)) 
                    EquippedWeaponId = w.Id;
                break;

            case SkinItemSO s:
                s.Owned = true;
                break;

            case ConsumableItemSO c:
                c.Quantity += 1;
                break;

            case SpacecraftItemSO sc:
                sc.Owned = true;
                if (string.IsNullOrEmpty(SelectedSpacecraftId))
                    SelectedSpacecraftId = sc.Id;
                break;
        }
    }

    public void ApplyLoadedData(GameSaveData data)
    {
        // אפס מצב קיים בזיכרון
        foreach (var list in categoryLists)
        {
            foreach (var it in list.Items)
            {
                if (it is WeaponItemSO w) w.Owned = false;
                if (it is SkinItemSO s) s.Owned = false;
                if (it is ConsumableItemSO c) c.Quantity = 0;
                if (it is SpacecraftItemSO sc) sc.Owned = false;
            }
        }

        // העמסה לפי שמירה
        foreach (var w in data.Weapons)
            if (FindItem(w.ItemId) is WeaponItemSO sw) sw.Owned = w.Owned;

        foreach (var c in data.Consumables)
            if (FindItem(c.ItemId) is ConsumableItemSO sc) sc.Quantity = c.Quantity;

        foreach (var s in data.Skins)
            if (FindItem(s.ItemId) is SkinItemSO ss) ss.Owned = s.Owned;

        foreach (var sp in data.Spacecrafts)
            if (FindItem(sp.ItemId) is SpacecraftItemSO ssc) ssc.Owned = sp.Owned;

        EquippedWeaponId = data.EquippedWeaponId;
        SelectedSpacecraftId = data.SelectedSpacecraftId;
    }

    public void FillSaveData(GameSaveData data)
    {
        data.Weapons.Clear();
        data.Consumables.Clear();
        data.Skins.Clear();
        data.Spacecrafts.Clear();

        foreach (var list in categoryLists)
            foreach (var it in list.Items)
            {
                if (it is WeaponItemSO w) data.Weapons.Add((WeaponSaveData)w.GetSaveData());
                if (it is ConsumableItemSO c) data.Consumables.Add((ConsumableSaveData)c.GetSaveData());
                if (it is SkinItemSO s) data.Skins.Add((SkinSaveData)s.GetSaveData());
                if (it is SpacecraftItemSO sc) data.Spacecrafts.Add((SpacecraftSaveData)sc.GetSaveData());
            }

        data.EquippedWeaponId = EquippedWeaponId;
    }

    private BaseItemSO FindItem(string id)
    {
        foreach (var list in categoryLists)
        {
            var it = list.GetItemById(id);
            if (it != null) return it;
        }
        return null;
    }

    public void SetSelectedSpacecraft(string id) { SelectedSpacecraftId = id; }

}
