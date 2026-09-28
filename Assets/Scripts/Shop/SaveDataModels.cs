// SaveDataModels.cs
using System;
using System.Collections.Generic;

[Serializable]
public class ItemSaveData { public string ItemId; }

[Serializable]
public class WeaponSaveData : ItemSaveData { public bool Owned; }

[Serializable]
public class ConsumableSaveData : ItemSaveData { public int Quantity; }

[Serializable]
public class SkinSaveData : ItemSaveData { public bool Owned; }

[Serializable]
public class SpacecraftSaveData : ItemSaveData { public bool Owned; }

[Serializable]
public class CategoryEquipData
{
    public StoreCategory Category;
    public string ItemId;
}

// Root save data for the game/store
[Serializable]
public class GameSaveData
{
    public int Credits;
    public List<CategoryEquipData> Equipped = new();
    public List<WeaponSaveData> Weapons = new();
    public List<ConsumableSaveData> Consumables = new();
    public List<SkinSaveData> Skins = new();
    public List<SpacecraftSaveData> Spacecrafts = new();
}
