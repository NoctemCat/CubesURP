using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class CubeItem : MonoBehaviour, ISerializationCallbackReceiver
{
    public BlockObject block;
    public MaterialsObject Materials;

    private void OnEnable()
    {
        Materials = Resources.Load<MaterialsObject>("Data/Mats");
    }
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void OnAfterDeserialize()
    {
    }

    public void OnBeforeSerialize()
    {

        //int vertexIndex = 0;
        //List<Vector3> vertices = new(24);
        //List<int> triangles = new(36);
        //List<Vector2> uvs = new(24);
        //for (VoxelFaces f = VoxelFaces.Back; f < VoxelFaces.Max; f++)
        //{
        //    Vector3 pos = new(-0.5f, -0.5f, -0.5f);
        //    vertices.Add(pos + VoxelDataStatic.voxelVerts[VoxelDataStatic.voxelTris[(int)f, 0]]);
        //    vertices.Add(pos + VoxelDataStatic.voxelVerts[VoxelDataStatic.voxelTris[(int)f, 1]]);
        //    vertices.Add(pos + VoxelDataStatic.voxelVerts[VoxelDataStatic.voxelTris[(int)f, 2]]);
        //    vertices.Add(pos + VoxelDataStatic.voxelVerts[VoxelDataStatic.voxelTris[(int)f, 3]]);
        //    AddTexture(ref uvs, block.GetTextureID((int)f));
        //    triangles.Add(vertexIndex);
        //    triangles.Add(vertexIndex + 1);
        //    triangles.Add(vertexIndex + 2);
        //    triangles.Add(vertexIndex + 2);
        //    triangles.Add(vertexIndex + 1);
        //    triangles.Add(vertexIndex + 3);
        //    vertexIndex += 4;
        //}

        //MeshFilter meshFilter = transform.GetComponentInChildren<MeshFilter>();

        //meshFilter.sharedMesh.SetVertices(vertices.ToArray(), 0, vertices.Count);
        //meshFilter.sharedMesh.SetTriangles(triangles.ToArray(), 0);
        //meshFilter.sharedMesh.SetUVs(0, uvs);
        //meshFilter.sharedMesh.RecalculateBounds();
        //meshFilter.sharedMesh.RecalculateNormals();

        //MeshRenderer renderer = transform.GetComponentInChildren<MeshRenderer>();
        //renderer.material = (!block.isTransparent) ? Materials.SolidMaterial : Materials.TransparentMaterial;

        //EditorUtility.SetDirty(meshFilter);
        //EditorUtility.SetDirty(renderer);
    }


    //    Vertices[4 * i + 0] = new Vertex() { Pos = dot0, Nor = normal, UV = uvs.c0 };
    //    Vertices[4 * i + 1] = new Vertex() { Pos = dot1, Nor = normal, UV = uvs.c1 };
    //    Vertices[4 * i + 2] = new Vertex() { Pos = dot2, Nor = normal, UV = uvs.c2 };
    //    Vertices[4 * i + 3] = new Vertex() { Pos = dot3, Nor = normal, UV = uvs.c3 };
    //}

    private void AddTexture(ref List<Vector2> uvs, int textureID)
    {
        float x = textureID % VoxelDataStatic.TextureAtlasSizeInBlocks * VoxelDataStatic.NormalizedBlockTextureSize;
        float y = textureID / VoxelDataStatic.TextureAtlasSizeInBlocks * VoxelDataStatic.NormalizedBlockTextureSize;

        y = 1f - y - VoxelDataStatic.NormalizedBlockTextureSize;

        uvs.Add(new Vector2(x, y));
        uvs.Add(new Vector2(x, y + VoxelDataStatic.NormalizedBlockTextureSize));
        uvs.Add(new Vector2(x + VoxelDataStatic.NormalizedBlockTextureSize, y));
        uvs.Add(new Vector2(x + VoxelDataStatic.NormalizedBlockTextureSize, y + VoxelDataStatic.NormalizedBlockTextureSize));
    }
}
