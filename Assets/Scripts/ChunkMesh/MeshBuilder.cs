using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

static class MeshBuilder
{

    //public static JobHandle GenerateMesh(
    //        World world,
    //        int3 chunkPos,
    //        in NativeArray<Block> voxelMap,
    //        //ChunkNeighbours neighbours,
    //        in NativeList<Vertex> vertices,
    //        in NativeList<uint> solidIndices,
    //        in NativeList<uint> transparentIndices,
    //        in NativeArray<int2> verticesRanges,
    //        JobHandle dependsOn = default
    //    )
    //{
    //    NativeArray<int> totals = new(2, Allocator.TempJob);
    //    NativeArray<int> counters = new(JobsUtility.MaxJobThreadCount * JobsUtility.CacheLineSize, Allocator.TempJob);

    //    CountBlockTypesJob countBlockTypes = new()
    //    {
    //        Blocks = world.Blocks,
    //        VoxelMap = voxelMap,
    //        Counters = counters,
    //    };
    //    JobHandle countHandle = countBlockTypes.Schedule(voxelMap.Length, 1, dependsOn);

    //    SumBlockTypesJob sumBlockJob = new()
    //    {
    //        Counters = counters,
    //        Totals = totals,
    //    };
    //    JobHandle sumHandle = sumBlockJob.Schedule(countHandle);

    //    counters.Dispose(sumHandle);

    //    NativeList<VoxelDataForMesh> allFaces = new(1, Allocator.TempJob);
    //    NativeList<VoxelDataForMesh> solidFaces = new(1, Allocator.TempJob);
    //    NativeList<VoxelDataForMesh> transparentFaces = new(1, Allocator.TempJob);
    //    NativeReference<int> solidOffset = new(Allocator.TempJob);

    //    InitFaceArrays initFaceArrays = new()
    //    {
    //        Types = totals,

    //        AllFaces = allFaces,
    //        SolidFaces = solidFaces,
    //        TransparentFaces = transparentFaces,
    //    };
    //    JobHandle initArrays = initFaceArrays.Schedule(sumHandle);

    //    SortVoxelFacesJob sortVoxelsJob = new()
    //    {
    //        Data = world.VoxelData,
    //        //ChunkVoxelMapPtrs = world.chunkVoxelMapPtrs.AsReadOnly(),
    //        //dummyMap = world.dummyMap,
    //        ChunkPos = chunkPos,
    //        VoxelMap = voxelMap,
    //        Blocks = world.Blocks,
    //        XYZMap = world.XYZMap,

    //        SolidFaces = solidFaces.AsParallelWriter(),
    //        TransparentFaces = transparentFaces.AsParallelWriter(),
    //    };
    //    JobHandle sortHandle = sortVoxelsJob.Schedule(VoxelDataStatic.ChunkSize, VoxelDataStatic.ChunkSize / 8, initArrays);

    //    ResizeVoxelArraysJob resizeJob = new()
    //    {
    //        SolidFaces = solidFaces,
    //        TransparentFaces = transparentFaces,
    //        AllFaces = allFaces,

    //        Vertices = vertices,
    //        SolidIndices = solidIndices,
    //        TransparentIndices = transparentIndices,
    //        SolidOffset = solidOffset,
    //        VerticesRanges = verticesRanges,
    //    };
    //    JobHandle resizeHandle = resizeJob.Schedule(sortHandle);

    //    FillVerticesJob fillVerticesJob = new()
    //    {
    //        Data = world.VoxelData,
    //        AllFaces = allFaces.AsDeferredJobArray(),
    //        Vertices = vertices.AsDeferredJobArray(),
    //    };
    //    JobHandle fillVerticesHandle = fillVerticesJob.Schedule(allFaces, 8, resizeHandle);

    //    FillSolidIndicesJob fillSolidJob = new()
    //    {
    //        SolidFaces = solidFaces.AsDeferredJobArray(),
    //        SolidIndices = solidIndices.AsDeferredJobArray(),
    //    };
    //    JobHandle fillSolidHandle = fillSolidJob.Schedule(solidFaces, 8, resizeHandle);

