using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

[CreateAssetMenu(fileName = "BiomeAttributes", menuName = "Cubes/Biome Attribute")]
public class BiomeAttributes : ScriptableObject
{
    public string biomeName;
    public int solidGroundHeight;
    public int maxTerrainHeight;
    public float terrainScale;

    [Header("Trees")]
    public float treeZoneScale = 1.3f;
    [Range(0.1f, 1f)]
    public float treeZoneThreshold = 0.6f;
    public float treePlacementScale = 15f;
    [Range(0.1f, 1f)]
    public float treePlacementThreshold = 0.8f;

    public int maxTreeHeight = 12;
    public int minTreeHeight = 5;

    public Lode[] lodes;
}

[System.Serializable]
public class Lode
{
    public string nodeName;
    public Block blockID;
    public int minHeight;
    public int maxHeight;
    public float scale;
    public float threshold;
    public float noiseOffset;
}

public readonly struct BiomeStruct
{
    readonly public int SolidGroundHeight;
    readonly public int MaxTerrainHeight;
    readonly public float TerrainScale;

    readonly public float TreeZoneScale;
    readonly public float TreeZoneThreshold;
    readonly public float TreePlacementScale;
    readonly public float TreePlacementThreshold;

    readonly public int MaxTreeHeight;
    readonly public int MinTreeHeight;

    readonly public NativeArray<LodeSctruct> Lodes;

    public BiomeStruct(BiomeAttributes biome)
    {
        SolidGroundHeight = biome.solidGroundHeight;
        MaxTerrainHeight = biome.maxTerrainHeight;
        TerrainScale = biome.terrainScale;
        TreeZoneScale = biome.treeZoneScale;
        TreeZoneThreshold = biome.treeZoneThreshold;
        TreePlacementScale = biome.treePlacementScale;
        TreePlacementThreshold = biome.treePlacementThreshold;
        MaxTreeHeight = biome.maxTreeHeight;
        MinTreeHeight = biome.minTreeHeight;

        Lodes = new(biome.lodes.Length, Allocator.Persistent);
        for (int i = 0; i < biome.lodes.Length; i++)
        {
            Lodes[i] = new(biome.lodes[i]);
        }
    }

    public readonly void Dispose(JobHandle inputDeps = default)
    {
        Lodes.Dispose(inputDeps);
    }
}

public struct LodeSctruct
{
    public Block BlockID;
    public int MinHeight;
    public int MaxHeight;
    public float Scale;
    public float Threshold;
    public float NoiseOffset;

    public LodeSctruct(Lode lode)
    {
        BlockID = lode.blockID;
        MinHeight = lode.minHeight;
        MaxHeight = lode.maxHeight;
        Scale = lode.scale;
        Threshold = lode.threshold;
        NoiseOffset = lode.noiseOffset;
    }
}