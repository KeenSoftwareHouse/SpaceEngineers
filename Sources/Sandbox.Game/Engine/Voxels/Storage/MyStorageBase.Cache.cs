using System;
using System.Collections.Generic;
using System.Diagnostics;
using Sandbox.Engine.Voxels.Storage;
using Sandbox.Game.World;
using VRage;
using VRage.Collections;
using VRage.Voxels;
using VRageMath;

namespace Sandbox.Engine.Voxels
{
    public abstract partial class MyStorageBase
    {
        public class VoxelChunk
        {
            public const int SizeBits = 3;
            public const int Size = 1 << SizeBits;
            public const int Volume = Size * Size * Size;

            public readonly Vector3I Coords;

            public byte MaxLod { get; internal set;}

            // Volume with lod data
            public static readonly int TotalVolume = Volume
                                                    + (Volume >> 3)
                                                    + (Volume >> 6)
                                                    + (Volume >> 9)
                                                    + (Volume >> 1)
                                                    + (Volume >> 15)
                                                    + (Volume >> 18)
                                                    + (Volume >> 21)
                                                    + (Volume >> 24); // Future proof :P

            public static readonly Vector3I SizeVector = new Vector3I(Size);

            public static readonly Vector3I MaxVector = new Vector3I(Size - 1);

            public byte[] Material;
            public byte[] Content;

            public MyStorageDataTypeFlags Dirty;

            public MyStorageDataTypeFlags Cached;

            public int HitCount;

            internal int TreeProxy;

            public FastResourceLock Lock = new FastResourceLock();

            public VoxelChunk(Vector3I coords)
            {
                Coords = coords;

                Material = new byte[TotalVolume];
                Content = new byte[TotalVolume];
            }

            public unsafe void UpdateLodData(int lod)
            {
                Debug.Assert(lod <= SizeBits);

                for (int i = MaxLod + 1; i <= lod; ++i)
                {
                    UpdateLodDataInternal(i, Content, MyOctreeNode.ContentFilter);
                    UpdateLodDataInternal(i, Material, MyOctreeNode.MaterialFilter);
                }

                MaxLod = (byte)lod;
            }

            private static unsafe void UpdateLodDataInternal(int lod, byte[] dataArray, MyOctreeNode.FilterFunction filter)
            {
                int offset = 0;
                for (int i = 0; i < lod - 1; ++i)
                {
                    offset += Volume >> (i + i + i);
                }

                var sx = Size >> lod;
                var sy = sx * sx;
                var sz = sy * sx;

                var psx = Size >> (lod - 1);
                var psy = psx * psx;
                var psz = psy * psx;

                ulong dataBit;

                byte* data = (byte*)&dataBit;
                fixed (byte* fixedDataArray = dataArray)
                {
                    byte* voxel = fixedDataArray + offset;

                    byte* store = voxel + psz;

                    for (int z = 0; z < sz; z += sy)
                    {
                        int z0 = z << 3, z1 = (z << 3) + psy;

                        for (int y = 0; y < sy; y += sx)
                        {
                            int y0 = y << 2, y1 = (y << 2) + psx;
                            for (int x = 0; x < sx; ++x)
                            {
                                // precompute corner indices, moar readable
                                int x0 = x << 1, x1 = (x << 1) + 1;

                                data[0] = voxel[z0 + y0 + x0];
                                data[1] = voxel[z0 + y0 + x1];
                                data[2] = voxel[z0 + y1 + x0];
                                data[3] = voxel[z0 + y1 + x1];
                                data[4] = voxel[z1 + y0 + x0];
                                data[5] = voxel[z1 + y0 + x1];
                                data[6] = voxel[z1 + y1 + x0];
                                data[7] = voxel[z1 + y1 + x1];

                                store[x + y + z] = filter(data, lod);
                            }
                        }
                    }
                }
            }

            public MyStorageData MakeData()
            {
                return new MyStorageData(SizeVector, Content, Material);
            }

