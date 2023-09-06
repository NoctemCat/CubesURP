

using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Item Database", menuName = "Inventory System/Items/Database")]
public class ItemDatabaseObject : ScriptableObject, ISerializationCallbackReceiver
{
    [field: SerializeField] public ItemObject[] ItemObjects { get; private set; }

    [ContextMenu("Update IDs")]
    public void UpdateID()
    {
        for (int i = 0; i < ItemObjects.Length; i++)
        {
            if (ItemObjects[i] == null) continue;
            ItemObjects[i].data.id = i;
            ItemObjects[i].data.itemName = ItemObjects[i].itemName;
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