using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInventory : MonoBehaviour
{
    private World World;
    private EventSystem _eventSystem;
    private Player _player;
    private InventoryObject _toolbar;
    private InventoryObject _inventory;
    public InventoryObject EquipmentObj;

    [SerializeField] private GameObject _inventoryScreen;
    [SerializeField] private GameObject _itemPrefab;
    [SerializeField] private InputActionReference _dropItemAction;

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
    private TooltipUi _tooltip;

    [Range(0.1f, 2f)]
    [SerializeField] private float _dropItemDelay;
    private void Start()
    {
        World = World.Instance;
        _player = GetComponent<Player>();
        _eventSystem = ServiceLocator.Get<EventSystem>();

        //InventorySystem.
        _tooltip = ServiceLocator.Get<TooltipUi>();
        var inventorySystem = ServiceLocator.Get<InventorySystem>();

        _toolbar = new InventoryObject(9, "Toolbar");
        _inventory = new InventoryObject(24, "Player Inventory");

        inventorySystem.RegisterInventory(_toolbar);
        inventorySystem.RegisterInventory(_inventory);

        inventorySystem.Show(_toolbar);

        //ToolbarObj = inventorySystem.Get("Toolbar");
        //InventoryObj = inventorySystem.Get("Player Inventory");

        //inventorySystem.Show(ToolbarObj);

        InInventory = false;

        var eventSystem = ServiceLocator.Get<EventSystem>();
        eventSystem.OnResumeGame += SetCursorState;

        DropItemDelay = new(DropItem, _dropItemDelay);
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            DropItemDelay.LoopDelay = _dropItemDelay;
        }
    }

    private void OnOpenInventory(InputValue value)
    {
        if (Time.timeScale == 0) return;

        InInventory = !InInventory;

        var inventorySystem = ServiceLocator.Get<InventorySystem>();

        if (InInventory) inventorySystem.Show(_inventory);
        else inventorySystem.Hide(_inventory);

        if (!InInventory)
        {
            _tooltip.HideTooltip();
        }
        SetCursorState();
    }

    UniTaskLoop DropItemDelay;
    private void OnDropItem(InputValue value)
    {
        Debug.Log(value.isPressed);
        if (value.isPressed)
            DropItemDelay.Start();
        else
            DropItemDelay.Stop();
    }

    private void DropItem()
    {
        if (_player.selectedBlockIndex <= 0) return;

        BlockObject selObj = World.Blocks[_player.selectedBlockIndex];
        //if (RemoveItem(new Item(selObj), 1))
        //{
        //}
        _eventSystem.DropItems(
            _player.transform.position + new Vector3(0.0f, 1.5f, 0.0f),
            _player.GetDropItemVelocity(),
            selObj, 1
        );
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

    public void AddItem(Item item, int amount)
    {
        if (!_toolbar.AddItem(item, amount))
        {
            _inventory.AddItem(item, amount);
        }
    }
    public bool RemoveItem(Item item, int amount)
    {
        return _toolbar.RemoveItem(item, amount);
    }

    private void OnApplicationQuit()
    {
        //ToolbarObj.Clear();
        //InventoryObj.Container.Clear();
        //EquipmentObj.Container.Clear();
    }
}

