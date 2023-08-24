using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public static class Structure
{
    //public static List<VoxelMod> MakeTree(in VoxelData data, ref List<VoxelMod> mods, int3 pos, int minTrunkHeight, int maxTrunkHeight)
    //{
    //    //List<VoxelMod> stack = new(20);

    //    int height = (int)(maxTrunkHeight * Noise.Get2DPerlin(data, new(pos.x, pos.z), 2000f, 3f));

    //    if (height < minTrunkHeight)
    //    {
    //        height = minTrunkHeight;
    //    }

    //    // Leaves
    //    for (int x = -2; x < 3; x++)
    //    {
    //        for (int z = -2; z < 3; z++)
    //        {
    //            mods.Add(new VoxelMod(new(pos.x + x, pos.y + height - 2, pos.z + z), Block.Leaves));
    //            mods.Add(new VoxelMod(new(pos.x + x, pos.y + height - 3, pos.z + z), Block.Leaves));
    //        }
    //    }

    //    for (int x = -1; x < 2; x++)
    //    {
    //        for (int z = -1; z < 2; z++)
    //        {
    //            mods.Add(new VoxelMod(new(pos.x + x, pos.y + height - 1, pos.z + z), Block.Leaves));
    //        }
    //    }
    //    for (int x = -1; x < 2; x++)
    //    {
    //        if (x == 0)
    //            for (int z = -1; z < 2; z++)
    //            {
    //                mods.Add(new VoxelMod(new(pos.x + x, pos.y + height, pos.z + z), Block.Leaves));
    //            }
    //        else
    //            mods.Add(new VoxelMod(new(pos.x + x, pos.y + height, pos.z), Block.Leaves));
    //    }

    //    // Trunk
    //    for (int i = 1; i < height; i++)
    //    {
    //        mods.Add(new VoxelMod(new(pos.x, pos.y + i, pos.z), Block.Wood));
    //    }

    //    return mods;
    //}



}
