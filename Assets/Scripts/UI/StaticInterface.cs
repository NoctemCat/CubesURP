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

            AddEvent(obj, EventTriggerType.PointerEnter, delegate { OnEnter(obj); });
            AddEvent(obj, EventTriggerType.PointerExit, delegate { OnExit(obj); });
            AddEvent(obj, EventTriggerType.BeginDrag, delegate { OnBeginDrag(obj); });
            AddEvent(obj, EventTriggerType.EndDrag, delegate { OnEndDrag(obj); });
            AddEvent(obj, EventTriggerType.Drag, delegate { OnDrag(obj); });

            Inventory.GetSlots[i].SlotDisplay = obj;
            slotsOnInterface[obj] = Inventory.Container.Slots[i];
        }
    }
}