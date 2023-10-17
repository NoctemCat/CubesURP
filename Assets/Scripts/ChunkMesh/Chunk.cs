using System;
using System.Collections.Generic;
using System.Threading;

//using System.ComponentModel;
//using System.Threading;
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
using UnityEngine.Assertions.Must;
using static CubesUtils;


public class Chunk
{
    private readonly GameObject _chunkObject;
    private readonly MeshRenderer _meshRenderer;
    private readonly MeshFilter _meshFilter;
    private readonly Mesh _chunkMesh;
    private readonly Material[] _materials = new Material[2];
    public string ChunkName { get; private set; }
    public int3 ChunkPos { get; private set; }
    public Vector3 WorldPos { get; private set; }

    private readonly World _world;
    private readonly BiomeGenerator _biomeGenerator;
    private readonly ChunkHeightGenerator _chunkHeightGenerator;
    //private readonly MeshDataPool _meshDataPool;
    private readonly EventSystem _eventSystem;
    private readonly StructureSystem _structureSystem;
    private VoxelData _data;

    // Data stored as xzy
    public NativeArray<Block> VoxelMap { get; private set; }
    public NativeList<VoxelMod> NeighbourModifications { get; private set; }
    private NativeList<StructureMarker> _structures;
    private NativeList<VoxelMod> _modifications;
    private MeshDataHolder _holder;

    private bool _isLoaded;
    private bool _voxelMapGenerated;
    private bool _isGeneratingMesh;
    private bool _dirtyMesh;
    public bool markedForSave = false;
    public bool IsDisposed { get; private set; }

    private CancellationTokenSource _ctsStopMeshGen = null;
    private bool _isActive;
    public bool IsActive
    {
        get { return _isActive; }
        set
        {
            _isActive = value;
            _chunkObject.SetActive(value);
            if (value == false)
                _ctsStopMeshGen?.Cancel();
        }
    }

    public VoxelFlags NeighboursGenerated { get; private set; }
    public JobHandle VoxelMapAccess { get; private set; }
    private Action _initActions;

    private Chunk(World world)
    {
        _world = world;
        _biomeGenerator = world.BiomeGenerator;
        //_meshDataPool = ServiceLocator.Get<MeshDataPool>();
        _structureSystem = world.StructureSystem;
        _eventSystem = world.EventSystem;
        _chunkHeightGenerator = world.ChunkHeightGenerator;

        _initActions += CheckNeighbours;
        _initActions += AddStructuresToWorld;

        _data = _world.VoxelData;

        VoxelMap = new(_data.ChunkSize, Allocator.Persistent);
        _structures = new(100, Allocator.Persistent);
        NeighbourModifications = new(1024, Allocator.Persistent);
        _modifications = new(100, Allocator.Persistent);

        _voxelMapGenerated = false;
        _isGeneratingMesh = false;
        //_requestingStop = false;

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

        _chunkMesh = new Mesh()
        {
            subMeshCount = 2
        };
        _chunkMesh.MarkDynamic();
        _meshFilter.mesh = _chunkMesh;

        IsActive = true;
        _holder = new();

        _eventSystem.StartListening(EventType.PlayerChunkChanged, PlayerChunkChanged);
    }

    public Chunk(World world, Vector3Int chunkPos) : this(world)
    {
        ChunkPos = new(chunkPos.x, chunkPos.y, chunkPos.z);
        WorldPos = new(chunkPos.x * _data.ChunkWidth, chunkPos.y * _data.ChunkHeight, chunkPos.z * _data.ChunkLength);
        _chunkObject.transform.position = WorldPos;
        _chunkObject.name = PathHelper.GenerateChunkName(ChunkPos);
        ChunkName = _chunkObject.name;
        _isLoaded = false;

        StartGenerating().Forget();
    }
    public Chunk(World world, ChunkData chunkData) : this(world)
    {
        Vector3Int chunkPos = chunkData.ChunkPos;
        ChunkPos = new(chunkPos.x, chunkPos.y, chunkPos.z);
        WorldPos = new(chunkPos.x * _data.ChunkWidth, chunkPos.y * _data.ChunkHeight, chunkPos.z * _data.ChunkLength);
        _chunkObject.transform.position = WorldPos;
        _chunkObject.name = PathHelper.GenerateChunkName(ChunkPos);
        ChunkName = _chunkObject.name;
        _isLoaded = true;

        VoxelMap.CopyFrom(chunkData.VoxelMap);
        NeighbourModifications.CopyFromNBC(chunkData.NeighbourModifications.ToArray());

        _voxelMapGenerated = true;
        _dirtyMesh = true;

        _world.CheckNeighbours(this);
        if ((NeighboursGenerated & VoxelFlags.All) == VoxelFlags.All)
        {
            _initActions -= CheckNeighbours;
        }
    }

