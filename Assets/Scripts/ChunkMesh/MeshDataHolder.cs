

using Cysharp.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

using static CubesUtils;

public struct MeshDataHolder
{
    World World;
    VoxelData Data;
    int3 ChunkPos;

    MeshData MeshData;
    MeshFacesData FacesData;

    NativeArray<int> CountBlocks;
    NativeArray<int> Counters;

    NativeArray<VertexAttributeDescriptor> _layout;

    int _numberOfFaces;
    public readonly bool HasFaces => _numberOfFaces > 0;

    /// <summary>
    /// Can only be called after CountBlockTypes
    /// </summary>
    public bool IsEmpty => CountBlocks[2] == 0;

    public void Init(int3 chunkPos)
    {
        World = World.Instance;
        Data = World.VoxelData;
        ChunkPos = chunkPos;

        MeshData.InitLists();
        FacesData.InitLists();
        //FacesData.AllFaces

        _layout = new NativeArray<VertexAttributeDescriptor>(3, Allocator.Persistent);
        _layout[0] = new(VertexAttribute.Position, VertexAttributeFormat.Float32, 3);
        _layout[1] = new(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3);
        _layout[2] = new(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 3);

        CountBlocks = new(64, Allocator.Persistent);
        Counters = new(JobsUtility.MaxJobThreadCount * JobsUtility.CacheLineSize, Allocator.Persistent);

        _numberOfFaces = 0;
    }

    public void Dispose()
    {
        MeshData.Dispose();
        FacesData.Dispose();
        CountBlocks.Dispose();
        Counters.Dispose();
        _layout.Dispose();
    }


    public JobHandle CountBlockTypes(JobHandle voxelMapAccess, NativeArray<Block> voxelMap)
    {
        for (var i = 0; i < JobsUtility.MaxJobThreadCount; i++)
        {
            int threadOffset = i * JobsUtility.CacheLineSize;
            Counters[threadOffset] = 0;
            Counters[threadOffset + 1] = 0;
            Counters[threadOffset + 2] = 0;
        }

        MeshBuilder.CountBlockTypesJob countBlockTypes = new()
        {
            Blocks = World.NativeBlocks,
            VoxelMap = voxelMap,
            Counters = Counters,
        };
        voxelMapAccess = countBlockTypes.Schedule(voxelMap.Length, 1, voxelMapAccess);

        MeshBuilder.SumBlockTypesJob sumBlockTypes = new()
        {
            Counters = Counters,
            Totals = CountBlocks,
        };

        return sumBlockTypes.Schedule(voxelMapAccess);
    }
    //public void PrintTotals()
    //{
    //    Debug.Log($"{CountBlocks[0]} / {CountBlocks[1]} / {CountBlocks[2]}");
    //}

    public void ResizeFacesData()
    {
        FacesData.AllFaces.Clear();
        FacesData.SolidFaces.Clear();
        FacesData.TransparentFaces.Clear();
        FacesData.SolidOffset.Value = 0;

        FacesData.AllFaces.Capacity = (CountBlocks[0] + CountBlocks[1]) * 6;
        FacesData.SolidFaces.Capacity = CountBlocks[0] * 6;
        FacesData.TransparentFaces.Capacity = CountBlocks[1] * 6;
    }

    public JobHandle SortVoxels(JobHandle voxelMapAccess, NativeArray<Block> voxelMap)
    {
        // TODO add neighbours
        JobHandle access = FillNeighbours(out Neighbours neighbours);
        MeshBuilder.SortVoxelFacesJob sortVoxelsJob = new()
        {
            Data = Data,
            ChunkNeighbours = neighbours,
            ChunkPos = ChunkPos,
            VoxelMap = voxelMap,
            Blocks = World.NativeBlocks,
            XYZMap = World.XYZMap,

            SolidFaces = FacesData.SolidFaces.AsParallelWriter(),
            TransparentFaces = FacesData.TransparentFaces.AsParallelWriter(),
        };
        //return sortVoxelsJob.Schedule(Data.ChunkSize, Data.ChunkSize / 8, voxelMapAccess);
        return sortVoxelsJob.Schedule(Data.ChunkSize, Data.ChunkSize / 8, JobHandle.CombineDependencies(voxelMapAccess, access));
    }

