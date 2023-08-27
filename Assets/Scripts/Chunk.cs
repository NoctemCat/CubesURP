using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

using static CubesUtils;
public struct MeshData
{
    public NativeList<Vertex> Vertices;
    public NativeList<uint> SolidIndices;
    public NativeList<uint> TransparentIndices;
    public NativeArray<int2> VerticesRanges;

    public void InitLists()
    {
        Vertices = new NativeList<Vertex>(1, Allocator.Persistent);
        SolidIndices = new NativeList<uint>(1, Allocator.Persistent);
        TransparentIndices = new NativeList<uint>(1, Allocator.Persistent);
        VerticesRanges = new NativeArray<int2>(2, Allocator.Persistent);
    }

    public void Dispose()
    {
        Vertices.Dispose();
        SolidIndices.Dispose();
        TransparentIndices.Dispose();
        VerticesRanges.Dispose();
    }
}

public struct MeshFacesData
{
    public NativeList<VoxelDataForMesh> AllFaces;
    public NativeList<VoxelDataForMesh> SolidFaces;
    public NativeList<VoxelDataForMesh> TransparentFaces;
    public NativeReference<int> SolidOffset;

    public void InitLists()
    {
        AllFaces = new NativeList<VoxelDataForMesh>(1, Allocator.Persistent);
        SolidFaces = new NativeList<VoxelDataForMesh>(1, Allocator.Persistent);
        TransparentFaces = new NativeList<VoxelDataForMesh>(1, Allocator.Persistent);
        SolidOffset = new NativeReference<int>(0, Allocator.Persistent);
    }

    public void Dispose()
    {
        AllFaces.Dispose();
        SolidFaces.Dispose();
        TransparentFaces.Dispose();
        SolidOffset.Dispose();
    }
}

public struct Neighbours
{
    public NativeArray<Block> Back;
    public NativeArray<Block> Front;
    public NativeArray<Block> Top;
    public NativeArray<Block> Bottom;
    public NativeArray<Block> Right;
    public NativeArray<Block> Left;
}

public struct VoxelMod
{
    public int3 Position;
    public Block Block;

    public VoxelMod(int3 _pos, Block _block) => (Position, Block) = (_pos, _block);
}

public class Chunk
{
    private readonly World World;
    VoxelData Data;
    public int3 ChunkPos;
    public Vector3 WorldPos;

    public NativeArray<Block> VoxelMap;
    public NativeList<StructureMarker> Structures;
    public NativeList<VoxelMod> Modifications;
    private MeshDataHolder _holder;

    private readonly Mesh _chunkMesh;

    public bool IsMeshDrawable;
    public bool IsGeneratingMesh;
    public bool RequestingStop;
    public bool DirtyMesh;

    public int NeighboursGenerated;
    private bool AllNeighbours;
    //UniTask GenTask;

    public JobHandle VoxelMapAccess;

    //public JobHandle FillingMods;


    public Chunk(Vector3Int chunkPos)
    {
        World = World.Instance;
        Data = World.VoxelData;
        ChunkPos = new(chunkPos.x, chunkPos.y, chunkPos.z);
        WorldPos = new(chunkPos.x * Data.ChunkWidth, chunkPos.y * Data.ChunkHeight, chunkPos.z * Data.ChunkLength);

        VoxelMap = new(Data.ChunkSize, Allocator.Persistent);
        Structures = new(100, Allocator.Persistent);

        _chunkMesh = new Mesh()
        {
            subMeshCount = 2
        };
        _chunkMesh.MarkDynamic();

        Modifications = new(100, Allocator.Persistent);

        IsMeshDrawable = false;
        IsGeneratingMesh = false;
        //VoxelMapReady = false;
        RequestingStop = false;

        DirtyMesh = false;

        _holder.Init(ChunkPos);
        NeighboursGenerated = 0;
        AllNeighbours = false;

        rp = new()
        {
            material = World.SolidMaterial,
            shadowCastingMode = ShadowCastingMode.TwoSided,
            receiveShadows = true,
            renderingLayerMask = GraphicsSettings.defaultRenderingLayerMask
        };
        trp = new()
        {
            material = World.TransparentMaterial,
            shadowCastingMode = ShadowCastingMode.TwoSided,
            receiveShadows = true,
            renderingLayerMask = GraphicsSettings.defaultRenderingLayerMask
        };

        StartGenerating().Forget();
    }

    ~Chunk()
    {
        VoxelMap.Dispose();
        Modifications.Dispose();
        Structures.Dispose();
        _holder.Dispose();
    }