    //    FillTransparentIndicesJob fillTransparentJob = new()
    //    {
    //        SolidOffset = solidOffset,
    //        TransparentFaces = transparentFaces.AsDeferredJobArray(),
    //        TransparentIndices = transparentIndices.AsDeferredJobArray(),
    //    };
    //    JobHandle fillTransparentHandle = fillTransparentJob.Schedule(transparentFaces, 8, resizeHandle);

    //    JobHandle all = JobHandle.CombineDependencies(fillVerticesHandle, fillSolidHandle, fillTransparentHandle);

    //    allFaces.Dispose(all);
    //    solidFaces.Dispose(all);
    //    transparentFaces.Dispose(all);
    //    solidOffset.Dispose(all);
    //    totals.Dispose(all);

    //    return all;
    //}

    [BurstCompile]
    public struct CountBlockTypesJob : IJobParallelFor
    {
        [NativeSetThreadIndex]
        private readonly int ThreadIndex;
        [ReadOnly]
        public NativeArray<BlockStruct> Blocks;
        [ReadOnly]
        [NativeDisableContainerSafetyRestriction]
        public NativeArray<Block> VoxelMap;

        [NativeDisableParallelForRestriction]
        public NativeArray<int> Counters;

        // 0 = isSolid
        // 1 = isTransparent
        // 2 = Non Empty blocks
        public void Execute(int i)
        {
            //ref var blocksVal = ref BlocksRef.Blocks.Value;
            //ref var blocks = ref blocksVal.BlocksArray;

            int threadOffset = ThreadIndex * JobsUtility.CacheLineSize;
            BlockStruct block = Blocks[(int)VoxelMap[i]];

            if (block.isTransparent && block.isSolid)
            {
                Counters[threadOffset + 1]++;
            }
            if (block.isSolid)
            {
                Counters[threadOffset]++;
            }
            if (VoxelMap[i] != Block.Air)
            {
                Counters[threadOffset + 2]++;
            }
        }
    }

    [BurstCompile]
    public struct SumBlockTypesJob : IJob
    {
        [ReadOnly] public NativeArray<int> Counters;
        public NativeArray<int> Totals;

        // 0 = isSolid
        // 1 = isTransparent
        public void Execute()
        {
            Totals[0] = 0;
            Totals[1] = 0;
            for (var i = 0; i < JobsUtility.MaxJobThreadCount; i++)
            {
                int threadOffset = i * JobsUtility.CacheLineSize;
                Totals[0] += Counters[threadOffset];
                Totals[1] += Counters[threadOffset + 1];
            }
        }
    }

    [BurstCompile]
    public struct InitFaceArrays : IJob
    {
        [ReadOnly] public NativeArray<int> Types;

        public NativeList<VoxelDataForMesh> AllFaces;
        public NativeList<VoxelDataForMesh> SolidFaces;
        public NativeList<VoxelDataForMesh> TransparentFaces;

        // [0] = solids
        // [1] = transparents
        public void Execute()
        {
            SolidFaces.Capacity = Types[0] * 6;
            TransparentFaces.Capacity = Types[1] * 6;
            AllFaces.Capacity = Types[0] + Types[1] * 6;
        }
    }

    [BurstCompile]
    public struct SortVoxelFacesJob : IJobParallelForBatch
    {
        [ReadOnly]
        public VoxelData Data;
        //[ReadOnly]
        //public NativeParallelHashMap<uint, IntPtr> ChunkVoxelMapPtrs;
        // TODO Return neighbours
        //[NativeDisableContainerSafetyRestriction]
        [ReadOnly]
        public Neighbours ChunkNeighbours;

