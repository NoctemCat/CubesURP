using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MemoryPack;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections.NotBurstCompatible;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

using static CubesUtils;


public class Chunk
{
    private GameObject _chunkObject;
    private MeshRenderer _meshRenderer;
    private MeshFilter _meshFilter;
    private Mesh _chunkMesh;
    private Material[] _materials = new Material[2];
    public string ChunkName { get; private set; }
    public int3 ChunkPos { get; private set; }
    public Vector3 WorldPos { get; private set; }

    private readonly World _world;
    private VoxelData _data;

    public NativeArray<Block> VoxelMap { get; private set; }
    public NativeList<VoxelMod> NeighbourModifications { get; private set; }
    private NativeList<StructureMarker> _structures;
    private NativeList<VoxelMod> _modifications;
    private MeshDataHolder _holder;

    private bool _isLoaded;
    private bool _voxelMapGenerated;
    private bool _isGeneratingMesh;
    private bool _dirtyMesh;
    public bool IsDisposed { get; private set; }

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

    public VoxelFlags NeighboursGenerated { get; private set; }
    public JobHandle VoxelMapAccess { get; private set; }
    private Action _initActions;

    public Chunk(Vector3Int chunkPos)
    {
        _world = World.Instance;
        Init(chunkPos);
        _isLoaded = false;

        StartGenerating().Forget();
    }
    public Chunk(ChunkData chunkData)
    {
        _world = World.Instance;
        Init(chunkData.ChunkPos);
        _isLoaded = true;

        //StartGenerating().Forget();

        //chunkData.VoxelMap.
        VoxelMap.CopyFrom(chunkData.VoxelMap);
        NeighbourModifications.CopyFromNBC(chunkData.NeighbourModifications.ToArray());
        //NeighbourModifications.AddRangeNoResize()

        _voxelMapGenerated = true;
        _dirtyMesh = true;

        _world.CheckNeighbours(this);
        if ((NeighboursGenerated & VoxelFlags.All) == VoxelFlags.All)
        {
            _initActions -= CheckNeighbours;
        }
    }

    private void Init(Vector3Int chunkPos)
    {
        _initActions += CheckNeighbours;
        _initActions += AddStructuresToWorld;

        _data = _world.VoxelData;
        ChunkPos = new(chunkPos.x, chunkPos.y, chunkPos.z);
        WorldPos = new(chunkPos.x * _data.ChunkWidth, chunkPos.y * _data.ChunkHeight, chunkPos.z * _data.ChunkLength);

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

        VoxelMap = new(_data.ChunkSize, Allocator.Persistent);
        _structures = new(100, Allocator.Persistent);
        NeighbourModifications = new(512, Allocator.Persistent);
        _modifications = new(100, Allocator.Persistent);

        _voxelMapGenerated = false;
        _isGeneratingMesh = false;
        _requestingStop = false;

        IsDisposed = false;
        _dirtyMesh = false;
        NeighboursGenerated = VoxelFlags.None;

        _chunkObject = new GameObject();
        _meshFilter = _chunkObject.AddComponent<MeshFilter>();
        _meshRenderer = _chunkObject.AddComponent<MeshRenderer>();

        _materials[0] = _world.SolidMaterial;
        _materials[1] = _world.TransparentMaterial;
        _meshRenderer.materials = _materials;

        _chunkObject.transform.SetParent(_world.transform);
        _chunkObject.transform.position = WorldPos;
        _chunkObject.name = PathHelper.GenerateChunkName(ChunkPos);
        ChunkName = _chunkObject.name;

        _chunkMesh = new Mesh()
        {
            subMeshCount = 2
        };
        _chunkMesh.MarkDynamic();
        _meshFilter.mesh = _chunkMesh;

        IsActive = true;
    }

    ~Chunk()
    {
    }

