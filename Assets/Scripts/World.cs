
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

using static CubesUtils;

public class World : MonoBehaviour
{
    public static World Instance { get; private set; }

    public VoxelData VoxelData { get; private set; }
    public NativeArray<BlockStruct> Blocks { get; private set; }
    public NativeArray<int3> XYZMap { get; private set; }
    public BiomeStruct Biome { get; private set; }

    [field: SerializeField] public BlockDictionary BlocksScObj { get; private set; }
    [SerializeField] private BiomeAttributes BiomeScObj;
    [field: SerializeField] public string RNGSeed { get; private set; }
    [field: SerializeField] public Material SolidMaterial { get; private set; }
    [field: SerializeField] public Material TransparentMaterial { get; private set; }

    public GameObject PlayerObj;

    public float3 RandomXYZ { get; private set; }

    public Dictionary<Vector3Int, Chunk> Chunks { get; private set; }
    //public NativeParallelHashMap<int3, NativeArray<Block>> ChunkMap;
    public NativeArray<Block> DummyMap { get; private set; }

    NativeList<StructureMarker> Structures;
    JobHandle GeneratingStructures;

    List<Vector3Int> ViewCoords;

    HashSet<Chunk> ActiveChunks;

    public Vector3Int PlayerChunk;
    [SerializeField] private int GenerateAtOnce = 8;

    private bool _inUI;
    public bool InUI
    {
        get { return _inUI; }
        set
        {
            _inUI = value;
        }
    }
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(Instance);

        Unity.Mathematics.Random rng = new(math.hash(new int2(RNGSeed.GetHashCode(), 0)));
        RandomXYZ = rng.NextFloat3() * 10000;

        VoxelData = new(RandomXYZ);
        Blocks = WorldHelper.InitBlocksMapping(BlocksScObj);
        XYZMap = WorldHelper.InitXYZMap(VoxelData);
        Biome = new(BiomeScObj);

        ViewCoords = WorldHelper.InitViewCoords(VoxelData.ViewDistanceInChunks);

