using Cysharp.Threading.Tasks;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;


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
    VoxelData Data;
    private readonly World World;
    public int3 ChunkPos;
    public Vector3 WorldPos;

    public NativeArray<Block> VoxelMap;

    MeshData MeshData;
    MeshFacesData FacesData;
    readonly Mesh ChunkMesh;

    NativeArray<int> CountBlocks;
    NativeArray<int> Counters;

    public bool IsMeshDrawable;
    public bool IsGeneratingMesh;
    //public bool VoxelMapReady;
    public bool RequestingStop;

    public bool DirtyMesh;

    //UniTask GenTask;

    JobHandle VoxelMapAccess;

    JobHandle FillingMods;

    public NativeList<VoxelMod> Modifications;

    public Chunk(World world, Vector3Int chunkPos)
    {
        World = world;
        Data = world.VoxelData;
        ChunkPos = new(chunkPos.x, chunkPos.y, chunkPos.z);
        WorldPos = new(ChunkPos.x * Data.ChunkWidth, ChunkPos.y * Data.ChunkHeight, ChunkPos.z * Data.ChunkLength);

        VoxelMap = new(Data.ChunkSize, Allocator.Persistent);

        ChunkMesh = new Mesh()
        {
            subMeshCount = 2
        };
        ChunkMesh.MarkDynamic();

        MeshData.InitLists();
        FacesData.InitLists();
        CountBlocks = new(2, Allocator.Persistent);
        Counters = new(JobsUtility.MaxJobThreadCount * JobsUtility.CacheLineSize, Allocator.Persistent);

        Modifications = new(100, Allocator.Persistent);

        IsMeshDrawable = false;
        IsGeneratingMesh = false;
        //VoxelMapReady = false;
        RequestingStop = false;

        DirtyMesh = false;

        StartGenerating();
    }

    ~Chunk()
    {
        VoxelMap.Dispose();
        MeshData.Dispose();
        FacesData.Dispose();
        CountBlocks.Dispose();
        Counters.Dispose();
        Modifications.Dispose();
    }

    public void Update()
    {
        if (DirtyMesh && !IsGeneratingMesh)
        {
            StartMeshGen().Forget();
        }

        if (Modifications.Length > 0 && !IsGeneratingMesh && !DirtyMesh)
        {
            DirtyMesh = true;
        }

        if (RequestingStop && !IsGeneratingMesh)
        {
            RequestingStop = false;
        }
    }

    public void Draw()
    {
        if (!IsMeshDrawable) return;

        RenderParams rp = new()
        {
            material = World.SolidMaterial,
            shadowCastingMode = ShadowCastingMode.TwoSided,
            receiveShadows = true,
            renderingLayerMask = GraphicsSettings.defaultRenderingLayerMask
        };
        RenderParams trp = new()
        {
            material = World.TransparentMaterial,
            shadowCastingMode = ShadowCastingMode.TwoSided,
            receiveShadows = true,
            renderingLayerMask = GraphicsSettings.defaultRenderingLayerMask
        };

        Graphics.RenderMesh(rp, ChunkMesh, 0, Matrix4x4.Translate(WorldPos));
        Graphics.RenderMesh(trp, ChunkMesh, 1, Matrix4x4.Translate(WorldPos));
    }

    void StartGenerating()
    {
        GenerateVoxelMap();
        //await VoxelMapAccess;
        //VoxelMapReady = true;
        DirtyMesh = true;
        World.AddChunkVoxelMap(ChunkPos, VoxelMap);
        //StartMeshGen();
    }

    //public async UniTask ApplyMod()
    //{
    //    NativeArray<VoxelMod> copy = new(Modifications.Length, Allocator.Persistent);
    //    copy.CopyFrom(Modifications.AsArray());
    //    ApplyModsJob applyModsJob = new()
    //    {
    //        Data = Data,
    //        Modifications = copy,
    //        VoxelMap = VoxelMap,
    //    };
    //    VoxelMapAccess = applyModsJob.Schedule(Modifications.Length, 1, VoxelMapAccess);
    //    await VoxelMapAccess;
    //    copy.Dispose();
    //    Modifications.Clear();
    //    //DirtyMesh = true;
    //}

    public async UniTaskVoid StartMeshGen()
    {
        if (!IsGeneratingMesh)
        {
            IsGeneratingMesh = true;

            var isFinished = await GenerateMesh();
            if (isFinished)
            {
                IsMeshDrawable = true;
                IsGeneratingMesh = false;
            }
            else
            {
                RequestingStop = false;
                IsGeneratingMesh = false;
            }
        }
    }

    async UniTask<bool> GenerateMesh()
    {
        //if (Modifications.Length > 0)
        //    await ApplyMod();

        await CountBlockTypes();

        if (RequestingStop) return false;

        ResizeFacesData();

        await SortVoxels();

        if (RequestingStop) return false;

        await ResizeMeshData();

        await FillMeshData();

        if (RequestingStop) return false;

        BuildMesh();

        return true;
    }

    void GenerateVoxelMap()
    {
        GenerateChunkJob generateChunk = new()
        {
            Data = Data,
            Biome = World.Biome,
            XYZMap = World.XYZMap,
            ChunkPos = ChunkPos,

            VoxelMap = VoxelMap,
            //Structures = World.Structures.AsParallelWriter(),
        };
        VoxelMapAccess = generateChunk.Schedule(VoxelMap.Length, 8);
    }

    async UniTask CountBlockTypes()
    {
        for (var i = 0; i < JobsUtility.MaxJobThreadCount; i++)
        {
            int threadOffset = i * JobsUtility.CacheLineSize;
            Counters[threadOffset] = 0;
            Counters[threadOffset + 1] = 0;
        }

        await VoxelMapAccess;
        MeshBuilder.CountBlockTypesJob countBlockTypes = new()
        {
            Blocks = World.Blocks,
            VoxelMap = VoxelMap,
            Counters = Counters,
        };
        VoxelMapAccess = countBlockTypes.Schedule(VoxelMap.Length, 1);

        await VoxelMapAccess;
        CountBlocks[0] = 0;
        CountBlocks[1] = 0;
        for (var i = 0; i < JobsUtility.MaxJobThreadCount; i++)
        {
            int threadOffset = i * JobsUtility.CacheLineSize;
            CountBlocks[0] += Counters[threadOffset];
            CountBlocks[1] += Counters[threadOffset + 1];
        }
    }

    private void ResizeFacesData()
    {
        FacesData.AllFaces.Clear();
        FacesData.SolidFaces.Clear();
        FacesData.TransparentFaces.Clear();
        FacesData.SolidOffset.Value = 0;

        FacesData.AllFaces.Capacity = (CountBlocks[0] + CountBlocks[1]) * 6;
        FacesData.SolidFaces.Capacity = CountBlocks[0] * 6;
        FacesData.TransparentFaces.Capacity = CountBlocks[1] * 6;
    }

    async UniTask SortVoxels()
    {
        await VoxelMapAccess;
        // TODO add neighbours
        JobHandle access = FillNeighbours(out Neighbours neighbours);
        MeshBuilder.SortVoxelFacesJob sortVoxelsJob = new()
        {
            Data = World.VoxelData,
            ChunkNeighbours = neighbours,
            ChunkPos = ChunkPos,
            VoxelMap = VoxelMap,
            Blocks = World.Blocks,
            XYZMap = World.XYZMap,

            SolidFaces = FacesData.SolidFaces.AsParallelWriter(),
            TransparentFaces = FacesData.TransparentFaces.AsParallelWriter(),
        };
        VoxelMapAccess = sortVoxelsJob.Schedule(Data.ChunkSize, Data.ChunkSize / 8, access);
    }

    JobHandle FillNeighbours(out Neighbours neighbours)
    {
        NativeList<JobHandle> accesses = new(4, Allocator.Temp);

        if (World.Chunks.TryGetValue(ToVInt3(ChunkPos + Data.FaceChecks[(int)VoxelFaces.Back]), out Chunk chunk))
        {
            neighbours.Back = chunk.VoxelMap;
            accesses.Add(chunk.VoxelMapAccess);
        }
        else
        {
            neighbours.Back = World.DummyMap;
        }
        if (World.Chunks.TryGetValue(ToVInt3(ChunkPos + Data.FaceChecks[(int)VoxelFaces.Front]), out chunk))
        {
            neighbours.Front = chunk.VoxelMap;
            accesses.Add(chunk.VoxelMapAccess);
        }
        else
        {
            neighbours.Front = World.DummyMap;
        }
        if (World.Chunks.TryGetValue(ToVInt3(ChunkPos + Data.FaceChecks[(int)VoxelFaces.Left]), out chunk))
        {
            neighbours.Left = chunk.VoxelMap;
            accesses.Add(chunk.VoxelMapAccess);
        }
        else
        {
            neighbours.Left = World.DummyMap;
        }
        if (World.Chunks.TryGetValue(ToVInt3(ChunkPos + Data.FaceChecks[(int)VoxelFaces.Right]), out chunk))
        {
            neighbours.Right = chunk.VoxelMap;
            accesses.Add(chunk.VoxelMapAccess);
        }
        else
        {
            neighbours.Right = World.DummyMap;
        }

        return JobHandle.CombineDependencies(accesses.AsArray());
    }

    Vector3Int ToVInt3(int3 v) => new(v.x, v.y, v.z);

    async UniTask ResizeMeshData()
    {
        await VoxelMapAccess;

        int allFacesCount = FacesData.SolidFaces.Length + FacesData.TransparentFaces.Length;

        FacesData.AllFaces.Clear();

        FacesData.AllFaces.CopyFrom(FacesData.SolidFaces);
        FacesData.AllFaces.AddRangeNoResize(FacesData.TransparentFaces);

        MeshData.Vertices.ResizeUninitialized(allFacesCount * 4);
        MeshData.SolidIndices.ResizeUninitialized(FacesData.SolidFaces.Length * 6);
        MeshData.TransparentIndices.ResizeUninitialized(FacesData.TransparentFaces.Length * 6);

        FacesData.SolidOffset.Value = FacesData.SolidFaces.Length * 4;

        int2 minmaxSolid = new()
        {
            x = 0,
            y = (FacesData.SolidFaces.Length - 1) * 4 + 3
        };
        int transparentLen = (FacesData.TransparentFaces.Length > 0) ? (FacesData.TransparentFaces.Length - 1) * 4 + 3 : 0;
        int2 minmaxTransparent = new()
        {
            x = FacesData.SolidOffset.Value,
            y = FacesData.SolidOffset.Value + transparentLen
        };

        MeshData.VerticesRanges[0] = minmaxSolid;
        MeshData.VerticesRanges[1] = minmaxTransparent;
    }

    JobHandle FillMeshData()
    {
        MeshBuilder.FillVerticesJob fillVerticesJob = new()
        {
            Data = World.VoxelData,
            AllFaces = FacesData.AllFaces.AsArray(),
            Vertices = MeshData.Vertices.AsArray(),
        };
        JobHandle fillVerticesHandle = fillVerticesJob.Schedule(FacesData.AllFaces.Length, 8);

        MeshBuilder.FillSolidIndicesJob fillSolidJob = new()
        {
            SolidFaces = FacesData.SolidFaces.AsArray(),
            SolidIndices = MeshData.SolidIndices.AsArray(),
        };
        JobHandle fillSolidHandle = fillSolidJob.Schedule(FacesData.SolidFaces.Length, 8);

        MeshBuilder.FillTransparentIndicesJob fillTransparentJob = new()
        {
            SolidOffset = FacesData.SolidOffset,
            TransparentFaces = FacesData.TransparentFaces.AsArray(),
            TransparentIndices = MeshData.TransparentIndices.AsArray(),
        };
        JobHandle fillTransparentHandle = fillTransparentJob.Schedule(FacesData.TransparentFaces.Length, 8);

        return JobHandle.CombineDependencies(fillVerticesHandle, fillSolidHandle, fillTransparentHandle);
    }

    void BuildMesh()
    {
        ChunkMesh.Clear();

        Bounds bounds = new(
            new(Data.ChunkWidth / 2f, Data.ChunkHeight / 2, Data.ChunkLength / 2),
            new(Data.ChunkWidth + 1f, Data.ChunkHeight + 1f, Data.ChunkLength + 1f)
        );

        var layout = new NativeArray<VertexAttributeDescriptor>(3, Allocator.Temp);
        layout[0] = new(VertexAttribute.Position, VertexAttributeFormat.Float32, 3);
        layout[1] = new(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3);
        layout[2] = new(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2);

        ChunkMesh.SetVertexBufferParams(MeshData.Vertices.Length, layout);
        ChunkMesh.SetVertexBufferData(MeshData.Vertices.AsArray(), 0, 0, MeshData.Vertices.Length, 0, MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontNotifyMeshUsers);

        ChunkMesh.SetIndexBufferParams(MeshData.SolidIndices.Length + MeshData.TransparentIndices.Length, IndexFormat.UInt32);
        ChunkMesh.SetIndexBufferData(MeshData.SolidIndices.AsArray(),   /**/0, 0,               /**/MeshData.SolidIndices.Length, MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontNotifyMeshUsers);
        ChunkMesh.SetIndexBufferData(MeshData.TransparentIndices.AsArray(), 0, MeshData.SolidIndices.Length, MeshData.TransparentIndices.Length, MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontNotifyMeshUsers);

        var meshDesc = new SubMeshDescriptor()
        {
            indexStart = 0,
            indexCount = MeshData.SolidIndices.Length,
            firstVertex = MeshData.VerticesRanges[0].x,
            vertexCount = MeshData.VerticesRanges[0].y,
            bounds = bounds,
        };
        ChunkMesh.SetSubMesh(0, meshDesc, MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontNotifyMeshUsers);
        ChunkMesh.subMeshCount = 2;

        var tMeshDesc = new SubMeshDescriptor()
        {
            indexStart = MeshData.SolidIndices.Length,
            indexCount = MeshData.TransparentIndices.Length,
            firstVertex = MeshData.VerticesRanges[1].x,
            vertexCount = MeshData.VerticesRanges[1].y,
            bounds = bounds,
        };

        ChunkMesh.SetSubMesh(1, tMeshDesc, MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontNotifyMeshUsers);
        ChunkMesh.bounds = bounds;

        layout.Dispose();
        DirtyMesh = false;
    }
}

