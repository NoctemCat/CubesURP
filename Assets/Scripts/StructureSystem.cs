

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
    private EventSystem _eventSystem;

    private Queue<List<VoxelMod>> _structuresPool;
    private List<VoxelMod> _structures;
    private Dictionary<Vector3Int, List<VoxelMod>> _sortedStructures;
    private float _timer;

    private void Awake()
    {
        World = World.Instance;

        _structuresPool = new(100);
        _structures = new(10000);
        _sortedStructures = new(100);
        _timer = 0f;
    }

    private List<VoxelMod> ClaimList()
    {
        if (_structuresPool.Count > 0)
            return _structuresPool.Dequeue();
        return new(20);
    }
    private void ReclaimList(List<VoxelMod> list)
    {
        list.Clear();
        _structuresPool.Enqueue(list);
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
        foreach (var kvp in _sortedStructures)
            ReclaimList(kvp.Value);
        _sortedStructures.Clear();

        if (_structures.Count <= 0) return;
        _sortedStructures.EnsureCapacity(_structures.Count);

        for (int i = 0; i < _structures.Count; i++)
        {
            VoxelMod mod = _structures[i];
            Vector3Int cPos = I3ToVI3(mod.chunkPos);
            if (!_sortedStructures.ContainsKey(cPos))
                _sortedStructures.Add(cPos, ClaimList());

            _sortedStructures[cPos].Add(mod);
        }

        _structures.Clear();
        AddSortedStructures();
    }

    public void AddSortedStructures()
    {
        foreach (var kvp in _sortedStructures)
        {
            if (kvp.Value.Count < 0) continue;
            Chunk chunk;
            if (World.Chunks.ContainsKey(kvp.Key))
            {
                chunk = World.Chunks[kvp.Key];
            }
            else
            {
                chunk = new(kvp.Key);
                World.Chunks[kvp.Key] = chunk;
            }
            chunk.AddRangeModification(kvp.Value).Forget();
        }
    }
}