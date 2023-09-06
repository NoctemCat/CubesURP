using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInventory : MonoBehaviour
{
    private World World;
    private EventSystem _eventSystem;
    private InventorySystem _inventorySystem;
    private Player _player;
    private InventoryObject _toolbar;
    private InventoryObject _inventory;
    private InventoryObject _creativeInventory;
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

    public bool creativeMode = false;

    private void Start()
    {
        World = World.Instance;
        _player = GetComponent<Player>();
        _eventSystem = ServiceLocator.Get<EventSystem>();

        //InventorySystem.
        _tooltip = ServiceLocator.Get<TooltipUi>();
        _inventorySystem = ServiceLocator.Get<InventorySystem>();

        _toolbar = new InventoryObject(9, "Toolbar");
        _inventory = new InventoryObject(24, "Player Inventory");
        _creativeInventory = new InventoryObject(24, "Creative Inventory");

        _inventorySystem.RegisterInventory(_toolbar);
        _inventorySystem.RegisterInventory(_inventory);
        _inventorySystem.RegisterInventory(_creativeInventory);

        _inventorySystem.Show(_toolbar);

        //ToolbarObj = inventorySystem.Get("Toolbar");
        //InventoryObj = inventorySystem.Get("Player Inventory");

        //inventorySystem.Show(ToolbarObj);
        PopulateCreativeInventory();
        InInventory = false;

        //eventSystem.OnResumeGame += SetCursorState;
        _eventSystem.StartListening(EventType.ResumeGame, SetCursorStateHandler);

        DropItemDelay = new(DropItem, _dropItemDelay);
    }

    private void PopulateCreativeInventory()
    {
        var items = ServiceLocator.Get<ItemDatabaseObject>();

        var itemsList = items.ItemObjects.ToList();
        var missing = (BlockObject)itemsList.Find((ItemObject item) => item is BlockObject block && block.BlockType == Block.Invalid);
        for (Block i = 0; i < Block.Invalid; i++)
        {
            int itemI = itemsList.FindIndex((ItemObject item) => item is BlockObject block && block.BlockType == i);
            if (itemI != -1)
            {
                _creativeInventory.AddItem(new(itemsList[itemI]), 99);
            }
        }
    }

    private void OnDestroy()
    {
        _eventSystem.StopListening(EventType.ResumeGame, SetCursorStateHandler);
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

        var inventory = creativeMode ? _creativeInventory : _inventory;

        if (InInventory) _inventorySystem.Show(inventory);
        else _inventorySystem.Hide(inventory);

        if (!InInventory)
        {
            _tooltip.HideTooltip();
        }
        SetCursorState();
    }

    UniTaskLoop DropItemDelay;
    private void OnDropItem(InputValue value)
    {
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
        DropItemsArgs itemsArgs = new()
        {
            origin = _player.transform.position + new Vector3(0.0f, 1.5f, 0.0f),
            velocity = _player.GetDropItemVelocity(),
            itemObject = selObj,
            amount = 1
        };
        _eventSystem.TriggerEvent(EventType.DropItems, itemsArgs);
    }

    public void SetCursorStateHandler(EventArgs _) => SetCursorState();
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

