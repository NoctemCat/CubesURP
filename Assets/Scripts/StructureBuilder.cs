

using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

using static CubesUtils;

public class StructureBuilder
{
    private readonly World World;
    private NativeList<VoxelMod> _structures;
    private readonly Dictionary<Vector3Int, List<VoxelMod>> _sortedStructures;

    public StructureBuilder(World world)
    {
        World = world;
        //_chunks = chunks;
        _structures = new NativeList<VoxelMod>(10000, Allocator.Persistent);
        _sortedStructures = new(100);

        CheckStructures().Forget();
    }

    ~StructureBuilder()
    {
        _structures.Dispose();
    }

    public void AddStructures(NativeList<VoxelMod> structures)
    {
        _structures.AddRange(structures.AsArray());
    }

    private async UniTaskVoid CheckStructures()
    {
        while (true)
        {
            await UniTask.WaitForSeconds(1);
            SortStructures();
        }
    }

    private void SortStructures()
    {
        if (_structures.Length <= 0) return;
        _sortedStructures.Clear();
        _sortedStructures.EnsureCapacity(_structures.Length);

        for (int i = 0; i < _structures.Length; i++)
        {
            VoxelMod mod = _structures[i];
            Vector3Int cPos = I3ToVI3(mod.ChunkPos);
            if (!_sortedStructures.ContainsKey(cPos))
                _sortedStructures.Add(cPos, new(20));

            _sortedStructures[cPos].Add(mod);
        }

        _structures.Clear();
        AddSortedStructures();
    }

    public void AddSortedStructures()
    {
        foreach (Vector3Int key in _sortedStructures.Keys)
        {
            if (_sortedStructures[key].Count < 0) continue;

            Chunk chunk;
            if (World.Chunks.ContainsKey(key))
            {
                chunk = World.Chunks[key];
            }
            else
            {
                chunk = new(key);
                World.Chunks[key] = chunk;
            }
            chunk.AddRangeModification(_sortedStructures[key]).Forget();
        }
    }
}