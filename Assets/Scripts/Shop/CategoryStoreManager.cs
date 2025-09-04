// CategoryStoreManager.cs
using System.Collections.Generic;
using UnityEngine;

public class CategoryStoreManager : MonoBehaviour
{
    [Header("Category")]
    public StoreCategory Category = StoreCategory.None;
    [SerializeField] private ItemSOList itemList;

    [Header("UI")]
    [SerializeField] private Transform itemsContainer; // Grid/Scroll Content
    [SerializeField] private ItemMono itemPrefab;

    private readonly List<ItemMono> _spawned = new();
    private StoreManager _store;
    public StoreManager Store => _store;

    public void Init(StoreManager store)
    {
        _store = store;
        BuildUI();
        _store.OnMoneyChanged += HandleMoneyChanged;
        _store.OnPurchaseSuccess += HandlePurchaseSuccess;
        _store.OnEquippedChanged += HandleEquippedChanged;
    }

    private void OnDestroy()
    {
        if (_store != null)
        {
            _store.OnMoneyChanged -= HandleMoneyChanged;
            _store.OnPurchaseSuccess -= HandlePurchaseSuccess;
            _store.OnEquippedChanged -= HandleEquippedChanged;
        }
    }


    public void BuildUI()
    {
        foreach (Transform c in itemsContainer) Destroy(c.gameObject);
        _spawned.Clear();

        int money = _store.CurrentCredits;
        foreach (var item in itemList.GetAllAvailableItems())
        {
            var go = Instantiate(itemPrefab, itemsContainer);
            bool ownedOrHasQty = _store.InventoryHas(item);
            go.Setup(item, this, money, ownedOrHasQty);
            _spawned.Add(go);
        }
    }

    public void OnClickBuy(BaseItemSO item)
    {
        if (!_store.InventoryHas(item))
        {
            _store.TryPurchase(item);           // קנייה
        }
        else if (item.CanBeEquipped)
        {
            _store.EquipItem(item);             // ציוד
        }
    }

    private void HandleMoneyChanged(int newMoney)
    {
        foreach (var mono in _spawned)
        {
            var item = mono.GetItem();
            bool ownedOrHasQty = _store.InventoryHas(item);
            mono.UpdateVisual(newMoney, ownedOrHasQty);
        }
    }

    private void HandlePurchaseSuccess(BaseItemSO purchasedItem)
    {
        foreach (var mono in _spawned)
        {
            if (mono.GetItem() == purchasedItem)
            {
                mono.UpdateVisual(_store.CurrentCredits, _store.InventoryHas(purchasedItem));
            }
        }
    }

    private void OnValidate()
    {
        if (itemList != null && itemList.Category != StoreCategory.None)
            Category = itemList.Category;
    }

    private void HandleEquippedChanged(StoreCategory cat, BaseItemSO _)
    {
        Debug.Log($"aaaaaaaaa  {cat}-=-=-=-{Category}");

        if (cat != Category) return; // רק הטאב הרלוונטי
        foreach (var mono in _spawned)
            mono.UpdateVisual(_store.CurrentCredits, _store.InventoryHas(mono.GetItem()));

        Debug.Log($"Refresh UI for {Category} ({_spawned.Count} cards)");
    }
}
