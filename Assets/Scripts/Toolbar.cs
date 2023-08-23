using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System;
using Cysharp.Threading.Tasks;

public class Toolbar : MonoBehaviour
{
    //World world;
    public Player player;
    public InputActionReference scrollRef;
    public RectTransform highlight;
    public ItemSlot[] itemSlots;

    int slotIndex = 0;

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

        if (slotIndex > itemSlots.Length - 1)
        {
            slotIndex = 0;
        }
        if (slotIndex < 0)
        {
            slotIndex = itemSlots.Length - 1;
        }

        highlight.anchoredPosition = new(24f * slotIndex, 0f);
        player.selectedBlockIndex = itemSlots[slotIndex].itemID;
    }


    private void Start()
    {
        for (int i = 0; i < 9; i++)
        {
            itemSlots[i].icon = transform.GetChild(i).GetChild(0).GetComponent<Image>();
        }

        foreach (ItemSlot slot in itemSlots)
        {
            slot.icon.sprite = World.Instance.BlocksScObj.blocks[slot.itemID].icon;
            slot.icon.enabled = true;
        }

        player.selectedBlockIndex = itemSlots[slotIndex].itemID;
    }
}

[Serializable]
public class ItemSlot
{
    public Block itemID;
    public Image icon;
}