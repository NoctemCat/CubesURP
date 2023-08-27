using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

[Serializable]
[CreateAssetMenu(fileName = "BlockDictionary", menuName = "Cubes/Block Dictionary")]
public class BlockDictionary : ScriptableObject
{
    private Dictionary<Block, BlockObject> _blocksDict = new();
    public Dictionary<Block, BlockObject> Blocks
    {
        get
        {
            if (_blocksDict.Count == 0)
                Init();

            return _blocksDict;
        }
    }
    public BlockObject MissingBlock;
    [SerializeField]
    private BlockObject[] _blocks;

    private void Init()
    {
        _blocksDict = new();
        for (int i = 0; i < _blocks.Length; i++)
        {
            if (_blocks[i] == null) continue;
            _blocksDict[_blocks[i].blockType] = _blocks[i];
        }

        for (Block block = Block.Air; block < Block.Invalid; block++)
        {
            if (!_blocksDict.ContainsKey(block))
            {
                _blocksDict[block] = MissingBlock;
            }
        }
    }

    //protected override void OnEnd()
    //{
    //    //throw new NotImplementedException();
    //}
}
