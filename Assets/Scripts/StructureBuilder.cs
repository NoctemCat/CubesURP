

using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

using static CubesUtils;

public class StructureBuilder
{
    private readonly World World;
    //private readonly Dictionary<Vector3Int, Chunk> _chunks;
    NativeList<StructureMarker> Structures;
    JobHandle GeneratingStructures;

    NativeParallelHashSet<int3> keys;
    NativeParallelMultiHashMap<int3, VoxelMod> mods;
    Dictionary<Vector3Int, List<VoxelMod>> tempDict;

    public StructureBuilder(World world)
    {
        World = world;
        //_chunks = chunks;
        Structures = new NativeList<StructureMarker>(10000, Allocator.Persistent);

        keys = new(512, Allocator.Persistent);
        mods = new(4096 * 3, Allocator.Persistent);
        tempDict = new(100);

        CheckStructures().Forget();
    }

    ~StructureBuilder()
    {
        Structures.Dispose();
        keys.Dispose();
        mods.Dispose();
    }

    public async UniTask AddStructures(NativeList<StructureMarker> structures)
    {
        //Debug.Log("add structures");
        await GeneratingStructures;
        GeneratingStructures.Complete();

        //GeneratingStructures.Complete();
        Structures.AddRange(structures.AsArray());
    }

    async UniTaskVoid CheckStructures()
    {
        while (true)
        {
            await UniTask.WaitForSeconds(1);
            SortStructures().Forget();
        }
    }

    async UniTaskVoid SortStructures()
    {
        await GeneratingStructures;
        GeneratingStructures.Complete();
        if (Structures.IsEmpty) return;

        keys.Clear();
        mods.Clear();

        GenerateStructuresJob structuresJob = new()
        {
            Data = World.VoxelData,
            Biomes = World.Biomes,
            Structures = Structures.AsArray(),

            Keys = keys.AsParallelWriter(),
            Modifications = mods.AsParallelWriter(),
        };
        GeneratingStructures = structuresJob.Schedule(Structures.Length, 8, GeneratingStructures);

        await GeneratingStructures;
        GeneratingStructures.Complete();
        Structures.Clear();

        tempDict.Clear();
        tempDict.EnsureCapacity(mods.Count());

        await UniTask.SwitchToThreadPool();
        foreach (int3 key in keys)
        {
            var values = mods.GetValuesForKey(key);
            using var enumerator = values.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Vector3Int vKey = I3ToVI3(key);
                if (!tempDict.ContainsKey(vKey))
                    tempDict[vKey] = new(50);

                tempDict[vKey].Add(enumerator.Current);
            }
        }
        await UniTask.SwitchToMainThread();


        AddSortedStructures();
    }

    public void AddSortedStructures()
    {
        foreach (Vector3Int key in tempDict.Keys)
        {
            if (World.Chunks.TryGetValue(key, out Chunk chunk))
            {
                chunk.AddRangeModification(tempDict[key]).Forget();
            }
            else
            {
                World.Chunks[key] = new(key);
                World.Chunks[key].AddRangeModification(tempDict[key]).Forget();
            }
        }
    }
}


[BurstCompile]
public struct GenerateStructuresJob : IJobParallelFor
{
    [ReadOnly]
    public VoxelData Data;
    [ReadOnly]
    public NativeArray<BiomeStruct> Biomes;
    [ReadOnly]
    public NativeArray<StructureMarker> Structures;

    public NativeParallelHashSet<int3>.ParallelWriter Keys;
    [WriteOnly]
    public NativeParallelMultiHashMap<int3, VoxelMod>.ParallelWriter Modifications;

    public void Execute(int i)
    {
        var biome = Biomes[Structures[i].BiomeIndex];
        switch (Structures[i].Type)
        {
            case StructureType.Tree:
                MakeTree(Structures[i].Position, biome.MinHeight, biome.MaxHeight);
                break;
            case StructureType.Cactus:
                MakeCacti(Structures[i].Position, biome.MinHeight, biome.MaxHeight);
                break;
        }

    }

    private void MakeTree(int3 pos, int minTrunkHeight, int maxTrunkHeight)
    {
        NativeList<int3> list = new(20, Allocator.Temp);

        int height = (int)(maxTrunkHeight * Get2DPerlin(Data, new(pos.x, pos.z), 2000f, 3f));

        if (height < minTrunkHeight)
        {
            height = minTrunkHeight;
        }

        // Trunk
        for (int i = 1; i < height; i++)
        {
            Add(ref list, new(pos.x, pos.y + i, pos.z), Block.Wood);
        }

        // Leaves
        for (int x = -2; x < 3; x++)
        {
            for (int z = -2; z < 3; z++)
            {
                Add(ref list, new(pos.x + x, pos.y + height - 2, pos.z + z), Block.Leaves);
                Add(ref list, new(pos.x + x, pos.y + height - 3, pos.z + z), Block.Leaves);
            }
        }

        for (int x = -1; x < 2; x++)
        {
            for (int z = -1; z < 2; z++)
            {
                Add(ref list, new(pos.x + x, pos.y + height - 1, pos.z + z), Block.Leaves);
            }
        }
        for (int x = -1; x < 2; x++)
        {
            if (x == 0)
                for (int z = -1; z < 2; z++)
                {
                    Add(ref list, new(pos.x + x, pos.y + height, pos.z + z), Block.Leaves);
                }
            else
                Add(ref list, new(pos.x + x, pos.y + height, pos.z), Block.Leaves);
        }
    }

    private void MakeCacti(int3 pos, int minTrunkHeight, int maxTrunkHeight)
    {
        NativeList<int3> list = new(20, Allocator.Temp);

        int height = (int)(maxTrunkHeight * Get2DPerlin(Data, new(pos.x, pos.z), 1246f, 2f));

        if (height < minTrunkHeight)
        {
            height = minTrunkHeight;
        }

        // Trunk
        for (int i = 1; i < height; i++)
        {
            Add(ref list, new(pos.x, pos.y + i, pos.z), Block.Cactus);
        }
    }


    private void Add(ref NativeList<int3> list, int3 pos, Block block)
    {
        if (!list.Contains(pos))
        {
            int3 cPos = ToChunkCoord(pos);
            Modifications.Add(cPos, new VoxelMod(GetPosInChunk(cPos, pos), block));
            Keys.Add(cPos);
            list.Add(pos);
        }
    }

    private readonly int3 ToChunkCoord(int3 pos) => (int3)math.floor(pos / (float3)Data.ChunkDimensions);
    private readonly int3 GetPosInChunk(int3 chunkPos, int3 worldPos) => worldPos - (chunkPos * Data.ChunkDimensions);
}
