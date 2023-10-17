using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

public struct VoxelData
{
    public readonly int ChunkWidth;
    public readonly int ChunkHeight;
    public readonly int ChunkLength;
    public readonly int3 ChunkDimensions;
    public readonly int ChunkSize;

    public readonly int BiomeRegionLength;
    public readonly int BiomeRegionVoxelLength;

    public readonly int TextureAtlasSizeInBlocks;
    public readonly float NormalizedBlockTextureSize;

    public readonly uint seed;
    public readonly float3 RandomXYZ;

    public NativeArray<float3> VoxelVerts;
    public NativeArray<int3> FaceChecks;
    public NativeArray<int4> VoxelTris;
    public NativeArray<float3> VoxelNormals;
    public NativeArray<int3> xyzMap;
    public NativeArray<int2> xzMap;

    public Unity.Mathematics.Random rng;

    public VoxelData(uint _seed, float3 _randomXYZ, Unity.Mathematics.Random _rng)
    {
        ChunkWidth = VoxelDataStatic.ChunkWidth;
        ChunkHeight = VoxelDataStatic.ChunkHeight;
        ChunkLength = VoxelDataStatic.ChunkLength;
        ChunkDimensions = new(ChunkWidth, ChunkHeight, ChunkLength);
        ChunkSize = VoxelDataStatic.ChunkSize;

        BiomeRegionLength = VoxelDataStatic.BiomeRegionLength;
        BiomeRegionVoxelLength = VoxelDataStatic.BiomeRegionVoxelLength;

        TextureAtlasSizeInBlocks = VoxelDataStatic.TextureAtlasSizeInBlocks;
        NormalizedBlockTextureSize = VoxelDataStatic.NormalizedBlockTextureSize;

        seed = _seed;
        RandomXYZ = _randomXYZ;

        VoxelVerts = new(VoxelDataStatic.voxelVerts.Length, Allocator.Persistent);
        for (int i = 0; i < VoxelDataStatic.voxelVerts.Length; i++)
        {
            VoxelVerts[i] = new(VoxelDataStatic.voxelVerts[i]);
        }
        FaceChecks = new(VoxelDataStatic.faceChecks.Length, Allocator.Persistent);
        for (int i = 0; i < VoxelDataStatic.faceChecks.Length; i++)
        {
            FaceChecks[i] = new(VoxelDataStatic.faceChecks[i]);
        }
        VoxelTris = new(6, Allocator.Persistent);
        for (int i = 0; i < 6; i++)
        {
            VoxelTris[i] = new(
                VoxelDataStatic.voxelTris[i, 0],
                VoxelDataStatic.voxelTris[i, 1],
                VoxelDataStatic.voxelTris[i, 2],
                VoxelDataStatic.voxelTris[i, 3]
            );
        }
        VoxelNormals = new(VoxelDataStatic.voxelNormals.Length, Allocator.Persistent);
        for (int i = 0; i < VoxelDataStatic.voxelNormals.Length; i++)
        {
            VoxelNormals[i] = new(VoxelDataStatic.voxelNormals[i]);
        }

        rng = _rng;

        xyzMap = new(ChunkSize, Allocator.Persistent);
        int index = 0;
        for (int x = 0; x < ChunkWidth; x++)
        {
            for (int y = 0; y < ChunkHeight; y++)
            {
                for (int z = 0; z < ChunkLength; z++)
                {
                    xyzMap[index++] = new(x, y, z);
                }
            }
        }

        xzMap = new(ChunkWidth * ChunkLength, Allocator.Persistent);
        index = 0;
        for (int x = 0; x < ChunkWidth; x++)
        {
            for (int z = 0; z < ChunkLength; z++)
            {
                xzMap[index++] = new(x, z);
            }
        }
    }

    public void Dispose(JobHandle dependsOn = default)
    {
        VoxelVerts.Dispose(dependsOn);
        FaceChecks.Dispose(dependsOn);
        VoxelTris.Dispose(dependsOn);
        VoxelNormals.Dispose(dependsOn);
        xyzMap.Dispose(dependsOn);
        xzMap.Dispose(dependsOn);
    }
}
