using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public static class CubesUtils
{
    public static Vector3Int I3ToVI3(int3 v) => new(v.x, v.y, v.z);
    public static int3 VI3ToI3(Vector3Int v) => new(v.x, v.y, v.z);

    //public static float Get2DPerlin(in VoxelData data, float2 position, float offset, float scale)
    //{
    //    return noise.cnoise(new float2(
    //        (position.x + 0.1f) / data.ChunkWidth * scale + offset + data.RandomXYZ.x,
    //        (position.y + 0.1f) / data.ChunkLength * scale + offset + data.RandomXYZ.y)
    //    );
    //}

    //public static float Get3DPerlin(in VoxelData data, float3 position, float offset, float scale)
    //{
    //    return noise.cnoise(new float3(
    //        (position.x + 0.1f) / data.ChunkWidth * scale + offset + data.RandomXYZ.x,
    //        (position.y + 0.1f) / data.ChunkHeight * scale + offset + data.RandomXYZ.y,
    //        (position.z + 0.1f) / data.ChunkLength * scale + offset + data.RandomXYZ.z)
    //    );
    //}

    public static float Get2DPerlin(in VoxelData data, float2 position, float offset, float scale)
    {
        return noise.cnoise(new float2(
            (position.x + 0.1f) / 16 * scale + offset + data.RandomXYZ.x,
            (position.y + 0.1f) / 16 * scale + offset + data.RandomXYZ.y)
        );
    }

    public static float Get3DPerlin(in VoxelData data, float3 position, float offset, float scale)
    {
        return noise.cnoise(new float3(
            (position.x + 0.1f) / 16 * scale + offset + data.RandomXYZ.x,
            (position.y + 0.1f) / 128 * scale + offset + data.RandomXYZ.y,
            (position.z + 0.1f) / 16 * scale + offset + data.RandomXYZ.z)
        );
    }



    public static IEnumerable<Vector3> GetIntersectedWorldBlocksD(Vector3 pointA, Vector3 pointB)
    {
        var line = new HashSet<Vector3>
        {
            new(Mathf.Round(pointA.x), Mathf.Round(pointA.y), Mathf.Round(pointA.z))
        };

        var x = (float)Mathf.Round(pointA.x);
        var y = (float)Mathf.Round(pointA.y);
        var z = (float)Mathf.Round(pointA.z);

        var delta = pointB - pointA;
        var deltaNormal = delta.normalized;
        var dx = deltaNormal.x;
        var dy = deltaNormal.y;
        var dz = deltaNormal.z;

        var stepX = Signum(dx);
        var stepY = Signum(dy);
        var stepZ = Signum(dz);

        var tMaxX = Intbound(pointA.x - 0.5f, deltaNormal.x);
        var tMaxY = Intbound(pointA.y - 0.5f, deltaNormal.y);
        var tMaxZ = Intbound(pointA.z - 0.5f, deltaNormal.z);

        var tDeltaX = stepX / dx;
        var tDeltaY = stepY / dy;
        var tDeltaZ = stepZ / dz;

        var lineLength = delta.magnitude;

        bool StepX()
        {
            if (tMaxX >= lineLength) return true;
            x += stepX;
            tMaxX += tDeltaX;
            return false;
        }

        bool StepY()
        {
            if (tMaxY >= lineLength) return true;
            y += stepY;
            tMaxY += tDeltaY;
            return false;
        }

        bool StepZ()
        {
            if (tMaxZ >= lineLength) return true;
            z += stepZ;
            tMaxZ += tDeltaZ;
            return false;
        }

        bool res;

        if (stepX != 0 && stepY != 0 && stepZ != 0)
            while (true)
            {
                var tX = tMaxX.Precision5();
                var tY = tMaxY.Precision5();
                var tZ = tMaxZ.Precision5();
                if (tX < tY)
                {
                    if (tX < tZ)
                        res = StepX();
                    else if (tZ < tX)
                        res = StepZ();
                    else
                        res = StepZ() || StepX();
                }
                else if (tY < tX)
                {
                    if (tY < tZ)
                        res = StepY();
                    else if (tZ < tY)
                        res = StepZ();
                    else
                        res = StepY() || StepZ();
                }
                else
                {
                    if (tY < tZ)
                        res = StepX() || StepY();
                    else if (tZ < tY)
                        res = StepZ();
                    else
                        res = StepX() || StepY() || StepZ();
                }

                line.Add(new Vector3(x, y, z));
                if (res) break;
            }
        else if (stepX == 0 && stepY != 0 && stepZ != 0)
            while (true)
            {
                var tY = tMaxY.Precision5();
                var tZ = tMaxZ.Precision5();
                if (tY < tZ)
                    res = StepY();
                else if (tZ < tY)
                    res = StepZ();
                else
                    res = StepY() || StepZ();
                line.Add(new Vector3(x, y, z));
                if (res) break;
            }
        else if (stepX != 0 && stepY == 0 && stepZ != 0)
            while (true)
            {
                var tX = tMaxX.Precision5();
                var tZ = tMaxZ.Precision5();
                if (tX < tZ)
                    res = StepX();
                else if (tZ < tX)
                    res = StepZ();
                else
                    res = StepZ() || StepX();

                line.Add(new Vector3(x, y, z));
                if (res)
                {
                    break;
                }
            }
        else if (stepX != 0 && stepY != 0 && stepZ == 0)
            while (true)
            {
                var tY = tMaxY.Precision5();
                var tX = tMaxZ.Precision5();
                if (tY < tX)
                    res = StepY();
                else if (tX < tY)
                    res = StepX();
                else
                    res = StepY() || StepX();

                line.Add(new Vector3(x, y, z));
                if (res) break;
            }
        else if (stepX != 0 && stepY == 0 && stepZ == 0)
            while (true)
            {
                res = StepX();
                line.Add(new Vector3(x, y, z));
                if (res) break;
            }
        else if (stepX == 0 && stepY != 0 && stepZ == 0)
            while (true)
            {
                res = StepY();
                line.Add(new Vector3(x, y, z));
                if (res) break;
            }
        else if (stepX == 0 && stepY == 0 && stepZ != 0)
            while (true)
            {
                res = StepZ();
                line.Add(new Vector3(x, y, z));
                if (res) break;
            }

        return line;
    }

    private static int Signum(float x)
    {
        return x > 0 ? 1 : x < 0 ? -1 : 0;
    }

    private static float Ceil(float s)
    {
        return s == 0f ? 1f : Mathf.Ceil(s);
    }

    private static float Intbound(float s, float ds)
    {
        if (ds < 0 && Mathf.Round(s) == s) return 0;
        s = Mod(s, 1);
        return (ds > 0 ? Ceil(s) - s : s - Mathf.Floor(s)) / Mathf.Abs(ds);
    }

    private static float Mod(float value, float modulus)
    {
        return (value % modulus + modulus) % modulus;
    }

    public static float Precision5(this float val)
    {
        return ((int)(100000 * val)) / 100000.0f;
    }
}