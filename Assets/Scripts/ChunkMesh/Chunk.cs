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

public class Chunk
{
    private readonly GameObject _chunkObject;
    private readonly MeshRenderer _meshRenderer;
    private readonly MeshFilter _meshFilter;
    private readonly Mesh _chunkMesh;
    private readonly Material[] _materials = new Material[2];

    private readonly World World;
    VoxelData Data;
    public int3 ChunkPos;
    public Vector3 WorldPos;

    public NativeArray<Block> VoxelMap { get; private set; }
    private NativeList<StructureMarker> _structures;
    private NativeList<VoxelMod> _modifications;
    private MeshDataHolder _holder;

    private bool _voxelMapGenerated;
    private bool _isGeneratingMesh;
    private bool _dirtyMesh;

    private bool _requestingStop;
    private bool _isActive;
    public bool IsActive
    {
        get { return _isActive; }
        set
        {
            _isActive = value;
            _chunkObject.SetActive(value);
            _requestingStop = !value;
        }
    }

    public VoxelFlags NeighboursGenerated;
    public JobHandle VoxelMapAccess { get; private set; }
    private Action _initActions;

    public Chunk(Vector3Int chunkPos)
    {
        _initActions += CheckNeighbours;
        _initActions += AddStructuresToWorld;

        World = World.Instance;
        Data = World.VoxelData;
        ChunkPos = new(chunkPos.x, chunkPos.y, chunkPos.z);
        WorldPos = new(chunkPos.x * Data.ChunkWidth, chunkPos.y * Data.ChunkHeight, chunkPos.z * Data.ChunkLength);

        //WorldPoints = new Vector3[9];
        //WorldPoints[0] = WorldPos;
        //WorldPoints[1] = WorldPos + new Vector3(Data.ChunkWidth + 1f, 0f, 0f);
        //WorldPoints[2] = WorldPos + new Vector3(0f, 0f, Data.ChunkLength + 1f);
        //WorldPoints[3] = WorldPos + new Vector3(Data.ChunkWidth + 1f, 0f, Data.ChunkLength + 1f);
        //WorldPoints[4] = WorldPos + new Vector3(0f, Data.ChunkHeight + 1f, 0f);
        //WorldPoints[5] = WorldPos + new Vector3(Data.ChunkWidth + 1f, Data.ChunkHeight + 1f, 0f);
        //WorldPoints[6] = WorldPos + new Vector3(0f, Data.ChunkHeight + 1f, Data.ChunkLength + 1f);
        //WorldPoints[7] = WorldPos + new Vector3(Data.ChunkWidth + 1f, Data.ChunkHeight + 1f, Data.ChunkLength + 1f);

        //WorldPoints[8] = WorldPos + new Vector3(Data.ChunkWidth + 1f / 2f, Data.ChunkHeight + 1f / 2f, Data.ChunkLength + 1f / 2f);

        VoxelMap = new(Data.ChunkSize, Allocator.Persistent);
        _structures = new(100, Allocator.Persistent);
        _modifications = new(100, Allocator.Persistent);
        _holder.Init(ChunkPos);

        _voxelMapGenerated = false;
        _isGeneratingMesh = false;
        _requestingStop = false;

        _dirtyMesh = false;
        NeighboursGenerated = VoxelFlags.None;

        _chunkObject = new GameObject();
        _meshFilter = _chunkObject.AddComponent<MeshFilter>();
        _meshRenderer = _chunkObject.AddComponent<MeshRenderer>();

        _materials[0] = World.SolidMaterial;
        _materials[1] = World.TransparentMaterial;
        _meshRenderer.materials = _materials;

        _chunkObject.transform.SetParent(World.transform);
        _chunkObject.transform.position = WorldPos;
        _chunkObject.name = $"Chunk {ChunkPos.x}/{ChunkPos.y}/{ChunkPos.z}";

        _chunkMesh = new Mesh()
        {
            subMeshCount = 2
        };
        _chunkMesh.MarkDynamic();
        _meshFilter.mesh = _chunkMesh;

        IsActive = true;

        StartGenerating().Forget();
    }

    ~Chunk()
    {
        VoxelMap.Dispose();
        _modifications.Dispose();
        _structures.Dispose();
        _holder.Dispose();
    }

    public void CheckNeighbours()
    {
        if ((NeighboursGenerated & VoxelFlags.All) == VoxelFlags.All)
        {
            _dirtyMesh = true;
            _initActions -= CheckNeighbours;
        }
    }

    public void AddStructuresToWorld()
    {
        if (!_voxelMapGenerated) return;

        _initActions -= AddStructuresToWorld;

        if (_structures.Length > 0)
        {
            World.StructureBuilder.AddStructures(_structures).ContinueWith(() =>
            {
                _structures.Clear();
            });
        }
    }

    public void Update()
    {
        _initActions?.Invoke();

        if (_modifications.Length > 0 && !_isGeneratingMesh)
        {
            _dirtyMesh = true;
        }

        if (_dirtyMesh && !_isGeneratingMesh)
        {
            StartMeshGen().Forget();
        }

        if (_requestingStop && !_isGeneratingMesh)
        {
            _requestingStop = false;
        }
    }

    public async UniTaskVoid AddModification(VoxelMod mod)
    {
        await VoxelMapAccess;
        VoxelMapAccess.Complete();
        _modifications.Add(mod);
    }

