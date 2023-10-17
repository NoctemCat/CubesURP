using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

public static class CubesUtils
{
    public static Vector3Int I3ToVI3(int3 v) => new(v.x, v.y, v.z);
    public static int3 VI3ToI3(Vector3Int v) => new(v.x, v.y, v.z);

    public static float Get2DPerlin(in VoxelData data, float2 position, float2 offset, float scale)
    {
        return noise.cnoise(new float2(
            (position.x + 0.1f) / 16 * scale + offset.x + data.RandomXYZ.x,
            (position.y + 0.1f) / 16 * scale + offset.y + data.RandomXYZ.y)
        );
    }

    public static float Get3DPerlin(in VoxelData data, float3 position, float2 offset, float scale)
    {
        return noise.cnoise(new float3(
            (position.x + 0.1f) / 16 * scale + offset.x + data.RandomXYZ.x,
            (position.y + 0.1f) / 128 * scale + data.RandomXYZ.y,
            (position.z + 0.1f) / 16 * scale + offset.x + data.RandomXYZ.z)
        );
    }

    public static float GetHeight(float2 pos, float scale, UnsafeList<OctaveValues> octaves, NativeArray<float2> octavesOffsets)
    {
        float frequency = 1f;
        float amplitute = 1f;
        float noiseHeight = 0f;

        for (int i = 0; i < octaves.Length; i++)
        {
            float2 sample = (pos / scale + octavesOffsets[i]) * frequency;

            float perlinValue = octaves[i].noiseType switch
            {
                NoiseType.Perlin => noise.cnoise(sample),
                NoiseType.Simplex => noise.snoise(sample),
                NoiseType.PerlinInversed => -noise.cnoise(sample),
                NoiseType.SimplexInversed => -noise.snoise(sample),
                NoiseType.PerlinRidged => 1f - math.abs(noise.cnoise(sample)),
                NoiseType.SimplexRidged => 1f - math.abs(noise.snoise(sample)),
                NoiseType.PerlinRidgedInversed => -1f + math.abs(noise.cnoise(sample)),
                NoiseType.SimplexRidgedInversed => -1f + math.abs(noise.snoise(sample)),
                _ => throw new NotImplementedException(),
            };

            noiseHeight += perlinValue * amplitute;

            frequency *= octaves[i].noiseLacunarity;
            amplitute *= octaves[i].noisePersistence;
        }

        return noiseHeight;
    }

    public static float GetHeight(float3 pos, float scale, UnsafeList<OctaveValues> octaves, NativeArray<float3> octavesOffsets)
    {
        float frequency = 1f;
        float amplitute = 1f;
        float noiseHeight = 0f;

        for (int i = 0; i < octaves.Length; i++)
        {
            float3 sample = (pos / scale + octavesOffsets[i]) * frequency;

            float perlinValue = octaves[i].noiseType switch
            {
                NoiseType.Perlin => noise.cnoise(sample),
                NoiseType.Simplex => noise.snoise(sample),
                NoiseType.PerlinInversed => -noise.cnoise(sample),
                NoiseType.SimplexInversed => -noise.snoise(sample),
                NoiseType.PerlinRidged => 1f - math.abs(noise.cnoise(sample)),
                NoiseType.SimplexRidged => 1f - math.abs(noise.snoise(sample)),
                NoiseType.PerlinRidgedInversed => -1f + math.abs(noise.cnoise(sample)),
                NoiseType.SimplexRidgedInversed => -1f + math.abs(noise.snoise(sample)),
                _ => throw new NotImplementedException(),
            };

            noiseHeight += perlinValue * amplitute;

            frequency *= octaves[i].noiseLacunarity;
            amplitute *= octaves[i].noisePersistence;
        }

        return noiseHeight;
    }

    public static Vector3Int GetChunkCoord(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x / VoxelDataStatic.ChunkWidth);
        int y = Mathf.FloorToInt(pos.y / VoxelDataStatic.ChunkHeight);
        int z = Mathf.FloorToInt(pos.z / VoxelDataStatic.ChunkLength);
        return new(x, y, z);
    }

    public static Vector3Int GetBiomeRegion(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x / (VoxelDataStatic.ChunkWidth * VoxelDataStatic.BiomeRegionLength));
        int y = Mathf.FloorToInt(pos.y / (VoxelDataStatic.ChunkHeight * VoxelDataStatic.BiomeRegionLength));
        int z = Mathf.FloorToInt(pos.z / (VoxelDataStatic.ChunkLength * VoxelDataStatic.BiomeRegionLength));
        return new(x, y, z);
    }
    public static Vector3Int GetBiomeRegion(Vector3Int chunkPos)
    {
        int x = Mathf.FloorToInt(chunkPos.x / VoxelDataStatic.BiomeRegionLength);
        int y = Mathf.FloorToInt(chunkPos.y / VoxelDataStatic.BiomeRegionLength);
        int z = Mathf.FloorToInt(chunkPos.z / VoxelDataStatic.BiomeRegionLength);
        return new(x, y, z);
    }

    public static int2 GetBiomeRegion(in VoxelData data, float2 pos)
    {
        int x = (int)math.floor(pos.x / (data.ChunkWidth * data.BiomeRegionLength));
        int y = (int)math.floor(pos.y / (data.ChunkLength * data.BiomeRegionLength));
        //int z = (int)math.floor(pos.z / (data.ChunkLength * data.BiomeRegionLength));
        return new(x, y);
    }

    public static int CalcIndex(in VoxelData data, int3 xyz) => xyz.x * data.ChunkHeight * data.ChunkLength + xyz.y * data.ChunkLength + xyz.z;
    //public static int CalcIndex(in VoxelData data, int3 xzy) => xzy.x * data.ChunkLength * data.ChunkHeight + xzy.z * data.ChunkHeight + xzy.y;

    //public static 

}