using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static CubesUtils;

public class BiomeGenerator : MonoBehaviour
{
    private EventSystem _eventSystem;
    public bool drawGizmo;
    VoxelData data;

    private BiomeDatabase _biomeDatabase;
    //[field: SerializeField] public List<BiomeAttributes> BiomeObjects { get; private set; }
    //public NativeArray<BiomeStruct> Biomes { get; private set; }

    public NativeHashMap<int2, BiomePoint> Grid { get; private set; }
    public NativeHashSet<int2> GeneratedRegions { get; private set; }
    public JobHandle GridAccess { get; private set; }

    //public float CellSize { get; private set; }
    //private int _biomesMinSize = int.MaxValue;
    //private int _biomesMaxSize = int.MinValue;

    NativeList<int2> viewCoords;
    int2 lastRegion;
    int regionDistance;
    Dictionary<int, Color> gizmoColors;

    private void Awake()
    {
        _eventSystem = ServiceLocator.Get<EventSystem>();
        _biomeDatabase = ServiceLocator.Get<BiomeDatabase>();
        ServiceLocator.Register(this);

        _biomeDatabase.Init();

        //NativeArray<BiomeStruct> biomesTemp = new(BiomeObjects.Count, Allocator.Persistent);
        //for (int i = 0; i < BiomeObjects.Count; i++)
        //{
        //    biomesTemp[i] = new(BiomeObjects[i]);
        //}
        //Biomes = biomesTemp;

        //for (int i = 0; i < BiomeObjects.Count; i++)
        //{
        //    _biomesMinSize = Mathf.Min(BiomeObjects[i].minSize, _biomesMinSize);
        //    _biomesMaxSize = Mathf.Max(BiomeObjects[i].maxSize, _biomesMaxSize);
        //}
        //CellSize = _biomesMinSize / math.SQRT2;
        //for (int i = 0; i < _biomeDatabase.BiomeObjects.Length; i++)
        //{
        //    Debug.Log("Objects: " + _biomeDatabase.BiomeObjects[i].id);
        //}
        //for (int i = 0; i < _biomeDatabase.Biomes.Length; i++)
        //{
        //    Debug.Log(_biomeDatabase.Biomes[i].id);
        //}

        Grid = new(100, Allocator.Persistent);
        GeneratedRegions = new(10, Allocator.Persistent);

        gizmoColors = new();

        regionDistance = 3;
        var tempViewCoords = WorldHelper.InitViewCoords2D(regionDistance);

        viewCoords = new(tempViewCoords.Count, Allocator.Persistent);
        for (int i = 0; i < tempViewCoords.Count; i++)
        {
            viewCoords.Add(new(tempViewCoords[i].x, tempViewCoords[i].y));
        }
    }

    private void Start()
    {
        World world = ServiceLocator.Get<World>();
        data = world.VoxelData;

        UnityEngine.Random.InitState((int)data.seed);

        for (int i = 0; i < _biomeDatabase.Biomes.Length; i++)
        {
            gizmoColors[_biomeDatabase.Biomes[i].id] = UnityEngine.Random.ColorHSV();
        }

        lastRegion = new(0, 0);

        PopulateBiomesGridJob populateJob = new()
        {
            data = data,
            biomes = _biomeDatabase.Biomes,
            viewCoords = viewCoords,
            regionCoord = lastRegion,
            cellSize = _biomeDatabase.BiomesGridCellSize,
            maxRadius = _biomeDatabase.BiomesMaxSize,

            grid = Grid,
            generatedRegions = GeneratedRegions,
        };
        GridAccess = populateJob.Schedule();

        _eventSystem.StartListening(EventType.PlayerChunkChanged, PlayerChunkChanged);
    }

    private void OnDestroy()
    {
        _eventSystem.StopListening(EventType.PlayerChunkChanged, PlayerChunkChanged);
        ServiceLocator.Unregister(this);

        GeneratedRegions.Dispose();
        Grid.Dispose();
        //foreach (var biome in Biomes)
        //{
        //biome.Dispose();
        //}
        viewCoords.Dispose();

        _biomeDatabase.Dispose();
        //Biomes.Dispose();
    }