    public void Dispose()
    {
        IsActive = false;
        IsDisposed = true;
        VoxelMapAccess.Complete();
        for (VoxelFaces i = 0; i < VoxelFaces.Max; i++)
        {
            if (_world.Chunks.TryGetValue(I3ToVI3(ChunkPos + _data.FaceChecks[(int)i]), out Chunk chunk))
                chunk.VoxelMapAccess.Complete();
        }
        NeighbourModifications.Dispose();
        _modifications.Dispose();
        _structures.Dispose();
        //_holder.Dispose();
        UnityEngine.Object.Destroy(_chunkMesh);
        UnityEngine.Object.Destroy(_chunkObject);
        VoxelMap.Dispose();
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

        if (NeighbourModifications.Length > 0)
        {
            _world.StructureSystem.AddStructures(NeighbourModifications);
        }
    }

    public void Update()
    {
        //if (_isDisposed) return;

        _initActions?.Invoke();

        //if (Input.GetKeyDown(KeyCode.LeftAlt))
        //{
        //    SaveVoxelMap().Forget();
        //}
        //if (Input.GetKeyDown(KeyCode.Tab))
        //{
        //    LoadVoxelMapNorm();
        //}
        if (_modifications.Length > 0 && !_isGeneratingMesh)
        {
            _dirtyMesh = true;
        }

        if (_voxelMapGenerated && _dirtyMesh && !_isGeneratingMesh)
        {
            StartMeshGen().Forget();
        }

        if (_requestingStop && !_isGeneratingMesh)
        {
            _requestingStop = false;
        }
    }

    public void AddNeighbours(VoxelFlags neighbours)
    {
        NeighboursGenerated |= neighbours;
    }


    public async UniTaskVoid AddModification(VoxelMod mod)
    {
        //_safeDelete = true;
        await VoxelMapAccess;
        VoxelMapAccess.Complete();
        _modifications.Add(mod);
    }

    // Right now it's only being used when adding structures from generation,
    public async UniTask AddRangeModification(List<VoxelMod> mod)
    {
        if (_isLoaded) return;
        //Debug.Log(mod.Count);
        await VoxelMapAccess;
        VoxelMapAccess.Complete();
        if (IsDisposed) return;
        _modifications.AddRange(mod.ToNativeArray(Allocator.Temp));
    }

    public void MarkDirty()
    {
        _dirtyMesh = true;
    }

    async UniTaskVoid StartGenerating()
    {
        VoxelMapAccess = GenerateVoxelMap();
        await VoxelMapAccess;
        if (IsDisposed) return;

        _structures.Clear();

        _voxelMapGenerated = true;
        _dirtyMesh = true;

        _world.CheckNeighbours(this);
        if ((NeighboursGenerated & VoxelFlags.All) == VoxelFlags.All)
        {
            _initActions -= CheckNeighbours;
        }
    }

    private async UniTaskVoid StartMeshGen()
    {
        _isGeneratingMesh = true;
        _holder.Init(ChunkPos);

        var isFinished = await GenerateMesh();
        if (isFinished)
        {
            _dirtyMesh = false;
            _world.ChunkCreated(this);
        }
        else
        {
            _requestingStop = false;
        }

        _holder.Dispose();
        _isGeneratingMesh = false;
    }

    JobHandle GenerateVoxelMap()
    {
        GenerateChunkJob generateChunk = new()
        {
            Data = _data,
            Biomes = _world.Biomes,
            XYZMap = _world.XYZMap,
            ChunkPos = ChunkPos,

            VoxelMap = VoxelMap,
            Structures = _structures.AsParallelWriter(),
        };

        BuildStructuresJob buildStructures = new()
        {
            Data = _data,
            Biomes = _world.Biomes,
            ChunkPos = ChunkPos,
            Structures = _structures.AsDeferredJobArray(),

            VoxelMap = VoxelMap,
            NeighbourModifications = NeighbourModifications.AsParallelWriter(),
        };

        JobHandle generateHandle = generateChunk.Schedule(VoxelMap.Length, 8);
        JobHandle structuresHandle = buildStructures.Schedule(_structures, 8, generateHandle);

        return structuresHandle;
    }

