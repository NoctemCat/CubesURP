
using UnityEngine.UI;

public struct ItemStack
{
    public Block block;
    public int amount;
}

public struct UIItemSlot
{
    public Image slotImage;
    public Image slotIcon;
}

public class ItemSlot
{
    private ItemStack _stack;
    private UIItemSlot _uiItemSlot;
}