            #region Read

            public void ReadLod(MyStorageData target, MyStorageDataTypeFlags dataTypes, ref Vector3I targetOffset, int lodIndex, ref Vector3I min, ref Vector3I max)
            {
                Debug.Assert(min.IsInsideInclusive(Vector3I.Zero, MaxVector >> lodIndex)
                    && max.IsInsideInclusive(Vector3I.Zero, MaxVector >> lodIndex));

                //using (Lock.AcquireSharedUsing())
                {
                    if (lodIndex > MaxLod)
                        UpdateLodData(lodIndex);

                    if (dataTypes.Requests(MyStorageDataTypeEnum.Content))
                    {
                        ReadLod(target, MyStorageDataTypeEnum.Content, Content, targetOffset, lodIndex, min, max);
                    }

                    if (dataTypes.Requests(MyStorageDataTypeEnum.Material))
                    {
                        ReadLod(target, MyStorageDataTypeEnum.Material, Material, targetOffset, lodIndex, min, max);
                    }
                }

                HitCount++;
            }

            private unsafe void ReadLod(MyStorageData target, MyStorageDataTypeEnum dataType, byte[] dataArray, Vector3I tofft, int lod, Vector3I min, Vector3I max)
            {
                int offset = 0;
                for (int i = 0; i < lod; ++i)
                {
                    offset += Volume >> (i + i + i);
                }

                var sy = Size >> lod;
                var sz = sy * sy;

                min.Y *= sy;
                min.Z *= sz;

                max.Y *= sy;
                max.Z *= sz;

                int tsx = target.StepX, tsy = target.StepY, tsz = target.StepZ;

                tofft.Y *= tsy;
                tofft.Z *= tsz;

                fixed (byte* fixedDataArray = dataArray)
                fixed (byte* targetData = target[dataType])
                {
                    byte* voxel = fixedDataArray + offset;

                    int x, y, z;
                    int tx, ty, tz;

                    for (z = min.Z, tz = tofft.Z; z <= max.Z; z += sz, tz += tsz)
                        for (y = min.Y, ty = tofft.Y; y <= max.Y; y += sy, ty += tsy)
                            for (x = min.X, tx = tofft.X; x <= max.X; ++x, tx += tsx)
                            {
                                targetData[tz + ty + tx] = voxel[z + y + x];
                            }
                }
            }

            #endregion

            #region Write

            public void Write(MyStorageData source, MyStorageDataTypeFlags dataTypes, ref Vector3I targetOffset, ref Vector3I min, ref Vector3I max)
            {
                //using (Lock.AcquireExclusiveUsing())
                {
                    //if (false) // disabled for testing :D
                    if (dataTypes.Requests(MyStorageDataTypeEnum.Content))
                    {
                        Write(source, MyStorageDataTypeEnum.Content, Content, targetOffset, min, max);
                    }

                    if (dataTypes.Requests(MyStorageDataTypeEnum.Material))
                    {
                        Write(source, MyStorageDataTypeEnum.Material, Material, targetOffset, min, max);
                    }

                    Cached |= dataTypes;

                    Dirty |= dataTypes;
                    MaxLod = 0;
                }
            }

            private unsafe void Write(MyStorageData source, MyStorageDataTypeEnum dataType, byte[] dataArray, Vector3I tofft, Vector3I min, Vector3I max)
            {
                Debug.Assert(Cached.Requests(dataType) || (min == Vector3I.Zero && max == MaxVector));

                var sy = Size;
                var sz = sy * sy;

                min.Y *= sy;
                min.Z *= sz;

                max.Y *= sy;
                max.Z *= sz;

                int tsx = source.StepX, tsy = source.StepY, tsz = source.StepZ;

                tofft.Y *= tsy;
                tofft.Z *= tsz;

                fixed (byte* voxel = dataArray)
                fixed (byte* sourceData = source[dataType])
                {
                    int x, y, z;
                    int tx, ty, tz;

                    for (z = min.Z, tz = tofft.Z; z <= max.Z; z += sz, tz += tsz)
                        for (y = min.Y, ty = tofft.Y; y <= max.Y; y += sy, ty += tsy)
                            for (x = min.X, tx = tofft.X; x <= max.X; ++x, tx += tsx)
                            {
                                voxel[z + y + x] = sourceData[tz + ty + tx];
                            }
                }
            }

