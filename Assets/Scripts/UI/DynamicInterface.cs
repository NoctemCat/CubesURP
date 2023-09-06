using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

public class DynamicInterface : UserInterface
{
    public GameObject SlotPrefab;
    public Transform Content;

    public override void CreateSlots()
    {
        slotsOnInterface = new();

        for (int i = 0; i < Inventory.Slots.Length; i++)
        {
            GameObject obj = Instantiate(SlotPrefab, Content);
            Inventory.Slots[i].SlotDisplay = obj;
            slotsOnInterface[obj] = Inventory.Slots[i];

            AddEvent(obj, EventTriggerType.PointerDown, delegate (BaseEventData eventData) { OnClick(eventData, obj); });
            AddEvent(obj, EventTriggerType.PointerEnter, delegate (BaseEventData eventData) { OnEnter(obj); });
            AddEvent(obj, EventTriggerType.PointerExit, delegate (BaseEventData eventData) { OnExit(obj); });
            AddEvent(obj, EventTriggerType.BeginDrag, delegate (BaseEventData eventData) { OnBeginDrag(eventData, obj); });
            AddEvent(obj, EventTriggerType.EndDrag, delegate (BaseEventData eventData) { OnEndDrag(eventData, obj); });
            AddEvent(obj, EventTriggerType.Drag, delegate (BaseEventData eventData) { OnDrag(obj); });
        }
    }

    public override void DestroySlots()
    {
        for (int i = 0; i < Inventory.Slots.Length; i++)
        {
            GameObject obj = Inventory.Slots[i].SlotDisplay;

            EventTrigger trigger = obj.GetComponentInChildren<EventTrigger>();
            trigger.triggers.Clear();

            slotsOnInterface.Remove(obj);
            Inventory.Slots[i].SlotDisplay = null;

            Destroy(obj);
        }
    }
}
