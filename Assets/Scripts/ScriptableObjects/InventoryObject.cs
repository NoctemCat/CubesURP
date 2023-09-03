

using System;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using UnityEditor;
using System.Runtime.Serialization;

[CreateAssetMenu(fileName = "New Inventory", menuName = "Inventory System/Inventory")]
public class InventoryObject : ScriptableObject
{
    [field: SerializeField] public string SavePath { get; private set; }
    //public int Capacity;
    public Inventory Container;
    private ItemDatabaseObject Database => ItemDatabaseObject.Instance;
    public InventorySlot[] GetSlots => Container.Slots;

    //private void OnEnable()
    //{
    //    Database = ;
    //}

    public bool AddItem(Item item, int amount)
    {
        //if
        InventorySlot slot = FindItemOnInventory(item);
        if (!Database.ItemObjects[item.Id].Stackable || slot == null)
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
        foreach (var slot in GetSlots)
        {
            if (slot.Item.Id == item.Id)
                return slot;
        }
        return null;
    }

    public int EmptySlotCount
    {
        get
        {
            int counter = 0;
            foreach (var slot in GetSlots)
            {
                if (slot.Item.Id <= -1)
                    counter++;
            }
            return counter;
        }
    }

    public InventorySlot SetEmptySlot(Item item, int amount)
    {
        for (int i = 0; i < GetSlots.Length; i++)
        {
            if (GetSlots[i].Item.Id <= -1)
            {
                GetSlots[i].UpdateSlot(item, amount);
                return GetSlots[i];
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

    [ContextMenu("Save")]
    public void Save()
    {
        //string saveData = JsonUtility.ToJson(this, true);
        //BinaryFormatter bf = new();
        //using FileStream file = File.Create(Path.Combine(Application.persistentDataPath, SavePath));
        //bf.Serialize(file, saveData);

        IFormatter formatter = new BinaryFormatter();
        using Stream stream = new FileStream(Path.Combine(Application.persistentDataPath, SavePath), FileMode.Create, FileAccess.Write);
        formatter.Serialize(stream, Container);
    }

    [ContextMenu("Load")]
    public void Load()
    {
        if (File.Exists(Path.Combine(Application.persistentDataPath, SavePath)))
        {
            //BinaryFormatter bf = new();
            //using FileStream file = File.Open(Path.Combine(Application.persistentDataPath, SavePath), FileMode.Open);
            //JsonUtility.FromJsonOverwrite(bf.Deserialize(file).ToString(), this);

            IFormatter formatter = new BinaryFormatter();
            using Stream stream = new FileStream(Path.Combine(Application.persistentDataPath, SavePath), FileMode.Open, FileAccess.Read);
            Inventory temp = (Inventory)formatter.Deserialize(stream);

            for (int i = 0; i < GetSlots.Length && i < temp.Slots.Length; i++)
            {
                GetSlots[i].UpdateSlot(temp.Slots[i]);
            }
        }
    }

    [ContextMenu("Clear")]
    public void Clear()
    {
        Container.Clear();
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

[Serializable]
public class Inventory
{
    public InventorySlot[] Slots = new InventorySlot[20];

    public void Clear()
    {
        for (int i = 0; i < Slots.Length; i++)
        {
            Slots[i].RemoveItem();
        }
    }
}

public delegate void SlotUpdated(InventorySlot slot);

[Serializable]
public class InventorySlot
{
    public ItemType[] AllowedItems = new ItemType[0];
    [NonSerialized]
    public GameObject SlotDisplay;
    [NonSerialized]
    public SlotUpdated OnAfterUpdate;
    [NonSerialized]
    public SlotUpdated OnBeforeUpdate;
    public Item Item;
    public int Amount;

    public ItemObject ItemObject => Item.Id >= 0 ? ItemDatabaseObject.Instance.ItemObjects[Item.Id] : null;

    public InventorySlot() => UpdateSlot(new(), 0);
    public InventorySlot(Item item, int amount) => UpdateSlot(item, amount);
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