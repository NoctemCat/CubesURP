using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[CreateAssetMenu(fileName = "BiomeAttributes", menuName = "Cubes/Biome Attribute")]
public class BiomeAttributes : ScriptableObject
{
    public int id;

    [Header("Biome Settings")]
    public string biomeName;
    public Vector2Int offset;
    public float scale;
    public float influenceMult = 1f;

    public int minSize;
    public int maxSize;

    public int minHeight = 5;
    public int maxHeight = 12;

    public int terrainHeight;
    public float terrainScale;

    public BiomeNoise noise;

    public Block surfaceBlock;
    public Block subsurfaceBlock;

    [Header("Major Flora")]
    public bool useFlora = true;
    public StructureType floraType;
    public float floraZoneScale = 1.3f;
    [Range(0.1f, 1f)]
    public float floraZoneThreshold = 0.6f;
    public float floraPlacementScale = 15f;
    [Range(0.1f, 1f)]
    public float floraPlacementThreshold = 0.8f;
    public int floraMinHeight = 5;
    public int floraMaxHeight = 12;


    public Lode[] lodes;

    [Header("Preview Settings(don't use them outside)")]
    public int previewWidth = 300;
    public int chunksShownWidth = 4;

    [HideInInspector] public bool needsAutoUpdate;
    protected void OnValidate() { needsAutoUpdate = true; }
    public void AutoUpdate() { needsAutoUpdate = false; }
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
    readonly public int id;

    readonly public int minSize;
    readonly public int maxSize;
    readonly public int2 offset;
    readonly public float scale;
    readonly public float influenceMult;

    readonly public int terrainHeight;
    readonly public float terrainScale;

    readonly public Block surfaceBlock;
    readonly public Block subsurfaceBlock;

    readonly public NoiseStruct noise;

    readonly public bool useFlora;
    readonly public StructureType floraType;
    readonly public float floraZoneScale;
    readonly public float floraZoneThreshold;
    readonly public float floraPlacementScale;
    readonly public float floraPlacementThreshold;
    readonly public int floraMinHeight;
    readonly public int floraMaxHeight;

    readonly public int maxHeight;
    readonly public int minHeight;

    readonly public UnsafeList<LodeSctruct> lodes;

    public BiomeStruct(BiomeAttributes biome)
    {
        id = biome.id;
        minSize = biome.minSize;
        maxSize = biome.maxSize;
        offset = new(biome.offset.x, biome.offset.y);
        scale = biome.scale;
        influenceMult = biome.influenceMult;

        terrainHeight = biome.terrainHeight;
        terrainScale = biome.terrainScale;
        surfaceBlock = biome.surfaceBlock;
        subsurfaceBlock = biome.subsurfaceBlock;
        noise = new(biome.noise);

        useFlora = biome.useFlora;
        floraType = biome.floraType;
        floraZoneScale = biome.floraZoneScale;
        floraZoneThreshold = biome.floraZoneThreshold;
        floraPlacementScale = biome.floraPlacementScale;
        floraPlacementThreshold = biome.floraPlacementThreshold;
        floraMinHeight = biome.floraMinHeight;
        floraMaxHeight = biome.floraMaxHeight;
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
        noise.Dispose(inputDeps);
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