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
    public readonly int WorldSizeInChunks;
    public readonly int WorldSizeInVoxels;

    public readonly int TextureAtlasSizeInBlocks;
    public readonly float NormalizedBlockTextureSize;

    public readonly float3 RandomXYZ;

    public NativeArray<float3> VoxelVerts;
    public NativeArray<int3> FaceChecks;
    public NativeArray<int4> VoxelTris;
    public NativeArray<float3> VoxelNormals;

    public VoxelData(float3 _randomXYZ)
    {
        ChunkWidth = VoxelDataStatic.ChunkWidth;
        ChunkHeight = VoxelDataStatic.ChunkHeight;
        ChunkLength = VoxelDataStatic.ChunkLength;
        ChunkDimensions = new(ChunkWidth, ChunkHeight, ChunkLength);
        ChunkSize = VoxelDataStatic.ChunkSize;
        WorldSizeInChunks = VoxelDataStatic.WorldSizeInChunks;
        WorldSizeInVoxels = WorldSizeInChunks * ChunkWidth;
        TextureAtlasSizeInBlocks = VoxelDataStatic.TextureAtlasSizeInBlocks;
        NormalizedBlockTextureSize = VoxelDataStatic.NormalizedBlockTextureSize;
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
    }

    public void Dispose(JobHandle dependsOn = default)
    {
        VoxelVerts.Dispose(dependsOn);
        FaceChecks.Dispose(dependsOn);
        VoxelTris.Dispose(dependsOn);
        VoxelNormals.Dispose(dependsOn);
    }
}