        Chunks = new(ViewCoords.Count);
        //ChunkMap = new(ViewCoords.Count, Allocator.Persistent);
        DummyMap = new(VoxelData.ChunkSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        Structures = new NativeList<StructureMarker>(10000, Allocator.Persistent);


        ActiveChunks = new(10);

        PlayerObj.transform.position = new(VoxelData.ChunkWidth / 2, 100, VoxelData.ChunkLength / 2);

        //LastPlayerChunk = new(-1000, -1000, -1000);
        PlayerChunk = GetChunkCoordFromVector3(PlayerObj.transform.position);

        //GenerateWorld();
        EmptyJob dummy = new() { };
        GeneratingStructures = dummy.Schedule();

        CheckDistance().Forget();
        CheckStructures().Forget();
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
    public async UniTaskVoid AddStructures(NativeList<StructureMarker> structures)
    {
        //Debug.Log("add structures");
        await GeneratingStructures;
        GeneratingStructures.Complete();

        //GeneratingStructures.Complete();
        Structures.AddRange(structures.AsArray());
    }

    public Chunk GetChunkFromVector3(Vector3Int pos)
    {
        return Chunks[pos];
    }
    public Chunk GetChunkFromVector3(Vector3 worldPos)
    {
        return Chunks[GetChunkCoordFromVector3(worldPos)];
    }

    public void PlaceBlock(Vector3 worldPos, Block block)
    {
        Vector3Int chunkPos = GetChunkCoordFromVector3(worldPos);
        int3 blockPos = GetPosInChunkFromVector3(chunkPos, worldPos);
        Chunks[chunkPos].AddModification(new(blockPos, block)).Forget();

        if (blockPos.z == 0)
            Chunks[chunkPos + I3ToVI3(VoxelData.FaceChecks[(int)VoxelFaces.Back])].MarkDirty();
        else if (blockPos.z == VoxelData.ChunkLength - 1)
            Chunks[chunkPos + I3ToVI3(VoxelData.FaceChecks[(int)VoxelFaces.Front])].MarkDirty();

        if (blockPos.y == VoxelData.ChunkHeight - 1)
            Chunks[chunkPos + I3ToVI3(VoxelData.FaceChecks[(int)VoxelFaces.Top])].MarkDirty();
        else if (blockPos.y == 0)
            Chunks[chunkPos + I3ToVI3(VoxelData.FaceChecks[(int)VoxelFaces.Bottom])].MarkDirty();

        if (blockPos.x == 0)
            Chunks[chunkPos + I3ToVI3(VoxelData.FaceChecks[(int)VoxelFaces.Left])].MarkDirty();
        else if (blockPos.x == VoxelData.ChunkWidth - 1)
            Chunks[chunkPos + I3ToVI3(VoxelData.FaceChecks[(int)VoxelFaces.Right])].MarkDirty();

        //Debug.Log($"{blockPos.x}, {blockPos.y}, {blockPos.z}");

    }


    private void Update()
    {
        PlayerChunk = GetChunkCoordFromVector3(PlayerObj.transform.position);

        foreach (var chunk in ActiveChunks)
        {
            chunk.Update();
            chunk.Draw();
        }
    }

    // Basic frustrum culling, currently slower than without it
    // Currently testing
    //private bool CheckChunk(Chunk chunk)
    //{
    //    Plane[] planes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
    //    for (int i = 0; i < chunk.WorldPoints.Length; i++)
    //    {
    //        bool insidePlane = true;

    //        for (int j = 0; j < planes.Length; j++)
    //            insidePlane = insidePlane && planes[j].GetSide(chunk.WorldPoints[i]);


    //        if (insidePlane)
    //            return true;
    //    }

    //    return false;
    //}

    async UniTaskVoid CheckStructures()
    {
        while (true)
        {
            await UniTask.WaitForSeconds(1);
            SortStructures().Forget();
        }
    }

    async UniTaskVoid CheckDistance()
    {
        Vector3Int LastPlayerChunk;

        while (true)
        {
            LastPlayerChunk = PlayerChunk;
            while (
                DeactivateFarChunks() &&
                !GenerateVoxelMaps()
            )
            {
                LastPlayerChunk = PlayerChunk;
                await UniTask.WaitForSeconds(0.25f);
            }

            Debug.Log("Finished generating available");
            await UniTask.WaitUntil(() => PlayerChunk != LastPlayerChunk);
        }
    }

    bool DeactivateFarChunks()
    {
        foreach (Chunk chunk in ActiveChunks)
        {
            Vector3Int chunkPos = new(chunk.ChunkPos.x, chunk.ChunkPos.y, chunk.ChunkPos.z);
            if ((chunkPos - PlayerChunk).sqrMagnitude > VoxelData.SqrViewDistanceInChunks)
            {
                chunk.RequestingStop = true;
            }
        }
        ActiveChunks.Clear();
        return true;
    }

    bool GenerateVoxelMaps()
    {
        bool allGenerated = true;
        int toGenerate = GenerateAtOnce;
        foreach (var coord in ViewCoords)
        {
            Vector3Int checkCoord = PlayerChunk + coord;
            if (!Chunks.ContainsKey(checkCoord))
            {
                var chunk = new Chunk(checkCoord);
                Chunks[checkCoord] = chunk;

                toGenerate--;
                if (toGenerate <= 0)
                {
                    allGenerated = false;
                    break;
                }
            }
        }
        ActivateNearChunks();
        JobHandle.ScheduleBatchedJobs();

        return allGenerated;
    }

    void ActivateNearChunks()
    {
        foreach (var coord in ViewCoords)
        {
            Vector3Int checkCoord = PlayerChunk + coord;
            if (Chunks.TryGetValue(checkCoord, out var chunk))
            {
                ActiveChunks.Add(chunk);
            }
        }

    }

    public void CheckNeighbours(Chunk chunk)
    {
        for (VoxelFaces f = VoxelFaces.Back; f < VoxelFaces.Max; f++)
        {
            if (Chunks.TryGetValue(I3ToVI3(chunk.ChunkPos + VoxelData.FaceChecks[(int)f]), out _))
            {
                VoxelFlags flag = f switch
                {
                    VoxelFaces.Back => VoxelFlags.Back,
                    VoxelFaces.Front => VoxelFlags.Front,
                    VoxelFaces.Top => VoxelFlags.Top,
                    VoxelFaces.Bottom => VoxelFlags.Bottom,
                    VoxelFaces.Left => VoxelFlags.Left,
                    VoxelFaces.Right => VoxelFlags.Right,
                    VoxelFaces.Max => VoxelFlags.None,
                    _ => VoxelFlags.None,
                };

                chunk.NeighboursGenerated |= flag;
            }
        }
    }

    public void ChunkCreated(Chunk chunk)
    {
        for (VoxelFaces f = VoxelFaces.Back; f < VoxelFaces.Max; f++)
        {
            if (Chunks.TryGetValue(I3ToVI3(chunk.ChunkPos + VoxelData.FaceChecks[(int)f]), out var otherChunk))
            {
                VoxelFlags flag = f switch
                {
                    VoxelFaces.Back => VoxelFlags.Front,
                    VoxelFaces.Front => VoxelFlags.Back,
                    VoxelFaces.Top => VoxelFlags.Bottom,
                    VoxelFaces.Bottom => VoxelFlags.Top,
                    VoxelFaces.Left => VoxelFlags.Right,
                    VoxelFaces.Right => VoxelFlags.Left,
                    VoxelFaces.Max => VoxelFlags.None,
                    _ => VoxelFlags.None,
                };

                otherChunk.NeighboursGenerated |= flag;
            }
        }
    }

    async UniTaskVoid SortStructures()
    {
        await GeneratingStructures;
        GeneratingStructures.Complete();
        if (Structures.IsEmpty) return;

        NativeParallelHashSet<int3> keys = new(100, Allocator.TempJob);
        NativeParallelMultiHashMap<int3, VoxelMod> mods = new(12000, Allocator.TempJob);
        GenerateStructuresJob structuresJob = new()
        {
            Data = VoxelData,
            Biome = Biome,
            Structures = Structures.AsArray(),

            Keys = keys.AsParallelWriter(),
            Modifications = mods.AsParallelWriter(),
        };
        GeneratingStructures = structuresJob.Schedule(Structures.Length, 8, GeneratingStructures);

        await GeneratingStructures;
        GeneratingStructures.Complete();
        Structures.Clear();

        Dictionary<Vector3Int, List<VoxelMod>> tempDict = new(mods.Count());
        foreach (int3 key in keys)
        {
            var values = mods.GetValuesForKey(key);
            using var enumerator = values.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Vector3Int vKey = I3ToVI3(key);
                if (!tempDict.ContainsKey(vKey))
                    tempDict[vKey] = new(50);

                VoxelMod mod = enumerator.Current;
                int3 newPos = GetPosInChunkFromVector3(vKey, mod.Position);
                tempDict[vKey].Add(new(newPos, mod.Block));
            }
        }
        keys.Dispose();
        mods.Dispose();

        foreach (Vector3Int key in tempDict.Keys)
        {
            if (Chunks.TryGetValue(key, out Chunk chunk))
            {
                chunk.AddRangeModification(tempDict[key]).Forget();
            }
            else
            {
                Chunks[key] = new(key);
                Chunks[key].AddRangeModification(tempDict[key]).Forget();
            }
        }
    }