            #endregion
        }

        public struct WriteCacheStats
        {
            public int QueuedWrites;
            public int CachedChunks;
            public IEnumerable<KeyValuePair<Vector3I, VoxelChunk>> Chunks;
        }

        #region Tweakable Parameters

        private const int MaxChunksToDiscard = 10;

        private const int MaximumHitsForDiscard = 100;

        #endregion

        private MyConcurrentQueue<Vector3I> m_pendingChunksToWrite;

        private MyQueue<Vector3I> m_chunksbyAge;

        private MyConcurrentDictionary<Vector3I, VoxelChunk> m_cachedChunks;

        private MyDynamicAABBTree m_cacheMap;

        private FastResourceLock m_cacheLock;

        [ThreadStatic]
        private static List<VoxelChunk> m_tmpChunks;

        private bool m_writeCacheThrough;

        private const int WriteCacheCap = 1024; // After this many entries any new operations are write through. We hope that never happens.

        private const int MaxWriteJobWorkMillis = 6;

        private static MyVoxelOperationsSessionComponent OperationsComponent
        {
            get { return MySession.Static.GetComponent<MyVoxelOperationsSessionComponent>(); }
        }

        public bool CachedWrites
        {
            get { return m_cachedWrites && MyVoxelOperationsSessionComponent.EnableCache; }
            set { m_cachedWrites = value; }
        }

        public bool HasPendingWrites
        {
            get { return m_pendingChunksToWrite.Count > 0; }
        }

        public bool HasCachedChunks
        {
            get { return m_chunksbyAge.Count - m_pendingChunksToWrite.Count > 0; }
        }

        private bool m_cachedWrites = false;

        public void InitWriteCache(int prealloc = 128)
        {
            //Debug.Assert(m_cachedChunks == null, "Error: Cache already initialized"); disabled due to shared storages

            if (m_cachedChunks != null) return;

            if (OperationsComponent != null)
                CachedWrites = true;
            else
                return;

            m_cachedChunks = new MyConcurrentDictionary<Vector3I, VoxelChunk>(prealloc, Vector3I.Comparer);
            m_pendingChunksToWrite = new MyConcurrentQueue<Vector3I>(prealloc / 10);
            m_chunksbyAge = new MyQueue<Vector3I>(prealloc);

            m_cacheMap = new MyDynamicAABBTree(Vector3.Zero);

            m_cacheLock = new FastResourceLock();

            OperationsComponent.Add(this);
        }

        private void WriteChunk(ref Vector3I coords, VoxelChunk chunk)
        {
            // We assume that we are locked here.
            Debug.Assert(m_storageLock.Owned);

            var start = coords << VoxelChunk.SizeBits;

            var end = ((coords + 1) << VoxelChunk.SizeBits) - 1;

            MyStorageData storage = chunk.MakeData();
            WriteRangeInternal(storage, chunk.Dirty, ref start, ref end);

            chunk.Dirty = 0;
        }

        private void GetChunk(ref Vector3I coord, out VoxelChunk chunk, MyStorageDataTypeFlags required)
        {
            using (m_cacheLock.AcquireExclusiveUsing())
            {
                if (!m_cachedChunks.TryGetValue(coord, out chunk))
                {
                    chunk = new VoxelChunk(coord);

                    var rangeStart = coord << VoxelChunk.SizeBits;
                    var rangeEnd = ((coord + 1) << VoxelChunk.SizeBits) - 1;

                    if (required != 0)
                    {
                        using (m_storageLock.AcquireSharedUsing())
                            ReadDatForChunk(chunk, required);
                    }

                    m_chunksbyAge.Enqueue(coord);
                    m_cachedChunks.Add(coord, chunk);

                    var bb = new BoundingBox(rangeStart, rangeEnd);

                    chunk.TreeProxy = m_cacheMap.AddProxy(ref bb, chunk, 0);
                }
                else if ((chunk.Cached & required) != required)
                {
                    using (m_storageLock.AcquireSharedUsing())
                        ReadDatForChunk(chunk, required & ~chunk.Cached);
                }

            }
        }

