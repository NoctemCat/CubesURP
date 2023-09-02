

using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

using static CubesUtils;

public class StructureSystem : MonoBehaviour
{
    private World World;
    private NativeList<VoxelMod> _structures;
    private Dictionary<Vector3Int, List<VoxelMod>> _sortedStructures;
    private float _timer;

    private void Awake()
    {
        World = World.Instance;
        //_chunks = chunks;
        _structures = new NativeList<VoxelMod>(10000, Allocator.Persistent);
        _sortedStructures = new(100);
        _timer = 0f;
    }

    private void OnDestroy()
    {
        _structures.Dispose();
    }

    public void AddStructures(NativeList<VoxelMod> structures)
    {
        _structures.AddRange(structures.AsArray());
    }

    private void Update()
    {
        _timer += Time.deltaTime;

        if (_timer > 1f)
        {
            SortStructures();
            _timer = 0f;
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