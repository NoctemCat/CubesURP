//using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static CubesUtils;

public class ChunkHeightGenerator : MonoBehaviour
{
    private EventSystem _eventSystem;
    private World _world;
    private BiomeDatabase _biomeDatabase;
    private BiomeGenerator _biomeGenerator;

    private VoxelData _data;
    private NativeHashMap<int2, NativeArray<float>> _chunkHeights;
    private NativeHashMap<int2, NativeArray<int>> _chunkBiomes;
    private NativeHashMap<int2, JobHandle> _heightsAccesses;

    int2 lastPlayerChunk;

    private void Awake()
    {
        _eventSystem = ServiceLocator.Get<EventSystem>();
        _biomeDatabase = ServiceLocator.Get<BiomeDatabase>();
        ServiceLocator.Register(this);

        _chunkHeights = new(100, Allocator.Persistent);
        _chunkBiomes = new(100, Allocator.Persistent);
        _heightsAccesses = new(100, Allocator.Persistent);
    }

    private void Start()
    {
        _world = ServiceLocator.Get<World>();
        _biomeGenerator = ServiceLocator.Get<BiomeGenerator>();

        World world = ServiceLocator.Get<World>();
        _data = world.VoxelData;

        lastPlayerChunk = new(0, 0);
        _eventSystem.StartListening(EventType.PlayerChunkChanged, PlayerChunkChanged);
    }

    private void OnDestroy()
    {
        _eventSystem.StopListening(EventType.PlayerChunkChanged, PlayerChunkChanged);
        ServiceLocator.Unregister(this);
        foreach (var kvp in _chunkHeights)
        {
            kvp.Value.Dispose();
        }
        _chunkHeights.Dispose();
        foreach (var kvp in _chunkBiomes)
        {
            kvp.Value.Dispose();
        }
        _chunkBiomes.Dispose();
        _heightsAccesses.Dispose();
    }

    private void PlayerChunkChanged(in EventArgs args)
    {
        if (args.eventType != EventType.PlayerChunkChanged)
            Debug.Log("ChunkHeightGenerator PlayerChunkChanged listener wrong EventArgs");

        Vector3Int playerChunkFull = (args as PlayerChunkChangedArgs).newChunkPos;

        //Vector3Int region = GetBiomeRegion(playerChunk);
        int2 playerChunk = new(playerChunkFull.x, playerChunkFull.z);

        if (lastPlayerChunk.x != playerChunk.x || lastPlayerChunk.y != playerChunk.y)
        {
            lastPlayerChunk = playerChunk;
            //ScheduleRegionsRefresh(regionFlat);
            CleanOldPlayerChunk();
            //GenerateChunkHeights(playerChunk);
        }
    }

    private void CleanOldPlayerChunk()
    {
        NativeList<int2> toDeleteChunks = new(20, Allocator.Temp);

        foreach (var kvp in _chunkHeights)
        {
            if (!IsInsideViewDistance(kvp.Key))
            {
                toDeleteChunks.Add(kvp.Key);
            }
        }

        for (int i = 0; i < toDeleteChunks.Length; i++)
        {
            DisposeLater(_heightsAccesses[toDeleteChunks[i]], _chunkHeights[toDeleteChunks[i]], _chunkBiomes[toDeleteChunks[i]]).Forget();

            _chunkHeights.Remove(toDeleteChunks[i]);
            _chunkBiomes.Remove(toDeleteChunks[i]);
            _heightsAccesses.Remove(toDeleteChunks[i]);
        }
    }

    private async UniTaskVoid DisposeLater(JobHandle access, NativeArray<float> heights, NativeArray<int> biomes)
    {
        await UniTask.WaitForSeconds(4f);
        await access;
        access.Complete();

        heights.Dispose();
        biomes.Dispose();
    }

    private bool IsInsideViewDistance(int2 viewChunkPos)
    {
        return viewChunkPos.x >= -_world.Settings.viewDistance && viewChunkPos.x <= _world.Settings.viewDistance &&
            viewChunkPos.y >= -_world.Settings.viewDistance && viewChunkPos.y <= _world.Settings.viewDistance;
    }

    public void RequestChunkHeights(int2 chunkPos, out NativeArray<float> chunkHeights, out NativeArray<int> closestBiomes, out JobHandle heightsFinish)
    {
        if (_chunkHeights.TryGetValue(chunkPos, out NativeArray<float> heights))
        {
            chunkHeights = heights;
            closestBiomes = _chunkBiomes[chunkPos];
            heightsFinish = _heightsAccesses[chunkPos];
            //return heights;
            return;
        }

        chunkHeights = new(_data.xzMap.Length, Allocator.Persistent);
        closestBiomes = new(_data.xzMap.Length, Allocator.Persistent);
        //Debug.Log(closestBiomes.Length);

        //Debug.Log(6 * _biomeDatabase.BiomesGridCellSize);
        PopulateHeightMap populateHeighstJob = new()
        {
            data = _data,
            biomesGrid = _biomeGenerator.Grid,
            biomes = _biomeDatabase.Biomes,
            biomesCellSize = _biomeDatabase.BiomesGridCellSize,
            //biomesCellSize = 2f * _biomeDatabase.BiomesMaxSize,
            maxRadius = _biomeDatabase.BiomesMaxSize,

            chunkPos = chunkPos,

            chunkHeights = chunkHeights,
            chunkBiomes = closestBiomes,
        };

        JobHandle populateHeightsHandle = populateHeighstJob.Schedule(_biomeGenerator.GridAccess);
        heightsFinish = populateHeightsHandle;

        _chunkHeights[chunkPos] = chunkHeights;
        _chunkBiomes[chunkPos] = closestBiomes;
        _heightsAccesses[chunkPos] = populateHeightsHandle;
    }
}