    public JobHandle FillNeighbours(out Neighbours neighbours)
    {
        NativeList<JobHandle> accesses = new(6, Allocator.Temp);
        if (World.Chunks.TryGetValue(I3ToVI3(ChunkPos + Data.FaceChecks[(int)VoxelFaces.Back]), out Chunk chunk))
        {
            neighbours.Back = chunk.VoxelMap;
            accesses.Add(chunk.VoxelMapAccess);
        }
        else
            neighbours.Back = World.DummyMap;

        if (World.Chunks.TryGetValue(I3ToVI3(ChunkPos + Data.FaceChecks[(int)VoxelFaces.Front]), out chunk))
        {
            neighbours.Front = chunk.VoxelMap;
            accesses.Add(chunk.VoxelMapAccess);
        }
        else
            neighbours.Front = World.DummyMap;

        if (World.Chunks.TryGetValue(I3ToVI3(ChunkPos + Data.FaceChecks[(int)VoxelFaces.Top]), out chunk))
        {
            neighbours.Top = chunk.VoxelMap;
            accesses.Add(chunk.VoxelMapAccess);
        }
        else
            neighbours.Top = World.DummyMap;

        if (World.Chunks.TryGetValue(I3ToVI3(ChunkPos + Data.FaceChecks[(int)VoxelFaces.Bottom]), out chunk))
        {
            neighbours.Bottom = chunk.VoxelMap;
            accesses.Add(chunk.VoxelMapAccess);
        }
        else
            neighbours.Bottom = World.DummyMap;

        if (World.Chunks.TryGetValue(I3ToVI3(ChunkPos + Data.FaceChecks[(int)VoxelFaces.Left]), out chunk))
        {
            neighbours.Left = chunk.VoxelMap;
            accesses.Add(chunk.VoxelMapAccess);
        }
        else
            neighbours.Left = World.DummyMap;

        if (World.Chunks.TryGetValue(I3ToVI3(ChunkPos + Data.FaceChecks[(int)VoxelFaces.Right]), out chunk))
        {
            neighbours.Right = chunk.VoxelMap;
            accesses.Add(chunk.VoxelMapAccess);
        }
        else
            neighbours.Right = World.DummyMap;


        return JobHandle.CombineDependencies(accesses.AsArray());
    }

    public void ResizeMeshData()
    {
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

    public JobHandle FillMeshData()
    {
        MeshBuilder.FillVerticesJob fillVerticesJob = new()
        {
            Data = Data,
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

    public JobHandle CalculateNormals()
    {
        //NativeParallelHashMap<float3, float3> hashNormals = new(MeshData.Vertices.Length, Allocator.Persistent);
        MeshBuilder.CalculateNormalsJob calculateJob = new()
        {
            Vertices = MeshData.Vertices.AsArray(),
            SolidIndices = MeshData.SolidIndices.AsArray(),
        };

        MeshBuilder.NormalizeNormalsJob normalizeJob = new()
        {
            Vertices = MeshData.Vertices.AsDeferredJobArray()
        };

        JobHandle calculateHandle = calculateJob.Schedule(MeshData.SolidIndices.Length / 3, 8);
        JobHandle normalizeHandle = normalizeJob.Schedule(MeshData.Vertices, 8, calculateHandle);

        return normalizeHandle;
    }

    public void BuildMesh(Mesh mesh)
    {
        mesh.Clear();

        Bounds bounds = new(
            new(Data.ChunkWidth / 2f, Data.ChunkHeight / 2, Data.ChunkLength / 2),
            new(Data.ChunkWidth + 1f, Data.ChunkHeight + 1f, Data.ChunkLength + 1f)
        );

        mesh.SetVertexBufferParams(MeshData.Vertices.Length, _layout);
        mesh.SetVertexBufferData(MeshData.Vertices.AsArray(), 0, 0, MeshData.Vertices.Length, 0, MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontNotifyMeshUsers);

        mesh.SetIndexBufferParams(MeshData.SolidIndices.Length + MeshData.TransparentIndices.Length, IndexFormat.UInt32);
        mesh.SetIndexBufferData(MeshData.SolidIndices.AsArray(),   /**/0, 0,               /**/MeshData.SolidIndices.Length, MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontNotifyMeshUsers);
        mesh.SetIndexBufferData(MeshData.TransparentIndices.AsArray(), 0, MeshData.SolidIndices.Length, MeshData.TransparentIndices.Length, MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontNotifyMeshUsers);

        var meshDesc = new SubMeshDescriptor()
        {
            indexStart = 0,
            indexCount = MeshData.SolidIndices.Length,
            firstVertex = MeshData.VerticesRanges[0].x,
            vertexCount = MeshData.VerticesRanges[0].y,
            bounds = bounds,
        };
        mesh.SetSubMesh(0, meshDesc, MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontNotifyMeshUsers);
        mesh.subMeshCount = 2;

        var tMeshDesc = new SubMeshDescriptor()
        {
            indexStart = MeshData.SolidIndices.Length,
            indexCount = MeshData.TransparentIndices.Length,
            firstVertex = MeshData.VerticesRanges[1].x,
            vertexCount = MeshData.VerticesRanges[1].y,
            bounds = bounds,
        };

        mesh.SetSubMesh(1, tMeshDesc, MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontNotifyMeshUsers);
        mesh.bounds = bounds;

        _numberOfFaces = FacesData.AllFaces.Length;

        //mesh.RecalculateNormals();
    }
}
