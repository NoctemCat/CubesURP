using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public static class VoxelDataStatic
{
    public static int ChunkWidth = 16;
    public static int ChunkHeight = 128;
    public static int ChunkLength = 16;
    public static int ChunkSize = ChunkWidth * ChunkHeight * ChunkLength;
    public static int WorldSizeInChunks = 100;
    public static int WorldSizeInVoxels = WorldSizeInChunks * ChunkWidth;
    public static int ViewDistanceInChunks = 6;

    public static int TextureAtlasSizeInBlocks = 16;
    public static float NormalizedBlockTextureSize => 1.0f / TextureAtlasSizeInBlocks;

    public static readonly Vector3[] voxelVerts = {
        new Vector3(0.0f, 0.0f, 0.0f),
        new Vector3(1.0f, 0.0f, 0.0f),
        new Vector3(1.0f, 1.0f, 0.0f),
        new Vector3(0.0f, 1.0f, 0.0f),
        new Vector3(0.0f, 0.0f, 1.0f),
        new Vector3(1.0f, 0.0f, 1.0f),
        new Vector3(1.0f, 1.0f, 1.0f),
        new Vector3(0.0f, 1.0f, 1.0f),
    };

    // Adjusted for holes
    //public static readonly Vector3[] voxelVerts = {
    //    new Vector3(-0.01f, -0.01f, -0.01f),
    //    new Vector3(1.001f, -0.01f, -0.01f),
    //    new Vector3(1.001f, 1.001f, -0.01f),
    //    new Vector3(-0.01f, 1.001f, -0.01f),
    //    new Vector3(-0.01f, -0.01f, 1.001f),
    //    new Vector3(1.001f, -0.01f, 1.001f),
    //    new Vector3(1.001f, 1.001f, 1.001f),
    //    new Vector3(-0.01f, 1.001f, 1.001f),
    //};


    public static readonly Vector3[] faceChecks = {
        new Vector3(0.0f, 0.0f, -1.0f),
        new Vector3(0.0f, 0.0f, 1.0f),
        new Vector3(0.0f, 1.0f, 0.0f),
        new Vector3(0.0f, -1.0f, 0.0f),
        new Vector3(-1.0f, 0.0f, 0.0f),
        new Vector3(1.0f, 0.0f, 0.0f),
    };

    public static readonly int[,] voxelTris = {
        // Back, Front, Top, Bottom, Left, Right

        {0, 3, 1, 2}, // Back Face
        {5, 6, 4, 7}, // Front Face
        {3, 7, 2, 6}, // Top Face
        {1, 5, 0, 4}, // Bottom Face
        {4, 7, 0, 3}, // Left Face
        {1, 2, 5, 6}, // Right Face
    };

    public static readonly Vector2[] voxelUvs = {
        new Vector2(0.0f, 0.0f),
        new Vector2(0.0f, 1.0f),
        new Vector2(1.0f, 0.0f),
        new Vector2(1.0f, 1.0f),
    };

    public static readonly Vector3[] voxelNormals = {
        new Vector3(0f, 0f, -1f),
        new Vector3(0f, 0f, 1f),
        new Vector3(0f, 1f, 0f),
        new Vector3(0f, -1f, 0f),
        new Vector3(-1f, 0f, 0f),
        new Vector3(1f, 0f, 0f),
    };
}

public enum VoxelFaces
{
    Back,
    Front,
    Top,
    Bottom,
    Left,
    Right,
    Max,
}


// Set someState |= State.IsCool;
// Unset someState &= ~State.IsCool;
// someState.HasFlag(State.IsCool)
[Flags]
public enum VoxelFlags : byte
{
    None = 0,
    Back = 1 << 1,
    Front = 1 << 2,
    Top = 1 << 3,
    Bottom = 1 << 4,
    Left = 1 << 5,
    Right = 1 << 6,
}