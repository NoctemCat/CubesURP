using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public static class CubesUtils
{
    public static Vector3Int I3ToVI3(int3 v) => new(v.x, v.y, v.z);
    public static int3 VI3ToI3(Vector3Int v) => new(v.x, v.y, v.z);

    public static float Get2DPerlin(in VoxelData data, float2 position, float offset, float scale)
    {
        return noise.cnoise(new float2(
            (position.x + 0.1f) / 16 * scale + offset + data.RandomXYZ.x,
            (position.y + 0.1f) / 16 * scale + offset + data.RandomXYZ.y)
        );
    }

    public static float Get3DPerlin(in VoxelData data, float3 position, float offset, float scale)
    {
        return noise.cnoise(new float3(
            (position.x + 0.1f) / 16 * scale + offset + data.RandomXYZ.x,
            (position.y + 0.1f) / 128 * scale + offset + data.RandomXYZ.y,
            (position.z + 0.1f) / 16 * scale + offset + data.RandomXYZ.z)
        );
    }

    public static Vector3Int GetChunkCoordFromVector3(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x / VoxelDataStatic.ChunkWidth);
        int y = Mathf.FloorToInt(pos.y / VoxelDataStatic.ChunkHeight);
        int z = Mathf.FloorToInt(pos.z / VoxelDataStatic.ChunkLength);
        return new(x, y, z);
    }
}