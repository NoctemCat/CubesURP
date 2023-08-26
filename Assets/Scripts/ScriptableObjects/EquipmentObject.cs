

using UnityEngine;

[CreateAssetMenu(fileName = "New Equipment Object", menuName = "Inventory System/Items/Equipment")]
public class EquipmentObject : ItemObject
{
    public int AtkBonus;
    public int DefenseBonus;

    public void Reset()
    {
        Type = ItemType.Chest;
    }
}