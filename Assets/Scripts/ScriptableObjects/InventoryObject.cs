

using System;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using UnityEditor;
using System.Runtime.Serialization;

//[CreateAssetMenu(fileName = "New Inventory", menuName = "Inventory System/Inventory")]
public class InventoryObject
{
    private readonly ItemDatabaseObject _database;
    public InventorySlot[] Slots { get; private set; }
    public string Id { get; private set; }

    public InventoryObject(int size = 20, string id = "")
    {
        _database = ServiceLocator.Get<ItemDatabaseObject>();
        Id = id;

        if (Id == "")
            Id = Guid.NewGuid().ToString();

        Slots = new InventorySlot[size];
        for (int i = 0; i < size; i++)
            Slots[i] = new();
    }


    public bool AddItem(Item item, int amount)
    {
        InventorySlot slot = FindItemOnInventory(item);
        if (!_database.ItemObjects[item.Id].Stackable || slot == null)
        {
            if (EmptySlotCount <= 0)
                return false;

            SetEmptySlot(item, amount);
            return true;
        }
        slot.AddAmount(amount);
        return true;
    }

    public bool RemoveItem(Item item, int amount)
    {
        InventorySlot slot = FindItemOnInventory(item);
        if (slot != null)
        {
            slot.AddAmount(-amount);
            if (slot.Amount <= 0)
                slot.RemoveItem();

            return true;
        }
        return false;
    }

    private InventorySlot FindItemOnInventory(Item item)
    {
        for (int i = 0; i < Slots.Length; i++)
        {
            if (Slots[i].Item.Id == item.Id)
                return Slots[i];
        }
        return null;
    }

    public int EmptySlotCount
    {
        get
        {
            int counter = 0;
            for (int i = 0; i < Slots.Length; i++)
            {
                if (Slots[i].Item.Id <= -1)
                    counter++;
            }
            return counter;
        }
    }

    public InventorySlot SetEmptySlot(Item item, int amount)
    {
        for (int i = 0; i < Slots.Length; i++)
        {
            if (Slots[i].Item.Id <= -1)
            {
                Slots[i].UpdateSlot(item, amount);
                return Slots[i];
            }
        }
        // TODO Do something when inventory is full
        return null;
    }

    public static void SwapItems(InventorySlot item1, InventorySlot item2)
    {
        if (item2.CanPlaceInSlot(item1.ItemObject) && item1.CanPlaceInSlot(item2.ItemObject))
        {
            InventorySlot temp = new();
            temp.UpdateSlot(item2);

            item2.UpdateSlot(item1);
            item1.UpdateSlot(temp);
        }
    }

    /// <summary>
    /// Merge item1 into item2
    /// </summary>
    /// <param name="item1"></param>
    /// <param name="item2"></param>
    public static void MergeItems(InventorySlot item1, InventorySlot item2)
    {
        item2.AddAmount(item1.Amount);
        item1.RemoveItem();
    }

    public static void RemoveItem(InventorySlot item)
    {
        item.RemoveItem();
    }

    ////[ContextMenu("Save")]
    //public void Save()
    //{
    //    //string saveData = JsonUtility.ToJson(this, true);
    //    //BinaryFormatter bf = new();
    //    //using FileStream file = File.Create(Path.Combine(Application.persistentDataPath, SavePath));
    //    //bf.Serialize(file, saveData);

    //    IFormatter formatter = new BinaryFormatter();
    //    using Stream stream = new FileStream(Path.Combine(Application.persistentDataPath, SavePath), FileMode.Create, FileAccess.Write);
    //    formatter.Serialize(stream, Slots);
    //}

    ////[ContextMenu("Load")]
    //public void Load()
    //{
    //    if (File.Exists(Path.Combine(Application.persistentDataPath, SavePath)))
    //    {
    //        //BinaryFormatter bf = new();
    //        //using FileStream file = File.Open(Path.Combine(Application.persistentDataPath, SavePath), FileMode.Open);
    //        //JsonUtility.FromJsonOverwrite(bf.Deserialize(file).ToString(), this);

    //        IFormatter formatter = new BinaryFormatter();
    //        using Stream stream = new FileStream(Path.Combine(Application.persistentDataPath, SavePath), FileMode.Open, FileAccess.Read);
    //        InventorySlot[] temp = (InventorySlot[])formatter.Deserialize(stream);

    //        for (int i = 0; i < Slots.Length && i < temp.Length; i++)
    //        {
    //            Slots[i].UpdateSlot(temp[i]);
    //        }
    //    }
    //}

    //[ContextMenu("Clear")]
    public void Clear()
    {
        for (int i = 0; i < Slots.Length; i++)
        {
            Slots[i].RemoveItem();
        }
    }

    //private void Reset()
    //{
    //    for (int i = 0; i < Container.Items.Length; i++)
    //    {
    //        Container.Items[i].ID = -1;
    //    }
    //}

    //public void OnAfterDeserialize()
    //{
    //    if (_database == null || !_database.AfterDeserialize) return;
    //    for (int i = 0; i < Container.Items.Count; i++)
    //    {
    //    }
    //}

    //public void OnBeforeSerialize()
    //{
    //}
}

//[Serializable]
//public class Inventory
//{
//    public InventorySlot[] Slots = new InventorySlot[20];

//    public void Clear()
//    {
//        for (int i = 0; i < Slots.Length; i++)
//        {
//            Slots[i].RemoveItem();
//        }
//    }
//}

[Serializable]
public class InventorySlot
{
    public ItemType[] AllowedItems = new ItemType[0];
    [NonSerialized]
    public GameObject SlotDisplay;
    [NonSerialized]
    private readonly ItemDatabaseObject _database;
    public event Action<InventorySlot> OnAfterUpdate;
    public event Action<InventorySlot> OnBeforeUpdate;
    public Item Item;
    public int Amount;

    public bool HasItem => Item.Id >= 0;
    public ItemObject ItemObject => HasItem ? _database.ItemObjects[Item.Id] : null;

    public InventorySlot()
    {
        //Debug.Log("InventorySlot Empty");
        _database = ServiceLocator.Get<ItemDatabaseObject>();
        UpdateSlot(new(), 0);
    }
    public InventorySlot(Item item, int amount)
    {
        _database = ServiceLocator.Get<ItemDatabaseObject>();
        UpdateSlot(item, amount);
    }
    public void UpdateSlot(InventorySlot other) => UpdateSlot(other.Item, other.Amount);
    public void UpdateSlot(Item item, int amount)
    {
        OnBeforeUpdate?.Invoke(this);
        Item = item;
        Amount = amount;
        OnAfterUpdate?.Invoke(this);
    }

    public void AddAmount(int value) => UpdateSlot(Item, Amount + value);
    public void RemoveItem() => UpdateSlot(new(), 0);

    public bool CanPlaceInSlot(ItemObject itemObj)
    {
        if (AllowedItems.Length <= 0 || itemObj == null || itemObj.Data.Id < 0)
            return true;

        for (int i = 0; i < AllowedItems.Length; i++)
        {
            if (itemObj.Type == AllowedItems[i])
                return true;
        }
        return false;
    }
}