    private Vector3Int GetChunkCoordFromVector3(int3 pos)
    {
        int x = Mathf.FloorToInt(pos.x / VoxelData.ChunkWidth);
        int y = Mathf.FloorToInt(pos.y / VoxelData.ChunkHeight);
        int z = Mathf.FloorToInt(pos.z / VoxelData.ChunkLength);
        return new(x, y, z);
    }

    public Vector3Int GetChunkCoordFromVector3(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x / VoxelData.ChunkWidth);
        int y = Mathf.FloorToInt(pos.y / VoxelData.ChunkHeight);
        int z = Mathf.FloorToInt(pos.z / VoxelData.ChunkLength);
        return new(x, y, z);
    }

    public int3 GetPosInChunkFromVector3(int3 chunkPos, Vector3 worldPos)
    {
        int x = (int)(worldPos.x - (chunkPos.x * VoxelData.ChunkWidth));
        int y = (int)(worldPos.y - (chunkPos.y * VoxelData.ChunkHeight));
        int z = (int)(worldPos.z - (chunkPos.z * VoxelData.ChunkLength));
        return new(x, y, z);
    }
    public int3 GetPosInChunkFromVector3(Vector3Int chunkPos, int3 worldPos)
    {
        int x = worldPos.x - (chunkPos.x * VoxelData.ChunkWidth);
        int y = worldPos.y - (chunkPos.y * VoxelData.ChunkHeight);
        int z = worldPos.z - (chunkPos.z * VoxelData.ChunkLength);
        return new(x, y, z);
    }
    public int3 GetPosInChunkFromVector3(Vector3Int chunkPos, Vector3 worldPos)
    {
        int x = (int)(worldPos.x - (chunkPos.x * VoxelData.ChunkWidth));
        int y = (int)(worldPos.y - (chunkPos.y * VoxelData.ChunkHeight));
        int z = (int)(worldPos.z - (chunkPos.z * VoxelData.ChunkLength));
        return new(x, y, z);
    }

    public bool CheckForVoxel(Vector3 worldPos)
    {
        Vector3Int thisChunk = GetChunkCoordFromVector3(worldPos);

        if (Chunks.TryGetValue(thisChunk, out Chunk chunk))
        {
            int3 block = GetPosInChunkFromVector3(thisChunk, worldPos);

            // TODO think something for it
            chunk.VoxelMapAccess.Complete();
            return Blocks[(int)chunk.VoxelMap[CalcIndex(block)]].isSolid;
        }
        return false;
    }
    public BlockObject GetVoxel(Vector3 worldPos)
    {
        Vector3Int thisChunk = GetChunkCoordFromVector3(worldPos);

        if (Chunks.TryGetValue(thisChunk, out Chunk chunk))
        {
            int3 block = GetPosInChunkFromVector3(thisChunk, worldPos);
            chunk.VoxelMapAccess.Complete();

            return BlocksScObj.Blocks[chunk.VoxelMap[CalcIndex(block)]];
        }
        return BlocksScObj.Blocks[Block.Air];
    }

    private int CalcIndex(int3 xyz) => xyz.x * VoxelData.ChunkHeight * VoxelData.ChunkLength + xyz.y * VoxelData.ChunkLength + xyz.z;
}

