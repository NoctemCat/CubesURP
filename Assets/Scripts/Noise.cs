

using Unity.Mathematics;

public static class Noise
{
    public static float Get2DPerlin(VoxelData data, float2 position, float offset, float scale)
    {
        return noise.cnoise(new float2(
            (position.x + 0.1f) / data.ChunkWidth * scale + offset + data.RandomXYZ.x,
            (position.y + 0.1f) / data.ChunkLength * scale + offset + data.RandomXYZ.y)
        );
    }

    public static float Get3DPerlin(VoxelData data, float3 position, float offset, float scale)
    {
        return noise.cnoise(new float3(
            (position.x + 0.1f) / data.ChunkWidth * scale + offset + data.RandomXYZ.x,
            (position.y + 0.1f) / data.ChunkHeight * scale + offset + data.RandomXYZ.y,
            (position.z + 0.1f) / data.ChunkLength * scale + offset + data.RandomXYZ.z)
        );
    }
}