    private void PlayerChunkChanged(in EventArgs args)
    {
        if (args.eventType != EventType.PlayerChunkChanged)
            Debug.Log("World PlayerChunkChanged listener wrong EventArgs");

        var playerChunk = (args as PlayerChunkChangedArgs).newChunkPos;

        Vector3Int chunkPos = I3ToVI3(ChunkPos);
        Vector3Int viewChunkPos = chunkPos - playerChunk;
        if (ChunkInsideViewDistance(viewChunkPos))
        {
            IsActive = true;
        }
        else if (ChunkOutsideDisposeDistance(viewChunkPos))
        {
            if (_world.Chunks.ContainsKey(chunkPos))
            {
                _world.Chunks.Remove(chunkPos);
                Dispose();
            }
        }
        else
        {
            IsActive = false;
        }
    }

    private bool ChunkInsideViewDistance(Vector3Int viewChunkPos)
    {
        return viewChunkPos.x >= -_world.Settings.viewDistance && viewChunkPos.x <= _world.Settings.viewDistance &&
            viewChunkPos.y >= -_world.Settings.viewDistance && viewChunkPos.y <= _world.Settings.viewDistance &&
            viewChunkPos.z >= -_world.Settings.viewDistance && viewChunkPos.z <= _world.Settings.viewDistance;
    }
    private bool ChunkOutsideDisposeDistance(Vector3Int viewChunkPos)
    {
        return viewChunkPos.x < -_world.Settings.viewDistance - 2 || viewChunkPos.x > _world.Settings.viewDistance + 2 ||
            viewChunkPos.y < -_world.Settings.viewDistance - 2 || viewChunkPos.y > _world.Settings.viewDistance + 2 ||
            viewChunkPos.z < -_world.Settings.viewDistance - 2 || viewChunkPos.z > _world.Settings.viewDistance + 2;
    }

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

    ~Chunk()
    {
        if (!IsDisposed)
            Dispose();
    }

    private void Deactivate()
    {
        if (IsDisposed) return;

        _eventSystem.StopListening(EventType.PlayerChunkChanged, PlayerChunkChanged);
        IsActive = false;
        IsDisposed = true;
        VoxelMapAccess.Complete();
    }

    public void Dispose()
    {
        Deactivate();
        if (markedForSave)
            return;

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

    public void FinishedSaving()
    {
        markedForSave = false;
        if (IsDisposed)
            Dispose();
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
            _structureSystem.AddStructuresToSort(NeighbourModifications);
        }
    }

    public void Update()
    {
        if (IsDisposed) return;
        _initActions?.Invoke();

        if (_modifications.Length > 0 && !_isGeneratingMesh)
        {
            _dirtyMesh = true;
        }

        if (_voxelMapGenerated && _dirtyMesh && !_isGeneratingMesh)
        {
            StartMeshGen().Forget();
        }
    }

    public void AddNeighbours(VoxelFlags neighbours)
    {
        NeighboursGenerated |= neighbours;
    }

    public async UniTaskVoid AddModification(VoxelMod mod)
    {
        await VoxelMapAccess;
        VoxelMapAccess.Complete();
        _modifications.Add(mod);
    }

    // Right now it's only being used when adding structures from generation,
    public async UniTask AddRangeModification(List<VoxelMod> mod)
    {
        if (_isLoaded) return;

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

        //MeshDataHolder holder = _meshDataPool.Get();
        //holder.chunkPos = ChunkPos;
        _holder.Init();
        _holder.chunkPos = ChunkPos;
        _ctsStopMeshGen = new();

        var isFinished = await GenerateMesh(_holder, _ctsStopMeshGen.Token);
        if (isFinished)
        {
            _dirtyMesh = false;
            _world.ChunkCreated(this);
        }

        _ctsStopMeshGen.Dispose();
        _ctsStopMeshGen = null;
        _holder.Dispose();
        //_meshDataPool.Reclaim(holder);
        _isGeneratingMesh = false;
    }

