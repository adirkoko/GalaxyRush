// ItemSOList.cs
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemSOList", menuName = "Store/ItemSOList (Per Category)")]
public class ItemSOList : ScriptableObject
{
    public StoreCategory Category = StoreCategory.None;
    public List<BaseItemSO> Items = new();

    public BaseItemSO GetItemById(string id) => Items.Find(i => i.Id == id);
    public List<BaseItemSO> GetAllAvailableItems() => Items;
}
