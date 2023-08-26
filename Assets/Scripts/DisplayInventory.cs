//using System;
//using System.Collections;
//using System.Collections.Generic;
//using TMPro;
//using UnityEngine;
//using UnityEngine.Events;
//using UnityEngine.EventSystems;
//using UnityEngine.UI;
//using UnityEngine.InputSystem;

//public class DisplayInventory : MonoBehaviour
//{
//    public MouseData MouseItem = new();

//    public InventoryObject Inventory;
//    public GameObject SlotPrefab;

//    readonly Dictionary<GameObject, InventorySlot> _itemsDisplayed = new();
//    Transform _content;

//    void Start()
//    {
//        _content = transform.Find("Viewport/InventoryContent");
//        for (int i = 0; i < Inventory.Container.Items.Length; i++)
//        {
//            GameObject obj = Instantiate(SlotPrefab, _content);
//            //UIInventorySlot uiSlot = obj.GetComponent<UIInventorySlot>();
//            _itemsDisplayed[obj] = Inventory.Container.Items[i];

//            AddEvent(obj, EventTriggerType.PointerEnter, delegate { OnEnter(obj); });
//            AddEvent(obj, EventTriggerType.PointerExit, delegate { OnExit(obj); });
//            AddEvent(obj, EventTriggerType.BeginDrag, delegate { OnBeginDrag(obj); });
//            AddEvent(obj, EventTriggerType.EndDrag, delegate { OnEndDrag(obj); });
//            AddEvent(obj, EventTriggerType.Drag, delegate { OnDrag(obj); });
//        }
//    }

//    void Update()
//    {
//        UpdateSlots();
//    }

//    public void UpdateSlots()
//    {
//        foreach (var (obj, slot) in _itemsDisplayed)
//        {
//            UIInventorySlot uiSlot = obj.GetComponent<UIInventorySlot>();
//            if (slot.ID < 0)
//                uiSlot.Disable();
//            else
//            {
//                uiSlot.Set(icon, slot.Amount.ToString("n0"), slot.Item.Name);
//            }
//        }
//    }

//    private void AddEvent(GameObject obj, EventTriggerType type, UnityAction<BaseEventData> action)
//    {
//        EventTrigger trigger = obj.GetComponentInChildren<EventTrigger>();
//        var eventTrigger = new EventTrigger.Entry
//        {
//            eventID = type
//        };
//        eventTrigger.callback.AddListener(action);
//        trigger.triggers.Add(eventTrigger);
//    }

//    private void OnEnter(GameObject obj)
//    {
//        MouseData.SlotHoverOver = obj;
//        if (_itemsDisplayed.TryGetValue(obj, out var item))
//            MouseData.HoverItem = item;
//    }

//    private void OnExit(GameObject obj)
//    {
//        MouseData.SlotHoverOver = null;
//        MouseData.HoverItem = null;
//    }

//    private void OnBeginDrag(GameObject obj)
//    {
//        var mouseObject = new GameObject();
//        var rt = mouseObject.AddComponent<RectTransform>();
//        rt.sizeDelta = new(36, 36);
//        mouseObject.transform.SetParent(transform.parent);

//        InventorySlot slot = _itemsDisplayed[obj];
//        if (slot.ID >= 0)
//        {
//            var img = mouseObject.AddComponent<Image>();
//            img.raycastTarget = false;
//        }
//        MouseData.TempItemBeingDragged = mouseObject;
//        MouseData.Item = _itemsDisplayed[obj];
//    }
//    private void OnDrag(GameObject obj)
//    {
//        if (MouseData.TempItemBeingDragged != null)
//        {
//            MouseData.TempItemBeingDragged.GetComponent<RectTransform>().position = Mouse.current.position.ReadValue();
//        }
//    }
//    private void OnEndDrag(GameObject obj)
//    {
//        if (MouseData.SlotHoverOver && MouseData.Item.ID >= 0)
//        {
//            Inventory.SwapItems(_itemsDisplayed[obj], _itemsDisplayed[MouseData.SlotHoverOver]);
//        }
//        else
//        {
//            Inventory.RemoveItem(_itemsDisplayed[obj]);
//        }
//        Destroy(MouseData.TempItemBeingDragged);
//        MouseData.Item = null;
//    }
//}
