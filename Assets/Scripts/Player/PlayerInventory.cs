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
    private Player _player;
    public InventoryObject ToolbarObj;
    public InventoryObject InventoryObj;
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
    private void Update()
    {
        //if (_dropItemAction.action.IsPressed())
        //{
        //    if (_player.selectedBlockIndex <= 0) return;

        //    Block selBlock = (Block)_player.selectedBlockIndex;
        //    BlockObject selObj = World.BlocksScObj.Blocks[selBlock];

        //    //if (RemoveItem(new Item(selObj), 1))
        //    //{
        //    //}
        //    GroundItemsPool.DropItems(
        //        _player.transform.position + new Vector3(0.0f, 1.5f, 0.0f),
        //        _player.GetDropItemVelocity(),
        //        selObj, 1
        //    );
        //}
    }

    //private readonly Collider[] _colliders = new Collider[10];
    //private void FixedUpdate()
    //{
    //    LayerMask mask = LayerMask.GetMask("GroundItems");
    //    int nums = Physics.OverlapSphereNonAlloc(transform.position, 0.5f, _colliders, mask);
    //    for (int i = 0; i < nums; i++)
    //    {
    //        if (gameObject == _colliders[i].gameObject) continue;

    //        if (_colliders[i].gameObject.TryGetComponent(out GroundItem other) && Time.time - other.CreateTime > 0.5f)
    //        {
    //            AddItem(other.ItemObj.Data, other.Amount);
    //            _colliders[i].gameObject.SetActive(false);
    //        }
    //    }
    //}

    public bool InInventory { get; private set; }

    [Range(0.1f, 2f)]
    [SerializeField] private float _dropItemDelay;
    private void Start()
    {
        World = World.Instance;
        _player = GetComponent<Player>();
        InInventory = false;

        World.OnResume += SetCursorState;

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

        _inventoryScreen.SetActive(InInventory);
        if (!InInventory)
        {
            TooltipScreenSpaceUI.HideTooltip_Static();
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
        Block selBlock = (Block)_player.selectedBlockIndex;
        BlockObject selObj = World.BlocksScObj.Blocks[selBlock];
        //if (RemoveItem(new Item(selObj), 1))
        //{
        //}
        GroundItemsPool.DropItems(
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

    public void OnTriggerEnter(Collider other)
    {
        //var item = other.GetComponent<GroundItem>();
        //if (item)
        //{
        //    if (InventoryObj.AddItem(new Item(item.ItemObj), 1))
        //    {
        //        Destroy(other.gameObject);
        //    }
        //}
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

