using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Serializable data model representing player's ship ownership and selection state.
/// Used for saving/loading player progress.
/// </summary>
[Serializable]
public class PlayerShipProfile
{
    /// <summary>
    /// Player's current credits (in-game currency).
    /// </summary>
    public int credits = 0;

    /// <summary>
    /// Serialized list for persistence (Unity JSON/Inspector compatible).
    /// </summary>
    [SerializeField] private List<string> ownedShipsList = new List<string>();

    /// <summary>
    /// Runtime HashSet for fast lookups and uniqueness; not serialized.
    /// </summary>
    [NonSerialized] public HashSet<string> ownedShipIds = new HashSet<string>();

    /// <summary>
    /// The ID of the currently selected ship.
    /// </summary>
    public string selectedShipId = null;


    /// <summary>
    /// Rebuilds the runtime HashSet from the serialized list after loading.
    /// </summary>
    public void SyncFromList() => ownedShipIds = new HashSet<string>(ownedShipsList);


    /// <summary>
    /// Updates the serialized list from the runtime HashSet before saving.
    /// </summary>
    public void SyncToList() => ownedShipsList = new List<string>(ownedShipIds);


    /// <summary>
    /// Check if player already owns a ship by its ID.
    /// </summary>
    /// <param name="id">Ship ID to check</param>
    /// <returns>True if owned, otherwise false</returns>
    public bool Owns(string id) => ownedShipIds.Contains(id);

    /// <summary>
    /// Attempt to purchase a ship: deducts credits and adds to owned list if affordable and not already owned.
    /// </summary>
    /// <param name="id">Ship ID</param>
    /// <param name="price">Ship cost</param>
    /// <returns>True if purchase succeeded</returns>
    public bool TryPurchase(string id, int price)
    {
        if (credits >= price && !Owns(id))
        {
            credits -= price;
            ownedShipIds.Add(id); // Add new ship to ownership
            return true;
        }
        return false;
    }

    /// <summary>
    /// Select an owned ship as the active ship.
    /// </summary>
    /// <param name="id">Ship ID to select</param>
    /// <returns>True if ship exists in owned list and was selected</returns>
    public bool SelectShip(string id)
    {
        if (Owns(id))
        {
            selectedShipId = id; // Set as current active ship
            return true;
        }
        return false;
    }
}