public struct PopulateHeightMap : IJob
{
    [ReadOnly]
    public VoxelData data;
    [ReadOnly]
    public NativeHashMap<int2, BiomePoint> biomesGrid;
    [ReadOnly]
    //[NativeDisableContainerSafetyRestriction]
    public NativeArray<BiomeStruct> biomes;
    public float biomesCellSize;
    public float maxRadius;
    public int2 chunkPos;

    [WriteOnly]
    public NativeArray<float> chunkHeights;
    [WriteOnly]
    public NativeArray<int> chunkBiomes;
    public void Execute()
    {
        NativeHashSet<int2> visitedCells = new(10, Allocator.Temp);
        NativeList<BiomePoint> nearBiomes = new(5, Allocator.Temp);
        //NativeList<float2> biomesPosns = new(5, Allocator.Temp);
        //NativeList<float> inverseDsts = new(5, Allocator.Temp);

        float radius = 2f * maxRadius;

        int2 chunkCenter = new int2(chunkPos.x, chunkPos.y) * new int2(data.ChunkWidth, data.ChunkLength) + new int2(data.ChunkWidth, data.ChunkLength) / 2;
        float gatherRadius = radius + math.max(data.ChunkWidth, data.ChunkLength);
        GatherPoints(chunkCenter, gatherRadius, visitedCells, nearBiomes);

        int maxOctaves = 16;
        Unity.Mathematics.Random rng = new(math.hash(new uint2(data.seed, data.seed)));

        NativeArray<float2> octavesOffsets = new(maxOctaves, Allocator.Temp);
        for (int i = 0; i < octavesOffsets.Length; i++)
        {
            octavesOffsets[i] = rng.NextFloat2(-100000f, 100000f);
        }

        int2 chunkScaled = new int2(chunkPos.x, chunkPos.y) * new int2(data.ChunkWidth, data.ChunkLength);
        NativeArray<float> biomeHeight = new(biomes.Length, Allocator.Temp);

        for (int h = 0; h < chunkHeights.Length; h++)
        {
            int2 xz = chunkScaled + data.xzMap[h];
            float totalHeight = 0f;
            float totalWeight = 0f;

            for (int i = 0; i < biomeHeight.Length; i++)
            {
                BiomeStruct current = biomes[i];
                float heightFull = current.minHeight + (current.maxHeight - current.minHeight)
                       * GetHeight(xz, current.terrainScale, current.noise.octaves, octavesOffsets);
                biomeHeight[i] = heightFull;
            }

            NativeArray<float> influences = new(biomes.Length, Allocator.Temp, NativeArrayOptions.ClearMemory);

            float radiusSq = radius * radius;
            for (int i = 0; i < nearBiomes.Length; i++)
            {
                float currentDstSq = math.distancesq(xz, nearBiomes[i].pos);
                if (currentDstSq < radiusSq)
                {
                    //float weight = radiusSq - currentDstSq;
                    BiomeStruct current = biomes[nearBiomes[i].biomeId];
                    float weight = radiusSq - currentDstSq;
                    weight *= current.influenceMult;
                    //weight *= weight;
                    influences[current.id] += weight;
                    totalWeight += weight;
                }
            }

            int idx = 0;
            for (int i = 0; i < influences.Length; i++)
            {
                totalHeight += biomeHeight[i] * (influences[i] / totalWeight);
                //influences[i] *= math.pow(influences[i], 8);
                if (influences[idx] < influences[i])
                {
                    idx = i;
                }
            }

            //chunkBiomes[h] = GetRandomWeightedIndex(influences, ref rng);
            chunkBiomes[h] = idx;
            chunkHeights[h] = totalHeight;
        }
    }

    public readonly int GetRandomWeightedIndex(NativeArray<float> weights, ref Unity.Mathematics.Random rng)
    {
        // Get the total sum of all the weights.
        float weightSum = 0f;
        for (int i = 0; i < weights.Length; ++i)
        {
            weightSum += weights[i];
        }

        // Step through all the possibilities, one by one, checking to see if each one is selected.
        int index = 0;
        int lastIndex = weights.Length - 1;

        float x = rng.NextFloat(0f, weightSum);
        while (index < lastIndex)
        {
            if ((x - weights[index]) < 0f)
            {
                return index;
            }

            x -= weights[index++];
        }

        // No other item was selected, so return very last index.
        return index;
    }

    private void GatherPoints(int2 gatherPoint, float radius, in NativeHashSet<int2> visitedCells, in NativeList<BiomePoint> nearBiomes)
    {
        int r = (int)math.ceil(radius);
        for (int x = gatherPoint.x - r; x <= gatherPoint.x + r; x++)
        {
            for (int z = gatherPoint.y - r; z <= gatherPoint.y + r; z++)
            {
                int2 cell = new()
                {
                    x = (int)math.ceil(x / biomesCellSize),
                    y = (int)math.ceil(z / biomesCellSize)
                };

                if (visitedCells.Contains(cell)) continue;
                if (biomesGrid.TryGetValue(cell, out BiomePoint point))
                {
                    nearBiomes.Add(point);
                }

                visitedCells.Add(cell);
            }
        }
    }
}
