using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;

[CreateAssetMenu(fileName = "Biome Noise", menuName = "Cubes/New Biome Noise")]
public class BiomeNoise : ScriptableObject
{
    [Header("If complex is false noise will use lacunarity and persistense of the first element")]
    public bool complexOctaves = true;
    public List<OctaveValues> octaves = new();

    [Header("Preview Options(Don't use them for code)")]
    public uint seed;
    public float noiseScale;
    public int chunksShownWidth;
    public Vector2 offset;
    public int previewWidth;

    [HideInInspector] public bool needsAutoUpdate;
    protected void OnValidate() { needsAutoUpdate = !Application.isPlaying; }
    public void AutoUpdate() { needsAutoUpdate = false; }
}

public readonly struct NoiseStruct
{
    public readonly bool complexOctaves;
    public readonly UnsafeList<OctaveValues> octaves;

    public NoiseStruct(BiomeNoise biomeNoise)
    {
        complexOctaves = biomeNoise.complexOctaves;
        octaves = new(biomeNoise.octaves.Count, Allocator.Persistent);
        for (int i = 0; i < biomeNoise.octaves.Count; i++)
        {
            octaves.Add(biomeNoise.octaves[i]);
        }
    }

    public readonly void Dispose(JobHandle inputDeps = default)
    {
        octaves.Dispose(inputDeps);
    }
}

public enum NoiseType
{
    Perlin,
    Simplex,
    PerlinInversed,
    SimplexInversed,
    PerlinRidged,
    SimplexRidged,
    PerlinRidgedInversed,
    SimplexRidgedInversed
}

[Serializable]
public struct OctaveValues
{
    [SerializeField] public NoiseType noiseType;
    [Range(1f, 10f)]
    [SerializeField] public float noiseLacunarity;
    [Range(0f, 1f)]
    [SerializeField] public float noisePersistence;
}