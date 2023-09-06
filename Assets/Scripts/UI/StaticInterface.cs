using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class StaticInterface : UserInterface
{
    public GameObject[] Slots;

    public override void CreateSlots()
    {
        slotsOnInterface = new();
        //Slots = new GameObject[Inventory.Container.Items.Length];

        for (int i = 0; i < Slots.Length; i++)
        {
            //Slots[i] = transform.GetChild(i).gameObject;
            var obj = Slots[i];

            AddEvent(obj, EventTriggerType.PointerDown, delegate (BaseEventData eventData) { OnClick(eventData, obj); });
            AddEvent(obj, EventTriggerType.PointerEnter, delegate (BaseEventData eventData) { OnEnter(obj); });
            AddEvent(obj, EventTriggerType.PointerExit, delegate (BaseEventData eventData) { OnExit(obj); });
            AddEvent(obj, EventTriggerType.BeginDrag, delegate (BaseEventData eventData) { OnBeginDrag(eventData, obj); });
            AddEvent(obj, EventTriggerType.EndDrag, delegate (BaseEventData eventData) { OnEndDrag(eventData, obj); });
            AddEvent(obj, EventTriggerType.Drag, delegate (BaseEventData eventData) { OnDrag(obj); });

            Inventory.Slots[i].SlotDisplay = obj;
            slotsOnInterface[obj] = Inventory.Slots[i];
        }
    }

    public override void DestroySlots()
    {
        for (int i = 0; i < Slots.Length; i++)
        {
            var obj = Slots[i];

            EventTrigger trigger = obj.GetComponentInChildren<EventTrigger>();
            trigger.triggers.Clear();

            Inventory.Slots[i].SlotDisplay = null;
            slotsOnInterface.Remove(obj);
        }
    }
}