    private void PlayerChunkChanged(in EventArgs args)
    {
        if (args.eventType != EventType.PlayerChunkChanged)
            Debug.Log("BiomeGenerator PlayerChunkChanged listener wrong EventArgs");

        Vector3Int playerChunk = (args as PlayerChunkChangedArgs).newChunkPos;

        Vector3Int region = GetBiomeRegion(playerChunk);
        int2 regionFlat = new(region.x, region.z);

        if (lastRegion.x != regionFlat.x || lastRegion.y != regionFlat.y)
        {
            lastRegion = regionFlat;
            ScheduleRegionsRefresh(regionFlat);
        }
    }

    private void ScheduleRegionsRefresh(int2 region)
    {
        CleanBiomesGridJob cleanGridJob = new()
        {
            data = data,
            cellSize = _biomeDatabase.BiomesGridCellSize,
            regionCoord = region,
            regionDistance = regionDistance,

            grid = Grid,
            generatedRegions = GeneratedRegions,
        };
        JobHandle cleanHandle = cleanGridJob.Schedule(GridAccess);
        PopulateBiomesGridJob populateJob = new()
        {
            data = data,
            biomes = _biomeDatabase.Biomes,
            viewCoords = viewCoords,
            regionCoord = region,
            cellSize = _biomeDatabase.BiomesGridCellSize,
            maxRadius = _biomeDatabase.BiomesMaxSize,

            grid = Grid,
            generatedRegions = GeneratedRegions,
        };
        GridAccess = populateJob.Schedule(cleanHandle);
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmo || !Grid.IsCreated || !GridAccess.IsCompleted) return;

        GridAccess.Complete();
        foreach (var kvp in Grid)
        {
            BiomePoint point = kvp.Value;
            Gizmos.color = gizmoColors[point.biomeId];
            Gizmos.DrawSphere(new(point.pos.x, 0, point.pos.y), point.radius);
            //Gizmos.DrawSphere(new(kvp.Key.x, 0, kvp.Key.y), point.radius);
        }
    }
}

public struct BiomePoint : IEquatable<BiomePoint>
{
    public float2 pos;
    public float radius;
    //publui
    public int biomeId;

    public readonly bool Equals(BiomePoint other) => biomeId == other.biomeId;
}

[BurstCompile]
public struct CleanBiomesGridJob : IJob
{

    [ReadOnly]
    public VoxelData data;
    public int2 regionCoord;
    //public int minRadius;
    public float cellSize;
    public int regionDistance;

    public NativeHashSet<int2> generatedRegions;
    public NativeHashMap<int2, BiomePoint> grid;

    public void Execute()
    {
        int2 scaled = new()
        {
            x = regionCoord.x * data.BiomeRegionVoxelLength,
            y = regionCoord.y * data.BiomeRegionVoxelLength
        };
        DeleteDifference(scaled, cellSize, 256);
    }

    private void DeleteDifference(int2 newRegion, float cellSize, int regionSize)
    {
        NativeList<int2> toDeleteRegion = new(10, Allocator.Temp);
        NativeList<int2> toDeletePoints = new(20, Allocator.Temp);

        //float cellSize = minRadius / math.SQRT2;
        int pointsGridSide = Mathf.CeilToInt(regionSize / cellSize);
        foreach (int2 coord in generatedRegions)
        {
            int2 viewRegionPos = newRegion - coord;
            if (!IsInsideViewDistance(viewRegionPos))
            {
                float2 offset = new()
                {
                    x = coord.x * regionSize,
                    y = coord.y * regionSize,
                };

                int2 offsetScaled = new((int)(offset.x / cellSize), (int)(offset.y / cellSize));

                for (int x = offsetScaled.x; x <= offsetScaled.x + pointsGridSide; x++)
                {
                    for (int y = offsetScaled.y; y <= offsetScaled.y + pointsGridSide; y++)
                    {
                        if (grid.ContainsKey(new(x, y)))
                        {
                            toDeletePoints.Add(new(x, y));
                        }
                    }
                }

                toDeleteRegion.Add(coord);
            }
        }
        for (int i = 0; i < toDeleteRegion.Length; i++)
        {
            generatedRegions.Remove(toDeleteRegion[i]);
        }
        for (int i = 0; i < toDeletePoints.Length; i++)
        {
            grid.Remove(toDeletePoints[i]);
        }
    }

