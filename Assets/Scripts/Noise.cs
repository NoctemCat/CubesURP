using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;


public class Noise : MonoBehaviour
{
    //[SerializeField] private int _mapWidth;
    //[SerializeField] private int _mapHeight;

    //[SerializeField] private float _noiseScale;
    //[SerializeField] private int _chunksShownSide = 4;
    //[SerializeField] private bool _complexOctaves = true;
    //[SerializeField] private List<OctaveValues> _octaves = new();
    //[SerializeField] private uint _seed;
    //[SerializeField] private float2 _offset;
    //[SerializeField] private Renderer _textureRenderer;

    //public bool autoUpdate;

    //void Start()
    //{

    //}

    //void Update()
    //{

    //}

    //private void OnValidate()
    //{
    //    if (_mapWidth < 1)
    //        _mapWidth = 1;
    //    if (_mapHeight < 1)
    //        _mapHeight = 1;
    //    if (_chunksShownSide < 1)
    //        _chunksShownSide = 1;
    //    if (_seed == 0)
    //        _seed = 1;

    //    if (autoUpdate)
    //    {
    //        GenerateMap();
    //    }
    //}

    //public void GenerateMap()
    //{
    //    float[,] noiseMap = GenerateNoiseMap(_mapWidth, _mapHeight, _seed, _noiseScale / _chunksShownSide, _offset);
    //    DrawNoiseMap(noiseMap);
    //}

    //public void DrawNoiseMap(float[,] noiseMap)
    //{
    //    int width = noiseMap.GetLength(0);
    //    int height = noiseMap.GetLength(1);

    //    Texture2D texture = new(width, height)
    //    {
    //        filterMode = FilterMode.Point,
    //        wrapMode = TextureWrapMode.Clamp
    //    };

    //    Color[] colourMap = new Color[width * height];
    //    for (int y = 0; y < height; y++)
    //    {
    //        for (int x = 0; x < width; x++)
    //        {
    //            colourMap[y * width + x] = Color.Lerp(Color.black, Color.white, noiseMap[x, y]);
    //        }
    //    }

    //    texture.SetPixels(colourMap);
    //    texture.Apply();

    //    _textureRenderer.sharedMaterial.mainTexture = texture;
    //    //_textureRenderer.transform.localScale = new(width, 1, height);
    //}

    //private float[,] GenerateNoiseMap(
    //    int mapWidth, int mapHeight, uint seed, float scale, float2 offset
    //)
    //{
    //    float[,] noiseMap = new float[mapWidth, mapHeight];

    //    Unity.Mathematics.Random prng = new(seed);
    //    float2[] octavesOffsets = new float2[_octaves.Count];

    //    for (int i = 0; i < _octaves.Count; i++)
    //    {
    //        octavesOffsets[i] = prng.NextFloat2(-100000, 100000) + offset;
    //    }

    //    if (scale <= 0)
    //    {
    //        scale = 0.001f;
    //    }

    //    float maxNoiseHeight = float.MinValue;
    //    float minNoiseHeight = float.MaxValue;


    //    for (int x = 0; x < mapWidth; x++)
    //    {
    //        for (int y = 0; y < mapHeight; y++)
    //        {
    //            //float noiseHeight = EH.GetHeight(new(x, y), new(mapWidth, mapHeight), scale, _octaves, octavesOffsets, _complexOctaves);

    //            if (noiseHeight < minNoiseHeight)
    //                minNoiseHeight = noiseHeight;
    //            if (noiseHeight > maxNoiseHeight)
    //                maxNoiseHeight = noiseHeight;

    //            noiseMap[x, y] = noiseHeight;
    //        }
    //    }
    //    for (int x = 0; x < mapWidth; x++)
    //    {
    //        for (int y = 0; y < mapHeight; y++)
    //        {
    //            noiseMap[x, y] = math.unlerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y]);
    //        }
    //    }
    //    return noiseMap;
    //}
}
