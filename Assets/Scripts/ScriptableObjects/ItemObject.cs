

using UnityEngine;

public enum ItemType
{
    Food,
    Equipment,
    Default
}

//[CreateAssetMenu(menuName = "CubesURP/ItemObject")]
public class ItemObject : ScriptableObject
{
    public GameObject prefab;
}