    JobHandle GenerateVoxelMap()
    {
        //Debug.Log(_biomeGenerator.Grid.Count);
        _chunkHeightGenerator.RequestChunkHeights(
            new(ChunkPos.x, ChunkPos.z),
            out NativeArray<float> chunkHeights,
            out NativeArray<int> closestBiomes,
            out JobHandle heightsFinish
        );
        GenerateChunkJobBatch generateChunk = new()
        {
            Data = _data,
            Biomes = _world.BiomeDatabase.Biomes,
            //BiomesGrid = _biomeGenerator.Grid,
            //BiomesCellSize = _biomeGenerator.CellSize,
            chunkHeights = chunkHeights,
            closestBiomesIndeces = closestBiomes,
            ChunkPos = ChunkPos,

            VoxelMap = VoxelMap,
            Structures = _structures.AsParallelWriter(),
        };
        JobHandle generateHandle = generateChunk.Schedule(_data.xzMap.Length, 8, heightsFinish);
        //GenerateChunkJob generateChunk = new()
        //{
        //    Data = _data,
        //    Biomes = _world.Biomes,
        //    XYZMap = _world.XYZMap,
        //    ChunkPos = ChunkPos,

        //    VoxelMap = VoxelMap,
        //    Structures = _structures.AsParallelWriter(),
        //};
        //JobHandle generateHandle = generateChunk.Schedule(VoxelMap.Length, 8);

        BuildStructuresJob buildStructures = new()
        {
            Data = _data,
            Biomes = _world.BiomeDatabase.Biomes,
            ChunkPos = ChunkPos,
            Structures = _structures.AsDeferredJobArray(),

            VoxelMap = VoxelMap,
            NeighbourModifications = NeighbourModifications.AsParallelWriter(),
        };

        JobHandle structuresHandle = buildStructures.Schedule(_structures, 8, generateHandle);

        return structuresHandle;
    }

