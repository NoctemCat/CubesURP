using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using MemoryPack;
using Unity.Mathematics;
using UnityEngine;

public class SaveSystem
{
    private readonly World World;
    public List<Chunk> ChunksToSave { get; private set; }
    public string SaveChunkPath { get; private set; }

    private float _time;
    private bool _forceSave;

    public SaveSystem(World world)
    {
        World = world;
        SaveChunkPath = Path.Combine(World.AppPath, "saves", "chunks");
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
        while (true)
        {
            await UniTask.WaitUntil(() => { return _time > 4f || _forceSave; });
            _forceSave = false;

            List<Chunk> copy = new(ChunksToSave);
            ChunksToSave.Clear();

            List<UniTask> tasks = new(copy.Count);

            foreach (var chunk in copy)
            {
                tasks.Add(SaveChunk(chunk));
            }

            await UniTask.WhenAll(tasks);

            _time = 0f;
        }
    }

    public async UniTask SaveChunk(Chunk chunk)
    {
        Directory.CreateDirectory(SaveChunkPath);
        using Stream stream = new FileStream(Path.Combine(SaveChunkPath, chunk.ChunkName), FileMode.Create, FileAccess.Write);

        ChunkData chunkData = new()
        {
            x = chunk.ChunkPos.x,
            y = chunk.ChunkPos.y,
            z = chunk.ChunkPos.z,
            VoxelMap = new Block[chunk.VoxelMap.Length],
            NeighbourModifications = new VoxelMod[chunk.NeighbourModifications.Length]
        };
        await chunk.VoxelMapAccess;
        chunk.VoxelMap.CopyTo(chunkData.VoxelMap);
        chunk.NeighbourModifications.AsArray().CopyTo(chunkData.NeighbourModifications);

        await MemoryPackSerializer.SerializeAsync(stream, chunkData);
    }

    /// <summary>
    /// File must exist
    /// </summary>
    /// <param name="chunkName"></param>
    /// <returns>UniTask with chunk data</returns>
    public async UniTask<ChunkData> LoadChunk(string chunkName)
    {
        using Stream stream = new FileStream(Path.Combine(SaveChunkPath, chunkName), FileMode.Open, FileAccess.Read);
        var chunkData = await MemoryPackSerializer.DeserializeAsync<ChunkData>(stream);
        return chunkData;
    }
}

[MemoryPackable]
public partial class ChunkData
{
    public int x;
    public int y;
    public int z;
    public Block[] VoxelMap;
    public VoxelMod[] NeighbourModifications;

    public Vector3Int ChunkPos => new(x, y, z);
}
