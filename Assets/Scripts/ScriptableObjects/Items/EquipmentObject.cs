

using UnityEngine;

[CreateAssetMenu(fileName = "New Equipment Object", menuName = "Inventory System/Items/Equipment")]
public class EquipmentObject : ItemObject, IDroppablePrefab
{
    public int atkBonus;
    public int defenseBonus;
    [field: SerializeField] public GameObject ItemPrefab { get; set; }
    [field: SerializeField] public float ItemScale { get; set; }
    [field: SerializeField] public Vector3 ColliderCenter { get; set; }
    [field: SerializeField] public Vector3 ColliderSize { get; set; }

    public void Reset()
    {
        type = ItemType.Weapon;
    }
}