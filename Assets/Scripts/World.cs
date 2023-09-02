
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

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
    [field: SerializeField] public ActiveWorldData WorldData { get; private set; }
    [field: SerializeField] public Material SolidMaterial { get; private set; }
    [field: SerializeField] public Material TransparentMaterial { get; private set; }

    public GameObject PlayerObj;

    public float3 RandomXYZ { get; private set; }

    public Dictionary<Vector3Int, Chunk> Chunks { get; private set; }
    public NativeArray<Block> DummyMap { get; private set; }

    public StructureSystem StructureSystem { get; private set; }
    public SaveSystem SaveSystem { get; private set; }

    List<Vector3Int> ViewCoords;

    List<Chunk> ActiveChunks;
    List<Chunk> DisposedChunks;

    public Vector3Int PlayerChunk;
    private bool _refreshDistance;

    public string WorldPath { get; private set; }
    public Action OnPause;
    public Action OnResume;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(Instance);

        WorldPath = Path.Combine(PathHelper.WorldsPath, WorldData.WorldName);

        LoadSettings();
        OnResume += LoadSettings;

        _refreshDistance = false;

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

        Chunks = new(ViewCoords.Count);
        //ChunkMap = new(ViewCoords.Count, Allocator.Persistent);
        DummyMap = new(VoxelData.ChunkSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);

        ActiveChunks = new(10);
        DisposedChunks = new(20);

        PlayerObj.transform.position = new(VoxelData.ChunkWidth / 2, 50f, VoxelData.ChunkLength / 2);

        //LastPlayerChunk = new(-1000, -1000, -1000);
        PlayerChunk = GetChunkCoordFromVector3(PlayerObj.transform.position);

        StructureSystem = GetComponent<StructureSystem>();
        SaveSystem = GetComponent<SaveSystem>();
    }

    private void Start()
    {
        CheckDistance(this.GetCancellationTokenOnDestroy()).Forget();
        CheckDisposeChunks(this.GetCancellationTokenOnDestroy()).Forget();
    }

    //private async UniTaskVoid Test(CancellationToken cancellationToken)
    //{
    //    //cancellationToken.IsCancellationRequested
    //    //CheckDistance()
    //}

    private void OnDestroy()
    {
        SaveSystem.DestroyForceSave();
        foreach (var kvp in Chunks)
        {
            kvp.Value.Dispose();
        }
        VoxelData.Dispose();
        Blocks.Dispose();
        XYZMap.Dispose();

        for (int i = 0; i < Biomes.Length; i++)
        {
            Biomes[i].Dispose();
        }
        Biomes.Dispose();

        DummyMap.Dispose();

        Destroy(Instance);
    }

    public void LoadSettings()
    {
        if (File.Exists($"{Application.dataPath}/settings.cfg"))
        {
            string loadSettings = File.ReadAllText($"{Application.dataPath}/settings.cfg");
            Settings = JsonUtility.FromJson<Settings>(loadSettings);
            ViewCoords = WorldHelper.InitViewCoords(Settings.ViewDistance);
            _refreshDistance = true;
        }
        else
        {
            Settings settings = new();
            settings.Init();
            Settings = settings;
            string saveSettings = JsonUtility.ToJson(settings, true);
            File.WriteAllText($"{Application.dataPath}/settings.cfg", saveSettings);
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

        SaveSystem.AddChunkToSave(Chunks[chunkPos]);

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

    async UniTaskVoid CheckDisposeChunks(CancellationToken token)
    {
        List<Vector3Int> toRemove = new();
        while (true)
        {
            foreach (var kvp in Chunks)
            {
                Vector3Int viewChunkPos = kvp.Key - PlayerChunk;
                if (
                    viewChunkPos.x < -Settings.ViewDistance - 2 || viewChunkPos.x > Settings.ViewDistance + 2 ||
                    viewChunkPos.y < -Settings.ViewDistance - 2 || viewChunkPos.y > Settings.ViewDistance + 2 ||
                    viewChunkPos.z < -Settings.ViewDistance - 2 || viewChunkPos.z > Settings.ViewDistance + 2
                )
                {
                    //chunk.Dispose();
                    DisposedChunks.Add(kvp.Value);
                    toRemove.Add(kvp.Key);

                    int i = ActiveChunks.IndexOf(kvp.Value);
                    if (i != -1)
                        ActiveChunks.RemoveAt(i);
                }
            }
            for (int i = 0; i < toRemove.Count; i++)
            {
                Chunks.Remove(toRemove[i]);
            }
            toRemove.Clear();

            await UniTask.WaitForSeconds(6f, true, PlayerLoopTiming.Initialization, token).SuppressCancellationThrow();
            if (token.IsCancellationRequested) return;

            DisposeChunks().Forget();
        }
    }

    async UniTaskVoid DisposeChunks()
    {
        await UniTask.WaitForSeconds(4f);
        for (int i = 0; i < DisposedChunks.Count; i++)
        {
            DisposedChunks[i].Dispose();
        }
        DisposedChunks.Clear();

        //GC.Collect();
        //GC.WaitForPendingFinalizers();
    }

    async UniTask CheckDistance(CancellationToken token)
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
                await UniTask.WaitForSeconds(Settings.TimeBetweenGenerating, false, PlayerLoopTiming.Initialization, token).SuppressCancellationThrow();
                if (token.IsCancellationRequested) return;
            }

            await UniTask.WaitUntil(() => PlayerChunk != LastPlayerChunk || _refreshDistance, PlayerLoopTiming.EarlyUpdate, token).SuppressCancellationThrow();
            if (token.IsCancellationRequested) return;
            _refreshDistance = false;
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
            if (Chunks.ContainsKey(checkCoord)) continue;

            string chunkName = PathHelper.GenerateChunkName(checkCoord);
            if (CheckChunkFile(chunkName))
            {
                SaveSystem.LoadChunkAsync(chunkName).ContinueWith(chunkData =>
                {
                    var chunk = new Chunk(chunkData);
                    Chunks[checkCoord] = chunk;

                    SaveSystem.ReclaimData(chunkData);
                });
            }
            else
            {
                var chunk = new Chunk(checkCoord);
                Chunks[checkCoord] = chunk;
            }

            toGenerate--;
            if (toGenerate <= 0)
            {
                allGenerated = false;
                break;
            }
        }
        ActivateNearChunks();
        JobHandle.ScheduleBatchedJobs();

        return allGenerated;
    }
    public bool CheckChunkFile(string chunkName)
    {
        string chunkFile = PathHelper.GetChunkPath(SaveSystem.SaveChunkPath, chunkName);
        if (File.Exists(chunkFile))
        {
            if (new FileInfo(chunkFile).Length > 0)
                return true;
            else
            {
                Debug.Log($"File {chunkFile} corrupted");
                return false;
            }
        }
        return false;
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

            if (!chunk.IsActive) return false;
            return Blocks[(int)chunk.VoxelMap[CalcIndex(block)]].IsSolid;
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