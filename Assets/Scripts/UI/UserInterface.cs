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

    protected Dictionary<GameObject, InventorySlot> slotsOnInterface = new();

    public virtual void Start()
    {
        for (int i = 0; i < Inventory.GetSlots.Length; i++)
        {
            Inventory.GetSlots[i].Parent = this;
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
        {
            Sprite icon = slot.ItemObject.UIDisplay;
            uiSlot.Set(icon, slot.Amount.ToString("n0"), slot.Item.Name);
        }
    }

    private void OnBeforeUpdate(InventorySlot slot)
    {
    }


    private void OnEnterInterface(GameObject obj)
    {
        MouseData.InterfaceMouseIsOver = obj.GetComponent<UserInterface>();
    }

    private void OnExitInterface(GameObject obj)
    {
        MouseData.InterfaceMouseIsOver = null;
    }

    public abstract void CreateSlots();

    //void Update()
    //{
    //    slotsOnInterface.UpdateSlotDisplay();
    //}

    protected void AddEvent(GameObject obj, EventTriggerType type, UnityAction<BaseEventData> action)
    {
        EventTrigger trigger = obj.GetComponentInChildren<EventTrigger>();
        var eventTrigger = new EventTrigger.Entry
        {
            eventID = type
        };
        eventTrigger.callback.AddListener(action);
        trigger.triggers.Add(eventTrigger);
    }

    protected void OnEnter(GameObject obj)
    {
        MouseData.SlotHoverOver = obj;

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

    protected void OnExit(GameObject obj)
    {
        MouseData.SlotHoverOver = null;
        TooltipScreenSpaceUI.HideTooltip_Static();
    }

    protected void OnBeginDrag(GameObject obj)
    {
        MouseData.TempItemBeingDragged = CreateTempItem(obj);
    }

    protected void OnDrag(GameObject obj)
    {
        if (MouseData.TempItemBeingDragged != null)
        {
            TooltipScreenSpaceUI.HideTooltip_Static();
            MouseData.TempItemBeingDragged.GetComponent<RectTransform>().position = Mouse.current.position.ReadValue();
        }

    }

    protected void OnEndDrag(GameObject obj)
    {
        Destroy(MouseData.TempItemBeingDragged);

        if (MouseData.InterfaceMouseIsOver == null)
        {
            slotsOnInterface[obj].RemoveItem();
            return;
        }

        if (MouseData.SlotHoverOver && slotsOnInterface[obj].Item.Id >= 0 && MouseData.SlotHoverOver != obj)
        {
            InventorySlot mouseHoverSlotData = MouseData.InterfaceMouseIsOver.slotsOnInterface[MouseData.SlotHoverOver];
            if (!slotsOnInterface[obj].Item.Equals(mouseHoverSlotData.Item))
            {
                Inventory.SwapItems(slotsOnInterface[obj], mouseHoverSlotData);
            }
            else
            {
                Inventory.MergeItems(slotsOnInterface[obj], mouseHoverSlotData);
            }
        }
    }

    public GameObject CreateTempItem(GameObject obj)
    {
        GameObject tempItem = null;
        if (slotsOnInterface[obj].Item.Id >= 0)
        {
            tempItem = new GameObject();
            var rt = tempItem.AddComponent<RectTransform>();
            rt.sizeDelta = new(36, 36);
            tempItem.transform.SetParent(transform.parent.parent);

            var img = tempItem.AddComponent<Image>();
            img.sprite = slotsOnInterface[obj].ItemObject.UIDisplay;
            img.raycastTarget = false;
        }
        return tempItem;
    }
}

public static class MouseData
{
    public static UserInterface InterfaceMouseIsOver;
    public static GameObject TempItemBeingDragged;
    public static GameObject SlotHoverOver;
}

public static class ExtensionMethods
{
    public static void UpdateSlotDisplay(this Dictionary<GameObject, InventorySlot> slotsOnInterface)
    {
        foreach (var (obj, slot) in slotsOnInterface)
        {
            UIInventorySlot uiSlot = obj.GetComponent<UIInventorySlot>();
            if (slot.Item.Id < 0)
                uiSlot.Disable();
            else
            {
                Sprite icon = slot.ItemObject.UIDisplay;
                uiSlot.Set(icon, slot.Amount.ToString("n0"), slot.Item.Name);
            }
        }
    }
}