        //[ReadOnly]
        //[NativeDisableUnsafePtrRestriction]
        //public IntPtr dummyMap;
        [ReadOnly]
        public int3 ChunkPos;
        [ReadOnly]
        public NativeArray<BlockStruct> Blocks;
        [ReadOnly]
        public NativeArray<int3> XYZMap;
        [ReadOnly]
        //[NativeDisableContainerSafetyRestriction]
        public NativeArray<Block> VoxelMap;

        [WriteOnly]
        public NativeList<VoxelDataForMesh>.ParallelWriter SolidFaces;
        [WriteOnly]
        public NativeList<VoxelDataForMesh>.ParallelWriter TransparentFaces;

        public void Execute(int startIndex, int count)
        {
            //FillNeighbours();

            int end = startIndex + count;

            for (int i = startIndex; i < end; i++)
            {
                BlockStruct current = Blocks[(int)VoxelMap[i]];
                int3 pos = XYZMap[i];

                if (!current.isSolid)
                {
                    continue;
                }

                for (VoxelFaces f = 0; f < VoxelFaces.Max; f++)
                {
                    int fi = (int)f;
                    int3 neighbourPos = pos + Data.FaceChecks[fi];

                    //bool neighbourTransparent = CheckVoxelTransparent(neighbourPos, f);
                    Block neighbour = GetNeighbour(neighbourPos, f);

                    // These faces will be drawn

                    if (!Blocks[(int)neighbour].isSolid || !current.isSolid
                        || (current.isSolid && Blocks[(int)neighbour].isTransparent)
                    )
                    {
                        SolidFaces.AddNoResize(new(pos, f, current));
                    }

                    //if (!Blocks[(int)neighbour].isSolid || !current.isSolid)
                    //{
                    //    if (!current.isTransparent)
                    //        SolidFaces.AddNoResize(new(pos, f, current));
                    //    else
                    //        TransparentFaces.AddNoResize(new(pos, f, current));
                    //}
                }
            }
        }

        bool CheckVoxelTransparent(int3 pos, VoxelFaces face)
        {

            if (!IsVoxelInChunk(pos))
            {
                NativeArray<Block> voxelMap;
                switch (face)
                {
                    case VoxelFaces.Back:
                        pos.z = Data.ChunkWidth - 1;
                        voxelMap = ChunkNeighbours.Back;
                        break;
                    case VoxelFaces.Front:
                        pos.z = 0;
                        voxelMap = ChunkNeighbours.Front;
                        break;
                    case VoxelFaces.Top:
                        pos.y = 0;
                        voxelMap = ChunkNeighbours.Top;
                        break;
                    case VoxelFaces.Bottom:
                        pos.y = Data.ChunkHeight - 1;
                        voxelMap = ChunkNeighbours.Bottom;
                        break;
                    case VoxelFaces.Left:
                        pos.x = Data.ChunkLength - 1;
                        voxelMap = ChunkNeighbours.Left;
                        break;
                    case VoxelFaces.Right:
                        pos.x = 0;
                        voxelMap = ChunkNeighbours.Right;
                        break;
                    case VoxelFaces.Max:
                    default:
                        return false;
                }

                return Blocks[(int)voxelMap[CalcIndex(pos)]].isTransparent;
            }

            return Blocks[(int)VoxelMap[CalcIndex(pos)]].isTransparent;
        }

        Block GetNeighbour(int3 pos, VoxelFaces face)
        {
            if (!IsVoxelInChunk(pos))
            {
                NativeArray<Block> voxelMap;
                switch (face)
                {
                    case VoxelFaces.Back:
                        pos.z = Data.ChunkWidth - 1;
                        voxelMap = ChunkNeighbours.Back;
                        break;
                    case VoxelFaces.Front:
                        pos.z = 0;
                        voxelMap = ChunkNeighbours.Front;
                        break;
                    case VoxelFaces.Top:
                        pos.y = 0;
                        voxelMap = ChunkNeighbours.Top;
                        break;
                    case VoxelFaces.Bottom:
                        pos.y = Data.ChunkHeight - 1;
                        voxelMap = ChunkNeighbours.Bottom;
                        break;
                    case VoxelFaces.Left:
                        pos.x = Data.ChunkLength - 1;
                        voxelMap = ChunkNeighbours.Left;
                        break;
                    case VoxelFaces.Right:
                        pos.x = 0;
                        voxelMap = ChunkNeighbours.Right;
                        break;
                    case VoxelFaces.Max:
                    default:
                        return Block.Air;
                }

                return voxelMap[CalcIndex(pos)];
                //return voxelMap.GetAt
            }

            return VoxelMap[CalcIndex(pos)];
        }


