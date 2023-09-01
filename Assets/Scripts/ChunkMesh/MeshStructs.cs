

using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Mathematics;

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
    public int3 ChunkPos;
    public int3 Position;
    public Block Block;

    public VoxelMod(int3 _chunkPos, int3 _pos, Block _block)
        => (ChunkPos, Position, Block) = (_chunkPos, _pos, _block);
}

public struct VoxelDataForMesh
{
    public float3 Pos;
    public VoxelFaces Face;
    public BlockStruct Block;

    public VoxelDataForMesh(int3 pos, VoxelFaces face, BlockStruct block)
    {
        Pos = pos;
        Face = face;
        Block = block;
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct Vertex
{
    public float3 Pos;
    public float3 Nor;
    public float3 UV;
}

public struct StructureMarker
{
    public int BiomeIndex;
    public int3 Position;
    public StructureType Type;

    public StructureMarker(int _biomeIndex, int3 _position, StructureType _type)
        => (BiomeIndex, Position, Type) = (_biomeIndex, _position, _type);
}

