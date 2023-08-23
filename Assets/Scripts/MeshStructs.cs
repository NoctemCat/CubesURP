

using System.Runtime.InteropServices;
using Unity.Mathematics;

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
    public float2 UV;
}

public struct StructureMarker
{
    public int3 Position;
    public StructureType Type;

    public StructureMarker(int3 _position, StructureType _type)
        => (Position, Type) = (_position, _type);
}