    public void Update()
    {
        if (!AllNeighbours && NeighboursGenerated >= 6)
        {
            AllNeighbours = true;
            DirtyMesh = true;
        }
        if (Modifications.Length > 0 && !IsGeneratingMesh)
        {
            DirtyMesh = true;
        }

        if (DirtyMesh && !IsGeneratingMesh)
        {
            StartMeshGen().Forget();
        }

        if (RequestingStop && !IsGeneratingMesh)
        {
            RequestingStop = false;
        }
    }
    RenderParams rp;
    RenderParams trp;

    public void Draw()
    {
        if (!IsMeshDrawable) return;

        Graphics.RenderMesh(rp, _chunkMesh, 0, Matrix4x4.Translate(WorldPos));
        Graphics.RenderMesh(trp, _chunkMesh, 1, Matrix4x4.Translate(WorldPos));
    }

    public async UniTaskVoid AddModification(VoxelMod mod)
    {
        await VoxelMapAccess;
        VoxelMapAccess.Complete();
        Modifications.Add(mod);
    }

    public async UniTaskVoid AddRangeModification(List<VoxelMod> mod)
    {
        await VoxelMapAccess;
        VoxelMapAccess.Complete();
        Modifications.AddRange(mod.ToNativeArray(Allocator.Temp));
    }

    //public void EditVoxel(Vector3 position, Block block)
    //{
    //    AddModification(new(World.GetPosInChunkFromVector3(ChunkPos, position), block)).Forget();
    //}

    public void MarkDirty()
    {
        DirtyMesh = true;
    }

    async UniTaskVoid StartGenerating()
    {
        VoxelMapAccess = GenerateVoxelMap();
        DirtyMesh = true;
        //World.AddChunkVoxelMap(ChunkPos, VoxelMap);
        await VoxelMapAccess;
        World.AddStructures(Structures).Forget();
    }


    public async UniTaskVoid StartMeshGen()
    {
        if (!IsGeneratingMesh)
        {
            IsGeneratingMesh = true;
            DirtyMesh = false;
            //DirtyMesh = false;

            var isFinished = await GenerateMesh();
            if (isFinished)
            {
                IsMeshDrawable = true;
                IsGeneratingMesh = false;

                World.ChunkCreated(I3ToVI3(ChunkPos));
            }
            else
            {
                RequestingStop = false;
                IsGeneratingMesh = false;
            }
        }
    }

    JobHandle GenerateVoxelMap()
    {
        GenerateChunkJob generateChunk = new()
        {
            Data = Data,
            Biome = World.Biome,
            XYZMap = World.XYZMap,
            ChunkPos = ChunkPos,

            VoxelMap = VoxelMap,
            Structures = Structures.AsParallelWriter(),
        };
        return generateChunk.Schedule(VoxelMap.Length, 8);
    }

    async UniTask<bool> GenerateMesh()
    {
        if (Modifications.Length > 0)
            await ApplyMods().ContinueWith(async () =>
            {
                await VoxelMapAccess;
                Modifications.Clear();
            });

        await _holder.CountBlockTypes(VoxelMapAccess, VoxelMap);
        if (_holder.IsEmpty) return true;

        if (RequestingStop) return false;

        _holder.ResizeFacesData();
        await VoxelMapAccess;

        JobHandle access = _holder.FillNeighbours(VoxelMapAccess, out Neighbours neighbours, ref NeighboursGenerated);

        VoxelMapAccess = _holder.SortVoxels(access, neighbours, VoxelMap);
        await VoxelMapAccess;

        if (RequestingStop) return false;

        _holder.ResizeMeshData();

        await _holder.FillMeshData();

        if (RequestingStop) return false;

        _holder.BuildMesh(_chunkMesh);

        return true;
    }


    public async UniTask ApplyMods()
    {
        await VoxelMapAccess;
        NativeList<JobHandle> neighbours = new(7, Allocator.Temp);
        for (VoxelFaces i = 0; i < VoxelFaces.Max; i++)
        {
            if (World.Chunks.TryGetValue(I3ToVI3(ChunkPos + Data.FaceChecks[(int)i]), out Chunk chunk))
                neighbours.Add(chunk.VoxelMapAccess);
        }
        neighbours.Add(VoxelMapAccess);

        ApplyModsJob applyModsJob = new()
        {
            Data = Data,
            Modifications = Modifications.AsArray(),
            VoxelMap = VoxelMap,
        };
        VoxelMapAccess = applyModsJob.Schedule(Modifications.Length, 1, JobHandle.CombineDependencies(neighbours.AsArray()));

        //DirtyMesh = true;
    }
}

