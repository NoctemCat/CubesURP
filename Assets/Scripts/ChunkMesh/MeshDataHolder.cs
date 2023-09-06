

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
    private World _world;
    private VoxelData data;
    private int3 _chunkPos;

    private MeshData _meshData;
    private MeshFacesData _facesData;

    private NativeArray<int> _countBlocks;
    private NativeArray<int> _counters;

    NativeArray<VertexAttributeDescriptor> _layout;

    int _numberOfFaces;
    public readonly bool HasFaces => _numberOfFaces > 0;

    /// <summary>
    /// Can only be called after CountBlockTypes
    /// </summary>
    public bool IsEmpty => _countBlocks[2] == 0;

    public void Init(int3 chunkPos)
    {
        _world = World.Instance;
        data = _world.VoxelData;
        _chunkPos = chunkPos;

        _meshData.InitLists();
        _facesData.InitLists();
        //FacesData.AllFaces

        _layout = new NativeArray<VertexAttributeDescriptor>(3, Allocator.Persistent);
        _layout[0] = new(VertexAttribute.Position, VertexAttributeFormat.Float32, 3);
        _layout[1] = new(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3);
        _layout[2] = new(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 3);

        _countBlocks = new(64, Allocator.Persistent);
        _counters = new(JobsUtility.MaxJobThreadCount * JobsUtility.CacheLineSize, Allocator.Persistent);

        _numberOfFaces = 0;
    }

    public void Dispose()
    {
        _meshData.Dispose();
        _facesData.Dispose();
        _countBlocks.Dispose();
        _counters.Dispose();
        _layout.Dispose();
    }

    public JobHandle CountBlockTypes(JobHandle voxelMapAccess, NativeArray<Block> voxelMap)
    {
        for (var i = 0; i < JobsUtility.MaxJobThreadCount; i++)
        {
            int threadOffset = i * JobsUtility.CacheLineSize;
            _counters[threadOffset] = 0;
            _counters[threadOffset + 1] = 0;
            _counters[threadOffset + 2] = 0;
        }

        MeshBuilder.CountBlockTypesJob countBlockTypes = new()
        {
            Blocks = _world.NativeBlocks,
            VoxelMap = voxelMap,
            Counters = _counters,
        };
        voxelMapAccess = countBlockTypes.Schedule(voxelMap.Length, 1, voxelMapAccess);

        MeshBuilder.SumBlockTypesJob sumBlockTypes = new()
        {
            Counters = _counters,
            Totals = _countBlocks,
        };

        return sumBlockTypes.Schedule(voxelMapAccess);
    }

    public void ResizeFacesData()
    {
        _facesData.allFaces.Clear();
        _facesData.solidFaces.Clear();
        _facesData.transparentFaces.Clear();
        _facesData.solidOffset.Value = 0;

        _facesData.allFaces.Capacity = (_countBlocks[0] + _countBlocks[1]) * 6;
        _facesData.solidFaces.Capacity = _countBlocks[0] * 6;
        _facesData.transparentFaces.Capacity = _countBlocks[1] * 6;
    }

    public JobHandle SortVoxels(JobHandle voxelMapAccess, NativeArray<Block> voxelMap)
    {
        // TODO add neighbours
        JobHandle access = FillNeighbours(out Neighbours neighbours);
        MeshBuilder.SortVoxelFacesJob sortVoxelsJob = new()
        {
            Data = data,
            ChunkNeighbours = neighbours,
            ChunkPos = _chunkPos,
            VoxelMap = voxelMap,
            Blocks = _world.NativeBlocks,
            XYZMap = _world.XYZMap,

            SolidFaces = _facesData.solidFaces.AsParallelWriter(),
            TransparentFaces = _facesData.transparentFaces.AsParallelWriter(),
        };
        //return sortVoxelsJob.Schedule(Data.ChunkSize, Data.ChunkSize / 8, voxelMapAccess);
        return sortVoxelsJob.Schedule(data.ChunkSize, data.ChunkSize / 8, JobHandle.CombineDependencies(voxelMapAccess, access));
    }

    public JobHandle FillNeighbours(out Neighbours neighbours)
    {
        NativeList<JobHandle> accesses = new(6, Allocator.Temp);
        if (_world.Chunks.TryGetValue(I3ToVI3(_chunkPos + data.FaceChecks[(int)VoxelFaces.Back]), out Chunk chunk))
        {
            neighbours.back = chunk.VoxelMap;
            accesses.Add(chunk.VoxelMapAccess);
        }
        else
            neighbours.back = _world.DummyMap;

        if (_world.Chunks.TryGetValue(I3ToVI3(_chunkPos + data.FaceChecks[(int)VoxelFaces.Front]), out chunk))
        {
            neighbours.front = chunk.VoxelMap;
            accesses.Add(chunk.VoxelMapAccess);
        }
        else
            neighbours.front = _world.DummyMap;

        if (_world.Chunks.TryGetValue(I3ToVI3(_chunkPos + data.FaceChecks[(int)VoxelFaces.Top]), out chunk))
        {
            neighbours.top = chunk.VoxelMap;
            accesses.Add(chunk.VoxelMapAccess);
        }
        else
            neighbours.top = _world.DummyMap;

        if (_world.Chunks.TryGetValue(I3ToVI3(_chunkPos + data.FaceChecks[(int)VoxelFaces.Bottom]), out chunk))
        {
            neighbours.bottom = chunk.VoxelMap;
            accesses.Add(chunk.VoxelMapAccess);
        }
        else
            neighbours.bottom = _world.DummyMap;

        if (_world.Chunks.TryGetValue(I3ToVI3(_chunkPos + data.FaceChecks[(int)VoxelFaces.Left]), out chunk))
        {
            neighbours.left = chunk.VoxelMap;
            accesses.Add(chunk.VoxelMapAccess);
        }
        else
            neighbours.left = _world.DummyMap;

        if (_world.Chunks.TryGetValue(I3ToVI3(_chunkPos + data.FaceChecks[(int)VoxelFaces.Right]), out chunk))
        {
            neighbours.right = chunk.VoxelMap;
            accesses.Add(chunk.VoxelMapAccess);
        }
        else
            neighbours.right = _world.DummyMap;


        return JobHandle.CombineDependencies(accesses.AsArray());
    }

    public void ResizeMeshData()
    {
        int allFacesCount = _facesData.solidFaces.Length + _facesData.transparentFaces.Length;

        _facesData.allFaces.Clear();

        _facesData.allFaces.CopyFrom(_facesData.solidFaces);
        _facesData.allFaces.AddRangeNoResize(_facesData.transparentFaces);

        _meshData.vertices.ResizeUninitialized(allFacesCount * 4);
        _meshData.solidIndices.ResizeUninitialized(_facesData.solidFaces.Length * 6);
        _meshData.transparentIndices.ResizeUninitialized(_facesData.transparentFaces.Length * 6);

        _facesData.solidOffset.Value = _facesData.solidFaces.Length * 4;

        int2 minmaxSolid = new()
        {
            x = 0,
            y = (_facesData.solidFaces.Length - 1) * 4 + 3
        };
        int transparentLen = (_facesData.transparentFaces.Length > 0) ? (_facesData.transparentFaces.Length - 1) * 4 + 3 : 0;
        int2 minmaxTransparent = new()
        {
            x = _facesData.solidOffset.Value,
            y = _facesData.solidOffset.Value + transparentLen
        };

        _meshData.verticesRanges[0] = minmaxSolid;
        _meshData.verticesRanges[1] = minmaxTransparent;
    }

    public JobHandle FillMeshData()
    {
        MeshBuilder.FillVerticesJob fillVerticesJob = new()
        {
            Data = data,
            AllFaces = _facesData.allFaces.AsArray(),
            Vertices = _meshData.vertices.AsArray(),
        };
        JobHandle fillVerticesHandle = fillVerticesJob.Schedule(_facesData.allFaces.Length, 8);

        MeshBuilder.FillSolidIndicesJob fillSolidJob = new()
        {
            SolidFaces = _facesData.solidFaces.AsArray(),
            SolidIndices = _meshData.solidIndices.AsArray(),
        };
        JobHandle fillSolidHandle = fillSolidJob.Schedule(_facesData.solidFaces.Length, 8);

        MeshBuilder.FillTransparentIndicesJob fillTransparentJob = new()
        {
            SolidOffset = _facesData.solidOffset,
            TransparentFaces = _facesData.transparentFaces.AsArray(),
            TransparentIndices = _meshData.transparentIndices.AsArray(),
        };
        JobHandle fillTransparentHandle = fillTransparentJob.Schedule(_facesData.transparentFaces.Length, 8);

        return JobHandle.CombineDependencies(fillVerticesHandle, fillSolidHandle, fillTransparentHandle);
    }

    public JobHandle CalculateNormals()
    {
        //NativeParallelHashMap<float3, float3> hashNormals = new(MeshData.Vertices.Length, Allocator.Persistent);
        MeshBuilder.CalculateNormalsJob calculateJob = new()
        {
            Vertices = _meshData.vertices.AsArray(),
            SolidIndices = _meshData.solidIndices.AsArray(),
        };

        MeshBuilder.NormalizeNormalsJob normalizeJob = new()
        {
            Vertices = _meshData.vertices.AsDeferredJobArray()
        };

        JobHandle calculateHandle = calculateJob.Schedule(_meshData.solidIndices.Length / 3, 8);
        JobHandle normalizeHandle = normalizeJob.Schedule(_meshData.vertices, 8, calculateHandle);

        return normalizeHandle;
    }

    public void BuildMesh(Mesh mesh)
    {
        mesh.Clear();

        Bounds bounds = new(
            new(data.ChunkWidth / 2f, data.ChunkHeight / 2, data.ChunkLength / 2),
            new(data.ChunkWidth + 1f, data.ChunkHeight + 1f, data.ChunkLength + 1f)
        );

        mesh.SetVertexBufferParams(_meshData.vertices.Length, _layout);
        mesh.SetVertexBufferData(_meshData.vertices.AsArray(), 0, 0, _meshData.vertices.Length, 0, MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontNotifyMeshUsers);

        mesh.SetIndexBufferParams(_meshData.solidIndices.Length + _meshData.transparentIndices.Length, IndexFormat.UInt32);
        mesh.SetIndexBufferData(_meshData.solidIndices.AsArray(),   /**/0, 0,               /**/_meshData.solidIndices.Length, MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontNotifyMeshUsers);
        mesh.SetIndexBufferData(_meshData.transparentIndices.AsArray(), 0, _meshData.solidIndices.Length, _meshData.transparentIndices.Length, MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontNotifyMeshUsers);

        var meshDesc = new SubMeshDescriptor()
        {
            indexStart = 0,
            indexCount = _meshData.solidIndices.Length,
            firstVertex = _meshData.verticesRanges[0].x,
            vertexCount = _meshData.verticesRanges[0].y,
            bounds = bounds,
        };
        mesh.SetSubMesh(0, meshDesc, MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontNotifyMeshUsers);
        mesh.subMeshCount = 2;

        var tMeshDesc = new SubMeshDescriptor()
        {
            indexStart = _meshData.solidIndices.Length,
            indexCount = _meshData.transparentIndices.Length,
            firstVertex = _meshData.verticesRanges[1].x,
            vertexCount = _meshData.verticesRanges[1].y,
            bounds = bounds,
        };

        mesh.SetSubMesh(1, tMeshDesc, MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontNotifyMeshUsers);
        mesh.bounds = bounds;

        _numberOfFaces = _facesData.allFaces.Length;

        //mesh.RecalculateNormals();
    }
}
