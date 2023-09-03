using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class DynamicInterface : UserInterface
{
    public GameObject SlotPrefab;
    public Transform Content;

    public override void CreateSlots()
    {
        slotsOnInterface = new();

        for (int i = 0; i < Inventory.GetSlots.Length; i++)
        {
            GameObject obj = Instantiate(SlotPrefab, Content);
            //UIInventorySlot uiSlot = obj.GetComponent<UIInventorySlot>();
            Inventory.GetSlots[i].SlotDisplay = obj;
            slotsOnInterface[obj] = Inventory.GetSlots[i];

            //delegate(BaseEventData eventData) { OnBeginDrag(eventData, obj); }
            AddEvent(obj, EventTriggerType.PointerDown, delegate (BaseEventData eventData) { OnClick(eventData, obj); });

            AddEvent(obj, EventTriggerType.PointerEnter, delegate (BaseEventData eventData) { OnEnter(obj); });
            AddEvent(obj, EventTriggerType.PointerExit, delegate (BaseEventData eventData) { OnExit(obj); });
            AddEvent(obj, EventTriggerType.BeginDrag, delegate (BaseEventData eventData) { OnBeginDrag(eventData, obj); });
            AddEvent(obj, EventTriggerType.EndDrag, delegate (BaseEventData eventData) { OnEndDrag(eventData, obj); });
            AddEvent(obj, EventTriggerType.Drag, delegate (BaseEventData eventData) { OnDrag(obj); });
        }
    }
}
