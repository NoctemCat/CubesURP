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
    public string Name;
    public Sprite UIDisplay;
    public bool Stackable;
    public ItemType Type;
    [TextArea(15, 20)]
    public string Decription;
    public Item Data = new();
    public Item CreateItem()
    {
        Item item = new(this);
        return item;
    }
}

[Serializable]
public class Item
{
    public string Name;
    public int Id = -1;
    public ItemBuff[] Buffs;
    public Item()
    {
        Name = "";
        Id = -1;
    }
    public Item(ItemObject item)
    {
        Name = item.Name;
        Id = item.Data.Id;
        Buffs = new ItemBuff[item.Data.Buffs.Length];

        for (int i = 0; i < item.Data.Buffs.Length; i++)
        {
            Buffs[i] = new(item.Data.Buffs[i].Min, item.Data.Buffs[i].Max)
            {
                Attribute = item.Data.Buffs[i].Attribute
            };
            Buffs[i].GenerateValue();
        }
    }

    public bool Equals(Item other)
    {
        return Id == other.Id && Enumerable.SequenceEqual(Buffs, other.Buffs);
    }
}

[Serializable]
public class ItemBuff
{
    public Attribute Attribute;
    public int Value;
    public int Min;
    public int Max;

    public ItemBuff(int _min, int _max)
    {
        Min = _min;
        Max = _max;
    }

    public void GenerateValue()
    {
        Value = UnityEngine.Random.Range(Min, Max);
    }
}