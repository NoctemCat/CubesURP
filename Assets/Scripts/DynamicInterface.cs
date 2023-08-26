using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class DynamicInterface : UserInterface
{
    public GameObject SlotPrefab;
    Transform _content;

    public override void CreateSlots()
    {
        slotsOnInterface = new();
        _content = transform.Find("Viewport/InventoryContent");

        for (int i = 0; i < Inventory.GetSlots.Length; i++)
        {
            GameObject obj = Instantiate(SlotPrefab, _content);
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
