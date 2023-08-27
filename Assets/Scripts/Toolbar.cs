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

    //public override void Start()
    //{
    //    base.Start();

    //}

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


    public override void Start()
    {
        base.Start();

        for (int i = 0; i < Inventory.GetSlots.Length; i++)
        {
            Inventory.GetSlots[i].OnAfterUpdate += OnToolbarUpdate;
            //Inventory.GetSlots[i].OnBeforeUpdate += OnBeforeUpdate;
        }
    }

    private void OnToolbarUpdate(InventorySlot slot)
    {
        UpdateSelectedBlock();
    }

    private void UpdateSelectedBlock()
    {
        InventorySlot item = slotsOnInterface[Slots[slotIndex]];

        if (item.Amount > 0 && item.ItemObject is BlockObject blockObject)
            player.selectedBlockIndex = (int)blockObject.blockType;
        else
            player.selectedBlockIndex = -1;
    }


    //private void Start()
    //{
    //    for (int i = 0; i < 9; i++)
    //    {
    //        itemSlots[i].icon = transform.GetChild(i).GetChild(0).GetComponent<Image>();
    //    }

    //    foreach (ItemSlot slot in itemSlots)
    //    {
    //        slot.icon.sprite = World.Instance.BlocksScObj.blocks[slot.itemID].icon;
    //        slot.icon.enabled = true;
    //    }

    //    player.selectedBlockIndex = itemSlots[slotIndex].itemID;
    //}
}

//[Serializable]
//public class ItemSlot
//{
//    public Block itemID;
//    public Image icon;
//}