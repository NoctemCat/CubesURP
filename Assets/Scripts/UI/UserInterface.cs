using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public abstract class UserInterface : MonoBehaviour
{
    protected InventoryObject Inventory = null;
    protected Dictionary<GameObject, InventorySlot> slotsOnInterface = new();

    private MouseDragData _mouseDragData;
    private TooltipUi _tooltip;

    public virtual void Start()
    {
        _tooltip = ServiceLocator.Get<TooltipUi>();
        _mouseDragData = ServiceLocator.Get<MouseDragData>();
        AddEvent(gameObject, EventTriggerType.PointerEnter, delegate { OnEnterInterface(gameObject); });
        AddEvent(gameObject, EventTriggerType.PointerExit, delegate { OnExitInterface(gameObject); });
    }

    public virtual void Load(InventoryObject inventory)
    {
        if (Inventory is not null && Inventory.Id == inventory.Id) return;

        Inventory = inventory;
        //_canvasTransform = transform.root.GetComponent<Canvas>().transform;
        CreateSlots();
        for (int i = 0; i < Inventory.Slots.Length; i++)
        {
            Inventory.Slots[i].OnAfterUpdate += OnSlotUpdate;
            Inventory.Slots[i].OnBeforeUpdate += OnBeforeUpdate;
        }

        for (int i = 0; i < Inventory.Slots.Length; i++)
        {
            UIInventorySlot uiSlot = Inventory.Slots[i].SlotDisplay.GetComponent<UIInventorySlot>();
            uiSlot.Init();
            OnSlotUpdate(Inventory.Slots[i]);
        }
    }

    public void Reset()
    {
        if (Inventory is not null)
        {
            for (int i = 0; i < Inventory.Slots.Length; i++)
            {
                Inventory.Slots[i].OnAfterUpdate -= OnSlotUpdate;
                Inventory.Slots[i].OnBeforeUpdate -= OnBeforeUpdate;
            }
            DestroySlots();


        }
    }


    public abstract void CreateSlots();
    public abstract void DestroySlots();

    private void OnSlotUpdate(InventorySlot slot)
    {
        UIInventorySlot uiSlot = slot.SlotDisplay.GetComponent<UIInventorySlot>();
        if (slot.Item.Id < 0)
            uiSlot.Disable();
        else
            uiSlot.Set(slot.ItemObject.UIDisplay, slot.Amount.ToString("n0"), slot.Item.Name);
    }

    private void OnBeforeUpdate(InventorySlot slot)
    {
    }


    private void OnEnterInterface(GameObject obj)
    {
        _mouseDragData.OverInterface = true;
    }

    private void OnExitInterface(GameObject obj)
    {
        _mouseDragData.OverInterface = false;
    }

    protected void AddEvent(GameObject obj, EventTriggerType type, UnityAction<BaseEventData> action)
    {
        EventTrigger trigger = obj.GetComponentInChildren<EventTrigger>();
        var eventTrigger = new EventTrigger.Entry
        {
            eventID = type,
        };
        eventTrigger.callback.AddListener(action);
        trigger.triggers.Add(eventTrigger);
    }

    protected void OnEnter(GameObject obj)
    {
        _mouseDragData.SetHoverSlot(slotsOnInterface[obj]);

        InventorySlot item = slotsOnInterface[obj];

        if (item.Item.Id < 0)
        {
            _tooltip.HideTooltip();
        }
        else
        {
            string tooltipText = $"{item.Item.Name}\n{item.Amount}".Trim();
            _tooltip.ShowTooltip(tooltipText);
        }
    }

    protected void OnClick(BaseEventData eventData, GameObject obj)
    {
        if (!_mouseDragData.OverInterface) return;

        PointerEventData pointerData = eventData as PointerEventData;
        if (pointerData.button == PointerEventData.InputButton.Left)
        {
            _mouseDragData.SwapMerge(_mouseDragData.Slot, slotsOnInterface[obj]);
        }
        else if (pointerData.button == PointerEventData.InputButton.Right)
        {
            HandleRightClick(obj);
        }
    }

    private void HandleRightClick(GameObject obj)
    {
        if (!_mouseDragData.HasItem)
        {
            int halfCeil = Mathf.CeilToInt(slotsOnInterface[obj].Amount / 2f);
            InventorySlot slot = new(slotsOnInterface[obj].Item, halfCeil);

            slotsOnInterface[obj].AddAmount(-halfCeil);
            if (slotsOnInterface[obj].Amount <= 0) slotsOnInterface[obj].RemoveItem();
            _mouseDragData.SwapMerge(_mouseDragData.Slot, slot);
        }
        else
        {
            if (_mouseDragData.Slot.Amount > 1 && _mouseDragData.Slot.CanPlaceInSlot(_mouseDragData.HoverSlot.ItemObject))
            {
                InventorySlot slot = new(_mouseDragData.Slot.Item, 1);
                _mouseDragData.Slot.AddAmount(-1);

                _mouseDragData.SwapMerge(slot, _mouseDragData.HoverSlot);
            }
            else
            {
                _mouseDragData.SwapMerge(_mouseDragData.Slot, _mouseDragData.HoverSlot);
            }
        }
    }

    protected void OnExit(GameObject obj)
    {
        _mouseDragData.SetHoverSlot(null);
        _tooltip.HideTooltip();
    }

    protected void OnBeginDrag(BaseEventData eventData, GameObject obj)
    {
    }

    protected void OnDrag(GameObject obj)
    {
    }

    protected void OnEndDrag(BaseEventData eventData, GameObject obj)
    {
        if (!_mouseDragData.OverInterface)
        {
            PointerEventData pointerData = eventData as PointerEventData;

            if (pointerData.button == PointerEventData.InputButton.Left)
                _mouseDragData.DropAllItems();
            else if (pointerData.button == PointerEventData.InputButton.Right)
                _mouseDragData.DropOneItem();
            return;
        }

        if (_mouseDragData.HasItem && _mouseDragData.HoverSlot is not null)
        {
            _mouseDragData.SwapMerge(_mouseDragData.Slot, _mouseDragData.HoverSlot);
        }
    }
}
