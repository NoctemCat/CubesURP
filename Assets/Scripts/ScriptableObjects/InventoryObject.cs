

using System;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using UnityEditor;

[CreateAssetMenu(fileName = "New Inventory", menuName = "Inventory System/Inventory")]
public class InventoryObject : ScriptableObject, ISerializationCallbackReceiver
{
    public string SavePath;
    private ItemDatabaseObject _database;
    public int Capacity;
    public List<InventorySlot> Container = new();

    private void OnEnable()
    {
        _database = Resources.Load<ItemDatabaseObject>("Data/Database");
    }

    public void AddItem(ItemObject _item, int _amount)
    {
        // -1 doesn't exist
        var itemInd = Container.FindIndex((InventorySlot slot) => { return slot.Item.type == _item.type; });

        if (itemInd != -1)
        {
            Container[itemInd].AddAmount(_amount);
        }
        else
        {
            int id = _database.GetId[_item];
            Container.Add(new InventorySlot(id, _item, _amount));
        }
    }

    public void Save()
    {
        string saveData = JsonUtility.ToJson(this, true);
        BinaryFormatter bf = new();
        using FileStream file = File.Create(Path.Combine(Application.persistentDataPath, SavePath));
        bf.Serialize(file, saveData);
    }

    public void Load()
    {
        if (File.Exists(Path.Combine(Application.persistentDataPath, SavePath)))
        {
            BinaryFormatter bf = new();
            using FileStream file = File.Open(Path.Combine(Application.persistentDataPath, SavePath), FileMode.Open);
            JsonUtility.FromJsonOverwrite(bf.Deserialize(file).ToString(), this);
        }
    }

    public void OnAfterDeserialize()
    {
        if (_database == null || !_database.AfterDeserialize) return;
        for (int i = 0; i < Container.Count; i++)
        {
            Container[i].Item = _database.GetItem[Container[i].ID];
        }
    }

    public void OnBeforeSerialize()
    {
    }
}

[Serializable]
public class InventorySlot
{
    public int ID;
    public ItemObject Item;
    public int Amount;
    public InventorySlot(int _id, ItemObject _item, int _amount)
        => (ID, Item, Amount) = (_id, _item, _amount);

    public void AddAmount(int value)
    {
        Amount += value;
    }
}