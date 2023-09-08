
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
    [field: SerializeField] public Settings Settings { get; private set; }

    public VoxelData VoxelData { get; private set; }
    public BlockObject[] Blocks { get; private set; }
    public NativeArray<BlockStruct> NativeBlocks { get; private set; }
    public NativeArray<int3> XYZMap { get; private set; }
    public NativeArray<BiomeStruct> Biomes { get; private set; }

    [SerializeField] private BiomeAttributes[] _biomeScObjs;
    [field: SerializeField] public ActiveWorldData WorldData { get; private set; }
    [field: SerializeField] public Material SolidMaterial { get; private set; }
    [field: SerializeField] public Material TransparentMaterial { get; private set; }

    public GameObject PlayerObj;

    public Dictionary<Vector3Int, Chunk> Chunks { get; private set; }
    public NativeArray<Block> DummyMap { get; private set; }

    //public StructureSystem StructureSystem { get; private set; }
    public SaveSystem SaveSystem { get; private set; }

    List<Vector3Int> ViewCoords;
    List<Chunk> ActiveChunks;

    private EventSystem _eventSystem;

    private void Awake()
    {
        ServiceLocator.Register(this);
        LoadSettings();

        _eventSystem = ServiceLocator.Get<EventSystem>();
        var itemDatabase = ServiceLocator.Get<ItemDatabaseObject>();

        uint seed = math.hash(new int2(WorldData.seed.GetHashCode(), 0));
        Unity.Mathematics.Random rng = new(seed);
        float3 randomXYZ = rng.NextFloat3() * 10000;

        VoxelData = new(seed, randomXYZ, rng);

        Blocks = WorldHelper.InitBlocksMapping(itemDatabase);
        NativeBlocks = WorldHelper.InitNativeBlocksMapping(Blocks);
        XYZMap = WorldHelper.InitXYZMap(VoxelData);

        NativeArray<BiomeStruct> biomesTemp = new(_biomeScObjs.Length, Allocator.Persistent);
        for (int i = 0; i < _biomeScObjs.Length; i++)
        {
            biomesTemp[i] = new(_biomeScObjs[i]);
        }
        Biomes = biomesTemp;

        Chunks = new(ViewCoords.Count);
        DummyMap = new(VoxelData.ChunkSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);

        ActiveChunks = new(10);

        PlayerObj.transform.position = new(VoxelData.ChunkWidth / 2, 80f, VoxelData.ChunkLength / 2);
    }

    private void OnEnable()
    {
        _eventSystem.StartListening(EventType.ResumeGame, LoadSettingsHandler);
        _eventSystem.StartListening(EventType.Chunk_AddSortedStructures, AddSortedStructures);
        _eventSystem.StartListening(EventType.PlayerChunkChanged, PlayerChunkChanged);
    }

    private void OnDisable()
    {
        _eventSystem.StopListening(EventType.ResumeGame, LoadSettingsHandler);
        _eventSystem.StopListening(EventType.Chunk_AddSortedStructures, AddSortedStructures);
        _eventSystem.StopListening(EventType.PlayerChunkChanged, PlayerChunkChanged);
    }

    private void AddSortedStructures(in EventArgs args)
    {
        if (args.eventType != EventType.Chunk_AddSortedStructures)
            Debug.Log($"World AddSortedStructures wrong event type {args.eventType}");

        var parsedArgs = (AddSortedStructuresArgs)args;
        Chunk chunk;
        if (Chunks.ContainsKey(parsedArgs.chunkPos))
        {
            chunk = Chunks[parsedArgs.chunkPos];
        }
        else
        {
            chunk = new(parsedArgs.chunkPos);
            Chunks[parsedArgs.chunkPos] = chunk;
        }
        chunk.AddRangeModification(parsedArgs.structures).Forget();
    }

    private CancellationTokenSource _cts;
    private bool _generatingChunks = false;
    private Vector3Int _playerChunk;
    private void PlayerChunkChanged(in EventArgs args)
    {
        if (args.eventType != EventType.PlayerChunkChanged)
            Debug.Log("World PlayerChunkChanged listener wrong EventArgs");

        var playerChunk = (args as PlayerChunkChangedArgs).newChunkPos;
        _playerChunk = playerChunk;

        if (_generatingChunks)
        {
            return;
        }

        //_cts = new();
        CheckDistanceNew(this.GetCancellationTokenOnDestroy()).Forget();
    }

    async UniTaskVoid CheckDistanceNew(CancellationToken token)
    {
        _generatingChunks = true;

        while (!GenerateVoxelMaps())
        {
            bool isCalncelled = await UniTask.WaitForSeconds(Settings.timeBetweenGenerating, false, PlayerLoopTiming.Initialization, token).SuppressCancellationThrow();
            if (isCalncelled) return;
        }
        _generatingChunks = false;
    }

    private void Start()
    {
        SaveSystem = ServiceLocator.Get<SaveSystem>();
    }

    private void OnDestroy()
    {
        //ServiceLocator.Unregister<MeshDataPool>();
        ServiceLocator.Unregister(this);

        foreach (var kvp in Chunks)
        {
            kvp.Value.Dispose();
        }
        NativeBlocks.Dispose();
        XYZMap.Dispose();

        for (int i = 0; i < Biomes.Length; i++)
        {
            Biomes[i].Dispose();
        }
        Biomes.Dispose();
        DummyMap.Dispose();

        //ServiceLocator.Get<MeshDataPool>().Dispose();
        VoxelData.Dispose();
    }

    public void LoadSettingsHandler(in EventArgs _) => LoadSettings();
    public void LoadSettings()
    {
        if (File.Exists($"{Application.dataPath}/settings.cfg"))
        {
            string loadSettings = File.ReadAllText($"{Application.dataPath}/settings.cfg");
            Settings = JsonUtility.FromJson<Settings>(loadSettings);
        }
        else
        {
            Settings settings = new();
            settings.Init();
            Settings = settings;
            string saveSettings = JsonUtility.ToJson(settings, true);
            File.WriteAllText($"{Application.dataPath}/settings.cfg", saveSettings);
        }
        ViewCoords = WorldHelper.InitViewCoords(Settings.viewDistance);
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

    //Vector3Int lastPlayerChunk = new(-100, -100);
    //float timer = 0f;
    //float timerGarbage = 0f;
    private void Update()
    {
        //timer += Time.deltaTime;
        ////PlayerChunk = GetChunkCoordFromVector3(PlayerObj.transform.position);
        _results.Clear();
        //if (timer > 20f)
        //{
        //    Debug.Log($"Exist {Chunks.Count} chunks");
        //}

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

    bool GenerateVoxelMaps()
    {
        bool allGenerated = true;
        int toGenerate = Settings.generateAtOnce;
        for (int i = 0; i < ViewCoords.Count; i++)
        {
            Vector3Int checkCoord = _playerChunk + ViewCoords[i];
            if (Chunks.ContainsKey(checkCoord)) continue;

            if (SaveSystem.SavedChunks.Contains(checkCoord))
            {
                string chunkName = PathHelper.GenerateChunkName(checkCoord);
                if (CheckChunkFile(chunkName))
                {
                    SaveSystem.LoadChunkAsync(checkCoord, chunkName).ContinueWith(chunkData =>
                    {
                        if (chunkData is null) return;

                        var chunk = new Chunk(chunkData);
                        Chunks[checkCoord] = chunk;

                        SaveSystem.ReclaimData(checkCoord, chunkData);
                    });
                }
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
        ActiveChunks.Clear();
        for (int i = 0; i < ViewCoords.Count; i++)
        {
            Vector3Int checkCoord = _playerChunk + ViewCoords[i];
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

                chunk.AddNeighbours(flag);
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

                otherChunk.AddNeighbours(flag);
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
        int x = Mathf.FloorToInt(pos.x / VoxelDataStatic.ChunkWidth);
        int y = Mathf.FloorToInt(pos.y / VoxelDataStatic.ChunkHeight);
        int z = Mathf.FloorToInt(pos.z / VoxelDataStatic.ChunkLength);
        return new(x, y, z);
    }

    //public int3 GetPosInChunkFromVector3(int3 chunkPos, Vector3 worldPos)
    //{
    //    int x = (int)(worldPos.x - (chunkPos.x * VoxelData.ChunkWidth));
    //    int y = (int)(worldPos.y - (chunkPos.y * VoxelData.ChunkHeight));
    //    int z = (int)(worldPos.z - (chunkPos.z * VoxelData.ChunkLength));
    //    return new(x, y, z);
    //}
    //public int3 GetPosInChunkFromVector3(Vector3Int chunkPos, int3 worldPos)
    //{
    //    int x = worldPos.x - (chunkPos.x * VoxelData.ChunkWidth);
    //    int y = worldPos.y - (chunkPos.y * VoxelData.ChunkHeight);
    //    int z = worldPos.z - (chunkPos.z * VoxelData.ChunkLength);
    //    return new(x, y, z);
    //}

    public int3 GetPosInChunkFromVector3(Vector3Int chunkPos, Vector3 worldPos)
    {
        int x = (int)(worldPos.x - (chunkPos.x * VoxelData.ChunkWidth));
        int y = (int)(worldPos.y - (chunkPos.y * VoxelData.ChunkHeight));
        int z = (int)(worldPos.z - (chunkPos.z * VoxelData.ChunkLength));
        return new(x, y, z);
    }

    private readonly Dictionary<Vector3, bool> _results = new();
    public bool CheckForVoxel(Vector3 worldPos)
    {
        Vector3Int worldPosInt = new(
            Mathf.FloorToInt(worldPos.x),
            Mathf.FloorToInt(worldPos.y),
            Mathf.FloorToInt(worldPos.z)
        );

        if (_results.TryGetValue(worldPosInt, out bool result)) return result;

        result = false;
        Vector3Int thisChunk = GetChunkCoordFromVector3(worldPos);

        if (Chunks.TryGetValue(thisChunk, out Chunk chunk))
        {
            int3 block = GetPosInChunkFromVector3(thisChunk, worldPos);

            // TODO think something for it
            chunk.VoxelMapAccess.Complete();

            if (!chunk.IsActive) return false;
            //Blocks[(int)chunk.VoxelMap[CalcIndex(block)]].
            result = Blocks[(int)chunk.VoxelMap[CalcIndex(VoxelData, block)]].isSolid;
        }

        _results.Add(worldPosInt, result);
        return result;
    }
    public BlockObject GetVoxel(Vector3 worldPos)
    {
        Vector3Int thisChunk = GetChunkCoordFromVector3(worldPos);

        if (Chunks.TryGetValue(thisChunk, out Chunk chunk))
        {
            int3 block = GetPosInChunkFromVector3(thisChunk, worldPos);
            chunk.VoxelMapAccess.Complete();

            return Blocks[(int)chunk.VoxelMap[CalcIndex(VoxelData, block)]];
        }
        return Blocks[(int)Block.Air];
    }

    //private int CalcIndex(int3 xyz) => xyz.x * VoxelData.ChunkHeight * VoxelData.ChunkLength + xyz.y * VoxelData.ChunkLength + xyz.z;
}

[Serializable]
public struct Settings
{
    public string version;

    [Header("Performance")]
    public int viewDistance;

    [Header("Chunk Generation")]
    public int generateAtOnce;
    public float timeBetweenGenerating;

    [Header("Controls")]
    [Range(0.1f, 50f)]
    public float mouseSenstivity;

    public void Init()
    {
        version = "0.0.01";
        viewDistance = 5;
        generateAtOnce = 8;
        timeBetweenGenerating = 0.25f;
        mouseSenstivity = 25f;
    }
}