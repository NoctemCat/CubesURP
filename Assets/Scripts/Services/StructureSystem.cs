

using System;
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
    private EventSystem _eventSystem;

    private Stack<List<VoxelMod>> _structuresPool;
    private List<VoxelMod> _structures;
    private Dictionary<Vector3Int, List<VoxelMod>> _sortedStructures;
    private float _timer;

    private void Awake()
    {
        _eventSystem = ServiceLocator.Get<EventSystem>();

        _structuresPool = new(100);
        _structures = new(10000);
        _sortedStructures = new(100);
        _timer = 0f;
    }

    private void OnEnable()
    {
        ServiceLocator.Register(this);
    }

    private void OnDisable()
    {
        ServiceLocator.Unregister(this);
    }

    private List<VoxelMod> GetList()
    {
        if (_structuresPool.Count > 0)
            return _structuresPool.Pop();
        return new(20);
    }
    private void ReclaimList(List<VoxelMod> list)
    {
        list.Clear();
        _structuresPool.Push(list);
    }

    public void AddStructuresToSort(NativeList<VoxelMod> structures)
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
                _sortedStructures.Add(cPos, GetList());

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

            _eventSystem.TriggerEvent(
                EventType.Chunk_AddSortedStructures,
                new AddSortedStructuresArgs() { chunkPos = kvp.Key, structures = kvp.Value }
            );
        }
    }
}

public class AddSortedStructuresArgs : EventArgs
{
    public Vector3Int chunkPos;
    public List<VoxelMod> structures;
    public AddSortedStructuresArgs() => eventType = EventType.Chunk_AddSortedStructures;
}
