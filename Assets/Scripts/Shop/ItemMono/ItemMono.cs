// ItemMono.cs
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemMono : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] protected TextMeshProUGUI nameText;
    [SerializeField] protected TextMeshProUGUI priceText;
    [SerializeField] protected Image icon;
    [SerializeField] protected TextMeshProUGUI buyButtonLabel;
    [SerializeField] protected Button buyButton;
    [SerializeField] protected GameObject purchasedBadge;

    protected BaseItemSO _item;
    protected CategoryStoreManager _owner;

    // ⭐ הפוך ל-virtual כדי שיורשים יוכלו להרחיב
    public virtual void Setup(BaseItemSO item, CategoryStoreManager owner, int currentMoney, bool ownedOrHasQty)
    {
        _item = item;
        _owner = owner;

        if (nameText) nameText.text = item.DisplayName;
        if (priceText) priceText.text = item.Price.ToString();
        if (icon) icon.sprite = item.Icon;

        if (buyButton)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(() => _owner.OnClickBuy(_item));
        }

        if (!buyButtonLabel && buyButton)
            buyButtonLabel = buyButton.GetComponentInChildren<TextMeshProUGUI>(true);

        UpdateVisual(currentMoney, ownedOrHasQty);
    }

    public virtual void UpdateVisual(int money, bool ownedOrHasQty)
    {
        bool canAfford = money >= _item.Price;
        bool canEquip = _item.CanBeEquipped;
        bool isOwned = ownedOrHasQty; // עבור Single זה "Owned", עבור Multiple זה "Quantity>0"
        bool isEquipped = canEquip && _owner.Store.IsEquipped(_item);

        if (!isOwned)
        {
            // מצב "לפני קנייה"
            if (buyButton) buyButton.interactable = canAfford;
            if (buyButtonLabel) buyButtonLabel.text = "Buy";
            if (purchasedBadge) purchasedBadge.SetActive(false);
            return;
        }

        // כאן: בבעלות
        if (canEquip)
        {
            // מצב Select/Equipped
            if (buyButton) buyButton.interactable = !isEquipped; // לא ניתן ללחוץ על Equipped
            if (buyButtonLabel) buyButtonLabel.text = isEquipped ? "Equipped" : "Select";
            if (purchasedBadge) purchasedBadge.SetActive(isEquipped); // אם רוצים, התג מציין Equipped
        }
        else
        {
            // פריט שלא ניתן לצייד (למשל Consumable)
            if (buyButton) buyButton.interactable = false;
            if (buyButtonLabel) buyButtonLabel.text = "Owned";
            if (purchasedBadge) purchasedBadge.SetActive(true);
        }
        Debug.Log($"{_item.Id} → text={buyButtonLabel?.text} isEquipped={_owner.Store.IsEquipped(_item)}");
    }

    public BaseItemSO GetItem() => _item;
}
