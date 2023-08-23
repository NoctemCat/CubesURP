using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "BlockDictionary", menuName = "Cubes/Block Dictionary")]
public class BlockDictionary : ScriptableObject
{
    public Dictionary<Block, BlockObject> blocks;
    public DictItem[] _blocks;

    public void Init()
    {
        PopulateDict();
    }

    public void PopulateDict()
    {
        if (blocks == null)
        {
            blocks = new();
            foreach (DictItem block in _blocks)
            {
                blocks[block.type] = block.block;
            }
        }
    }
}

[Serializable]
public class DictItem
{
    public Block type;
    public BlockObject block;
}
