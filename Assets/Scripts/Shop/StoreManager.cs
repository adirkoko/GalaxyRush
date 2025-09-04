// StoreManager.cs
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StoreManager : MonoBehaviour
{
    public const string SAVE_FILE_NAME = "store_save.json";

    [Header("כסף")]
    [SerializeField] private int startCredits = 0;
    [SerializeField] private TextMeshProUGUI creditsText;

    [Serializable]
    public struct TabConfig
    {
        public StoreCategory Category;
        public Button TabButton;
        public GameObject TabRoot; // מכיל CategoryStoreManager
    }
    [SerializeField] private List<TabConfig> tabs;

    [SerializeField] private InventoryManager inventory;
    [SerializeField] private Button backButton;

    // אירועים לצרכנים אחרים במשחק
    public event Action<BaseItemSO> OnPurchaseSuccess;
    public event Action<BaseItemSO, string> OnPurchaseFailed;
    public event Action<int> OnMoneyChanged;
    public event Action<StoreCategory, BaseItemSO> OnEquippedChanged;

    private readonly Dictionary<StoreCategory, CategoryStoreManager> _catManagers = new();
    private readonly Dictionary<StoreCategory, string> _equippedByCategory = new();


    public int CurrentCredits { get; private set; }

    private void Awake()
    {
        // חבר טאב־כפתור
        foreach (var t in tabs)
        {
            if (t.TabButton != null)
            {
                var captured = t;
                t.TabButton.onClick.AddListener(() => OpenTab(captured.Category));
            }

            var csm = t.TabRoot.GetComponent<CategoryStoreManager>();
            if (csm != null)
            {
                csm.Category = t.Category;
                _catManagers[csm.Category] = csm;
            }
        }
    }

    private void Start()
    {
        if (backButton) backButton.onClick.AddListener(BackToMainMenu);

        LoadAll();
        UpdateMoneyUI();
        // אתחל מנהלי קטגוריות
        foreach (var kv in _catManagers)
            kv.Value.Init(this);

        // פתח טאב ראשון שאינו None
        foreach (var t in tabs)
            if (t.Category != StoreCategory.None) { OpenTab(t.Category); break; }
    }

    public void OpenTab(StoreCategory category)
    {
        foreach (var t in tabs)
        {
            bool active = t.Category == category;
            if (t.TabRoot) t.TabRoot.SetActive(active);
        }
    }

    public bool InventoryHas(BaseItemSO item)
    {
        if (item is ConsumableItemSO c) return c.Quantity > 0;
        if (item is WeaponItemSO w) return w.Owned;
        if (item is SkinItemSO s) return s.Owned;
        if (item is SpacecraftItemSO sc) return sc.Owned;
        return false;
    }

    public void AddCredits(int amount)
    {
        CurrentCredits += amount;
        UpdateMoneyUI();
        OnMoneyChanged?.Invoke(CurrentCredits);
        SaveAll();
    }

    public bool TryPurchase(BaseItemSO item)
    {
        // בדיקת כסף
        if (CurrentCredits < item.Price)
        {
            OnPurchaseFailed?.Invoke(item, "Not enough credits");
            return false;
        }

        // לוגיקת Single/Multiple
        if (item.PurchaseMode == PurchaseType.Single && InventoryHas(item))
        {
            OnPurchaseFailed?.Invoke(item, "Already owned");
            return false;
        }

        // הורדת כסף
        CurrentCredits -= item.Price;
        UpdateMoneyUI();
        OnMoneyChanged?.Invoke(CurrentCredits);

        // עדכון אינבנטורי
        inventory.RegisterPurchase(item);

        // אירוע הצלחה
        OnPurchaseSuccess?.Invoke(item);

        // שמירה
        SaveAll();

        return true;
    }

    private void UpdateMoneyUI()
    {
        if (creditsText) creditsText.text = $"Credits: {CurrentCredits}";
    }

    #region Save/Load
    public void SaveAll()
    {
        var data = new GameSaveData { Credits = CurrentCredits };
        inventory.FillSaveData(data);

        data.Equipped = _equippedByCategory
            .Select(kv => new CategoryEquipData { Category = kv.Key, ItemId = kv.Value })
            .ToList();

        SaveLoadManager.Save(SAVE_FILE_NAME, data);
    }

    public void LoadAll()
    {
        if (SaveLoadManager.Load<GameSaveData>(SAVE_FILE_NAME, out var data))
        {
            CurrentCredits = data.Credits == 0 ? startCredits : data.Credits;
            inventory.ApplyLoadedData(data);

            _equippedByCategory.Clear();
            if (data.Equipped != null)
            {
                foreach (var ce in data.Equipped)
                    if (!string.IsNullOrEmpty(ce.ItemId))
                        _equippedByCategory[ce.Category] = ce.ItemId;
            }
        }
        else
        {
            CurrentCredits = startCredits;
        }
    }
    #endregion

    public bool IsEquipped(BaseItemSO item)
    => item != null &&
       _equippedByCategory.TryGetValue(item.Category, out var id) &&
       id == item.Id;

    public void EquipItem(BaseItemSO item)
    {
        if (item == null) return;
        if (!item.CanBeEquipped) return;
        if (!InventoryHas(item)) return; // מותר לצייד רק אם בבעלות

        _equippedByCategory[item.Category] = item.Id;
        SaveAll();
        OnEquippedChanged?.Invoke(item.Category, item);
        Debug.Log($"Equipped: {item.DisplayName} in {item.Category}");
    }



    private void BackToMainMenu()
        => SceneLoader.LoadScene(SceneLoader.Scene.MainMenuScene);
}
