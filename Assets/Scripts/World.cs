
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class World : MonoBehaviour
{
    public static World Instance;

    public VoxelData VoxelData;
    public NativeArray<BlockStruct> Blocks;
    public NativeArray<int3> XYZMap;
    public BiomeStruct Biome;

    [SerializeField]
    private BlockDictionary BlocksScObj;
    [SerializeField]
    private BiomeAttributes BiomeScObj;
    public string RNGSeed;
    public Material SolidMaterial;
    public Material TransparentMaterial;

    public GameObject PlayerObj;

    private float3 _RandomXYZ;
    public float3 RandomXYZ => _RandomXYZ;

    public Dictionary<Vector3Int, Chunk> Chunks;
    //public NativeParallelHashMap<int3, NativeArray<Block>> ChunkMap;
    public NativeArray<Block> DummyMap;

    public NativeList<StructureMarker> Structures;

    List<Vector3Int> ViewCoords;
    List<Vector3Int> VoxelGenCoords;

    List<Chunk> ActiveChunks;

    Vector3Int PlayerChunk;
    Vector3Int LastPlayerChunk;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(Instance);

        BlocksScObj.Init();

        Unity.Mathematics.Random rng = new(math.hash(new int2(RNGSeed.GetHashCode(), 0)));
        _RandomXYZ = rng.NextFloat3() * 10000;

        VoxelData = new(_RandomXYZ);
        Blocks = WorldHelper.InitBlocksMapping(BlocksScObj);
        XYZMap = WorldHelper.InitXYZMap(VoxelData);
        Biome = new(BiomeScObj);

        ViewCoords = WorldHelper.InitViewCoords(VoxelData.ViewDistanceInChunks);
        VoxelGenCoords = WorldHelper.InitViewCoords(VoxelData.ViewDistanceInChunks + 1);

        Chunks = new(ViewCoords.Count);
        //ChunkMap = new(ViewCoords.Count, Allocator.Persistent);
        DummyMap = new(VoxelData.ChunkSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        Structures = new NativeList<StructureMarker>(10000, Allocator.Persistent);


        ActiveChunks = new(10);

        PlayerObj.transform.position = new(VoxelData.ChunkWidth / 2, VoxelData.ChunkHeight - 50, VoxelData.ChunkLength / 2);

        LastPlayerChunk = new(-1000, -1000, -1000);
        PlayerChunk = GetChunkCoordFromVector3(PlayerObj.transform.position);

        //GenerateWorld();
    }

    private void OnDestroy()
    {
        VoxelData.Dispose();
        Blocks.Dispose();
        XYZMap.Dispose();
        Biome.Dispose();

        //ChunkMap.Dispose();
        DummyMap.Dispose();
        Structures.Dispose();
    }

    //public void AddChunkVoxelMap(int3 chunk, NativeArray<Block> voxelMap)
    //{
    //    ChunkMap[chunk] = voxelMap;
    //}

    public Chunk GetChunkFromVector3(Vector3Int pos)
    {
        return Chunks[pos];
    }

    private void Update()
    {
        PlayerChunk = GetChunkCoordFromVector3(PlayerObj.transform.position);
        if (PlayerChunk != LastPlayerChunk)
        {
            CheckDistance();
        }

        if (Structures.Length > 0)
        {
            SortStructures().Forget();
        }

        foreach (var chunk in ActiveChunks)
        {
            chunk.Update();
            chunk.Draw();
        }
    }

    void CheckDistance()
    {
        LastPlayerChunk = PlayerChunk;
        foreach (var chunk in ActiveChunks)
        {
            Vector3Int chunkPos = new(chunk.ChunkPos.x, chunk.ChunkPos.y, chunk.ChunkPos.z);
            if ((chunkPos - PlayerChunk).sqrMagnitude > VoxelData.SqrViewDistanceInChunks)
            {
                chunk.RequestingStop = true;
            }
        }
        ActiveChunks.Clear();

        foreach (var coord in VoxelGenCoords)
        {
            Vector3Int checkCoord = PlayerChunk + coord;
            if (!Chunks.ContainsKey(checkCoord))
            {

                var chunk = new Chunk(checkCoord);
                Chunks[checkCoord] = chunk;
            }
        }

        foreach (var coord in ViewCoords)
        {
            Vector3Int checkCoord = PlayerChunk + coord;
            ActiveChunks.Add(Chunks[checkCoord]);
        }

        JobHandle.ScheduleBatchedJobs();
    }

    async UniTaskVoid SortStructures()
    {
        await UniTask.SwitchToThreadPool();
        List<VoxelMod> mods = new(1000);
        for (int i = 0; i < Structures.Length; i++)
        {
            Structure.MakeTree(VoxelData, ref mods, Structures[i].Position, Biome.MinTreeHeight, Biome.MaxTreeHeight);
        }
        Structures.Clear();

        Dictionary<Vector3Int, List<VoxelMod>> dict = new(100);
        for (int i = 0; i < mods.Count; i++)
        {
            Vector3Int checkCoord = GetChunkCoordFromVector3(mods[i].Position);
            if (!dict.ContainsKey(checkCoord))
            {
                dict[checkCoord] = new(10);
            }
            int3 newPos = GetPosInChunkFromVector3(checkCoord, mods[i].Position);

            //Debug.Log($"Chunk {checkCoord.x}, {checkCoord.y}, {checkCoord.z}: World {mods[i].Position.x}, {mods[i].Position.y}, {mods[i].Position.z} | {newPos.x}, {newPos.y}, {newPos.z}");
            dict[checkCoord].Add(new(newPos, mods[i].Block));
        }
        await UniTask.SwitchToMainThread();

        foreach (Vector3Int checkCoord in dict.Keys)
        {
            if (Chunks.TryGetValue(checkCoord, out Chunk chunk))
            {
                var copy = dict[checkCoord].ToNativeArray(Allocator.Persistent);
                chunk.Modifications.AddRange(copy);
                copy.Dispose();
            }
            else
            {
                var copy = dict[checkCoord].ToNativeArray(Allocator.Persistent);
                chunk = new Chunk(checkCoord);
                Chunks[checkCoord] = chunk;
                chunk.Modifications.AddRange(copy);
                copy.Dispose();
            }
        }
        await UniTask.NextFrame();
    }

    private Vector3Int GetChunkCoordFromVector3(int3 pos)
    {
        pos.x -= (pos.x < 0 && pos.x % VoxelData.ChunkWidth != 0) ? VoxelData.ChunkWidth : 0;
        pos.y -= (pos.y < 0 && pos.y % VoxelData.ChunkHeight != 0) ? VoxelData.ChunkHeight : 0;
        pos.z -= (pos.z < 0 && pos.z % VoxelData.ChunkLength != 0) ? VoxelData.ChunkLength : 0;
        int x = pos.x / VoxelData.ChunkWidth;
        int y = pos.y / VoxelData.ChunkHeight;
        int z = pos.z / VoxelData.ChunkLength;
        return new(x, y, z);
    }
    Vector3Int GetChunkCoordFromVector3(Vector3 pos)
    {
        pos.x -= (pos.x < 0 && pos.x % VoxelData.ChunkWidth != 0) ? VoxelData.ChunkWidth : 0;
        pos.y -= (pos.y < 0 && pos.y % VoxelData.ChunkHeight != 0) ? VoxelData.ChunkHeight : 0;
        pos.z -= (pos.z < 0 && pos.z % VoxelData.ChunkLength != 0) ? VoxelData.ChunkLength : 0;
        int x = (int)(pos.x / VoxelData.ChunkWidth);
        int y = (int)(pos.y / VoxelData.ChunkHeight);
        int z = (int)(pos.z / VoxelData.ChunkLength);
        return new(x, y, z);
    }

    private int3 GetPosInChunkFromVector3(Vector3Int chunkPos, int3 worldPos)
    {
        int x = worldPos.x - (chunkPos.x * VoxelData.ChunkWidth);
        int y = worldPos.y - (chunkPos.y * VoxelData.ChunkHeight);
        int z = worldPos.z - (chunkPos.z * VoxelData.ChunkLength);
        return new(x, y, z);
    }
    private int3 GetPosInChunkFromVector3(Vector3Int chunkPos, Vector3 worldPos)
    {
        int x = Mathf.FloorToInt(worldPos.x - (chunkPos.x * VoxelData.ChunkWidth));
        int y = Mathf.FloorToInt(worldPos.y - (chunkPos.y * VoxelData.ChunkHeight));
        int z = Mathf.FloorToInt(worldPos.z - (chunkPos.z * VoxelData.ChunkLength));
        return new(x, y, z);
    }

    public bool CheckForVoxel(Vector3 worldPos)
    {
        Vector3Int thisChunk = GetChunkCoordFromVector3(worldPos);

        if (Chunks.TryGetValue(thisChunk, out Chunk chunk))
        {
            int3 block = GetPosInChunkFromVector3(thisChunk, worldPos);
            return Blocks[(int)chunk.VoxelMap[CalcIndex(block)]].isSolid;
        }
        return false;
    }

    int CalcIndex(int3 xyz) => xyz.x * VoxelData.ChunkHeight * VoxelData.ChunkLength + xyz.y * VoxelData.ChunkLength + xyz.z;
}
