using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public InventoryObject InventoryObj;

    private void Update()
    {

        if (Input.GetKeyDown(KeyCode.LeftAlt))
        {
            Debug.Log("Saving");
            InventoryObj.Save();
        }
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            Debug.Log("Loading");
            InventoryObj.Load();
        }
    }

    public void OnTriggerEnter(Collider other)
    {
        var item = other.GetComponent<Item>();
        if (item)
        {
            InventoryObj.AddItem(item.ItemObj, 1);
            Destroy(other.gameObject);
        }
    }

    private void OnApplicationQuit()
    {
        InventoryObj.Container.Clear();
    }
}
