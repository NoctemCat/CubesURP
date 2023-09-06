using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System;
using Cysharp.Threading.Tasks;

public class Toolbar : StaticInterface
{
    //World world;
    public Player player;
    public InputActionReference scrollRef;
    public RectTransform highlight;
    int slotIndex = 0;

    public override void Start()
    {
        base.Start();
        var inventorySystem = ServiceLocator.Get<InventorySystem>();
        inventorySystem.RegisterInventoryUI("Toolbar", this);
    }

    public override void CreateSlots()
    {
        base.CreateSlots();
        for (int i = 0; i < Inventory.Slots.Length; i++)
            Inventory.Slots[i].OnAfterUpdate += OnToolbarUpdate;
    }

    private void OnEnable()
    {
        // started, cancelled, performed 
        scrollRef.action.performed += ScrollHandler;
    }

    private void OnDisable()
    {
        scrollRef.action.performed -= ScrollHandler;
    }

    private void ScrollHandler(InputAction.CallbackContext context)
    {
        float scroll = context.ReadValue<Vector2>().y;
        if (scroll > 0f)
        {
            slotIndex--;
        }
        else if (scroll < 0f)
        {
            slotIndex++;
        }

        if (slotIndex > Slots.Length - 1)
        {
            slotIndex = 0;
        }
        if (slotIndex < 0)
        {
            slotIndex = Slots.Length - 1;
        }

        highlight.anchoredPosition = new(48f * slotIndex, 0f);

        UpdateSelectedBlock();
    }

    private void OnToolbarUpdate(InventorySlot slot)
    {
        UpdateSelectedBlock();
    }

    private void UpdateSelectedBlock()
    {
        InventorySlot item = slotsOnInterface[Slots[slotIndex]];

        if (item.Amount > 0 && item.ItemObject is BlockObject blockObject)
            player.selectedBlockIndex = (int)blockObject.BlockType;
        else
            player.selectedBlockIndex = -1;
    }
}

//[Serializable]
//public class ItemSlot
//{
//    public Block itemID;
//    public Image icon;
//}