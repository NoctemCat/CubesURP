
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public static class WorldHelper
{
    public static BlockObject[] InitBlocksMapping(ItemDatabaseObject items)
    {
        BlockObject[] blocks = new BlockObject[(int)Block.Invalid];

        var itemsList = items.ItemObjects.ToList();
        var missing = (BlockObject)itemsList.Find((ItemObject item) => item is BlockObject block && block.BlockType == Block.Invalid);
        for (Block i = 0; i < Block.Invalid; i++)
        {
            int itemI = itemsList.FindIndex((ItemObject item) => item is BlockObject block && block.BlockType == i);
            if (itemI != -1)
            {
                blocks[(int)i] = (BlockObject)itemsList[itemI];
            }
            else
            {
                blocks[(int)i] = missing;
            }
        }

        return blocks;
    }

    public static NativeArray<BlockStruct> InitNativeBlocksMapping(BlockObject[] blocks)
    {
        NativeArray<BlockStruct> nativeBlocks = new(blocks.Length, Allocator.Persistent);

        for (int i = 0; i < blocks.Length; i++)
        {
            nativeBlocks[i] = new(blocks[i]);
        }

        return nativeBlocks;
    }

    public static NativeArray<int3> InitXYZMap(VoxelData data)
    {
        NativeArray<int3> map = new(data.ChunkSize, Allocator.Persistent);

        int index = 0;
        for (int x = 0; x < data.ChunkWidth; x++)
        {
            for (int y = 0; y < data.ChunkHeight; y++)
            {
                for (int z = 0; z < data.ChunkLength; z++)
                {
                    map[index++] = new(x, y, z);
                }
            }
        }

        return map;
    }

    public static List<Vector3Int> InitViewCoords(int viewDistanceInChunks)
    {
        List<Vector3Int> check = new();
        for (int x = -viewDistanceInChunks; x <= viewDistanceInChunks; x++)
        {
            for (int y = -viewDistanceInChunks; y <= viewDistanceInChunks; y++)
            //for (int y = 0; y <= 0; y++)
            {
                for (int z = -viewDistanceInChunks; z <= viewDistanceInChunks; z++)
                {
                    //if (math.sqrt(x * x + y * y + z * z) <= viewDistanceInChunks)
                    //{
                    //}
                    check.Add(new(x, y, z));
                }
            }
        }

        check.Sort((Vector3Int a, Vector3Int b) =>
        {
            if (a.sqrMagnitude > b.sqrMagnitude)
                return 1;
            else if (a.sqrMagnitude < b.sqrMagnitude)
                return -1;
            else
                return 0;
        });
        return check;
    }

}