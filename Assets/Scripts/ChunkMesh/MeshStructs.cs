

using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Mathematics;

public struct MeshData
{
    public NativeList<Vertex> vertices;
    public NativeList<uint> solidIndices;
    public NativeList<uint> transparentIndices;
    public NativeArray<int2> verticesRanges;

    public void InitLists()
    {
        vertices = new NativeList<Vertex>(1, Allocator.Persistent);
        solidIndices = new NativeList<uint>(1, Allocator.Persistent);
        transparentIndices = new NativeList<uint>(1, Allocator.Persistent);
        verticesRanges = new NativeArray<int2>(2, Allocator.Persistent);
    }

    public void Dispose()
    {
        vertices.Dispose();
        solidIndices.Dispose();
        transparentIndices.Dispose();
        verticesRanges.Dispose();
    }
}

public struct MeshFacesData
{
    public NativeList<VoxelDataForMesh> allFaces;
    public NativeList<VoxelDataForMesh> solidFaces;
    public NativeList<VoxelDataForMesh> transparentFaces;
    public NativeReference<int> solidOffset;

    public void InitLists()
    {
        allFaces = new NativeList<VoxelDataForMesh>(1, Allocator.Persistent);
        solidFaces = new NativeList<VoxelDataForMesh>(1, Allocator.Persistent);
        transparentFaces = new NativeList<VoxelDataForMesh>(1, Allocator.Persistent);
        solidOffset = new NativeReference<int>(0, Allocator.Persistent);
    }

    public void Dispose()
    {
        allFaces.Dispose();
        solidFaces.Dispose();
        transparentFaces.Dispose();
        solidOffset.Dispose();
    }
}

public struct Neighbours
{
    public NativeArray<Block> back;
    public NativeArray<Block> front;
    public NativeArray<Block> top;
    public NativeArray<Block> bottom;
    public NativeArray<Block> right;
    public NativeArray<Block> left;
}

public struct VoxelMod
{
    public int3 chunkPos;
    public int3 position;
    public Block block;

    public VoxelMod(int3 _chunkPos, int3 _pos, Block _block)
        => (chunkPos, position, block) = (_chunkPos, _pos, _block);
}

public struct VoxelDataForMesh
{
    public float3 position;
    public VoxelFaces face;
    public BlockStruct block;

    public VoxelDataForMesh(int3 _pos, VoxelFaces _face, BlockStruct _block)
    {
        position = _pos;
        face = _face;
        block = _block;
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct Vertex
{
    public float3 position;
    public float3 normal;
    public float3 uv;
}

public struct StructureMarker
{
    public int biomeIndex;
    public int3 position;
    public StructureType type;

    public StructureMarker(int _biomeIndex, int3 _position, StructureType _type)
        => (biomeIndex, position, type) = (_biomeIndex, _position, _type);
}