    public async UniTaskVoid AddRangeModification(List<VoxelMod> mod)
    {
        await VoxelMapAccess;
        VoxelMapAccess.Complete();
        _modifications.AddRange(mod.ToNativeArray(Allocator.Temp));
    }

    public void MarkDirty()
    {
        _dirtyMesh = true;
    }

    async UniTaskVoid StartGenerating()
    {
        VoxelMapAccess = GenerateVoxelMap();
        _dirtyMesh = true;

        await VoxelMapAccess;
        _voxelMapGenerated = true;

        World.CheckNeighbours(this);
        if ((NeighboursGenerated & VoxelFlags.All) == VoxelFlags.All)
        {
            _initActions -= CheckNeighbours;
        }
    }

    private async UniTaskVoid StartMeshGen()
    {
        _isGeneratingMesh = true;
        //DirtyMesh = false;

        var isFinished = await GenerateMesh();
        if (isFinished)
        {
            _dirtyMesh = false;
            World.ChunkCreated(this);
        }
        else
        {
            _requestingStop = false;
        }

        _isGeneratingMesh = false;
    }

    JobHandle GenerateVoxelMap()
    {
        GenerateChunkJob generateChunk = new()
        {
            Data = Data,
            Biomes = World.Biomes,
            XYZMap = World.XYZMap,
            ChunkPos = ChunkPos,

            VoxelMap = VoxelMap,
            Structures = _structures.AsParallelWriter(),
        };
        return generateChunk.Schedule(VoxelMap.Length, 8);
    }

    async UniTask<bool> GenerateMesh()
    {
        if (_modifications.Length > 0)
            ApplyMods().Forget();

        VoxelMapAccess = _holder.CountBlockTypes(VoxelMapAccess, VoxelMap);

        await VoxelMapAccess;
        if (_holder.IsEmpty) return true;
        if (_requestingStop) return false;
        _holder.ResizeFacesData();

        VoxelMapAccess = _holder.SortVoxels(VoxelMap);
        await VoxelMapAccess;

        if (_requestingStop) return false;

        _holder.ResizeMeshData();

        await _holder.FillMeshData();

        if (_requestingStop) return false;

        _holder.BuildMesh(_chunkMesh);

        //_holder.

        return true;
    }


    private async UniTaskVoid ApplyMods()
    {
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
            Modifications = _modifications.AsArray(),
            VoxelMap = VoxelMap,
        };
        VoxelMapAccess = applyModsJob.Schedule(_modifications.Length, 1, JobHandle.CombineDependencies(neighbours.AsArray()));

        await VoxelMapAccess;
        _modifications.Clear();
    }
}

[BurstCompile]
public struct GenerateChunkJob : IJobParallelFor
{
    [ReadOnly]
    public VoxelData Data;
    [ReadOnly]
    public NativeArray<BiomeStruct> Biomes;
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

        // BIOME SELECTION PASS

        int solidGroundHeight = 42;
        float sumOfHeights = 0f;
        int count = 0;
        float strongestWeight = 0f;
        int strongestBiomeIndex = 0;

        for (int i = 0; i < Biomes.Length; i++)
        {
            float weight = Get2DPerlin(Data, new float2(pos.x, pos.z), Biomes[i].Offset, Biomes[i].Scale);

            if (weight > strongestWeight)
            {
                strongestWeight = weight;
                strongestBiomeIndex = i;
            }

            float height = Biomes[i].TerrainHeight * Get2DPerlin(Data, new float2(pos.x, pos.z), 0f, Biomes[i].TerrainScale) * weight;
            sumOfHeights += height;
            count++;
        }

        BiomeStruct biome = Biomes[strongestBiomeIndex];
        sumOfHeights /= count;


        // BASIC TERRAIN PASSS

        //int terrainHeight = (int)(math.floor(Biome.MaxTerrainHeight * Get2DPerlin(Data, new float2(pos.x, pos.z), 0f, Biome.TerrainScale)) + Biome.SolidGroundHeight)

        int terrainHeight = (int)math.floor(sumOfHeights + solidGroundHeight);

        Block voxelValue;

        if (pos.y == terrainHeight)
        {
            voxelValue = biome.SurfaceBlock;
        }
        else if (pos.y < terrainHeight && pos.y > terrainHeight - 4)
        {
            voxelValue = biome.SubsurfaceBlock;
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
            for (int i = 0; i < biome.Lodes.Length; i++)
            {
                LodeSctruct lode = biome.Lodes[i];
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
            if (Get2DPerlin(Data, new float2(pos.x, pos.z), 750, biome.FloraZoneScale) > biome.FloraZoneThreshold)
            {
                // Checking transparentness
                if (Get2DPerlin(Data, new float2(pos.x, pos.z), 1250, biome.FloraPlacementScale) > biome.FloraPlacementThreshold)
                {
                    Structures.AddNoResize(new(strongestBiomeIndex, pos, biome.FloraType));
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
        VoxelMap[CalcIndex(Modifications[i].Position)] = Modifications[i].Block;
    }

    readonly int CalcIndex(int3 xyz) => xyz.x * Data.ChunkHeight * Data.ChunkLength + xyz.y * Data.ChunkLength + xyz.z;
}

[BurstCompile]
public struct ClearListJob : IJob
{
    public NativeList<VoxelMod> List;

    public void Execute()
    {
        List.Clear();
    }
}