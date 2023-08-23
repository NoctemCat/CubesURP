using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "BlockType", menuName = "Cubes/Block Type")]
public class BlockObject : ScriptableObject
{
    public string blockName;
    public bool isSolid;
    public bool isTransparent;
    public Sprite icon;

    [Header("Texture Values")]
    public int backfaceTexture;
    public int frontfaceTexture;
    public int topfaceTexture;
    public int bottomfaceTexture;
    public int leftfaceTexture;
    public int rightfaceTexture;

    // Back, Front, Top, Bottom, Left, Right
    public int GetTextureID(int faceIndex)
    {
        switch (faceIndex)
        {
            case 0:
                return backfaceTexture;
            case 1:
                return frontfaceTexture;
            case 2:
                return topfaceTexture;
            case 3:
                return bottomfaceTexture;
            case 4:
                return leftfaceTexture;
            case 5:
                return rightfaceTexture;
            default:
                Debug.Log("Error in GetTextureID; invalid face index");
                return 0;
        }
    }
}

public struct BlockStruct
{

    public bool isSolid;
    public bool isTransparent;

    public int backfaceTexture;
    public int frontfaceTexture;
    public int topfaceTexture;
    public int bottomfaceTexture;
    public int leftfaceTexture;
    public int rightfaceTexture;

    public BlockStruct(BlockObject block)
    {
        isSolid = block.isSolid;
        isTransparent = block.isTransparent;

        backfaceTexture = block.backfaceTexture;
        frontfaceTexture = block.frontfaceTexture;
        topfaceTexture = block.topfaceTexture;
        bottomfaceTexture = block.bottomfaceTexture;
        leftfaceTexture = block.leftfaceTexture;
        rightfaceTexture = block.rightfaceTexture;
    }

    // Back, Front, Top, Bottom, Left, Right
    public readonly int GetTextureID(int faceIndex)
    {
        switch (faceIndex)
        {
            case 0:
                return backfaceTexture;
            case 1:
                return frontfaceTexture;
            case 2:
                return topfaceTexture;
            case 3:
                return bottomfaceTexture;
            case 4:
                return leftfaceTexture;
            case 5:
                return rightfaceTexture;
            default:
                Debug.Log("Error in GetTextureID; invalid face index");
                return 0;
        }
    }
}