[BurstCompile]
public struct GenerateStructuresJob : IJobParallelFor
{
    [ReadOnly]
    public VoxelData Data;
    [ReadOnly]
    public BiomeStruct Biome;
    [ReadOnly]
    public NativeArray<StructureMarker> Structures;

    public NativeParallelHashSet<int3>.ParallelWriter Keys;
    [WriteOnly]
    public NativeParallelMultiHashMap<int3, VoxelMod>.ParallelWriter Modifications;

    public void Execute(int i)
    {
        MakeTree(Structures[i].Position, Biome.MinTreeHeight, Biome.MaxTreeHeight);
    }

    public void MakeTree(int3 pos, int minTrunkHeight, int maxTrunkHeight)
    {
        int height = (int)(maxTrunkHeight * Get2DPerlin(Data, new(pos.x, pos.z), 2000f, 3f));

        if (height < minTrunkHeight)
        {
            height = minTrunkHeight;
        }

        // Leaves
        for (int x = -2; x < 3; x++)
        {
            for (int z = -2; z < 3; z++)
            {
                TryAdd(new(pos.x + x, pos.y + height - 2, pos.z + z), Block.Leaves);
                TryAdd(new(pos.x + x, pos.y + height - 3, pos.z + z), Block.Leaves);
            }
        }

        for (int x = -1; x < 2; x++)
        {
            for (int z = -1; z < 2; z++)
            {
                TryAdd(new(pos.x + x, pos.y + height - 1, pos.z + z), Block.Leaves);
            }
        }
        for (int x = -1; x < 2; x++)
        {
            if (x == 0)
                for (int z = -1; z < 2; z++)
                {
                    TryAdd(new(pos.x + x, pos.y + height, pos.z + z), Block.Leaves);
                }
            else
                TryAdd(new(pos.x + x, pos.y + height, pos.z), Block.Leaves);
        }

        // Trunk
        for (int i = 1; i < height; i++)
        {
            TryAdd(new(pos.x, pos.y + i, pos.z), Block.Wood);
        }
    }

    private void TryAdd(int3 pos, Block block)
    {
        int3 cPos = ToChunkCoord(pos);
        Modifications.Add(cPos, new VoxelMod(pos, block));
        Keys.Add(cPos);
    }

    private readonly int3 ToChunkCoord(int3 pos)
    {
        pos.x -= (pos.x < 0 && pos.x % Data.ChunkWidth != 0) ? Data.ChunkWidth : 0;
        pos.y -= (pos.y < 0 && pos.y % Data.ChunkHeight != 0) ? Data.ChunkHeight : 0;
        pos.z -= (pos.z < 0 && pos.z % Data.ChunkLength != 0) ? Data.ChunkLength : 0;
        int x = pos.x / Data.ChunkWidth;
        int y = pos.y / Data.ChunkHeight;
        int z = pos.z / Data.ChunkLength;
        return new(x, y, z);
    }
}


public struct EmptyJob : IJob
{
    public void Execute()
    {
    }
}