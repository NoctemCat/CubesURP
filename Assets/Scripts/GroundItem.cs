using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

public class GroundItem : MonoBehaviour
{
    public ItemObject ItemObj;
    public int Amount;
    public float CreateTime { get; private set; }
    private static readonly Collider[] _colliders = new Collider[10];
    private static LayerMask _mask;
    private static bool _initMask;

    private void Start()
    {
        if (!_initMask)
        {
            _initMask = true;
            _mask = LayerMask.GetMask("GroundItems");
        }
        CreateTime = Time.time;
    }

    public void SetItem(ItemObject item, int amount)
    {
        ItemObj = item;
        Amount = amount;
        CreateTime = Time.time;

        if (item is BlockObject blockObject)
        {
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            Mesh mesh = new();

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
                uvs[fi * 4 + 0] = new(0f, 0f, blockObject.GetTextureID((int)i));
                uvs[fi * 4 + 1] = new(0f, 1f, blockObject.GetTextureID((int)i));
                uvs[fi * 4 + 2] = new(1f, 0f, blockObject.GetTextureID((int)i));
                uvs[fi * 4 + 3] = new(1f, 1f, blockObject.GetTextureID((int)i));

                indices[fi * 6 + 0] = (uint)(fi * 4 + 0);
                indices[fi * 6 + 1] = (uint)(fi * 4 + 1);
                indices[fi * 6 + 2] = (uint)(fi * 4 + 2);
                indices[fi * 6 + 3] = (uint)(fi * 4 + 2);
                indices[fi * 6 + 4] = (uint)(fi * 4 + 1);
                indices[fi * 6 + 5] = (uint)(fi * 4 + 3);
            }

            mesh.SetVertexBufferParams(24, layout);
            mesh.SetVertexBufferData(verts, 0, 0, verts.Length, 0);
            mesh.SetVertexBufferData(normals, 0, 0, normals.Length, 1);
            mesh.SetVertexBufferData(uvs, 0, 0, uvs.Length, 2);

            mesh.SetIndexBufferParams(indices.Length, IndexFormat.UInt32);
            mesh.SetIndexBufferData(indices, 0, 0, indices.Length);

            Bounds bounds = new(new(0f, 0f, 0f), new(1f, 1f, 1f));

            var meshDesc = new SubMeshDescriptor()
            {
                indexStart = 0,
                indexCount = indices.Length,
                firstVertex = 0,
                vertexCount = verts.Length,
                bounds = bounds,
            };
            mesh.SetSubMesh(0, meshDesc);
            mesh.bounds = bounds;
            mesh.UploadMeshData(false);
            meshFilter.mesh = mesh;
        }
    }

    private void FixedUpdate()
    {
        if (Time.time - CreateTime <= 0.2f) return;

        int nums = Physics.OverlapSphereNonAlloc(transform.position, transform.localScale.x / 2 / 2, _colliders, _mask);
        for (int i = 0; i < nums; i++)
        {
            if (gameObject == _colliders[i].gameObject) continue;

            if (_colliders[i].gameObject.TryGetComponent(out GroundItem other) && Time.time - other.CreateTime > 0.2f && ItemObj.Data.Id == other.ItemObj.Data.Id)
            {
                Amount += other.Amount;
                _colliders[i].gameObject.SetActive(false);
            }
        }
    }

    //private void OnTriggerEnter(Collider other)
    //{
    //}

}


