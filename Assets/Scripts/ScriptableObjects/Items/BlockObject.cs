using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Rendering;

[Serializable]
[CreateAssetMenu(fileName = "BlockType", menuName = "Cubes/Block Type")]
public class BlockObject : ItemObject, IDroppable
{
    public Block BlockType;
    public bool IsSolid;
    public bool IsTransparent;
    //[NonSerialized] public Mesh ItemMesh;

    [Header("Texture Values")]
    public int BackfaceTexture;
    public int FrontfaceTexture;
    public int TopfaceTexture;
    public int BottomfaceTexture;
    public int LeftfaceTexture;
    public int RightfaceTexture;
    public Mesh ItemMesh { get; set; }

    // Back, Front, Top, Bottom, Left, Right
    public int GetTextureID(int faceIndex)
    {
        return faceIndex switch
        {
            0 => BackfaceTexture,
            1 => FrontfaceTexture,
            2 => TopfaceTexture,
            3 => BottomfaceTexture,
            4 => LeftfaceTexture,
            5 => RightfaceTexture,
            _ => throw new ArgumentOutOfRangeException("Invalid face index"),
        };
    }

    protected override void OnEnable()
    {
        GenerateItemMesh();
    }

    public void GenerateItemMesh()
    {
        ItemMesh = new Mesh();

        Vector3[] verts = new Vector3[24];
        Vector3[] normals = new Vector3[24];
        Vector3[] uvs = new Vector3[24];
        uint[] indices = new uint[36];

        VertexAttributeDescriptor[] layout = new VertexAttributeDescriptor[3];
        layout[0] = new(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, 0);
        layout[1] = new(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3, 1);
        layout[2] = new(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 3, 2);

        for (VoxelFaces i = VoxelFaces.Back; i < VoxelFaces.Max; i++)
        {
            int fi = (int)i;
            verts[fi * 4 + 0] = VoxelDataStatic.voxelVerts[VoxelDataStatic.voxelTris[fi, 0]] - new Vector3(0.5f, 0.5f, 0.5f);
            verts[fi * 4 + 1] = VoxelDataStatic.voxelVerts[VoxelDataStatic.voxelTris[fi, 1]] - new Vector3(0.5f, 0.5f, 0.5f);
            verts[fi * 4 + 2] = VoxelDataStatic.voxelVerts[VoxelDataStatic.voxelTris[fi, 2]] - new Vector3(0.5f, 0.5f, 0.5f);
            verts[fi * 4 + 3] = VoxelDataStatic.voxelVerts[VoxelDataStatic.voxelTris[fi, 3]] - new Vector3(0.5f, 0.5f, 0.5f);
            normals[fi * 4 + 0] = VoxelDataStatic.voxelNormals[fi];
            normals[fi * 4 + 1] = VoxelDataStatic.voxelNormals[fi];
            normals[fi * 4 + 2] = VoxelDataStatic.voxelNormals[fi];
            normals[fi * 4 + 3] = VoxelDataStatic.voxelNormals[fi];
            uvs[fi * 4 + 0] = new(0f, 0f, GetTextureID((int)i));
            uvs[fi * 4 + 1] = new(0f, 1f, GetTextureID((int)i));
            uvs[fi * 4 + 2] = new(1f, 0f, GetTextureID((int)i));
            uvs[fi * 4 + 3] = new(1f, 1f, GetTextureID((int)i));

            indices[fi * 6 + 0] = (uint)(fi * 4 + 0);
            indices[fi * 6 + 1] = (uint)(fi * 4 + 1);
            indices[fi * 6 + 2] = (uint)(fi * 4 + 2);
            indices[fi * 6 + 3] = (uint)(fi * 4 + 2);
            indices[fi * 6 + 4] = (uint)(fi * 4 + 1);
            indices[fi * 6 + 5] = (uint)(fi * 4 + 3);
        }

        ItemMesh.SetVertexBufferParams(24, layout);
        ItemMesh.SetVertexBufferData(verts, 0, 0, verts.Length, 0);
        ItemMesh.SetVertexBufferData(normals, 0, 0, normals.Length, 1);
        ItemMesh.SetVertexBufferData(uvs, 0, 0, uvs.Length, 2);

        ItemMesh.SetIndexBufferParams(indices.Length, IndexFormat.UInt32);
        ItemMesh.SetIndexBufferData(indices, 0, 0, indices.Length);

        Bounds bounds = new(new(0f, 0f, 0f), new(1f, 1f, 1f));

        var meshDesc = new SubMeshDescriptor()
        {
            indexStart = 0,
            indexCount = indices.Length,
            firstVertex = 0,
            vertexCount = verts.Length,
            bounds = bounds,
        };
        ItemMesh.SetSubMesh(0, meshDesc);
        ItemMesh.bounds = bounds;
        ItemMesh.UploadMeshData(false);
    }

    private void Reset()
    {
        Type = ItemType.Block;
        Stackable = true;
    }

}

public struct BlockStruct
{

    public bool IsSolid;
    public bool IsTransparent;

    public int BackfaceTexture;
    public int FrontfaceTexture;
    public int TopfaceTexture;
    public int BottomfaceTexture;
    public int LeftfaceTexture;
    public int RightfaceTexture;

    public BlockStruct(BlockObject block)
    {
        IsSolid = block.IsSolid;
        IsTransparent = block.IsTransparent;

        BackfaceTexture = block.BackfaceTexture;
        FrontfaceTexture = block.FrontfaceTexture;
        TopfaceTexture = block.TopfaceTexture;
        BottomfaceTexture = block.BottomfaceTexture;
        LeftfaceTexture = block.LeftfaceTexture;
        RightfaceTexture = block.RightfaceTexture;
    }

    // Back, Front, Top, Bottom, Left, Right
    public readonly int GetTextureID(int faceIndex)
    {
        return faceIndex switch
        {
            0 => BackfaceTexture,
            1 => FrontfaceTexture,
            2 => TopfaceTexture,
            3 => BottomfaceTexture,
            4 => LeftfaceTexture,
            5 => RightfaceTexture,
            _ => throw new ArgumentOutOfRangeException("Invalid face index"),
        };
    }
}