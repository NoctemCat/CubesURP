using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "New Biome Database", menuName = "Cubes/Biome Database")]
public class BiomeDatabase : ScriptableObject, ISerializationCallbackReceiver
{
    [field: SerializeField] public BiomeAttributes[] BiomeObjects { get; private set; }
    [field: NonSerialized] public NativeArray<BiomeStruct> Biomes { get; private set; }
    [field: NonSerialized] public int BiomesMinSize { get; private set; }
    [field: NonSerialized] public int BiomesMaxSize { get; private set; }
    [field: NonSerialized] public int BiomesMinHeight { get; private set; }
    [field: NonSerialized] public int BiomesMaxHeight { get; private set; }
    [field: NonSerialized] public float BiomesGridCellSize { get; private set; }

    [NonSerialized] private bool created = false;

    [ContextMenu("Update IDs")]
    public void UpdateID()
    {
        for (int i = 0; i < BiomeObjects.Length; i++)
        {
            if (BiomeObjects[i] == null) continue;
            BiomeObjects[i].id = i;
        }
    }

    public void Init()
    {
        UpdateID();
        CalculateMinMaxes();
        Convert();
    }

    private void OnDestroy()
    {
        Dispose();
    }

    private void CalculateMinMaxes()
    {
        BiomesMinSize = int.MaxValue;
        BiomesMaxSize = int.MinValue;
        BiomesMinHeight = int.MaxValue;
        BiomesMaxHeight = int.MinValue;

        for (int i = 0; i < BiomeObjects.Length; i++)
        {
            if (BiomeObjects[i] == null) continue;

            BiomeObjects[i].id = i;

            BiomesMinSize = Mathf.Min(BiomeObjects[i].minSize, BiomesMinSize);
            BiomesMaxSize = Mathf.Max(BiomeObjects[i].maxSize, BiomesMaxSize);
            BiomesMinHeight = Mathf.Min(BiomeObjects[i].minHeight, BiomesMinHeight);
            BiomesMaxHeight = Mathf.Max(BiomeObjects[i].maxHeight, BiomesMaxHeight);
        }

        BiomesGridCellSize = BiomesMinSize / Mathf.Sqrt(2);
    }

    private void Convert()
    {
        if (created)
        {
            Dispose();
        }

        NativeArray<BiomeStruct> temp = new(BiomeObjects.Length, Allocator.Persistent);
        for (int i = 0; i < BiomeObjects.Length; i++)
        {
            temp[i] = new(BiomeObjects[i]);
        }
        Biomes = temp;
        created = true;
    }

    public void Dispose()
    {
        for (int i = 0; i < Biomes.Length; i++)
        {
            Biomes[i].Dispose();
        }
        Biomes.Dispose();
        created = false;
    }

    public void OnBeforeSerialize()
    {
        //UpdateID();
    }

    public void OnAfterDeserialize()
    {
        //if (created)
        //{
        //    Dispose();
        //}
    }
}