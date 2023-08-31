using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(BlockObject))]
public class BlockTypesEditor : Editor
{

    BlockObject block;
    Texture2DArray blockAtlas;

    private void OnEnable()
    {
        //target is by default available for you
        //because we inherite Editor
        block = target as BlockObject;
    }

    public override void OnInspectorGUI()
    {
        //Draw whatever we already have in SO definition
        base.OnInspectorGUI();
        blockAtlas = (Texture2DArray)Resources.Load("BlockAtlas");

        Texture2D backface = PrepareTexture(block.backfaceTexture);
        Texture2D frontface = PrepareTexture(block.frontfaceTexture);
        Texture2D topface = PrepareTexture(block.topfaceTexture);
        Texture2D bottomface = PrepareTexture(block.bottomfaceTexture);
        Texture2D leftface = PrepareTexture(block.leftfaceTexture);
        Texture2D rightface = PrepareTexture(block.rightfaceTexture);

        GUILayout.Space(10);
        GUILayout.BeginHorizontal();

        AddTexturePreview("Back face", backface);
        GUILayout.Space(10);
        AddTexturePreview("Front face", frontface);
        GUILayout.Space(10);
        AddTexturePreview("Top face", topface);

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        AddTexturePreview("Bottom face", bottomface);
        GUILayout.Space(10);
        AddTexturePreview("Left face", leftface);
        GUILayout.Space(10);
        AddTexturePreview("Right face", rightface);

        GUILayout.EndHorizontal();
    }

    private void AddTexturePreview(string name, Texture2D texture)
    {
        GUILayout.BeginVertical();
        GUILayout.Label(name, GUILayout.Height(20), GUILayout.Width(80));
        GUILayout.Label("", GUILayout.Height(80), GUILayout.Width(80));
        GUI.DrawTexture(GUILayoutUtility.GetLastRect(), texture);
        GUILayout.EndVertical();
    }

    private Texture2D PrepareTexture(int textureID)
    {

        Texture2D croppedTexture = new(blockAtlas.width, blockAtlas.height)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Point,
        };
        if (textureID >= 0 && textureID < blockAtlas.width * blockAtlas.height)
        {
            croppedTexture.SetPixels32(blockAtlas.GetPixels32(textureID));
        }
        croppedTexture.Apply();

        return croppedTexture;
    }

    //Texture2D for later
    //public override void OnInspectorGUI()
    //{
    //    //Draw whatever we already have in SO definition
    //    base.OnInspectorGUI();
    //    blockAtlas = (Texture2D)Resources.Load("BlockAtlas");

    //    Texture2D backface = PrepareTexture(block.backfaceTexture);
    //    Texture2D frontface = PrepareTexture(block.frontfaceTexture);
    //    Texture2D topface = PrepareTexture(block.topfaceTexture);
    //    Texture2D bottomface = PrepareTexture(block.bottomfaceTexture);
    //    Texture2D leftface = PrepareTexture(block.leftfaceTexture);
    //    Texture2D rightface = PrepareTexture(block.rightfaceTexture);

    //    GUILayout.Space(10);
    //    GUILayout.BeginHorizontal();

    //    AddTexturePreview("Back face", backface);
    //    GUILayout.Space(10);
    //    AddTexturePreview("Front face", frontface);
    //    GUILayout.Space(10);
    //    AddTexturePreview("Top face", topface);

    //    GUILayout.EndHorizontal();

    //    GUILayout.BeginHorizontal();

    //    AddTexturePreview("Bottom face", bottomface);
    //    GUILayout.Space(10);
    //    AddTexturePreview("Left face", leftface);
    //    GUILayout.Space(10);
    //    AddTexturePreview("Right face", rightface);

    //    GUILayout.EndHorizontal();
    //}

    //private void AddTexturePreview(string name, Texture2D texture)
    //{
    //    GUILayout.BeginVertical();
    //    GUILayout.Label(name, GUILayout.Height(20), GUILayout.Width(80));
    //    GUILayout.Label("", GUILayout.Height(80), GUILayout.Width(80));
    //    GUI.DrawTexture(GUILayoutUtility.GetLastRect(), texture);
    //    GUILayout.EndVertical();
    //}

    //private Texture2D PrepareTexture(int textureID)
    //{
    //    float x = textureID % VoxelDataStatic.TextureAtlasSizeInBlocks * VoxelDataStatic.TextureAtlasSizeInBlocks;
    //    float y = textureID / VoxelDataStatic.TextureAtlasSizeInBlocks * VoxelDataStatic.TextureAtlasSizeInBlocks;

    //    y = blockAtlas.height - y - VoxelDataStatic.TextureAtlasSizeInBlocks;

    //    Texture2D croppedTexture = new(VoxelDataStatic.TextureAtlasSizeInBlocks, VoxelDataStatic.TextureAtlasSizeInBlocks)
    //    {
    //        wrapMode = TextureWrapMode.Clamp,
    //        filterMode = FilterMode.Point,
    //    };
    //    if (textureID >= 0 && textureID < VoxelDataStatic.TextureAtlasSizeInBlocks * VoxelDataStatic.TextureAtlasSizeInBlocks)
    //    {
    //        croppedTexture.SetPixels(blockAtlas.GetPixels((int)x, (int)y, VoxelDataStatic.TextureAtlasSizeInBlocks, VoxelDataStatic.TextureAtlasSizeInBlocks));
    //    }
    //    croppedTexture.Apply();

    //    return croppedTexture;
    //}
}