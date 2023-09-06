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
    public string biomeName;
    public int offset;
    public float scale;

    public int terrainHeight;
    public float terrainScale;

    public Block surfaceBlock;
    public Block subsurfaceBlock;

    [Header("Major Flora")]
    public StructureType floraType;
    public float floraZoneScale = 1.3f;
    [Range(0.1f, 1f)]
    public float floraZoneThreshold = 0.6f;
    public float floraPlacementScale = 15f;
    [Range(0.1f, 1f)]
    public float floraPlacementThreshold = 0.8f;

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
    readonly public int offset;
    readonly public float scale;
    readonly public int terrainHeight;
    readonly public float terrainScale;

    readonly public Block surfaceBlock;
    readonly public Block subsurfaceBlock;

    readonly public StructureType floraType;
    readonly public float floraZoneScale;
    readonly public float floraZoneThreshold;
    readonly public float floraPlacementScale;
    readonly public float floraPlacementThreshold;

    readonly public int maxHeight;
    readonly public int minHeight;

    readonly public UnsafeList<LodeSctruct> lodes;

    public BiomeStruct(BiomeAttributes biome)
    {
        offset = biome.offset;
        scale = biome.scale;
        terrainHeight = biome.terrainHeight;
        terrainScale = biome.terrainScale;
        surfaceBlock = biome.surfaceBlock;
        subsurfaceBlock = biome.subsurfaceBlock;
        floraType = biome.floraType;
        floraZoneScale = biome.floraZoneScale;
        floraZoneThreshold = biome.floraZoneThreshold;
        floraPlacementScale = biome.floraPlacementScale;
        floraPlacementThreshold = biome.floraPlacementThreshold;
        maxHeight = biome.maxHeight;
        minHeight = biome.minHeight;

        lodes = new(biome.lodes.Length, Allocator.Persistent);
        for (int i = 0; i < biome.lodes.Length; i++)
        {
            lodes.Add(new(biome.lodes[i]));
        }
    }

    public readonly void Dispose(JobHandle inputDeps = default)
    {
        lodes.Dispose(inputDeps);
    }
}

public struct LodeSctruct
{
    public Block blockID;
    public int minHeight;
    public int maxHeight;
    public float scale;
    public float threshold;
    public float noiseOffset;

    public LodeSctruct(Lode lode)
    {
        blockID = lode.blockID;
        minHeight = lode.minHeight;
        maxHeight = lode.maxHeight;
        scale = lode.scale;
        threshold = lode.threshold;
        noiseOffset = lode.noiseOffset;
    }
}