using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "BlockType", menuName = "Cubes/Block Type")]
public class BlockObject : ItemObject
{
    public Block BlockType;
    public bool IsSolid;
    public bool IsTransparent;

    [Header("Texture Values")]
    public int BackfaceTexture;
    public int FrontfaceTexture;
    public int TopfaceTexture;
    public int BottomfaceTexture;
    public int LeftfaceTexture;
    public int RightfaceTexture;

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