    async UniTask<bool> GenerateMesh(MeshDataHolder holder, CancellationToken token)
    {
        if (_modifications.Length > 0)
            ApplyMods();

        VoxelMapAccess = holder.CountBlockTypes(VoxelMapAccess, VoxelMap);

        await VoxelMapAccess;
        if (token.IsCancellationRequested) return false;
        if (holder.IsEmpty) return true;
        holder.ResizeFacesData();

        VoxelMapAccess = holder.SortVoxels(VoxelMapAccess, VoxelMap);
        await VoxelMapAccess;

        if (token.IsCancellationRequested) return false;

        holder.ResizeMeshData();

        await holder.FillMeshData();

        if (token.IsCancellationRequested) return false;

        holder.BuildMesh(_chunkMesh);

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
        //int count = 0;
        float strongestWeight = 0f;
        int strongestBiomeIndex = 0;

        NativeArray<float> heights = new(Biomes.Length, Allocator.Temp);
        for (int i = 0; i < Biomes.Length; i++)
        {
            float weight = Get2DPerlin(Data, new float2(pos.x, pos.z), Biomes[i].offset, Biomes[i].scale);
            if (weight > strongestWeight)
            {
                strongestWeight = weight;
                strongestBiomeIndex = i;
            }


            //float height = Biomes[i].terrainHeight * Get2DPerlin(Data, new float2(pos.x, pos.z), 0f, Biomes[i].terrainScale) * weight;
            heights[i] = Biomes[i].terrainHeight * Get2DPerlin(Data, new float2(pos.x, pos.z), 0f, Biomes[i].terrainScale);

            //if (height > 0f)
            //{
            //}
            //sumOfHeights += height;
            //count++;
            //sumOfHeights += height;
            //count++;
        }

        for (int i = 0; i < heights.Length; i++)
        {
            float weight = Get2DPerlin(Data, new float2(pos.x, pos.z), Biomes[i].offset, Biomes[i].scale);
            if (i == strongestBiomeIndex)
            {
                sumOfHeights += heights[i] * weight;
            }
            else
            {
                sumOfHeights += heights[i] * weight * 0.75f;
            }
        }

        //int bIndex = Data.rng.NextInt(0, Biomes.Length);

        BiomeStruct biome = Biomes[strongestBiomeIndex];
        sumOfHeights /= heights.Length;
        //sumOfHeights = strongestHeight;


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

/// <summary>
/// Number of chunk height
/// </summary> 
[BurstCompile]
public struct GenerateChunkJobBatch : IJobParallelFor
{
    [ReadOnly]
    public VoxelData Data;
    [ReadOnly]
    public NativeArray<BiomeStruct> Biomes;
    [ReadOnly]
    public NativeArray<float> chunkHeights;
    [ReadOnly]
    public NativeArray<int> closestBiomesIndeces;
    //[ReadOnly]
    //public NativeHashMap<int2, BiomePoint> BiomesGrid;
    //public float BiomesCellSize;
    //[ReadOnly]
    //public NativeArray<int3> XYZMap;
    //public NativeArray<int3> XYMap;
    [ReadOnly]
    public int3 ChunkPos;

    [WriteOnly]
    [NativeDisableContainerSafetyRestriction]
    public NativeArray<Block> VoxelMap;

    //[WriteOnly]
    //[NativeDisableContainerSafetyRestriction]
    public NativeList<StructureMarker>.ParallelWriter Structures;

    public void Execute(int xzPos)
    {
        int2 xz = Data.xzMap[xzPos];
        int2 scaledXZ = new int2(ChunkPos.x, ChunkPos.z) * new int2(Data.ChunkWidth, Data.ChunkLength) + xz;
        float3 scaledXYZ = new(scaledXZ.x, 1, scaledXZ.y);

        float height = chunkHeights[xzPos];
        int closest = closestBiomesIndeces[xzPos];
        //float height = Biomes[0].terrainHeight * GetHeight(scaledXZ, Biomes[0].terrainScale, Biomes[0].noise.octaves, octavesOffsets, Biomes[0].noise.complexOctaves);
        for (int y = 0; y < Data.ChunkHeight; y++)
        {
            int3 xyzPos = new(xz.x, y, xz.y);
            int3 scaledPos = ChunkPos * Data.ChunkDimensions + xyzPos;
            VoxelMap[CalcIndex(Data, xyzPos)] = GetVoxel(Biomes[closest], height, scaledPos);
        }
    }

    private float2 CalculateHeight(int2 xz)
    {
        //int solidGroundHeight = 42;
        float sumOfHeights = 0f;
        //int count = 0;
        float strongestWeight = 0f;
        int strongestBiomeIndex = 0;

        NativeArray<float> heights = new(Biomes.Length, Allocator.Temp);
        for (int i = 0; i < Biomes.Length; i++)
        {
            float weight = Get2DPerlin(Data, new float2(xz.x, xz.y), Biomes[i].offset, Biomes[i].scale);
            if (weight > strongestWeight)
            {
                strongestWeight = weight;
                strongestBiomeIndex = i;
            }

            heights[i] = Biomes[i].terrainHeight * Get2DPerlin(Data, new float2(xz.x, xz.y), 0f, Biomes[i].terrainScale);
        }

        for (int i = 0; i < heights.Length; i++)
        {
            float weight = Get2DPerlin(Data, new float2(xz.x, xz.y), Biomes[i].offset, Biomes[i].scale);
            if (i == strongestBiomeIndex)
                sumOfHeights += heights[i] * weight;
            else
                sumOfHeights += heights[i] * weight * 0.75f;
        }

        return new(strongestBiomeIndex, sumOfHeights / heights.Length);
    }

    private float2 CalculateHeightTemp(int2 xz)
    {
        Unity.Mathematics.Random rng = new(Data.seed);
        NativeArray<float2> octavesOffsets = new(Biomes[0].noise.octaves.Length, Allocator.Temp);
        for (int i = 0; i < octavesOffsets.Length; i++)
        {
            octavesOffsets[i] = rng.NextFloat2(-100000, 100000) + Biomes[0].offset;
        }
        float height = GetHeight(xz, Biomes[0].terrainScale, Biomes[0].noise.octaves, octavesOffsets);
        return new(0, Biomes[0].terrainHeight * height);
    }

    public Block GetVoxel(BiomeStruct biome, float height, int3 pos)
    {
        // IMMUTABLE PASS
        //if (pos.y == 0)
        //    return Block.Bedrock;

        // BIOME SELECTION PASS

        //BiomeStruct biome = Biomes[biomeIndex];
        //int solidGroundHeight = 42;

        int terrainHeight = (int)math.floor(height);

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
                    //Structures.AddNoResize(new(biomeIndex, pos, biome.floraType));
                }
            }
        }

        return voxelValue;
    }

    //public Block GetVoxel()
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
                VoxelMap[CalcIndex(Data, pos)] = block;
            else
                NeighbourModifications.AddNoResize(new VoxelMod(cPos, pos, block));

            list.Add(pos);
        }
    }

    private readonly int3 ToChunkCoord(int3 pos) => (int3)math.floor(pos / (float3)Data.ChunkDimensions);
    private readonly int3 GetPosInChunk(int3 chunkPos, int3 worldPos) => worldPos - (chunkPos * Data.ChunkDimensions);
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
        VoxelMap[CalcIndex(Data, Modifications[i].position)] = Modifications[i].block;
    }

    //readonly int CalcIndex(int3 xyz) => xyz.x * Data.ChunkHeight * Data.ChunkLength + xyz.y * Data.ChunkLength + xyz.z;
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


