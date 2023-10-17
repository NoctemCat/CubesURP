using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class PoissonDisk2DTest : MonoBehaviour
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

    public Dictionary<Vector2Int, Point2D> grid = new();

    public List<Vector2Int> gridCoords = new();

    [Range(0f, 1f)]
    public float edgeOffset;

    private void OnValidate()
    {
        if (gridSize < 1)
            gridSize = 1;

        if (minRadius <= 0.001f)
            minRadius = 0.001f;
        if (minRadius > maxRadius)
            maxRadius = minRadius;

        if (rejectionSamples > 50)
            rejectionSamples = 50;

        Random.InitState(seed);

        grid.Clear();
        gridCoords.Clear();

        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                gridCoords.Add(new(x, y));
            }
        }
        gridCoords.Sort((Vector2Int a, Vector2Int b) =>
        {
            if (a.sqrMagnitude > b.sqrMagnitude) return 1;
            if (a.sqrMagnitude < b.sqrMagnitude) return -1;
            return 0;
        });

        foreach (Vector2Int coord in gridCoords)
        {
            Vector2Int offset = new()
            {
                x = coord.x * regionSize,
                y = coord.y * regionSize,
            };
            GeneratePoints(grid, minRadius, maxRadius, offset, regionSize, rejectionSamples);
        }
    }

    private Vector2 V2intToV2(Vector2Int vec) => new(vec.x, vec.y);
    private void OnDrawGizmos()
    {
        if (!drawGizmo) return;
        float cellSize = minRadius / Mathf.Sqrt(2);
        int pointsGridSide = Mathf.CeilToInt(regionSize / cellSize);
        foreach (Vector2Int coord in gridCoords)
        {
            //Vector2Int offsetCoord = coord + gridSizeOffset;
            Vector2 offset = new()
            {
                x = coord.x * regionSize,
                y = coord.y * regionSize,
            };

            Vector2 sum = Sum(offset, regionSize / 2);
            Gizmos.DrawWireCube(new(sum.x, 0, sum.y), new(regionSize, 0, regionSize));
            Vector3Int offsetScaled = new((int)(offset.x / cellSize), (int)(offset.y / cellSize));

            for (int x = offsetScaled.x; x < offsetScaled.x + pointsGridSide; x++)
            {
                for (int y = offsetScaled.y; y < offsetScaled.y + pointsGridSide; y++)
                {
                    if (grid.TryGetValue(new(x, y), out Point2D Point2D))
                    {
                        Gizmos.DrawSphere(new(Point2D.x, 0, Point2D.y), Point2D.radius);
                    }
                }
            }
        }
        //foreach (var (_, point) in grid)
        //{
        //    Gizmos.DrawSphere(new(point.pos.x, 0, point.pos.y), point.radius);
        //}
    }
    private Vector2 Sum(Vector2 vec, int num)
    {
        vec.x += num;
        vec.y += num;
        return vec;
    }

    public void GeneratePoints(in Dictionary<Vector2Int, Point2D> grid, float minRadius, float maxRadius, Vector2Int offset, int sampleRegionSize, int numSamplesBeforeRejection = 30)
    {
        float cellSize = minRadius / Mathf.Sqrt(2);
        Random.InitState(new Vector2Int(offset.x + seed, offset.y + seed).GetHashCode());

        Dictionary<Vector2Int, Point2D> localGrid = new();

        for (int spawnAttempts = numSamplesBeforeRejection; spawnAttempts > 0; spawnAttempts--)
        {
            Point2D point = new()
            {
                x = Random.Range(0, sampleRegionSize) + offset.x,
                y = Random.Range(0, sampleRegionSize) + offset.y,
                radius = Random.Range(minRadius, maxRadius)
            };

            if (IsValid(point, sampleRegionSize, offset, cellSize, maxRadius, localGrid))
            {
                Vector2Int gridCell = point.GetGridCell(cellSize);

                grid[gridCell] = point;
                localGrid[gridCell] = point;

                spawnAttempts += numSamplesBeforeRejection;
            }
        }
    }

    bool IsValid(Point2D candidate, int sampleRegionSize, Vector2Int offset, float cellSize, float maxRadius, in Dictionary<Vector2Int, Point2D> grid)
    {
        Vector2 offsetPos = candidate.Pos - offset;
        float edge = candidate.radius * edgeOffset;
        if (
            offsetPos.x < edge ||
            offsetPos.x > sampleRegionSize - edge ||
            offsetPos.y < edge ||
            offsetPos.y > sampleRegionSize - edge
        )
        {
            return false;
        }
        Vector2Int cell = candidate.GetGridCell(cellSize);

        int maxRadiusCells = Mathf.CeilToInt((maxRadius + candidate.radius) / cellSize);
        //maxRadiusCells = 2;
        int searchStartX = cell.x - maxRadiusCells;
        int searchEndX = cell.x + maxRadiusCells;
        int searchStartY = cell.y - maxRadiusCells;
        int searchEndY = cell.y + maxRadiusCells;

        for (int x = searchStartX; x <= searchEndX; x++)
        {
            for (int y = searchStartY; y <= searchEndY; y++)
            {
                if (grid.TryGetValue(new(x, y), out Point2D other))
                {
                    float sqrDst = (candidate.Pos - other.Pos).sqrMagnitude;
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

public struct Point2D
{
    public float x;
    public float y;
    public float radius;
    public Vector2 Pos { readonly get => new(x, y); set { x = value.x; y = value.y; } }

    public readonly Vector2Int GetGridCell(float cellSize) => new(Mathf.FloorToInt(x / cellSize), Mathf.FloorToInt(y / cellSize));
}