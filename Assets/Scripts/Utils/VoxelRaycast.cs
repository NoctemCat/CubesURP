using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

// Taken from https://gist.github.com/dogfuntom/cc881c8fc86ad43d55d8
// Made by Maxim Kamalov in the comments

// Heavily based on:
// http://gamedev.stackexchange.com/a/49423/8806
public class VoxelRaycast
{
    private readonly BoundsInt? worldBounds;

    public VoxelRaycast(BoundsInt? _worldBounds)
    {
        worldBounds = _worldBounds;
    }

    /**
     * Call the callback with (position, face) of all blocks along the line
     * segment from point 'origin' in vector direction 'direction' of length
     * 'radius'. 'radius' may be infinite, but beware infinite loop in this case.
     *
     * 'face' is the normal vector of the face of that block that was entered.
     *
     * If the callback returns a true value, the traversal will be stopped.
     */
    public void Raycast(
        Vector3 origin,
        Vector3 direction,
        float radius,
        Func<Vector3Int, Vector3Int, bool> callback)
    {
        // From "A Fast Voxel Traversal Algorithm for Ray Tracing"
        // by John Amanatides and Andrew Woo, 1987
        // <http://www.cse.yorku.ca/~amana/research/grid.pdf>
        // <http://citeseer.ist.psu.edu/viewdoc/summary?doi=10.1.1.42.3443>
        // Extensions to the described algorithm:
        //   • Imposed a distance limit.
        //   • The face passed through to reach the current cube is provided to
        //     the callback.

        // The foundation of this algorithm is a parameterized representation of
        // the provided ray,
        //                    origin + t * direction,
        // except that t is not actually stored; rather, at any given point in the
        // traversal, we keep track of the *greater* t values which we would have
        // if we took a step sufficient to cross a cube boundary along that axis
        // (i.e. change the integer part of the coordinate) in the variables
        // tMaxX, tMaxY, and tMaxZ.

        // Cube containing origin point.
        var x = Mathf.FloorToInt(origin[0]);
        var y = Mathf.FloorToInt(origin[1]);
        var z = Mathf.FloorToInt(origin[2]);

        // Break out direction vector.
        var dx = direction[0];
        var dy = direction[1];
        var dz = direction[2];

        // Direction to increment x,y,z when stepping.
        var stepX = Signum(dx);
        var stepY = Signum(dy);
        var stepZ = Signum(dz);

        // See description above. The initial values depend on the fractional
        // part of the origin.
        var tMaxX = Intbound(origin[0], dx);
        var tMaxY = Intbound(origin[1], dy);
        var tMaxZ = Intbound(origin[2], dz);

        // The change in t when taking a step (always positive).
        var tDeltaX = stepX / dx;
        var tDeltaY = stepY / dy;
        var tDeltaZ = stepZ / dz;

        // Buffer for reporting faces to the callback.
        var face = new Vector3Int();

        // Avoids an infinite loop.
        if (dx == 0 && dy == 0 && dz == 0)
            throw new Exception("Ray-cast in zero direction!");

        // Rescale from units of 1 cube-edge to units of 'direction' so we can
        // compare with 't'.
        radius /= Mathf.Sqrt(dx * dx + dy * dy + dz * dz);

        // Deal with world bounds or their absence.
        Vector3Int min, max;
        min = max = default;
        var worldIsUnlimited = !worldBounds.HasValue;
        if (!worldIsUnlimited)
        {
            min = worldBounds.Value.min;
            max = worldBounds.Value.max;
        }

        while (worldIsUnlimited || (
            /* ray has not gone past bounds of world */
            (stepX > 0 ? x < max.x : x >= min.x) &&
            (stepY > 0 ? y < max.y : y >= min.y) &&
            (stepZ > 0 ? z < max.z : z >= min.z)))
        {
            // Invoke the callback, unless we are not *yet* within the bounds of the
            // world.
            if (worldIsUnlimited ||
                !(x < min.x || y < min.y || z < min.z || x >= max.x || y >= max.y || z >= max.z))
                if (callback(new Vector3Int(x, y, z), face))
                    break;

            // tMaxX stores the t-value at which we cross a cube boundary along the
            // X axis, and similarly for Y and Z. Therefore, choosing the least tMax
            // chooses the closest cube boundary. Only the first case of the four
            // has been commented in detail.
            if (tMaxX < tMaxY)
            {
                if (tMaxX < tMaxZ)
                {
                    if (tMaxX > radius)
                        break;
                    // Update which cube we are now in.
                    x += stepX;
                    // Adjust tMaxX to the next X-oriented boundary crossing.
                    tMaxX += tDeltaX;
                    // Record the normal vector of the cube face we entered.
                    face[0] = -stepX;
                    face[1] = 0;
                    face[2] = 0;
                }
                else
                {
                    if (tMaxZ > radius)
                        break;
                    z += stepZ;
                    tMaxZ += tDeltaZ;
                    face[0] = 0;
                    face[1] = 0;
                    face[2] = -stepZ;
                }
            }
            else
            {
                if (tMaxY < tMaxZ)
                {
                    if (tMaxY > radius)
                        break;
                    y += stepY;
                    tMaxY += tDeltaY;
                    face[0] = 0;
                    face[1] = -stepY;
                    face[2] = 0;
                }
                else
                {
                    // Identical to the second case, repeated for simplicity in
                    // the conditionals.
                    if (tMaxZ > radius)
                        break;
                    z += stepZ;
                    tMaxZ += tDeltaZ;
                    face[0] = 0;
                    face[1] = 0;
                    face[2] = -stepZ;
                }
            }
        }
    }

    private static float Intbound(float s, float ds)
    {
        // Some kind of edge case, see:
        // http://gamedev.stackexchange.com/questions/47362/cast-ray-to-select-block-in-voxel-game#comment160436_49423
        var sIsInteger = Mathf.Round(s) == s;
        if (ds < 0 && sIsInteger)
            return 0;

        return (ds > 0 ? Ceil(s) - s : s - Mathf.Floor(s)) / Mathf.Abs(ds);
    }

    private static float Ceil(float s) => s != 0f ? Mathf.Ceil(s) : 1f;

    private static int Signum(float x) => x > 0 ? 1 : x < 0 ? -1 : 0;

}
