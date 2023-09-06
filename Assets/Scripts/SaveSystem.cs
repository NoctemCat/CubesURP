using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using MemoryPack;
using Unity.Mathematics;
using UnityEngine;

public class SaveSystem : MonoBehaviour
{
    private World World;
    private ChunkDataPool _pool;

    public List<Chunk> ChunksToSave { get; private set; }
    public string SaveChunkPath { get; private set; }

    private float _time;
    private bool _forceSave;

    private void Awake()
    {
        World = World.Instance;
        _pool = new(World);

        SaveChunkPath = Path.Combine(World.WorldPath, "chunks");
        ChunksToSave = new(20);

        _time = 0f;
        _forceSave = false;
    }

    private void Start()
    {
        WatchUpdate(this.GetCancellationTokenOnDestroy()).Forget();
    }

    public void AddChunkToSave(Chunk chunk)
    {
        if (!ChunksToSave.Contains(chunk))
            ChunksToSave.Add(chunk);
    }

    public void Update()
    {
        _time += Time.unscaledDeltaTime;
    }

    public void ForceSave()
    {
        _forceSave = true;
    }

    private async UniTask WatchUpdate(CancellationToken token)
    {
        List<UniTask> tasks = new(10);
        List<Chunk> copy = new(10);
        await UniTask.SwitchToThreadPool();
        while (true)
        {
            bool isCalncelled = await UniTask.WaitUntil(() => { return _time > 4f || _forceSave; }, PlayerLoopTiming.EarlyUpdate, token).SuppressCancellationThrow();
            if (isCalncelled) return;

            _forceSave = false;

            if (ChunksToSave.Count > 0)
            {
                for (int i = 0; i < ChunksToSave.Count; i++)
                    copy.Add(ChunksToSave[i]);
                ChunksToSave.Clear();

                for (int i = 0; i < copy.Count; i++)
                    tasks.Add(SaveChunkAsync(copy[i]));

                await UniTask.WhenAll(tasks);

                tasks.Clear();
                copy.Clear();
            }

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
        for (int i = 0; i < chunk.NeighbourModifications.Length; i++)
        {
            chunkData.NeighbourModifications.Add(chunk.NeighbourModifications[i]);
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
        ChunkData chunkData = _pool.Get();
        await UniTask.SwitchToThreadPool();

        using Stream stream = new FileStream(PathHelper.GetChunkPath(SaveChunkPath, chunkName), FileMode.Open, FileAccess.Read);
        using MemoryStream ms = new();

        await stream.CopyToAsync(ms);

        MemoryPackSerializer.Deserialize(ms.ToArray(), ref chunkData);

        await UniTask.SwitchToMainThread();
        return chunkData;
    }

    public void ReclaimData(ChunkData data)
    {
        _pool.Reclaim(data);
    }


    public void OnDestroyForceSave()
    {
        for (int i = 0; i < ChunksToSave.Count; i++)
        {
            SaveChunk(ChunksToSave[i]);
        }
    }

    public void SaveChunk(Chunk chunk)
    {
        if (chunk.IsDisposed) return;

        Directory.CreateDirectory(SaveChunkPath);

        ChunkData chunkData = new()
        {
            x = chunk.ChunkPos.x,
            y = chunk.ChunkPos.y,
            z = chunk.ChunkPos.z,
            VoxelMap = new Block[World.VoxelData.ChunkSize],
            NeighbourModifications = new(chunk.NeighbourModifications.Length)
        };

        chunk.VoxelMap.CopyTo(chunkData.VoxelMap);
        for (int i = 0; i < chunk.NeighbourModifications.Length; i++)
        {
            chunkData.NeighbourModifications.Add(chunk.NeighbourModifications[i]);
        }

        using Stream stream = new FileStream(PathHelper.GetChunkPath(SaveChunkPath, chunk.ChunkName), FileMode.Create, FileAccess.Write);
        _ = MemoryPackSerializer.SerializeAsync(stream, chunkData);
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

public class ChunkDataPool
{
    private readonly World World;
    private readonly List<ChunkData> _datas;

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

public static class ListExtras
{
    //    list: List<T> to resize
    //    size: desired new size
    // element: default value to insert

    public static void Resize<T>(this List<T> list, int size, T element = default)
    {
        int count = list.Count;

        if (size < count)
        {
            list.RemoveRange(size, count - size);
        }
        else if (size > count)
        {
            if (size > list.Capacity)
                list.Capacity = size;

            list.AddRange(Enumerable.Repeat(element, size - count));
        }
    }
}