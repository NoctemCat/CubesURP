using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public InventoryObject ToolbarObj;
    public InventoryObject InventoryObj;
    public InventoryObject EquipmentObj;

    //private void Update()
    //{

    //    if (Input.GetKeyDown(KeyCode.LeftAlt))
    //    {
    //        ToolbarObj.Save();
    //        InventoryObj.Save();
    //        EquipmentObj.Save();
    //    }
    //    if (Input.GetKeyDown(KeyCode.Tab))
    //    {
    //        ToolbarObj.Load();
    //        InventoryObj.Load();
    //        EquipmentObj.Load();
    //    }
    //}

    public void OnTriggerEnter(Collider other)
    {
        var item = other.GetComponent<GroundItem>();
        if (item)
        {
            if (InventoryObj.AddItem(new Item(item.ItemObj), 1))
            {
                Destroy(other.gameObject);
            }
        }
    }

    public void AddItem(Item item, int amount)
    {
        if (!ToolbarObj.AddItem(item, amount))
        {
            InventoryObj.AddItem(item, amount);
        }
    }
    public bool RemoveItem(Item item, int amount)
    {
        return ToolbarObj.RemoveItem(item, amount);
    }

    private void OnApplicationQuit()
    {
        ToolbarObj.Clear();
        InventoryObj.Container.Clear();
        EquipmentObj.Container.Clear();
    }
}