public struct MeshDataHolder
{

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
    //public NativeList<StructureMarker>.ParallelWriter Structures;

    public void Execute(int i)
    {
        //ref var voxelData = ref VoxelDataRef.Data.Value;
        int3 scaledPos = new(
            ChunkPos.x * Data.ChunkWidth,
            ChunkPos.y * Data.ChunkHeight,
            ChunkPos.z * Data.ChunkLength
        );

        VoxelMap[i] = GetVoxel(ChunkPos, scaledPos + XYZMap[i]);
    }

    public Block GetVoxel(int3 chunkPos, int3 pos)
    {
        // IMMUTABLE PASS
        if (pos.y == 0)
            return Block.Bedrock;

        // BASIC TERRAIN PASSS
        int terrainHeight = (int)(math.floor(Biome.MaxTerrainHeight * Get2DPerlin(new float2(pos.x, pos.z), 0f, Biome.TerrainScale)) + Biome.SolidGroundHeight);

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
                    if (Get3DPerlin(pos, lode.NoiseOffset, lode.Scale) > lode.Threshold)
                    {
                        voxelValue = lode.BlockID;
                    }
                }
            }
        }

        if (pos.y == terrainHeight)
        {
            if (Get2DPerlin(new float2(pos.x, pos.z), 750, Biome.TreeZoneScale) > Biome.TreeZoneThreshold)
            {
                // Checking transparentness
                voxelValue = Block.Glass;
                if (Get2DPerlin(new float2(pos.x, pos.z), 1250, Biome.TreePlacementScale) > Biome.TreePlacementThreshold)
                {
                    //Debug.Log("Tree placed");
                    // TODO re
                    //Structures.AddNoResize(new(pos, StructureType.Tree));
                }
            }
        }

        return voxelValue;
    }

    readonly float Get2DPerlin(float2 position, float offset, float scale)
    {
        return noise.cnoise(new float2(
            (position.x + 0.1f) / Data.ChunkWidth * scale + offset + Data.RandomXYZ.x,
            (position.y + 0.1f) / Data.ChunkLength * scale + offset + Data.RandomXYZ.y)
        );
    }

    readonly float Get3DPerlin(float3 position, float offset, float scale)
    {
        return noise.cnoise(new float3(
            (position.x + 0.1f) / Data.ChunkWidth * scale + offset + Data.RandomXYZ.x,
            (position.y + 0.1f) / Data.ChunkHeight * scale + offset + Data.RandomXYZ.y,
            (position.z + 0.1f) / Data.ChunkLength * scale + offset + Data.RandomXYZ.z)
        );
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