    private readonly bool IsInsideViewDistance(int2 viewChunkPos)
    {
        return viewChunkPos.x >= -regionDistance && viewChunkPos.x <= regionDistance &&
            viewChunkPos.y >= -regionDistance && viewChunkPos.y <= regionDistance;
    }
}


[BurstCompile]
public struct PopulateBiomesGridJob : IJob
{
    [ReadOnly]
    public VoxelData data;
    [ReadOnly]
    public NativeArray<BiomeStruct> biomes;
    [ReadOnly]
    public NativeList<int2> viewCoords;
    public int2 regionCoord;
    public float cellSize;
    public int maxRadius;

    [WriteOnly]
    public NativeHashMap<int2, BiomePoint> grid;
    public NativeHashSet<int2> generatedRegions;

    public void Execute()
    {

        for (int i = 0; i < viewCoords.Length; i++)
        {
            int2 coord = regionCoord + viewCoords[i];
            if (!generatedRegions.Contains(coord))
            {
                generatedRegions.Add(coord);

                int2 scaled = new()
                {
                    x = coord.x * data.BiomeRegionVoxelLength,
                    y = coord.y * data.BiomeRegionVoxelLength
                };
                GeneratePoints(scaled, cellSize, maxRadius, data.BiomeRegionVoxelLength, data.seed);
            }
        }
    }

    public void GeneratePoints(float2 offset, float cellSize, float maxRadius, int regionLength, uint seed, int numSamplesBeforeRejection = 30)
    {
        //float cellSize = minRadius / math.SQRT2;
        Unity.Mathematics.Random rng = new(math.hash(new float2(offset.x + seed, offset.y + seed)));

        NativeHashMap<int2, BiomePoint> localGrid = new(20, Allocator.Temp);
        NativeArray<float> weights = new(biomes.Length, Allocator.Temp);
        for (int i = 0; i < biomes.Length; i++)
        {
            weights[i] = biomes[i].biomeWeight;
        }
        for (int spawnAttempts = numSamplesBeforeRejection; spawnAttempts > 0; spawnAttempts--)
        {

            BiomeStruct biome = biomes[GetRandomWeightedIndex(weights, ref rng)];
            BiomePoint point = new()
            {
                biomeId = biome.id,
                pos = rng.NextFloat2(0f, regionLength) + offset,
                radius = rng.NextFloat(biome.minSize, biome.maxSize),
            };

            if (IsValid(point, regionLength, offset, cellSize, maxRadius, localGrid))
            {
                int2 cell = new()
                {
                    x = (int)math.ceil(point.pos.x / cellSize),
                    y = (int)math.ceil(point.pos.y / cellSize)
                };

                grid[cell] = point;
                localGrid[cell] = point;

                spawnAttempts += numSamplesBeforeRejection;
            }
        }

        localGrid.Dispose();
    }

    readonly bool IsValid(BiomePoint candidate, int sampleRegionSize, float2 offset, float cellSize, float maxRadius, in NativeHashMap<int2, BiomePoint> grid)
    {
        float2 offsetPos = candidate.pos - offset;
        float edge = candidate.radius * 0.55f;
        if (
            offsetPos.x < edge ||
            offsetPos.x > sampleRegionSize - edge ||
            offsetPos.y < edge ||
            offsetPos.y > sampleRegionSize - edge
        )
        {
            return false;
        }
        int2 cell = new()
        {
            x = (int)math.ceil(candidate.pos.x / cellSize),
            y = (int)math.ceil(candidate.pos.y / cellSize)
        };

        int maxRadiusCells = (int)math.ceil((maxRadius + candidate.radius) / cellSize);

        int searchStartX = cell.x - maxRadiusCells;
        int searchEndX = cell.x + maxRadiusCells;
        int searchStartY = cell.y - maxRadiusCells;
        int searchEndY = cell.y + maxRadiusCells;

        for (int x = searchStartX; x <= searchEndX; x++)
        {
            for (int y = searchStartY; y <= searchEndY; y++)
            {
                if (grid.TryGetValue(new(x, y), out BiomePoint other))
                {
                    float sqrDst = math.distancesq(candidate.pos, other.pos);
                    float combRad = candidate.radius + other.radius;
                    if (sqrDst <= combRad * combRad)
                    {
                        return false;
                    }
                }
            }
        }
        return true;
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
}