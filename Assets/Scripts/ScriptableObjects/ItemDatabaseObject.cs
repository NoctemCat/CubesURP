

using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Item Database", menuName = "Inventory System/Items/Database")]
public class ItemDatabaseObject : ScriptableObject, ISerializationCallbackReceiver
{
    public ItemObject[] Items;
    public Dictionary<ItemObject, int> GetId = new();
    public Dictionary<int, ItemObject> GetItem = new();

    [System.NonSerialized]
    private bool _afterDeserialize = false;
    public bool AfterDeserialize => _afterDeserialize;

    public void OnAfterDeserialize()
    {
        _afterDeserialize = true;

        GetId = new();
        GetItem = new();
        for (int i = 0; i < Items.Length; i++)
        {
            if (Items[i] == null) continue;
            GetId.Add(Items[i], i);
            GetItem.Add(i, Items[i]);
        }
    }

    public void OnBeforeSerialize()
    {

    }
}