        readonly int CalcIndex(int3 xyz) => xyz.x * Data.ChunkHeight * Data.ChunkLength + xyz.y * Data.ChunkLength + xyz.z;

        readonly bool IsVoxelInChunk(int3 pos)
        {
            return pos.x >= 0 && pos.x < Data.ChunkWidth &&
                pos.y >= 0 && pos.y < Data.ChunkHeight &&
                pos.z >= 0 && pos.z < Data.ChunkLength;
        }

    }

    [BurstCompile]
    public struct ResizeVoxelArraysJob : IJob
    {
        [ReadOnly]
        public NativeList<VoxelDataForMesh> SolidFaces;
        [ReadOnly]
        public NativeList<VoxelDataForMesh> TransparentFaces;

        public NativeList<VoxelDataForMesh> AllFaces;

        public NativeList<Vertex> Vertices;
        public NativeList<uint> SolidIndices;
        public NativeList<uint> TransparentIndices;

        public NativeReference<int> SolidOffset;
        public NativeArray<int2> VerticesRanges;

        public void Execute()
        {
            int allFacesCount = SolidFaces.Length + TransparentFaces.Length;

            AllFaces.CopyFrom(SolidFaces);
            AllFaces.AddRangeNoResize(TransparentFaces);

            Vertices.ResizeUninitialized(allFacesCount * 4);
            SolidIndices.ResizeUninitialized(SolidFaces.Length * 6);
            TransparentIndices.ResizeUninitialized(TransparentFaces.Length * 6);

            SolidOffset.Value = SolidFaces.Length * 4;

            int2 minmaxSolid = new()
            {
                x = 0,
                y = (SolidFaces.Length - 1) * 4 + 3
            };
            int2 minmaxTransparent = new()
            {
                x = SolidOffset.Value,
                y = SolidOffset.Value + (TransparentFaces.Length - 1) * 4 + 3
            };

            VerticesRanges[0] = minmaxSolid;
            VerticesRanges[1] = minmaxTransparent;
        }
    }

    [BurstCompile]
    public struct FillVerticesJob : IJobParallelFor
    {
        [ReadOnly]
        public VoxelData Data;
        [ReadOnly]
        public NativeArray<VoxelDataForMesh> AllFaces;

        [WriteOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<Vertex> Vertices;

        public void Execute(int i)
        {
            float3 pos = AllFaces[i].Pos;
            int f = (int)AllFaces[i].Face;
            BlockStruct block = AllFaces[i].Block;

            float3 dot0 = pos + Data.VoxelVerts[Data.VoxelTris[f].x];
            float3 dot1 = pos + Data.VoxelVerts[Data.VoxelTris[f].y];
            float3 dot2 = pos + Data.VoxelVerts[Data.VoxelTris[f].z];
            float3 dot3 = pos + Data.VoxelVerts[Data.VoxelTris[f].w];

            float3 normal = Data.VoxelNormals[f];
            float2x4 uvs = AddTexture(block.GetTextureID(f));

            Vertices[4 * i + 0] = new Vertex() { Pos = dot0, Nor = normal, UV = uvs.c0 };
            Vertices[4 * i + 1] = new Vertex() { Pos = dot1, Nor = normal, UV = uvs.c1 };
            Vertices[4 * i + 2] = new Vertex() { Pos = dot2, Nor = normal, UV = uvs.c2 };
            Vertices[4 * i + 3] = new Vertex() { Pos = dot3, Nor = normal, UV = uvs.c3 };
        }

        private readonly float2x4 AddTexture(int textureID)
        {
            float x = textureID % Data.TextureAtlasSizeInBlocks * Data.NormalizedBlockTextureSize;
            float y = textureID / Data.TextureAtlasSizeInBlocks * Data.NormalizedBlockTextureSize;

            y = 1f - y - Data.NormalizedBlockTextureSize;

            return new float2x4(
                new(x, y),
                new(x, y + Data.NormalizedBlockTextureSize),
                new(x + Data.NormalizedBlockTextureSize, y),
                new(x + Data.NormalizedBlockTextureSize, y + Data.NormalizedBlockTextureSize)
            );
        }
    }

