using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using MemoryPack;
using Unity.Mathematics;
using UnityEngine;

using static CubesUtils;
public class SaveSystem : MonoBehaviour
{
    //private World World;
    [field: SerializeField] public ActiveWorldData WorldData { get; private set; }
    //private EventSystem _eventSystem;
    private HashSet<Vector3Int> _savedChunks = null;
    public HashSet<Vector3Int> SavedChunks => _savedChunks;
    private ChunkDataPool _pool;

    public List<Chunk> ChunksToSave { get; private set; }
    public string SaveChunkPath { get; private set; }
    public string SavedChunksPath { get; private set; }

    private float _time;
    private bool _forceSave;

    private void Awake()
    {
        ServiceLocator.Register(this);
        _pool = new();
        SaveChunkPath = Path.Combine(PathHelper.WorldsPath, WorldData.worldName, "chunks");
        SavedChunksPath = Path.Combine(PathHelper.WorldsPath, WorldData.worldName, "chunks.saved");
        ChunksToSave = new(20);

        if (File.Exists(SavedChunksPath))
        {
            LoadSavedChunks();
            //LoadedChunks = new(_savedChunks);
            _savedChunks ??= new();
        }
        else
        {
            _savedChunks = new();
            //LoadedChunks = new();
        }

        _time = 0f;
        _forceSave = false;
    }

    private void LoadSavedChunks()
    {
        using Stream stream = new FileStream(SavedChunksPath, FileMode.Open, FileAccess.Read);
        using MemoryStream ms = new();

        stream.CopyTo(ms);
        MemoryPackSerializer.Deserialize(ms.ToArray(), ref _savedChunks);
    }

    private void OnDestroy()
    {
        WriteToFile(SavedChunksPath, _savedChunks).Forget();
        ServiceLocator.Unregister(this);
        for (int i = 0; i < ChunksToSave.Count; i++)
        {
            SaveChunk(ChunksToSave[i]);
        }
    }

    private void Start()
    {
        WatchUpdate(this.GetCancellationTokenOnDestroy()).Forget();
    }

    //private void AddChunkToSaveHandler(EventArgs args)
    //{
    //    if (args.eventType != EventType.AddChunkToSave)
    //        Debug.Log($"Save System AddChunkToSave wrong EventArgs {args.eventType}");
    //    AddChunkToSave((args as AddChunkToSaveArgs).chunk);
    //}

    public void AddChunkToSave(Chunk chunk)
    {
        _savedChunks.Add(I3ToVI3(chunk.ChunkPos));
        if (!ChunksToSave.Contains(chunk))
        {
            chunk.markedForSave = true;
            ChunksToSave.Add(chunk);
        }
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
        while (true)
        {
            bool isCalncelled = await UniTask.WaitUntil(() => { return _time > 4f || _forceSave; }, PlayerLoopTiming.EarlyUpdate, token).SuppressCancellationThrow();
            if (isCalncelled) return;

            _forceSave = false;

            if (ChunksToSave.Count > 0)
            {
                WriteToFile(SavedChunksPath, _savedChunks).Forget();

                copy.AddRange(ChunksToSave);
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
        await UniTask.SwitchToThreadPool();

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
        chunk.FinishedSaving();

        await WriteToFile(PathHelper.GetChunkPath(SaveChunkPath, chunk.ChunkName), chunkData);

        _pool.Reclaim(chunkData);
    }

    HashSet<Vector3Int> _loadedChunks = new();
    /// <summary>
    /// File must exist
    /// </summary>
    /// <param name="chunkName"></param>
    /// <returns>UniTask with chunk data</returns>
    public async UniTask<ChunkData> LoadChunkAsync(Vector3Int chunkPos, string chunkName)
    {
        if (_loadedChunks.Contains(chunkPos)) return null;
        _loadedChunks.Add(chunkPos);

        ChunkData chunkData = _pool.Get();
        await UniTask.SwitchToThreadPool();

        using Stream stream = new FileStream(PathHelper.GetChunkPath(SaveChunkPath, chunkName), FileMode.Open, FileAccess.Read);
        using MemoryStream ms = new();

        await stream.CopyToAsync(ms);

        MemoryPackSerializer.Deserialize(ms.ToArray(), ref chunkData);

        await UniTask.SwitchToMainThread();
        return chunkData;
    }

    public void ReclaimData(Vector3Int chunkPos, ChunkData data)
    {
        _loadedChunks.Remove(chunkPos);
        _pool.Reclaim(data);
    }

    public void SaveChunk(Chunk chunk)
    {
        string chunkPath = PathHelper.GetChunkPath(SaveChunkPath, chunk.ChunkName);


        ChunkData chunkData = _pool.Get();

        chunkData.x = chunk.ChunkPos.x;
        chunkData.y = chunk.ChunkPos.y;
        chunkData.z = chunk.ChunkPos.z;
        chunk.VoxelMap.CopyTo(chunkData.VoxelMap);
        for (int i = 0; i < chunk.NeighbourModifications.Length; i++)
        {
            chunkData.NeighbourModifications.Add(chunk.NeighbourModifications[i]);
        }
        chunk.FinishedSaving();

        WriteToFile(chunkPath, chunkData).Forget();
    }

    private async UniTask WriteToFile<T>(string filePath, T data)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
        Stream stream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        await MemoryPackSerializer.SerializeAsync(stream, data);

        stream.Flush();
        stream.Close();
        stream.Dispose();
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
    private readonly ConcurrentQueue<ChunkData> _datas;

    public ChunkDataPool()
    {
        _datas = new();
    }

    public void AddToPool(int add = 5)
    {
        for (int i = 0; i < add; i++)
            _datas.Enqueue(Construct());
    }

    private ChunkData Construct()
    {
        return new()
        {
            x = 0,
            y = 0,
            z = 0,
            VoxelMap = new Block[VoxelDataStatic.ChunkSize],
            NeighbourModifications = new()
        };
    }

    public ChunkData Get()
    {
        if (_datas.IsEmpty)
            AddToPool();

        if (_datas.TryDequeue(out ChunkData data)) { return data; }
        return Construct();
    }

    public void Reclaim(ChunkData data)
    {
        data.NeighbourModifications.Clear();
        _datas.Enqueue(data);
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