
using System.IO;
using Unity.Mathematics;
using UnityEngine;

public static class PathHelper
{
    public static readonly string WorldsPath = Path.Combine(Application.persistentDataPath, "Worlds");
    public static string GetWorldDataPath(string dirName) => Path.Combine(dirName, "WorldData.json");
    public static string GetChunkPath(string savesDir, string chunkName) => Path.Combine(savesDir, $"{chunkName}.chunk");

    public static string GenerateChunkName(int3 chunkPos) => $"Chunk-{chunkPos.x},{chunkPos.y},{chunkPos.z}";
    public static string GenerateChunkName(Vector3Int chunkPos) => $"Chunk-{chunkPos.x},{chunkPos.y},{chunkPos.z}";
}