    async UniTask<bool> GenerateMesh()
    {
        if (_modifications.Length > 0)
            ApplyMods();

        VoxelMapAccess = _holder.CountBlockTypes(VoxelMapAccess, VoxelMap);

        await VoxelMapAccess;
        if (_requestingStop) return false;
        if (_holder.IsEmpty) return true;
        _holder.ResizeFacesData();

        VoxelMapAccess = _holder.SortVoxels(VoxelMapAccess, VoxelMap);
        await VoxelMapAccess;

        if (_requestingStop) return false;

        _holder.ResizeMeshData();

        await _holder.FillMeshData();

        if (_requestingStop) return false;

        _holder.BuildMesh(_chunkMesh);

        return true;
    }


    private void ApplyMods()
    {
        NativeList<JobHandle> neighbours = new(7, Allocator.Temp);
        for (VoxelFaces i = 0; i < VoxelFaces.Max; i++)
        {
            if (_world.Chunks.TryGetValue(I3ToVI3(ChunkPos + _data.FaceChecks[(int)i]), out Chunk chunk))
                neighbours.Add(chunk.VoxelMapAccess);
        }
        neighbours.Add(VoxelMapAccess);

        NativeArray<VoxelMod> mods = _modifications.ToArray(Allocator.Persistent);
        _modifications.Clear();

        ApplyModsJob applyModsJob = new()
        {
            Data = _data,
            Modifications = mods,
            VoxelMap = VoxelMap,
        };
        VoxelMapAccess = applyModsJob.Schedule(mods.Length, 4, JobHandle.CombineDependencies(neighbours.AsArray()));
        mods.Dispose(VoxelMapAccess);
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

    //[WriteOnly]
    //[NativeDisableContainerSafetyRestriction]
    public NativeArray<Block> VoxelMap;

    //[WriteOnly]
    //[NativeDisableContainerSafetyRestriction]
    public NativeList<StructureMarker>.ParallelWriter Structures;

    public void Execute(int i)
    {
        int3 scaledPos = ChunkPos * Data.ChunkDimensions + XYZMap[i];
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
            float weight = Get2DPerlin(Data, new float2(pos.x, pos.z), Biomes[i].offset, Biomes[i].scale);

            if (weight > strongestWeight)
            {
                strongestWeight = weight;
                strongestBiomeIndex = i;
            }

            float height = Biomes[i].terrainHeight * Get2DPerlin(Data, new float2(pos.x, pos.z), 0f, Biomes[i].terrainScale) * weight;
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
            voxelValue = biome.surfaceBlock;
        }
        else if (pos.y < terrainHeight && pos.y > terrainHeight - 4)
        {
            voxelValue = biome.subsurfaceBlock;
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
            for (int i = 0; i < biome.lodes.Length; i++)
            {
                LodeSctruct lode = biome.lodes[i];
                if (pos.y > lode.minHeight && pos.y < lode.maxHeight)
                {
                    if (Get3DPerlin(Data, pos, lode.noiseOffset, lode.scale) > lode.threshold)
                    {
                        voxelValue = lode.blockID;
                    }
                }
            }
        }

        if (pos.y == terrainHeight)
        {
            if (Get2DPerlin(Data, new float2(pos.x, pos.z), 750, biome.floraZoneScale) > biome.floraZoneThreshold)
            {
                if (Get2DPerlin(Data, new float2(pos.x, pos.z), 1250, biome.floraPlacementScale) > biome.floraPlacementThreshold)
                {
                    Structures.AddNoResize(new(strongestBiomeIndex, pos, biome.floraType));
                }
            }
        }

        return voxelValue;
    }
}

[BurstCompile]
public struct BuildStructuresJob : IJobParallelForDefer
{
    [ReadOnly]
    public VoxelData Data;
    [ReadOnly]
    public NativeArray<BiomeStruct> Biomes;
    [ReadOnly]
    public int3 ChunkPos;
    [ReadOnly]
    public NativeArray<StructureMarker> Structures;

