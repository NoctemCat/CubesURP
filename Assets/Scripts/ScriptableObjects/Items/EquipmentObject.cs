

using UnityEngine;

[CreateAssetMenu(fileName = "New Equipment Object", menuName = "Inventory System/Items/Equipment")]
public class EquipmentObject : ItemObject
{
    public int atkBonus;
    public int defenseBonus;

    public void Reset()
    {
        type = ItemType.Chest;
    }
}