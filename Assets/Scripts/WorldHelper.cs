
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public static class WorldHelper
{
    public static NativeArray<BlockStruct> InitBlocksMapping(BlockDictionary dict)
    {
        NativeArray<BlockStruct> blocks = new((int)Block.Invalid, Allocator.Persistent);

        for (Block i = 0; i < Block.Invalid; i++)
        {
            if (dict.Blocks.TryGetValue(i, out var blockObj))
            {
                blocks[(int)i] = new(blockObj);
            }
            else
            {
                blocks[(int)i] = new(dict.MissingBlock);
            }
        }

        return blocks;
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

        check.Sort(delegate (Vector3Int a, Vector3Int b)
        {
            if (a.sqrMagnitude > b.sqrMagnitude)
                return 1;
            if (a.sqrMagnitude < b.sqrMagnitude)
                return -1;
            else
                return 0;
        });
        return check;
    }

}