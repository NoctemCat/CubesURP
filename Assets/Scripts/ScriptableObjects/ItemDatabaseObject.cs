

using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Item Database", menuName = "Inventory System/Items/Database")]
public class ItemDatabaseObject : ScriptableObject, ISerializationCallbackReceiver
{
    private static ItemDatabaseObject _instance;
    public static ItemDatabaseObject Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<ItemDatabaseObject>("Data/Database");
            }
            return _instance;
        }
    }

    public ItemObject[] ItemObjects;

    [ContextMenu("Update IDs")]
    public void UpdateID()
    {
        for (int i = 0; i < ItemObjects.Length; i++)
        {
            if (ItemObjects[i] == null) continue;
            ItemObjects[i].Data.Id = i;
            ItemObjects[i].Data.Name = ItemObjects[i].Name;
        }
    }

    public void OnAfterDeserialize()
    {
        UpdateID();
    }

    public void OnBeforeSerialize()
    {
    }
}