[BurstCompile]
public struct GenerateChunkJob : IJobParallelFor
{
    [ReadOnly]
    public VoxelData Data;
    [ReadOnly]
    public BiomeStruct Biome;
    [ReadOnly]
    public NativeArray<int3> XYZMap;
    [ReadOnly]
    public int3 ChunkPos;

    [WriteOnly]
    //[NativeDisableContainerSafetyRestriction]
    public NativeArray<Block> VoxelMap;

    //[WriteOnly]
    //[NativeDisableContainerSafetyRestriction]
    public NativeList<StructureMarker>.ParallelWriter Structures;

    public void Execute(int i)
    {
        //ref var voxelData = ref VoxelDataRef.Data.Value;
        int3 scaledPos = new(
            ChunkPos.x * Data.ChunkWidth + XYZMap[i].x,
            ChunkPos.y * Data.ChunkHeight + XYZMap[i].y,
            ChunkPos.z * Data.ChunkLength + XYZMap[i].z
        );

        //int x = worldPos.x - (chunkPos.x * VoxelData.ChunkWidth);
        //int y = worldPos.y - (chunkPos.y * VoxelData.ChunkHeight);
        //int z = worldPos.z - (chunkPos.z * VoxelData.ChunkLength);
        // pos in chunk XYZMap[i]
        // chunk pos ChunkPos

        VoxelMap[i] = GetVoxel(ChunkPos, scaledPos);
    }

    public Block GetVoxel(int3 chunkPos, int3 pos)
    {
        // IMMUTABLE PASS
        //if (pos.y == 0)
        //    return Block.Bedrock;

        // BASIC TERRAIN PASSS
        //Debug.Log($"{chunkPos.x}, {chunkPos.y}, {chunkPos.z}");
        int terrainHeight = (int)(math.floor(Biome.MaxTerrainHeight * Get2DPerlin(Data, new float2(pos.x, pos.z), 0f, Biome.TerrainScale)) + Biome.SolidGroundHeight);

        Block voxelValue;

        if (pos.y == terrainHeight)
        {
            voxelValue = Block.Grass;
        }
        else if (pos.y < terrainHeight && pos.y > terrainHeight - 4)
        {
            voxelValue = Block.Dirt;
        }
        else if (pos.y > terrainHeight)
        {
            return Block.Air;
        }
        else
        {
            voxelValue = Block.Stone;
        }

        // SECOND PASS
        if (voxelValue == Block.Stone)
        {
            for (int i = 0; i < Biome.Lodes.Length; i++)
            {
                LodeSctruct lode = Biome.Lodes[i];
                if (pos.y > lode.MinHeight && pos.y < lode.MaxHeight)
                {
                    if (Get3DPerlin(Data, pos, lode.NoiseOffset, lode.Scale) > lode.Threshold)
                    {
                        voxelValue = lode.BlockID;
                    }
                }
            }
        }

        if (pos.y == terrainHeight)
        {
            if (Get2DPerlin(Data, new float2(pos.x, pos.z), 750, Biome.TreeZoneScale) > Biome.TreeZoneThreshold)
            {
                // Checking transparentness
                voxelValue = Block.Glass;
                if (Get2DPerlin(Data, new float2(pos.x, pos.z), 1250, Biome.TreePlacementScale) > Biome.TreePlacementThreshold)
                {
                    //Debug.Log("Tree placed");
                    // TODO re
                    Structures.AddNoResize(new(pos, StructureType.Tree));
                }
            }
        }

        return voxelValue;
    }


}

[BurstCompile]
public struct ApplyModsJob : IJobParallelFor
{
    [ReadOnly]
    public VoxelData Data;
    [ReadOnly]
    public NativeArray<VoxelMod> Modifications;

    [WriteOnly]
    [NativeDisableParallelForRestriction]
    public NativeArray<Block> VoxelMap;

    public void Execute(int i)
    {
        int3 pos = Modifications[i].Position;
        //int index = pos.x * Data.ChunkHeight * Data.ChunkLength + pos.y * Data.ChunkLength + pos.z;

        VoxelMap[CalcIndex(pos)] = Modifications[i].Block;
    }

    readonly int CalcIndex(int3 xyz) => xyz.x * Data.ChunkHeight * Data.ChunkLength + xyz.y * Data.ChunkLength + xyz.z;
}