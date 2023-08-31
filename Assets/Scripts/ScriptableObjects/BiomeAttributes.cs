using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;

[CreateAssetMenu(fileName = "BiomeAttributes", menuName = "Cubes/Biome Attribute")]
public class BiomeAttributes : ScriptableObject
{
    [Header("Biome Settings")]
    public string BiomeName;
    public int Offset;
    public float Scale;

    public int TerrainHeight;
    public float TerrainScale;

    public Block SurfaceBlock;
    public Block SubsurfaceBlock;

    [Header("Major Flora")]
    public StructureType FloraType;
    public float FloraZoneScale = 1.3f;
    [Range(0.1f, 1f)]
    public float FloraZoneThreshold = 0.6f;
    public float FloraPlacementScale = 15f;
    [Range(0.1f, 1f)]
    public float FloraPlacementThreshold = 0.8f;

    public int maxHeight = 12;
    public int minHeight = 5;

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
    readonly public int Offset;
    readonly public float Scale;
    readonly public int TerrainHeight;
    readonly public float TerrainScale;

    readonly public Block SurfaceBlock;
    readonly public Block SubsurfaceBlock;

    readonly public StructureType FloraType;
    readonly public float FloraZoneScale;
    readonly public float FloraZoneThreshold;
    readonly public float FloraPlacementScale;
    readonly public float FloraPlacementThreshold;

    readonly public int MaxHeight;
    readonly public int MinHeight;

    readonly public UnsafeList<LodeSctruct> Lodes;

    public BiomeStruct(BiomeAttributes biome)
    {
        Offset = biome.Offset;
        Scale = biome.Scale;
        TerrainHeight = biome.TerrainHeight;
        TerrainScale = biome.TerrainScale;
        SurfaceBlock = biome.SurfaceBlock;
        SubsurfaceBlock = biome.SubsurfaceBlock;
        FloraType = biome.FloraType;
        FloraZoneScale = biome.FloraZoneScale;
        FloraZoneThreshold = biome.FloraZoneThreshold;
        FloraPlacementScale = biome.FloraPlacementScale;
        FloraPlacementThreshold = biome.FloraPlacementThreshold;
        MaxHeight = biome.maxHeight;
        MinHeight = biome.minHeight;

        Lodes = new(biome.lodes.Length, Allocator.Persistent);
        for (int i = 0; i < biome.lodes.Length; i++)
        {
            Lodes.Add(new(biome.lodes[i]));
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