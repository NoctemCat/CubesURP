using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInventory : MonoBehaviour
{
    private World World;
    public InventoryObject ToolbarObj;
    public InventoryObject InventoryObj;
    public InventoryObject EquipmentObj;

    [SerializeField] private GameObject _inventoryScreen;

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
    public bool InInventory { get; private set; }

    private void Start()
    {
        World = World.Instance;
        InInventory = false;

        World.OnResume += SetCursorState;
    }

    private void OnOpenInventory(InputValue value)
    {
        if (Time.timeScale == 0) return;

        InInventory = !InInventory;

        _inventoryScreen.SetActive(InInventory);
        if (!InInventory)
        {
            TooltipScreenSpaceUI.HideTooltip_Static();
        }
        SetCursorState();
    }

    public void SetCursorState()
    {
        if (InInventory)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
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
