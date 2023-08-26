using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryOn : MonoBehaviour
{
    public InventoryObject InventoryObj;
    public InventoryObject EquipmentObj;

    private void Update()
    {

        if (Input.GetKeyDown(KeyCode.LeftAlt))
        {
            InventoryObj.Save();
            EquipmentObj.Save();
        }
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            InventoryObj.Load();
            EquipmentObj.Load();
        }
    }

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

    private void OnApplicationQuit()
    {
        InventoryObj.Container.Clear();
        EquipmentObj.Container.Clear();
    }
}
