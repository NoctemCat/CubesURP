using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using MemoryPack;
using Unity.Mathematics;
using UnityEngine;

public class ChunkDataPool
{
    private readonly World World;
    private List<ChunkData> _datas;

    public ChunkDataPool(World world)
    {
        World = world;
        _datas = new List<ChunkData>(5);
    }

    public void AddToPool(int add = 5)
    {
        _datas.Capacity += add;
        for (int i = 0; i < add; i++)
        {
            ChunkData data = new()
            {
                x = 0,
                y = 0,
                z = 0,
                VoxelMap = new Block[World.VoxelData.ChunkSize],
                NeighbourModifications = new()
            };
            _datas.Add(data);
        }
    }

    public ChunkData Get()
    {
        if (_datas.Count <= 1)
            AddToPool();

        ChunkData data = _datas[^1];
        _datas.RemoveAt(_datas.Count - 1);
        return data;
    }

    public void Reclaim(ChunkData data)
    {
        _datas.Add(data);
    }
}

public class SaveSystem
{
    private readonly World World;
    private readonly ChunkDataPool _pool;

    public List<Chunk> ChunksToSave { get; private set; }
    public string SaveChunkPath { get; private set; }

    private float _time;
    private bool _forceSave;

    public SaveSystem(World world)
    {
        World = world;
        _pool = new(World);

        SaveChunkPath = Path.Combine(World.WorldPath, "chunks");
        ChunksToSave = new(20);

        _time = 0f;
        _forceSave = false;

        WatchUpdate().Forget();
    }

    public void AddChunkToSave(Chunk chunk)
    {
        if (!ChunksToSave.Contains(chunk))
            ChunksToSave.Add(chunk);
    }

    public void Update(float deltaTime)
    {
        _time += deltaTime;
    }

    public void ForceSave()
    {
        _forceSave = true;
    }




    private async UniTask WatchUpdate()
    {
        List<UniTask> tasks = new(10);
        List<Chunk> copy = new(10);
        await UniTask.SwitchToThreadPool();
        while (true)
        {
            await UniTask.WaitUntil(() => { return _time > 4f || _forceSave; });
            _forceSave = false;

            for (int i = 0; i < ChunksToSave.Count; i++)
                copy.Add(ChunksToSave[i]);
            ChunksToSave.Clear();

            tasks.Clear();
            foreach (var chunk in copy)
            {
                tasks.Add(SaveChunkAsync(chunk));
            }

            await UniTask.WhenAll(tasks);

            copy.Clear();
            _time = 0f;
        }
    }

    public async UniTask SaveChunkAsync(Chunk chunk)
    {

        Directory.CreateDirectory(SaveChunkPath);

        ChunkData chunkData = _pool.Get();

        chunkData.x = chunk.ChunkPos.x;
        chunkData.y = chunk.ChunkPos.y;
        chunkData.z = chunk.ChunkPos.z;
        chunkData.NeighbourModifications.Clear();
        chunkData.NeighbourModifications.Capacity = chunk.NeighbourModifications.Length;

        await chunk.VoxelMapAccess;
        chunk.VoxelMap.CopyTo(chunkData.VoxelMap);
        foreach (var mod in chunk.NeighbourModifications)
        {
            chunkData.NeighbourModifications.Add(mod);
        }

        using Stream stream = new FileStream(PathHelper.GetChunkPath(SaveChunkPath, chunk.ChunkName), FileMode.Create, FileAccess.Write);
        await MemoryPackSerializer.SerializeAsync(stream, chunkData);

        _pool.Reclaim(chunkData);
    }

    /// <summary>
    /// File must exist
    /// </summary>
    /// <param name="chunkName"></param>
    /// <returns>UniTask with chunk data</returns>
    public async UniTask<ChunkData> LoadChunkAsync(string chunkName)
    {
        await UniTask.SwitchToThreadPool();

        using Stream stream = new FileStream(PathHelper.GetChunkPath(SaveChunkPath, chunkName), FileMode.Open, FileAccess.Read);
        using MemoryStream ms = new();

        await stream.CopyToAsync(ms);
        ChunkData chunkData = _pool.Get();

        MemoryPackSerializer.Deserialize(ms.ToArray(), ref chunkData);

        await UniTask.SwitchToMainThread();
        return chunkData;
    }

    public void ReclaimData(ChunkData data)
    {
        _pool.Reclaim(data);
    }


    public void DestroyForceSave()
    {
        foreach (var chunk in ChunksToSave)
        {
            SaveChunk(chunk);
        }
    }

    public void SaveChunk(Chunk chunk)
    {
        Directory.CreateDirectory(SaveChunkPath);

        ChunkData chunkData = _pool.Get();

        chunkData.x = chunk.ChunkPos.x;
        chunkData.y = chunk.ChunkPos.y;
        chunkData.z = chunk.ChunkPos.z;
        chunkData.NeighbourModifications.Clear();
        chunkData.NeighbourModifications.Capacity = chunk.NeighbourModifications.Length;

        chunk.VoxelMap.CopyTo(chunkData.VoxelMap);
        foreach (var mod in chunk.NeighbourModifications)
        {
            chunkData.NeighbourModifications.Add(mod);
        }

        using Stream stream = new FileStream(PathHelper.GetChunkPath(SaveChunkPath, chunk.ChunkName), FileMode.Create, FileAccess.Write);
        _ = MemoryPackSerializer.SerializeAsync(stream, chunkData);

        _pool.Reclaim(chunkData);
    }
}

[MemoryPackable]
public partial class ChunkData
{
    public int x;
    public int y;
    public int z;
    public Block[] VoxelMap;
    public List<VoxelMod> NeighbourModifications;

    [MemoryPackIgnore]
    public Vector3Int ChunkPos => new(x, y, z);
}


public static class ListExtras
{
    //    list: List<T> to resize
    //    size: desired new size
    // element: default value to insert

    public static void Resize<T>(this List<T> list, int size, T element = default(T))
    {
        int count = list.Count;

        if (size < count)
        {
            list.RemoveRange(size, count - size);
        }
        else if (size > count)
        {
            if (size > list.Capacity)   // Optimization
                list.Capacity = size;

            list.AddRange(Enumerable.Repeat(element, size - count));
        }
    }
}