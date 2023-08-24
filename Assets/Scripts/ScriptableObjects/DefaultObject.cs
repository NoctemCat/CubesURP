

using UnityEngine;

[CreateAssetMenu(fileName = "New Default Object", menuName = "Inventory System/Items/Default")]
public class DefaultObject : ItemObject
{

    private void Reset()
    {
        type = ItemType.Default;
    }

}