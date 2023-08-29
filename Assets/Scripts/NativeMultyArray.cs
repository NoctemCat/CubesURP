

using Unity.Collections;

//public struct NativeMultyArray<T>
//{
//    private NativeArray<T> holder;

//    public NativeMultyArray(int length, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.ClearMemory)
//    {
//        this.holder = new(length, allocator, options);
//    }
//}

public static class NativeExtensionMethods
{
    //public static void UpdateSlotDisplay(this Dictionary<GameObject, InventorySlot> slotsOnInterface)
    //{
    //    foreach (var (obj, slot) in slotsOnInterface)
    //    {
    //        UIInventorySlot uiSlot = obj.GetComponent<UIInventorySlot>();
    //        if (slot.Item.Id < 0)
    //            uiSlot.Disable();
    //        else
    //        {
    //            Sprite icon = slot.ItemObject.UIDisplay;
    //            uiSlot.Set(icon, slot.Amount.ToString("n0"), slot.Item.Name);
    //        }
    //    }
    //}

    //public static Block GetAt(this NativeArray<Block> blocks, int i)
    //{
    //    return blocks[i];
    //}
}