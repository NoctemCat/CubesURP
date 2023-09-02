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

            AddEvent(obj, EventTriggerType.PointerEnter, delegate { OnEnter(obj); });
            AddEvent(obj, EventTriggerType.PointerExit, delegate { OnExit(obj); });
            AddEvent(obj, EventTriggerType.BeginDrag, delegate { OnBeginDrag(obj); });
            AddEvent(obj, EventTriggerType.EndDrag, delegate { OnEndDrag(obj); });
            AddEvent(obj, EventTriggerType.Drag, delegate { OnDrag(obj); });
        }
    }
}
