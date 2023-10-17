using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static CubesUtils;

public class BiomeGeneratorPrototype : MonoBehaviour
{
    public bool drawGizmo;
    [field: SerializeField] public List<BiomeAttributes> BiomeObjects { get; private set; }
    public NativeArray<BiomeStruct> Biomes { get; private set; }

    [field: SerializeField] public GameObject Cube { get; private set; }
    [field: SerializeField] public Transform Player { get; private set; }

    public NativeHashMap<int2, BiomePointTest> Grid { get; private set; }
    public NativeHashSet<int2> GeneratedRegions { get; private set; }

    private int _biomesMinSize = int.MaxValue;
    private int _biomesMaxSize = int.MinValue;

    NativeList<int2> _viewCoords;
    int2 lastRegion;
    int regionDistance;

    JobHandle gridAccess;
    private void Start()
    {
        NativeArray<BiomeStruct> biomesTemp = new(BiomeObjects.Count, Allocator.Persistent);
        for (int i = 0; i < BiomeObjects.Count; i++)
        {
            biomesTemp[i] = new(BiomeObjects[i]);
        }
        Biomes = biomesTemp;

        for (int i = 0; i < BiomeObjects.Count; i++)
        {
            _biomesMinSize = Mathf.Min(BiomeObjects[i].minSize, _biomesMinSize);
            _biomesMaxSize = Mathf.Max(BiomeObjects[i].maxSize, _biomesMaxSize);
        }

        Grid = new(100, Allocator.Persistent);
        GeneratedRegions = new(10, Allocator.Persistent);
        //_biomesDict = new();

        regionDistance = 3;
        List<Vector2Int> tempViewCoords = WorldHelper.InitViewCoords2D(regionDistance);

        _viewCoords = new(tempViewCoords.Count, Allocator.Persistent);
        for (int i = 0; i < tempViewCoords.Count; i++)
        {
            _viewCoords.Add(new(tempViewCoords[i].x, tempViewCoords[i].y));
        }

        Vector3Int region = GetBiomeRegion(Player.position);
        lastRegion = new(region.x, region.z);

        PopulateBiomesGridJobTest populateJob = new()
        {
            biomes = Biomes,
            viewCoords = _viewCoords,
            regionCoord = lastRegion,
            minRadius = _biomesMinSize,
            maxRadius = _biomesMaxSize,
            regionLength = 256,
            seed = 2,

            grid = Grid,
            generatedRegions = GeneratedRegions,
        };
        gridAccess = populateJob.Schedule();
    }

    private void OnDestroy()
    {
        Grid.Dispose();
        GeneratedRegions.Dispose();
        foreach (var biome in Biomes)
        {
            biome.Dispose();
        }
        _viewCoords.Dispose();
        Biomes.Dispose();
    }

    //Vector3Int playerRegion;
    void Update()
    {
        Vector3Int region = GetBiomeRegion(Player.position);
        int2 regionFlat = new(region.x, region.z);

        if (lastRegion.x != regionFlat.x || lastRegion.y != regionFlat.y)
        {
            lastRegion = regionFlat;

            CleanBiomesGridJobTest cleanGridJob = new()
            {
                minRadius = _biomesMinSize,
                regionCoord = regionFlat,
                regionDistance = regionDistance,
                regionLength = 256,

                grid = Grid,
                generatedRegions = GeneratedRegions,
            };
            JobHandle cleanHandle = cleanGridJob.Schedule(gridAccess);
            PopulateBiomesGridJobTest populateJob = new()
            {
                biomes = Biomes,
                viewCoords = _viewCoords,
                regionCoord = regionFlat,
                minRadius = _biomesMinSize,
                maxRadius = _biomesMaxSize,
                regionLength = 256,
                seed = 2,

                grid = Grid,
                generatedRegions = GeneratedRegions,
            };
            gridAccess = populateJob.Schedule(cleanHandle);
        }
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmo || !Grid.IsCreated || !gridAccess.IsCompleted) return;

        gridAccess.Complete();
        foreach (var kvp in Grid)
        {
            BiomePointTest point = kvp.Value;
            Gizmos.DrawSphere(new(point.pos.x, 0, point.pos.y), point.radius);
        }
    }
}

public struct BiomePointTest
{
    public float2 pos;
    public float radius;
    public BiomeStruct biome;
}

[BurstCompile]
public struct CleanBiomesGridJobTest : IJob
{
    public int2 regionCoord;
    public int minRadius;
    public int regionDistance;
    public int regionLength;

    public NativeHashSet<int2> generatedRegions;
    public NativeHashMap<int2, BiomePointTest> grid;

    public void Execute()
    {
        int2 scaled = new()
        {
            x = regionCoord.x * regionLength,
            y = regionCoord.y * regionLength
        };
        DeleteDifference(scaled, minRadius, 256);
    }

    private void DeleteDifference(int2 newRegion, float minRadius, int regionSize)
    {
        NativeList<int2> toDeleteRegion = new(10, Allocator.Temp);
        NativeList<int2> toDeletePoints = new(20, Allocator.Temp);

        float cellSize = minRadius / math.SQRT2;
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
public struct PopulateBiomesGridJobTest : IJob
{
    [ReadOnly]
    public NativeArray<BiomeStruct> biomes;
    [ReadOnly]
    public NativeList<int2> viewCoords;
    public int2 regionCoord;
    public int minRadius;
    public int maxRadius;
    public int regionLength;
    public uint seed;

    [WriteOnly]
    public NativeHashMap<int2, BiomePointTest> grid;
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
                    x = coord.x * regionLength,
                    y = coord.y * regionLength
                };
                GeneratePoints(scaled, minRadius, maxRadius, regionLength, seed);
            }
        }
    }

    public void GeneratePoints(float2 offset, float minRadius, float maxRadius, int regionLength, uint seed, int numSamplesBeforeRejection = 24)
    {
        float cellSize = minRadius / math.SQRT2;
        Unity.Mathematics.Random rng = new(math.hash(new float2(offset.x + seed, offset.y + seed)));

        NativeHashMap<int2, BiomePointTest> localGrid = new(20, Allocator.Temp);
        for (int spawnAttempts = numSamplesBeforeRejection; spawnAttempts > 0; spawnAttempts--)
        {
            BiomeStruct biome = biomes[rng.NextInt(0, biomes.Length)];
            BiomePointTest point = new()
            {
                biome = biome,
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

    readonly bool IsValid(BiomePointTest candidate, int sampleRegionSize, float2 offset, float cellSize, float maxRadius, in NativeHashMap<int2, BiomePointTest> grid)
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
                if (grid.TryGetValue(new(x, y), out BiomePointTest other))
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
}