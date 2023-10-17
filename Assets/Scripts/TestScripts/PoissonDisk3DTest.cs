using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class PoissonDisk3DTest : MonoBehaviour
{
    //public Vector2Int gridSizeOffset = Vector2Int.one;
    public bool drawGizmo = true;
    public int gridSize = 1;
    public float minRadius = 1f;
    public float maxRadius = 2f;
    public int regionSize = 256;
    [SerializeField] private int rejectionSamples = 30;
    public int seed = 30;
    //public Vector2Int offset;

    public Dictionary<Vector3Int, Point3D> grid = new();

    public List<Vector3Int> gridCoords = new();

    private void OnValidate()
    {
        if (gridSize < 1)
            gridSize = 1;

        if (minRadius <= 0.001f)
            minRadius = 0.001f;
        if (minRadius > maxRadius)
            maxRadius = minRadius + 0.001f;

        if (rejectionSamples > 50)
            rejectionSamples = 50;

        Random.InitState(seed);

        grid.Clear();
        gridCoords.Clear();

        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                for (int z = 0; z < gridSize; z++)
                {
                    gridCoords.Add(new(x, y, z));
                }
            }
        }
        gridCoords.Sort((Vector3Int a, Vector3Int b) =>
        {
            if (a.sqrMagnitude > b.sqrMagnitude) return 1;
            if (a.sqrMagnitude < b.sqrMagnitude) return -1;
            return 0;
        });

        foreach (Vector3Int coord in gridCoords)
        {
            Vector3Int offset = new()
            {
                x = coord.x * regionSize,
                y = coord.y * regionSize,
                z = coord.z * regionSize,
            };
            GeneratePoints(ref grid, minRadius, maxRadius, offset, regionSize, rejectionSamples);
        }
    }

    private Vector2 V2intToV2(Vector2Int vec) => new(vec.x, vec.y);
    private void OnDrawGizmos()
    {
        if (!drawGizmo) return;
        float cellSize = minRadius / Mathf.Sqrt(2);
        int pointsGridSide = Mathf.CeilToInt(regionSize / cellSize);
        foreach (Vector3Int coord in gridCoords)
        {
            //Vector2Int offsetCoord = coord + gridSizeOffset;
            Vector3 offset = new()
            {
                x = coord.x * regionSize,
                y = coord.y * regionSize,
                z = coord.z * regionSize,
            };

            Gizmos.DrawWireCube(Sum(offset, regionSize / 2), new(regionSize, regionSize, regionSize));
            Vector3Int offsetScaled = new((int)(offset.x / cellSize), (int)(offset.y / cellSize), (int)(offset.z / cellSize));

            for (int x = offsetScaled.x; x < offsetScaled.x + pointsGridSide; x++)
            {
                for (int y = offsetScaled.y; y < offsetScaled.y + pointsGridSide; y++)
                {
                    for (int z = offsetScaled.z; z < offsetScaled.z + pointsGridSide; z++)
                    {
                        if (grid.TryGetValue(new(x, y, z), out Point3D point))
                        {
                            Gizmos.DrawSphere(point.pos, point.radius);
                        }
                    }
                }
            }
        }
    }
    private Vector3 Sum(Vector3 vec, int num)
    {
        vec.x += num;
        vec.y += num;
        vec.z += num;
        return vec; ;
    }

    public void GeneratePoints(ref Dictionary<Vector3Int, Point3D> grid, float minRadius, float maxRadius, Vector3Int offset, int sampleRegionSize, int numSamplesBeforeRejection = 30)
    {
        float cellSize = minRadius / Mathf.Sqrt(2);
        Random.InitState(new Vector3Int(offset.x + seed, offset.y + seed, offset.z + seed).GetHashCode());

        Dictionary<Vector3Int, Point3D> localGrid = new();
        Vector3Int randPos = new()
        {
            x = Random.Range(0, sampleRegionSize),
            y = Random.Range(0, sampleRegionSize),
            z = Random.Range(0, sampleRegionSize),
        };
        List<Point3D> spawnPoints = new()
        {
            new() { pos = offset + randPos }
        };

        while (spawnPoints.Count > 0)
        {
            int spawnIndex = Random.Range(0, spawnPoints.Count);
            Point3D spawnCenter = spawnPoints[spawnIndex];
            bool candidateAccepted = false;

            for (int i = 0; i < numSamplesBeforeRejection; i++)
            {
                bool success = TrySpawn(spawnCenter, maxRadius, offset, sampleRegionSize, cellSize, spawnPoints, grid, localGrid);

                if (success)
                {
                    candidateAccepted = true;
                    break;
                }
            }
            if (!candidateAccepted)
            {
                spawnPoints.RemoveAt(spawnIndex);
            }

        }
    }

    bool TrySpawn(Point3D spawnCenter, float maxRadius, Vector3Int offset, int sampleRegionSize, float cellSize, in List<Point3D> spawnPoints, in Dictionary<Vector3Int, Point3D> grid, in Dictionary<Vector3Int, Point3D> localGrid)
    {
        Vector3 newPos = 4 * maxRadius * Random.insideUnitSphere;

        Point3D point = new()
        {
            pos = spawnCenter.pos + newPos,
            radius = Random.Range(minRadius, maxRadius)
        };

        if (IsValid(point, sampleRegionSize, offset, cellSize, maxRadius, localGrid))
        {
            Vector3Int gridCell = new((int)(point.pos.x / cellSize), (int)(point.pos.y / cellSize), (int)(point.pos.z / cellSize));

            grid[gridCell] = point;
            localGrid[gridCell] = point;
            spawnPoints.Add(point);

            return true;
        }

        return false;
    }

    bool IsValid(Point3D candidate, int sampleRegionSize, Vector3Int offset, float cellSize, float maxRadius, in Dictionary<Vector3Int, Point3D> grid)
    {
        Vector3 offsetPos = candidate.pos - offset;
        float edgeOffset = candidate.radius * 0.4f;
        if (
            offsetPos.x < edgeOffset ||
            offsetPos.x > sampleRegionSize - edgeOffset ||
            offsetPos.y < edgeOffset ||
            offsetPos.y > sampleRegionSize - edgeOffset ||
            offsetPos.z < edgeOffset ||
            offsetPos.z > sampleRegionSize - edgeOffset
        )
        {
            return false;
        }

        int cellX = (int)(candidate.pos.x / cellSize);
        int cellY = (int)(candidate.pos.y / cellSize);
        int cellZ = (int)(candidate.pos.z / cellSize);

        int maxRadiusCells = Mathf.FloorToInt((maxRadius + candidate.radius) / cellSize);
        int searchStartX = cellX - maxRadiusCells;
        int searchEndX = cellX + maxRadiusCells;
        int searchStartY = cellY - maxRadiusCells;
        int searchEndY = cellY + maxRadiusCells;
        int searchStartZ = cellZ - maxRadiusCells;
        int searchEndZ = cellZ + maxRadiusCells;

        for (int x = searchStartX; x <= searchEndX; x++)
        {
            for (int y = searchStartY; y <= searchEndY; y++)
            {
                for (int z = searchStartZ; z <= searchEndZ; z++)
                {
                    if (grid.TryGetValue(new(x, y, z), out Point3D other))
                    {
                        float sqrDst = (candidate.pos - other.pos).sqrMagnitude;
                        float combRad = candidate.radius + other.radius;
                        if (sqrDst < combRad * combRad)
                        {
                            return false;
                        }
                    }
                }
            }
        }
        return true;
    }

    bool IsInsideCircle(Vector2Int center, Vector2Int point, float radius)
    {
        float dx = center.x - point.x,
             dy = center.y - point.y;
        float distanceSqr = dx * dx + dy * dy;
        return distanceSqr <= radius * radius;
    }
}

public struct Point3D
{
    public Vector3 pos;
    public float radius;
}