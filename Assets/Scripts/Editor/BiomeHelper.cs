using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public static class BiomeHelper
{
    public static void FillTexture(ref Texture2D _noiseTexture, int mapWidth, int mapHeight, uint seed, List<OctaveValues> octaves, bool complexOctaves, float scale, Vector2 offset)
    {
        if (_noiseTexture == null) return;
        float[,] noiseMap = GenerateNoiseMap(mapWidth, mapHeight, seed, octaves, complexOctaves, scale, offset);

        Color[] colourMap = new Color[mapWidth * mapHeight];
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                colourMap[y * mapWidth + x] = Color.Lerp(Color.black, Color.white, noiseMap[x, y]);
            }
        }

        _noiseTexture.SetPixels(colourMap);
        _noiseTexture.Apply();
    }

    private static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, uint seed, List<OctaveValues> octaves, bool complexOctaves, float scale, Vector2 offset)
    {
        float[,] noiseMap = new float[mapWidth, mapHeight];

        Unity.Mathematics.Random prng = new(seed);
        float2[] octavesOffsets = new float2[octaves.Count];

        for (int i = 0; i < octaves.Count; i++)
        {
            octavesOffsets[i] = prng.NextFloat2(-100000, 100000) + (float2)offset;
        }

        if (scale <= 0)
        {
            scale = 0.001f;
        }

        float maxNoiseHeight = float.MinValue;
        float minNoiseHeight = float.MaxValue;

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                float noiseHeight = GetHeight(new(x, y), new(mapWidth, mapHeight), scale, octaves, octavesOffsets, complexOctaves);

                if (noiseHeight < minNoiseHeight)
                    minNoiseHeight = noiseHeight;
                if (noiseHeight > maxNoiseHeight)
                    maxNoiseHeight = noiseHeight;

                noiseMap[x, y] = noiseHeight;
            }
        }
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                noiseMap[x, y] = math.unlerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y]);
            }
        }
        return noiseMap;
    }

    public static float GetHeight(float2 pos, float2 size, float scale, List<OctaveValues> octaves, float2[] octavesOffsets, bool complexOctaves)
    {
        float frequency = 1f;
        float amplitute = 1f;
        float noiseHeight = 0f;

        for (int i = 0; i < octaves.Count; i++)
        {
            float2 sample = ((pos - size / 2) / scale + octavesOffsets[i]) * frequency;

            float perlinValue = (complexOctaves ? octaves[i].noiseType : octaves[0].noiseType) switch
            {
                NoiseType.Perlin => noise.cnoise(sample),
                NoiseType.Simplex => noise.snoise(sample),
                NoiseType.PerlinInversed => -noise.cnoise(sample),
                NoiseType.SimplexInversed => -noise.snoise(sample),
                NoiseType.PerlinRidged => 1f - math.abs(noise.cnoise(sample)),
                NoiseType.SimplexRidged => 1f - math.abs(noise.snoise(sample)),
                NoiseType.PerlinRidgedInversed => -1f + math.abs(noise.cnoise(sample)),
                NoiseType.SimplexRidgedInversed => -1f + math.abs(noise.snoise(sample)),
                _ => throw new NotImplementedException(),
            };

            noiseHeight += perlinValue * amplitute;

            frequency *= complexOctaves ? octaves[i].noiseLacunarity : octaves[0].noiseLacunarity;
            amplitute *= complexOctaves ? octaves[i].noisePersistence : octaves[0].noisePersistence;
        }

        return noiseHeight;
    }
}