using System;
using System.Linq;
using UnityEngine;

public enum ItemType
{
    Block,
    Food,
    Helmet,
    Weapon,
    Shield,
    Boots,
    Chest,
    Default
}

public enum Attributes
{
    Agility,
    Intellect,
    Stamina,
    Strength
}

//[CreateAssetMenu(menuName = "CubesURP/ItemObject")]
[Serializable]
public abstract class ItemObject : ScriptableObject
{
    public string itemName;
    public Sprite uiDisplay;
    public bool stackable;
    public ItemType type;
    [TextArea(15, 20)]
    public string decription;
    public Item data = new();
    public Item CreateItem()
    {
        Item item = new(this);
        return item;
    }

    protected virtual void OnEnable() { }
}

[Serializable]
public class Item
{
    public string itemName;
    public int id = -1;
    public ItemBuff[] buffs;
    public Item()
    {
        itemName = "";
        id = -1;
        buffs = new ItemBuff[0];
    }
    public Item(ItemObject item)
    {
        itemName = item.itemName;
        id = item.data.id;
        buffs = new ItemBuff[item.data.buffs.Length];

        for (int i = 0; i < item.data.buffs.Length; i++)
        {
            buffs[i] = new(item.data.buffs[i].min, item.data.buffs[i].max)
            {
                attribute = item.data.buffs[i].attribute
            };
            buffs[i].GenerateValue();
        }
    }

    public bool Equals(Item other)
    {
        return id == other.id && Enumerable.SequenceEqual(buffs, other.buffs);
    }
}

[Serializable]
public class ItemBuff
{
    public Attribute attribute;
    public int value;
    public int min;
    public int max;

    public ItemBuff(int _min, int _max)
    {
        min = _min;
        max = _max;
    }

    public void GenerateValue()
    {
        value = UnityEngine.Random.Range(min, max);
    }
}