    [BurstCompile]
    public struct FillSolidIndicesJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<VoxelDataForMesh> SolidFaces;

        [WriteOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<uint> SolidIndices;

        // i for each face
        public void Execute(int i)
        {

            SolidIndices[i * 6 + 0] = (uint)((i * 4) + 0);
            SolidIndices[i * 6 + 1] = (uint)((i * 4) + 1);
            SolidIndices[i * 6 + 2] = (uint)((i * 4) + 2);
            SolidIndices[i * 6 + 3] = (uint)((i * 4) + 2);
            SolidIndices[i * 6 + 4] = (uint)((i * 4) + 1);
            SolidIndices[i * 6 + 5] = (uint)((i * 4) + 3);
        }
    }

    [BurstCompile]
    public struct FillTransparentIndicesJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeReference<int> SolidOffset;
        [ReadOnly]
        public NativeArray<VoxelDataForMesh> TransparentFaces;

        [WriteOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<uint> TransparentIndices;

        // i for each face
        public void Execute(int i)
        {
            TransparentIndices[i * 6 + 0] = (uint)(SolidOffset.Value + (i * 4) + 0);
            TransparentIndices[i * 6 + 1] = (uint)(SolidOffset.Value + (i * 4) + 1);
            TransparentIndices[i * 6 + 2] = (uint)(SolidOffset.Value + (i * 4) + 2);
            TransparentIndices[i * 6 + 3] = (uint)(SolidOffset.Value + (i * 4) + 2);
            TransparentIndices[i * 6 + 4] = (uint)(SolidOffset.Value + (i * 4) + 1);
            TransparentIndices[i * 6 + 5] = (uint)(SolidOffset.Value + (i * 4) + 3);
        }
    }

    // SolidIndices / 2
    public struct CalculateNormalsJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<uint> SolidIndices;
        [NativeDisableParallelForRestriction]
        public NativeArray<Vertex> Vertices;

        public void Execute(int index)
        {
            int i = index * 3;
            int indexA = (int)SolidIndices[i];
            int indexB = (int)SolidIndices[i + 1];
            int indexC = (int)SolidIndices[i + 2];

            var vertexA = Vertices[indexA];
            var vertexB = Vertices[indexB];
            var vertexC = Vertices[indexC];

            float3 pointA = vertexA.Pos;
            float3 pointB = vertexB.Pos;
            float3 pointC = vertexC.Pos;

            float3 sideAB = pointB - pointA;
            float3 sideAC = pointC - pointA;

            float3 triangleNormal = math.normalize(math.cross(sideAB, sideAC));

            vertexA.Nor += triangleNormal;
            vertexB.Nor += triangleNormal;
            vertexC.Nor += triangleNormal;

            Vertices[indexA] = vertexA;
            Vertices[indexB] = vertexB;
            Vertices[indexC] = vertexC;
        }
    }

    public struct NormalizeNormalsJob : IJobParallelForDefer
    {
        public NativeArray<Vertex> Vertices;

        public void Execute(int i)
        {
            var vertex = Vertices[i];
            vertex.Nor = math.normalize(vertex.Nor);
            Vertices[i] = vertex;
        }
    }
}

