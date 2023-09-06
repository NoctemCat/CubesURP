using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Triggers;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MouseDragData : MonoBehaviour
{
    private EventSystem _eventSystem;
    private UIInventorySlot _uiInventorySlot;

    [field: NonSerialized] public InventorySlot Slot { get; private set; }
    [field: NonSerialized] public InventorySlot HoverSlot { get; private set; }

    private RectTransform _rect;
    public bool HasItem => Slot.Item is not null && Slot.Item.Id >= 0;
    public bool OverInterface { get; set; }

    [SerializeField] private Player _player;
    [SerializeField] private InputActionReference _leftMouse;
    [SerializeField] private InputActionReference _rightMouse;

    private void Awake()
    {
        ServiceLocator.Register(this);
    }

    private void Start()
    {
        _eventSystem = ServiceLocator.Get<EventSystem>();
        Slot = new();

        _rect = GetComponent<RectTransform>();
        _uiInventorySlot = GetComponent<UIInventorySlot>();
        OverInterface = false;

        Slot.SlotDisplay = gameObject;
        Slot.OnAfterUpdate += SlotUpdate;

        _leftMouse.action.performed += DropAllItemsCallback;
        _rightMouse.action.performed += DropOneItemCallback;
    }

    private void OnDestroy()
    {
        _leftMouse.action.performed -= DropAllItemsCallback;
        _rightMouse.action.performed -= DropOneItemCallback;

        ServiceLocator.Unregister(this);
    }

    private void SlotUpdate(InventorySlot slot)
    {
        if (slot.Item.Id < 0)
            _uiInventorySlot.Disable();
        else
            _uiInventorySlot.Set(slot.ItemObject.UIDisplay, slot.Amount.ToString("n0"), slot.Item.Name);
    }

    private void DropOneItemCallback(InputAction.CallbackContext context)
    {
        DropOneItem();
    }

    private void DropAllItemsCallback(InputAction.CallbackContext context)
    {
        DropAllItems();
    }


    public void DropOneItem()
    {
        if (!HasItem || OverInterface) return;
        Slot.AddAmount(-1);

        DropItemsArgs itemsArgs = new()
        {
            origin = _player.transform.position + new Vector3(0.0f, 1.5f, 0.0f),
            velocity = _player.GetDropItemVelocity(),
            itemObject = Slot.ItemObject,
            amount = 1
        };
        _eventSystem.TriggerEvent(EventType.DropItems, itemsArgs);

        if (Slot.Amount <= 0) Slot.RemoveItem();
    }

    public void DropAllItems()
    {
        if (!HasItem || OverInterface) return;

        DropItemsArgs itemsArgs = new()
        {
            origin = _player.transform.position + new Vector3(0.0f, 1.5f, 0.0f),
            velocity = _player.GetDropItemVelocity(),
            itemObject = Slot.ItemObject,
            amount = Slot.Amount
        };
        _eventSystem.TriggerEvent(EventType.DropItems, itemsArgs);
        Slot.RemoveItem();
    }

    private void Update()
    {
        if (HasItem)
            _rect.position = Mouse.current.position.ReadValue();
    }

    public void SwapMerge(InventorySlot slot1, InventorySlot slot2)
    {
        if (slot2.HasItem && slot2.Item.Equals(slot1.Item) && slot2.ItemObject.Stackable)
        {
            InventoryObject.MergeItems(slot1, slot2);
        }
        else
        {
            InventoryObject.SwapItems(slot1, slot2);
        }
    }

    public void SetHoverSlot(InventorySlot slot)
    {
        HoverSlot = slot;
    }
}

