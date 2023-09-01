
using System;
using System.Collections.Generic;
using System.IO;
//using System.Linq;
using Cysharp.Threading.Tasks;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Serialization.Binary;
using UnityEngine;
using UnityEngine.PlayerLoop;
using static CubesUtils;

public class World : MonoBehaviour
{
    public static World Instance { get; private set; }
    [field: SerializeField] public Settings Settings { get; private set; }

    public VoxelData VoxelData { get; private set; }
    public NativeArray<BlockStruct> Blocks { get; private set; }
    public NativeArray<int3> XYZMap { get; private set; }
    public NativeArray<BiomeStruct> Biomes { get; private set; }

    [field: SerializeField] public BlockDictionary BlocksScObj { get; private set; }
    [SerializeField] private BiomeAttributes[] BiomeScObjs;
    [SerializeField] private WorldData WorldData;
    [field: SerializeField] public Material SolidMaterial { get; private set; }
    [field: SerializeField] public Material TransparentMaterial { get; private set; }

    public GameObject PlayerObj;

    public float3 RandomXYZ { get; private set; }

    public Dictionary<Vector3Int, Chunk> Chunks { get; private set; }
    public NativeArray<Block> DummyMap { get; private set; }

    public StructureBuilder StructureBuilder { get; private set; }

    List<Vector3Int> ViewCoords;

    List<Chunk> ActiveChunks;

    public Vector3Int PlayerChunk;

    private bool _inUI;
    public bool InUI
    {
        get { return _inUI; }
        set
        {
            _inUI = value;
        }
    }

    public string AppPath { get; private set; }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(Instance);

        AppPath = Path.Combine(Application.persistentDataPath, WorldData.WorldName);

        LoadSettings();

        Unity.Mathematics.Random rng = new(math.hash(new int2(WorldData.Seed.GetHashCode(), 0)));
        RandomXYZ = rng.NextFloat3() * 10000;

        VoxelData = new(RandomXYZ);
        Blocks = WorldHelper.InitBlocksMapping(BlocksScObj);
        XYZMap = WorldHelper.InitXYZMap(VoxelData);
        //Biomes = new BiomeStruct[BiomeScObjs.Length];
        NativeArray<BiomeStruct> biomesTemp = new(BiomeScObjs.Length, Allocator.Persistent);
        for (int i = 0; i < BiomeScObjs.Length; i++)
        {
            biomesTemp[i] = new(BiomeScObjs[i]);
        }
        Biomes = biomesTemp;

        //Debug.Log(Settings.ViewDistance);
        ViewCoords = WorldHelper.InitViewCoords(Settings.ViewDistance);

        Chunks = new(ViewCoords.Count);
        //ChunkMap = new(ViewCoords.Count, Allocator.Persistent);
        DummyMap = new(VoxelData.ChunkSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);

        ActiveChunks = new(10);

        PlayerObj.transform.position = new(VoxelData.ChunkWidth / 2, 50f, VoxelData.ChunkLength / 2);

        //LastPlayerChunk = new(-1000, -1000, -1000);
        PlayerChunk = GetChunkCoordFromVector3(PlayerObj.transform.position);

        StructureBuilder = new(this);
        //GenerateWorld();
        //EmptyJob dummy = new() { };
        //GeneratingStructures = dummy.Schedule();

        CheckDistance().Forget();
        //LastPlayerChunk = PlayerChunk;
        //GenerateVoxelMaps();
        //CheckStructures().Forget();
    }

    private void OnDestroy()
    {
        VoxelData.Dispose();
        Blocks.Dispose();
        XYZMap.Dispose();
        for (int i = 0; i < Biomes.Length; i++)
        {
            Biomes[i].Dispose();
        }
        Biomes.Dispose();

        DummyMap.Dispose();
    }

    public void SaveSettings()
    {
        string saveSettings = JsonUtility.ToJson(Settings, true);
        File.WriteAllText($"{Application.dataPath}/settings.cfg", saveSettings);
    }

    public void LoadSettings()
    {
        if (File.Exists($"{Application.dataPath}/settings.cfg"))
        {
            string loadSettings = File.ReadAllText($"{Application.dataPath}/settings.cfg");
            Settings = JsonUtility.FromJson<Settings>(loadSettings);
        }
        else
        {
            SaveSettings();
        }
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
        Chunks[chunkPos].AddModification(new(VI3ToI3(chunkPos), blockPos, block)).Forget();

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

        for (int i = 0; i < ActiveChunks.Count; i++)
        {
            ActiveChunks[i].Update();
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
                await UniTask.WaitForSeconds(Settings.TimeBetweenGenerating);
            }

            //Debug.Log("Finished generating available");
            await UniTask.WaitUntil(() => PlayerChunk != LastPlayerChunk);
        }
    }

    bool DeactivateFarChunks()
    {
        foreach (Chunk chunk in ActiveChunks)
        {
            Vector3Int viewChunkPos = I3ToVI3(chunk.ChunkPos) - PlayerChunk;
            if (
                viewChunkPos.x < -Settings.ViewDistance || viewChunkPos.x > Settings.ViewDistance ||
                viewChunkPos.y < -Settings.ViewDistance || viewChunkPos.y > Settings.ViewDistance ||
                viewChunkPos.z < -Settings.ViewDistance || viewChunkPos.z > Settings.ViewDistance
            )
            {
                chunk.IsActive = false;
            }
        }
        ActiveChunks.Clear();
        return true;
    }

    bool GenerateVoxelMaps()
    {
        bool allGenerated = true;
        int toGenerate = Settings.GenerateAtOnce;
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
                if (!chunk.IsActive)
                    chunk.IsActive = true;
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

    public void GetAccessForPlayer()
    {
        for (VoxelFaces f = VoxelFaces.Back; f < VoxelFaces.Max; f++)
        {
            if (Chunks.TryGetValue(PlayerChunk + I3ToVI3(VoxelData.FaceChecks[(int)f]), out Chunk chunk))
            {
                chunk.VoxelMapAccess.Complete();
            }
        }
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

[Serializable]
public struct Settings
{
    public string Version;

    [Header("Performance")]
    public int ViewDistance;

    [Header("Chunk Generation")]
    public int GenerateAtOnce;
    public float TimeBetweenGenerating;

    [Header("Controls")]
    [Range(0.1f, 50f)]
    public float MouseSenstivity;

    public void Init()
    {
        Version = "0.0.01";
        ViewDistance = 5;
        GenerateAtOnce = 8;
        TimeBetweenGenerating = 0.25f;
        MouseSenstivity = 25f;
    }
}