        private void ReadDatForChunk(VoxelChunk chunk, MyStorageDataTypeFlags data)
        {
            var rangeStart = chunk.Coords << VoxelChunk.SizeBits;
            var rangeEnd = ((chunk.Coords + 1) << VoxelChunk.SizeBits) - 1;

            MyStorageData storage = chunk.MakeData();

            MyVoxelRequestFlags flags = 0;

            ReadRangeInternal(storage, ref Vector3I.Zero, data, 0, ref rangeStart, ref rangeEnd, ref flags);

            chunk.Cached |= data;
            chunk.MaxLod = 0;
        }

        private void DequeueDirtyChunk(out VoxelChunk chunk, out Vector3I coords)
        {
            coords = m_pendingChunksToWrite.Dequeue();
            m_cachedChunks.TryGetValue(coords, out chunk);

            Debug.Assert(chunk != null);
        }

        public bool CleanCachedChunks()
        {
            int count = m_chunksbyAge.Count;
            int discarded = 0;

            // This procedure is thread safe enough because even if a chunk
            // is discarded when in use the data is still valid.
            for (int i = 0; i < count && discarded < MaxChunksToDiscard; ++i)
            {
                var coord = m_chunksbyAge.Dequeue();

                var ck = m_cachedChunks[coord];

                // this allows us to skip locking on most chunks
                if (ck.Dirty == 0 && ck.HitCount <= MaximumHitsForDiscard)
                    using (ck.Lock.AcquireSharedUsing())
                    {
                        if (ck.Dirty == 0 && ck.HitCount <= MaximumHitsForDiscard)
                        {
                            using (m_cacheLock.AcquireExclusiveUsing())
                            {
                                m_cachedChunks.Remove(coord);
                                m_cacheMap.RemoveProxy(ck.TreeProxy);
                            }
                        }
                        else
                        {
                            // not removed, requeue, refresh age
                            m_chunksbyAge.Enqueue(coord);
                            ck.HitCount = 0; // reset hit count for next time.
                        }
                    }
                else
                {
                    m_chunksbyAge.Enqueue(coord);
                    ck.HitCount = 0; // reset hit count for next time.
                }
            }

            return m_cachedChunks.Count == 0;
        }

        public bool WritePending(bool force = false)
        {
            Stopwatch s = Stopwatch.StartNew();

            while ((s.ElapsedMilliseconds < MaxWriteJobWorkMillis || force) && m_pendingChunksToWrite.Count > 0)
            {
                VoxelChunk chunk;
                Vector3I coords;
                DequeueDirtyChunk(out chunk, out coords);

                using (m_storageLock.AcquireExclusiveUsing())
                using (chunk.Lock.AcquireExclusiveUsing())
                    WriteChunk(ref coords, chunk);
            }

            // if timeout
            return m_pendingChunksToWrite.Count == 0;
        }

        public void GetStats(out WriteCacheStats stats)
        {
            stats.CachedChunks = m_cachedChunks.Count;
            stats.QueuedWrites = m_pendingChunksToWrite.Count;
            stats.Chunks = m_cachedChunks;
        }

        private bool OverlapsAnyCachedCell(Vector3I voxelRangeMin, Vector3I voxelRangeMax)
        {
            var querybb = new BoundingBox(voxelRangeMin, voxelRangeMax);

            return m_cacheMap.OverlapsAnyLeafBoundingBox(ref querybb);
        }
    }
}
