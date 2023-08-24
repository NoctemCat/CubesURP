using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DisplayInventory : MonoBehaviour
{
    public InventoryObject Inventory;
    public GameObject SlotPrefab;

    readonly Dictionary<InventorySlot, UIInventorySlot> _itemsDisplayed = new();
    Transform _content;

    void Start()
    {
        _content = transform.Find("Viewport/InventoryContent");
        for (int i = 0; i < Inventory.Capacity; i++)
        {
            Instantiate(SlotPrefab, _content);
        }
        for (int i = 0; i < Inventory.Container.Count; i++)
        {
            InventorySlot slot = Inventory.Container[i];
            UIInventorySlot child = _content.GetChild(i).GetComponent<UIInventorySlot>();
            child.Set(slot.Item.icon, slot.Amount.ToString("n0"));
            _itemsDisplayed[slot] = child;
        }
    }

    void Update()
    {
        for (int i = 0; i < Inventory.Container.Count; i++)
        {
            InventorySlot slot = Inventory.Container[i];
            if (_itemsDisplayed.TryGetValue(slot, out UIInventorySlot child))
            {
                child.Set(slot.Item.icon, slot.Amount.ToString("n0"));
            }
            else
            {
                child = _content.GetChild(i).GetComponent<UIInventorySlot>();
                child.Set(Inventory.Container[i].Item.icon, Inventory.Container[i].Amount.ToString("n0"));
                _itemsDisplayed[slot] = child;
            }
        }
    }
}