    [WriteOnly]
    public NativeList<VoxelMod>.ParallelWriter NeighbourModifications;
    [WriteOnly]
    [NativeDisableContainerSafetyRestriction]
    public NativeArray<Block> VoxelMap;

    public void Execute(int i)
    {
        var biome = Biomes[Structures[i].biomeIndex];
        switch (Structures[i].type)
        {
            case StructureType.Tree:
                MakeTree(Structures[i].position, biome.minHeight, biome.maxHeight);
                break;
            case StructureType.Cactus:
                MakeCacti(Structures[i].position, biome.minHeight, biome.maxHeight);
                break;
        }
    }

    private void MakeTree(int3 pos, int minTrunkHeight, int maxTrunkHeight)
    {
        NativeList<int3> list = new(20, Allocator.Temp);

        int height = (int)(maxTrunkHeight * Get2DPerlin(Data, new(pos.x, pos.z), 2000f, 3f));

        if (height < minTrunkHeight)
        {
            height = minTrunkHeight;
        }

        // Trunk
        for (int i = 1; i < height; i++)
        {
            Add(ref list, new(pos.x, pos.y + i, pos.z), Block.Wood);
        }

        // Leaves
        for (int x = -2; x < 3; x++)
        {
            for (int z = -2; z < 3; z++)
            {
                Add(ref list, new(pos.x + x, pos.y + height - 2, pos.z + z), Block.Leaves);
                Add(ref list, new(pos.x + x, pos.y + height - 3, pos.z + z), Block.Leaves);
            }
        }

        for (int x = -1; x < 2; x++)
        {
            for (int z = -1; z < 2; z++)
            {
                Add(ref list, new(pos.x + x, pos.y + height - 1, pos.z + z), Block.Leaves);
            }
        }
        for (int x = -1; x < 2; x++)
        {
            if (x == 0)
                for (int z = -1; z < 2; z++)
                {
                    Add(ref list, new(pos.x + x, pos.y + height, pos.z + z), Block.Leaves);
                }
            else
                Add(ref list, new(pos.x + x, pos.y + height, pos.z), Block.Leaves);
        }
    }

    private void MakeCacti(int3 pos, int minTrunkHeight, int maxTrunkHeight)
    {
        NativeList<int3> list = new(20, Allocator.Temp);

        int height = (int)(maxTrunkHeight * Get2DPerlin(Data, new(pos.x, pos.z), 1246f, 2f));

        if (height < minTrunkHeight)
        {
            height = minTrunkHeight;
        }

        // Trunk
        for (int i = 1; i < height; i++)
        {
            Add(ref list, new(pos.x, pos.y + i, pos.z), Block.Cactus);
        }
    }


    private void Add(ref NativeList<int3> list, int3 worldPos, Block block)
    {
        if (!list.Contains(worldPos))
        {
            int3 cPos = ToChunkCoord(worldPos);
            int3 pos = GetPosInChunk(cPos, worldPos);
            if (ChunkPos.Equals(cPos))
                VoxelMap[CalcIndex(pos)] = block;
            else
                NeighbourModifications.AddNoResize(new VoxelMod(cPos, pos, block));

            list.Add(pos);
        }
    }

    private readonly int3 ToChunkCoord(int3 pos) => (int3)math.floor(pos / (float3)Data.ChunkDimensions);
    private readonly int3 GetPosInChunk(int3 chunkPos, int3 worldPos) => worldPos - (chunkPos * Data.ChunkDimensions);
    readonly int CalcIndex(int3 xyz) => xyz.x * Data.ChunkHeight * Data.ChunkLength + xyz.y * Data.ChunkLength + xyz.z;
    //private readonly int3 IsInChunk(int3 pos) => worldPos - (chunkPos * Data.ChunkDimensions);
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
        VoxelMap[CalcIndex(Modifications[i].position)] = Modifications[i].block;
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