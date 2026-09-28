// InventoryManager.cs
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    [Header("SO Sources (Id -> SO lookup)")]
    [SerializeField] private List<ItemSOList> categoryLists;

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
        // Single items: 1 if owned, otherwise 0
        return IsOwned(itemId) ? 1 : 0;
    }

    public void RegisterPurchase(BaseItemSO item)
    {
        switch (item)
        {
            case WeaponItemSO w:
                w.Owned = true;
                break;

            case SkinItemSO s:
                s.Owned = true;
                break;

            case ConsumableItemSO c:
                c.Quantity += 1;
                break;

            case SpacecraftItemSO sc:
                sc.Owned = true;
                break;
        }
    }

    public void ApplyLoadedData(GameSaveData data)
    {
        // Reset runtime state before applying the save
        foreach (var list in categoryLists)
        {
            if (list == null) continue;
            foreach (var it in list.Items)
            {
                if (it is WeaponItemSO w) w.Owned = false;
                if (it is SkinItemSO s) s.Owned = false;
                if (it is ConsumableItemSO c) c.Quantity = 0;
                if (it is SpacecraftItemSO sc) sc.Owned = false;
            }
        }

        // Lists may be null in older save files
        foreach (var w in data.Weapons ?? new())
            if (FindItem(w.ItemId) is WeaponItemSO sw) sw.Owned = w.Owned;

        foreach (var c in data.Consumables ?? new())
            if (FindItem(c.ItemId) is ConsumableItemSO sc) sc.Quantity = c.Quantity;

        foreach (var s in data.Skins ?? new())
            if (FindItem(s.ItemId) is SkinItemSO ss) ss.Owned = s.Owned;

        foreach (var sp in data.Spacecrafts ?? new())
            if (FindItem(sp.ItemId) is SpacecraftItemSO ssc) ssc.Owned = sp.Owned;
    }

    public void FillSaveData(GameSaveData data)
    {
        data.Weapons.Clear();
        data.Consumables.Clear();
        data.Skins.Clear();
        data.Spacecrafts.Clear();

        foreach (var list in categoryLists)
        {
            if (list == null) continue;
            foreach (var it in list.Items)
            {
                if (it is WeaponItemSO w) data.Weapons.Add((WeaponSaveData)w.GetSaveData());
                if (it is ConsumableItemSO c) data.Consumables.Add((ConsumableSaveData)c.GetSaveData());
                if (it is SkinItemSO s) data.Skins.Add((SkinSaveData)s.GetSaveData());
                if (it is SpacecraftItemSO sc) data.Spacecrafts.Add((SpacecraftSaveData)sc.GetSaveData());
            }
        }
    }

    private BaseItemSO FindItem(string id)
    {
        foreach (var list in categoryLists)
        {
            if (list == null) continue;
            var it = list.GetItemById(id);
            if (it != null) return it;
        }
        return null;
    }
}
