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
    public InventoryObject Inventory;
    public MouseDragData mouseDragData;

    protected Dictionary<GameObject, InventorySlot> slotsOnInterface = new();

    //private Transform _canvasTransform;

    public virtual void Start()
    {
        //_canvasTransform = transform.root.GetComponent<Canvas>().transform;
        for (int i = 0; i < Inventory.GetSlots.Length; i++)
        {
            Inventory.GetSlots[i].OnAfterUpdate += OnSlotUpdate;
            Inventory.GetSlots[i].OnBeforeUpdate += OnBeforeUpdate;
        }
        CreateSlots();
        AddEvent(gameObject, EventTriggerType.PointerEnter, delegate { OnEnterInterface(gameObject); });
        AddEvent(gameObject, EventTriggerType.PointerExit, delegate { OnExitInterface(gameObject); });
    }


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
        mouseDragData.OverInterface = true;
    }

    private void OnExitInterface(GameObject obj)
    {
        mouseDragData.OverInterface = false;
    }

    public abstract void CreateSlots();


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
        mouseDragData.SetHoverSlot(slotsOnInterface[obj]);

        InventorySlot item = slotsOnInterface[obj];

        if (item.Item.Id < 0)
        {
            TooltipScreenSpaceUI.HideTooltip_Static();
        }
        else
        {
            string tooltipText = $"{item.Item.Name}\n{item.Amount}".Trim();
            TooltipScreenSpaceUI.ShowTooltip_Static(tooltipText);
        }
    }

    protected void OnClick(BaseEventData eventData, GameObject obj)
    {
        PointerEventData pointerData = eventData as PointerEventData;
        if (pointerData.button == PointerEventData.InputButton.Left)
        {
            mouseDragData.SwapMerge(mouseDragData.Slot, slotsOnInterface[obj]);
        }
        else if (pointerData.button == PointerEventData.InputButton.Right)
        {
            HandleRightClick(obj);
        }
    }

    private void HandleRightClick(GameObject obj)
    {
        if (!mouseDragData.HasItem)
        {
            int halfCeil = Mathf.CeilToInt(slotsOnInterface[obj].Amount / 2f);
            InventorySlot slot = new(slotsOnInterface[obj].Item, halfCeil);

            slotsOnInterface[obj].AddAmount(-halfCeil);
            if (slotsOnInterface[obj].Amount <= 0) slotsOnInterface[obj].RemoveItem();
            mouseDragData.SwapMerge(mouseDragData.Slot, slot);

        }
        else
        {
            if (mouseDragData.Slot.Amount > 1 && mouseDragData.Slot.CanPlaceInSlot(mouseDragData.HoverSlot.ItemObject))
            {
                InventorySlot slot = new(mouseDragData.Slot.Item, 1);
                mouseDragData.Slot.AddAmount(-1);

                mouseDragData.SwapMerge(slot, mouseDragData.HoverSlot);
            }
            else
            {
                mouseDragData.SwapMerge(mouseDragData.Slot, mouseDragData.HoverSlot);
            }
        }
    }

    protected void OnExit(GameObject obj)
    {
        mouseDragData.SetHoverSlot(null);
        TooltipScreenSpaceUI.HideTooltip_Static();
    }

    protected void OnBeginDrag(BaseEventData eventData, GameObject obj)
    {
    }

    protected void OnDrag(GameObject obj)
    {
    }

    protected void OnEndDrag(BaseEventData eventData, GameObject obj)
    {
        if (!mouseDragData.OverInterface)
        {
            PointerEventData pointerData = eventData as PointerEventData;

            if (pointerData.button == PointerEventData.InputButton.Left)
                mouseDragData.DropAllItems();
            else if (pointerData.button == PointerEventData.InputButton.Right)
                mouseDragData.DropOneItem();
            return;
        }

        if (mouseDragData.HasItem)
        {
            mouseDragData.SwapMerge(mouseDragData.Slot, mouseDragData.HoverSlot);
        }
    }
}
