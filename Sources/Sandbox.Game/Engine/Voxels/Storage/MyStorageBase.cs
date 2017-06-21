using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using ParallelTasks;
using VRage;
using VRage.Collections;
using VRage.FileSystem;
using VRage.Profiler;
using VRage.Utils;
using VRage.Voxels;
using VRageMath;

namespace Sandbox.Engine.Voxels
{
    public abstract partial class MyStorageBase : IMyStorage
    {
        struct MyVoxelObjectDefinition
        {
            public readonly string FilePath;
            public readonly Dictionary<byte, byte> Changes;

            public MyVoxelObjectDefinition(string filePath, Dictionary<byte, byte> changes)
            {
                FilePath = filePath;
                Changes = changes;
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = 17;
                    hash = hash * 486187739 + FilePath.GetHashCode();
                    if (Changes != null)
                    {
                        foreach (var modifier in Changes)
                        {
                            hash = hash * 486187739 + modifier.Key.GetHashCode();
                            hash = hash * 486187739 + modifier.Value.GetHashCode();
                        }
                    }
                    return hash;
                }
            }
        }

        protected const string STORAGE_TYPE_NAME_CELL = "Cell";
        protected const int STORAGE_TYPE_VERSION_CELL = 2;
        protected const string STORAGE_TYPE_NAME_OCTREE = "Octree";
        protected const int STORAGE_TYPE_VERSION_OCTREE = 1;

        protected byte[] m_compressedData;
        private readonly MyVoxelGeometry m_geometry = new MyVoxelGeometry();
        protected readonly FastResourceLock m_storageLock = new FastResourceLock();
        protected byte m_defaultMaterial = MyDefinitionManager.Static.GetDefaultVoxelMaterialDefinition().Index;

        public abstract IMyStorageDataProvider DataProvider { get; set; }

        public static bool UseStorageCache = true;
        static LRUCache<int, MyStorageBase> m_storageCache = new LRUCache<int, MyStorageBase>(16);
        static FastResourceLock m_loadCompressLock = new FastResourceLock();

        public bool Shared { get; protected set; }

        public bool MarkedForClose { get; private set; }

        protected bool Pinned
        {
            get { return m_pinCount > 0; }
        }

        public MyVoxelGeometry Geometry
        {
            get { return m_geometry; }
        }

        public Vector3I Size
        {
            get;
            protected set;
        }

        public event RangeChangedDelegate RangeChanged;

        protected internal void OnRangeChanged(Vector3I voxelRangeMin, Vector3I voxelRangeMax, MyStorageDataTypeFlags changedData)
        {
            if (RangeChanged != null)
            {
                m_compressedData = null;
                this.ClampVoxelCoord(ref voxelRangeMin);
                this.ClampVoxelCoord(ref voxelRangeMax);
                RangeChanged(voxelRangeMin, voxelRangeMax, changedData);
            }
        }

        public MyStorageBase()
        {
            Closed = false;
        }

        public unsafe bool ChangeMaterials(Dictionary<byte, byte> map)
        {
            int rewrites = 0;

            if ((Size + 1).Size > 4 * 1024 * 1024)
            {
                MyLog.Default.Error("Cannot overwrite materials for a storage 4 MB or larger.");
                return false;
            }

            Vector3I minCorner = Vector3I.Zero;
            Vector3I maxCorner = Size - 1;

            // I don't like this but write range will also allocate so f.. it.
            MyStorageData cache = new MyStorageData();
            cache.Resize(Size);

            ReadRange(cache, MyStorageDataTypeFlags.Material, 0, ref minCorner, ref maxCorner);

            var len = cache.SizeLinear;

            fixed (byte* data = cache[MyStorageDataTypeEnum.Material])
                for (int i = 0; i < len; i++)
                {
                    byte to;
                    if (map.TryGetValue(data[i], out to))
                    {
                        data[i] = to;
                        rewrites++;
                    }
                }

            if (rewrites > 0) this.WriteRange(cache, MyStorageDataTypeFlags.Material, ref minCorner, ref maxCorner);

            return rewrites > 0;
        }

        public static MyStorageBase LoadFromFile(string absoluteFilePath, Dictionary<byte, byte> modifiers = null)
        {
            //get hash code
            MyVoxelObjectDefinition definition = new MyVoxelObjectDefinition(absoluteFilePath, modifiers);

            int sh = definition.GetHashCode();

            MyStorageBase result = null;

            if (UseStorageCache)
            {
                result = m_storageCache.Read(sh);
                if (result != null)
                {
                    result.Shared = true;
                    return result;
                }
            }

            const string loadingMessage = "Loading voxel storage from file '{0}'";
            if (!MyFileSystem.FileExists(absoluteFilePath))
            {
                var oldPath = Path.ChangeExtension(absoluteFilePath, "vox");
                MySandboxGame.Log.WriteLine(string.Format(loadingMessage, oldPath));
                if (!MyFileSystem.FileExists(oldPath))
                {
                    //Debug.Fail("Voxel map could not be loaded! " + absoluteFilePath);
                    return null;
                }
                UpdateFileFormat(oldPath);
            }
            else
            {
                MySandboxGame.Log.WriteLine(string.Format(loadingMessage, absoluteFilePath));
            }
            Debug.Assert(absoluteFilePath.EndsWith(MyVoxelConstants.FILE_EXTENSION));


            byte[] compressedData = null;
            using (var file = MyFileSystem.OpenRead(absoluteFilePath))
            {
                compressedData = new byte[file.Length];
                file.Read(compressedData, 0, compressedData.Length);
            }

            // JC: with Parallelization, it was crashing the game here, without lock
            using (m_loadCompressLock.AcquireExclusiveUsing())
            {
                result = Load(compressedData);
            }

            //change materials
            if (definition.Changes != null)
                result.ChangeMaterials(definition.Changes);

            if (UseStorageCache)
            {
                m_storageCache.Write(sh, result);
                result.Shared = true;
            }
            else
                m_storageCache.Reset();

            return result;
        }

        public static void ResetCache()
        {
            m_storageCache.Reset();
        }

        public static MyStorageBase Load(string name)
        {
            Debug.Assert(name != null, "Name shouldn't be null");

            MyStorageBase result = null;
            //If there are some voxels from multiplayer, use them (because it appears that we changed to server from client)
            if (MyMultiplayer.Static == null || MyMultiplayer.Static.IsServer)
            {
                var filePath = Path.IsPathRooted(name) ? name : Path.Combine(MySession.Static.CurrentPath, name + MyVoxelConstants.FILE_EXTENSION);

                //By Gregory: Added for compatibility with old saves
                result = LoadFromFile(filePath);
            }
            else
            {
                if (MyMultiplayer.Static.VoxelMapData.ContainsKey(name))
                    result = Load(MyMultiplayer.Static.VoxelMapData[name]);
                else
                {
                    Debug.Fail("Missing voxel map data! : " + name);
                    Sandbox.Engine.Networking.MyAnalyticsHelper.ReportActivityStart(null, "Missing voxel map data!", name, "DevNote", "", false);
                    throw  new Exception(string.Format("Missing voxel map data! : {0}",name));
                }
            }
            return result;
        }

        public static MyStorageBase Load(byte[] memoryBuffer)
        {
            MyStorageBase storage;
            bool isOldFormat;
            using (var ms = new MemoryStream(memoryBuffer, false))
            using (var gz = new GZipStream(ms, CompressionMode.Decompress))
            {
                Load(gz, out storage, out isOldFormat);
            }
            if (!isOldFormat)
                storage.m_compressedData = memoryBuffer;
            else
            {
                MySandboxGame.Log.WriteLine("Voxel storage was in old format. It is updated but needs to be saved.");
                storage.m_compressedData = null;
            }
            return storage;
        }

        private static void Load(Stream stream, out MyStorageBase storage, out bool isOldFormat)
        {
            ProfilerShort.Begin("MyStorageBase.Load");
            try
            {
                isOldFormat = false;
                string storageType = stream.ReadString();
                int version = stream.Read7BitEncodedInt();
                if (storageType == STORAGE_TYPE_NAME_CELL)
                {
                    storage = Compatibility_LoadCellStorage(version, stream);
                    isOldFormat = true;
                }
                else if (storageType == STORAGE_TYPE_NAME_OCTREE)
                {
                    storage = new MyOctreeStorage();
                    storage.LoadInternal(version, stream, ref isOldFormat);
                    storage.m_geometry.Init(storage);
                }
                else
                {
                    throw new InvalidBranchException();
                }
            }
            finally
            {
                ProfilerShort.End();
            }
        }

        public void Save(out byte[] outCompressedData)
        {
            MyPrecalcComponent.AssertUpdateThread();

            // We must flush all caches before saving.
            if (CachedWrites)
                WritePending(true);

            ProfilerShort.Begin("MyStorageBase.Save");
            try
            {
                if (m_compressedData == null)
                {
                    SaveCompressedData(out m_compressedData);
                }

                outCompressedData = m_compressedData;
            }
            finally
            {
                ProfilerShort.End();
            }
        }

        protected void SaveCompressedData(out byte[] compressedData)
        {
            MemoryStream ms;
            using (ms = new MemoryStream(0x4000))
            using (GZipStream gz = new GZipStream(ms, CompressionMode.Compress))
            using (BufferedStream buf = new BufferedStream(gz, 0x4000))
            {
                string name;
                int version;
                if (GetType() == typeof(MyOctreeStorage))
                {
                    name = STORAGE_TYPE_NAME_OCTREE;
                    version = STORAGE_TYPE_VERSION_OCTREE;
                }
                else
                {
                    throw new InvalidBranchException();
                }
                buf.WriteNoAlloc(name);
                buf.Write7BitEncodedInt(version);
                SaveInternal(buf);
            }

            compressedData = ms.ToArray();
        }

        /// <summary>
        /// Resets the data specified by flags to values from data provider, or default if no provider is assigned.
        /// </summary>
        /// <param name="dataToReset"></param>
        public void Reset(MyStorageDataTypeFlags dataToReset)
        {
            MyPrecalcComponent.AssertUpdateThread();

            using (m_storageLock.AcquireExclusiveUsing())
            {
                m_compressedData = null;
                ResetInternal(dataToReset);
            }
            OnRangeChanged(Vector3I.Zero, Size - 1, dataToReset);
        }

        public void OverwriteAllMaterials(MyVoxelMaterialDefinition material)
        {
            MyPrecalcComponent.AssertUpdateThread();

            using (m_storageLock.AcquireExclusiveUsing())
            {
                m_compressedData = null;
                OverwriteAllMaterialsInternal(material);
            }
            OnRangeChanged(Vector3I.Zero, Size - 1, MyStorageDataTypeFlags.Material);
        }

        public void WriteRange(MyStorageData source, MyStorageDataTypeFlags dataToWrite, ref Vector3I voxelRangeMin, ref Vector3I voxelRangeMax)
        {
            MyPrecalcComponent.AssertUpdateThread();

            ProfilerShort.Begin(GetType().Name + ".WriteRange");
            try
            {
                m_compressedData = null;

                if (CachedWrites && (m_pendingChunksToWrite.Count < WriteCacheCap || OverlapsAnyCachedCell(voxelRangeMin, voxelRangeMax)))
                {
                    var lodDiff = VoxelChunk.SizeBits;

                    var chunkMin = voxelRangeMin >> lodDiff;
                    var chunkMax = voxelRangeMax >> lodDiff;

                    var pos = Vector3I.Zero;
                    for (pos.Z = chunkMin.Z; pos.Z <= chunkMax.Z; ++pos.Z)
                        for (pos.Y = chunkMin.Y; pos.Y <= chunkMax.Y; ++pos.Y)
                            for (pos.X = chunkMin.X; pos.X <= chunkMax.X; ++pos.X)
                            {
                                var celPos = pos << lodDiff;

                                var lodCkStart = pos << lodDiff;
                                lodCkStart = Vector3I.Max(lodCkStart, voxelRangeMin);

                                var lodCkEnd = ((pos + 1) << lodDiff) - 1;
                                lodCkEnd = Vector3I.Min(lodCkEnd, voxelRangeMax);

                                var targetOffset = lodCkStart - voxelRangeMin;

                                VoxelChunk chunk;
                                // Do not read the chunk if the range overlaps the whole chunk
                                var toRead = (lodCkEnd - lodCkStart + 1).Size != VoxelChunk.Volume ? dataToWrite : 0;
                                GetChunk(ref pos, out chunk, toRead);

                                lodCkStart -= celPos;
                                lodCkEnd -= celPos;

                                using (chunk.Lock.AcquireExclusiveUsing())
                                {
                                    bool dirty = chunk.Dirty != 0;
                                    chunk.Write(source, dataToWrite, ref targetOffset, ref lodCkStart, ref lodCkEnd);
                                    if (!dirty) m_pendingChunksToWrite.Enqueue(pos);
                                }
                            }
                }
                else
                {
                    using (m_storageLock.AcquireExclusiveUsing())
                        WriteRangeInternal(source, dataToWrite, ref voxelRangeMin, ref voxelRangeMax);
                }

                ProfilerShort.BeginNextBlock(GetType().Name + ".OnRangeChanged");

                OnRangeChanged(voxelRangeMin, voxelRangeMax, dataToWrite);

            }
            finally
            {
                ProfilerShort.End();
            }
        }

        public void ReadRange(MyStorageData target, MyStorageDataTypeFlags dataToRead, int lodIndex, ref Vector3I lodVoxelRangeMin, ref Vector3I lodVoxelRangeMax)
        {
            MyVoxelRequestFlags flags = 0;
            ReadRange(target, dataToRead, lodIndex, ref lodVoxelRangeMin, ref lodVoxelRangeMax, ref flags);
        }

        private void ReadRangeAdviseCache(MyStorageData target, MyStorageDataTypeFlags dataToRead, ref Vector3I lodVoxelRangeMin, ref Vector3I lodVoxelRangeMax)
        {
            // Skip if too many cached cells
            if (m_pendingChunksToWrite.Count > WriteCacheCap)
            {
                ReadRange(target, dataToRead, 0, ref lodVoxelRangeMin, ref lodVoxelRangeMax);
                return;
            }

            if (CachedWrites)
            {
                var lodDiff = VoxelChunk.SizeBits;

                var chunkMin = lodVoxelRangeMin >> lodDiff;
                var chunkMax = lodVoxelRangeMax >> lodDiff;

                var pos = Vector3I.Zero;
                for (pos.Z = chunkMin.Z; pos.Z <= chunkMax.Z; ++pos.Z)
                    for (pos.Y = chunkMin.Y; pos.Y <= chunkMax.Y; ++pos.Y)
                        for (pos.X = chunkMin.X; pos.X <= chunkMax.X; ++pos.X)
                        {
                            var celPos = pos << lodDiff;

                            var lodCkStart = pos << lodDiff;
                            lodCkStart = Vector3I.Max(lodCkStart, lodVoxelRangeMin);

                            var targetOffset = lodCkStart - lodVoxelRangeMin;

                            var lodCkEnd = ((pos + 1) << lodDiff) - 1;
                            lodCkEnd = Vector3I.Min(lodCkEnd, lodVoxelRangeMax);

                            VoxelChunk chunk;
                            GetChunk(ref pos, out chunk, dataToRead);

                            lodCkStart -= celPos;
                            lodCkEnd -= celPos;

                            using (chunk.Lock.AcquireSharedUsing())
                                chunk.ReadLod(target, dataToRead, ref targetOffset, 0, ref lodCkStart, ref lodCkEnd);
                        }
            }
        }

        public void ReadRange(MyStorageData target, MyStorageDataTypeFlags dataToRead, int lodIndex, ref Vector3I lodVoxelRangeMin, ref Vector3I lodVoxelRangeMax, ref MyVoxelRequestFlags requestFlags)
        {
            ProfilerShort.Begin(GetType().Name + ".ReadRange");

            if ((dataToRead & MyStorageDataTypeFlags.Content) != 0)
            {
                target.ClearContent(0);
            }

            if (requestFlags.HasFlags(MyVoxelRequestFlags.AdviseCache) && lodIndex == 0 && CachedWrites)
            {
                ReadRangeAdviseCache(target, dataToRead, ref lodVoxelRangeMin, ref lodVoxelRangeMax);
                ProfilerShort.End();
                return;
            }

            if (CachedWrites && lodIndex <= VoxelChunk.SizeBits && m_cachedChunks.Count > 0)
            {
                // read occlusion separate
                if (dataToRead.Requests(MyStorageDataTypeEnum.Occlusion))
                {
                    using (m_storageLock.AcquireSharedUsing())
                        ReadRangeInternal(target, ref Vector3I.Zero, MyStorageDataTypeFlags.Occlusion, lodIndex, ref lodVoxelRangeMin, ref lodVoxelRangeMax, ref requestFlags);

                    dataToRead ^= MyStorageDataTypeFlags.Occlusion;
                }

                if (m_tmpChunks == null) m_tmpChunks = new List<VoxelChunk>();

                var lodDiff = VoxelChunk.SizeBits - lodIndex;

                // We fetch which chunks overlap our current range from the chunk tree, then we read all data from storage and apply those changes
                var querybb = new BoundingBox(lodVoxelRangeMin << lodIndex, lodVoxelRangeMax << lodIndex);

                using (m_cacheLock.AcquireSharedUsing())
                    m_cacheMap.OverlapAllBoundingBox(ref querybb, m_tmpChunks, 0, false);

                if (m_tmpChunks.Count > 0)
                {
                    var chunkMin = lodVoxelRangeMin >> lodDiff;
                    var chunkMax = lodVoxelRangeMax >> lodDiff;

                    bool readFromStorage = false;

                    if ((chunkMax - chunkMin + 1).Size > m_tmpChunks.Count)
                    {
                        using (m_storageLock.AcquireSharedUsing())
                            ReadRangeInternal(target, ref Vector3I.Zero, dataToRead, lodIndex, ref lodVoxelRangeMin, ref lodVoxelRangeMax, ref requestFlags);
                        readFromStorage = true;
                    }

                    for (int i = 0; i < m_tmpChunks.Count; ++i)
                    {
                        var chunk = m_tmpChunks[i];
                        var pos = chunk.Coords;

                        var celPos = pos << lodDiff;

                        var lodCkStart = pos << lodDiff;
                        lodCkStart = Vector3I.Max(lodCkStart, lodVoxelRangeMin);

                        var targetOffset = lodCkStart - lodVoxelRangeMin;

                        var lodCkEnd = ((pos + 1) << lodDiff) - 1;
                        lodCkEnd = Vector3I.Min(lodCkEnd, lodVoxelRangeMax);

                        lodCkStart -= celPos;
                        lodCkEnd -= celPos;

                        if ((chunk.Cached & dataToRead) != dataToRead && !readFromStorage)
                        {
                            using (m_storageLock.AcquireSharedUsing())
                                if ((chunk.Cached & dataToRead) != dataToRead)
                                    ReadDatForChunk(chunk, dataToRead);
                        }

                        using (chunk.Lock.AcquireSharedUsing())
                            chunk.ReadLod(target, !readFromStorage ? dataToRead : dataToRead & chunk.Cached, ref targetOffset, lodIndex, ref lodCkStart, ref lodCkEnd);
                    }

                    m_tmpChunks.Clear();
                    ProfilerShort.End();
                    return;
                }
            }

            // all else
            using (m_storageLock.AcquireSharedUsing())
                ReadRangeInternal(target, ref Vector3I.Zero, dataToRead, lodIndex, ref lodVoxelRangeMin, ref lodVoxelRangeMax, ref requestFlags);

            ProfilerShort.End();
        }

        public ContainmentType Intersect(ref BoundingBox box, bool lazy)
        {
            using (m_storageLock.AcquireSharedUsing())
            {
                if (Closed) return ContainmentType.Disjoint;
                return IntersectInternal(ref box, lazy);
            }
        }

        public bool Intersect(ref LineD line)
        {
            using (m_storageLock.AcquireSharedUsing())
            {
                if (Closed) return false;
                return IntersectInternal(ref line);
            }
        }

        public abstract ContainmentType IntersectInternal(ref BoundingBox box, bool lazy);

        public abstract bool IntersectInternal(ref LineD line);

        protected abstract void LoadInternal(int fileVersion, Stream stream, ref bool isOldFormat);
        protected abstract void SaveInternal(Stream stream);

        protected abstract void ResetInternal(MyStorageDataTypeFlags dataToReset);
        protected abstract void OverwriteAllMaterialsInternal(MyVoxelMaterialDefinition material);
        protected abstract void WriteRangeInternal(MyStorageData source, MyStorageDataTypeFlags dataToWrite, ref Vector3I voxelRangeMin, ref Vector3I voxelRangeMax);

        protected abstract void ReadRangeInternal(MyStorageData target, ref Vector3I targetWriteRange, MyStorageDataTypeFlags dataToRead, int lodIndex, ref Vector3I lodVoxelRangeMin, ref Vector3I lodVoxelRangeMax, ref MyVoxelRequestFlags requestFlags);

        public virtual void DebugDraw(MyVoxelBase voxelMap, MyVoxelDebugDrawMode mode) { }

        private static void UpdateFileFormat(string originalVoxFile)
        {
            var newFile = Path.ChangeExtension(originalVoxFile, MyVoxelConstants.FILE_EXTENSION);
            if (!File.Exists(originalVoxFile))
            {
                MySandboxGame.Log.Error("Voxel file '{0}' does not exist!", originalVoxFile);
                return;
            }

            if (Path.GetExtension(originalVoxFile) != ".vox")
            {
                MySandboxGame.Log.Warning("Unexpected voxel file extensions in path: '{0}'", originalVoxFile);
            }

            try
            {
                using (var decompressFile = new MyCompressionFileLoad(originalVoxFile))
                using (var file = MyFileSystem.OpenWrite(newFile))
                using (var gzip = new GZipStream(file, CompressionMode.Compress))
                using (var buffer = new BufferedStream(gzip))
                {
                    buffer.WriteNoAlloc(STORAGE_TYPE_NAME_CELL);

                    // File version. New format will store it in 7bit encoded int right after the name of storage.
                    buffer.Write7BitEncodedInt(decompressFile.GetInt32());

                    // All remaining data is unchanged. Just copy it to new file.
                    byte[] tmp = new byte[0x4000];
                    int bytesRead = decompressFile.GetBytes(tmp.Length, tmp);
                    while (bytesRead != 0)
                    {
                        buffer.Write(tmp, 0, bytesRead);
                        bytesRead = decompressFile.GetBytes(tmp.Length, tmp);
                    }
                }
            }
            catch (Exception e)
            {
                MySandboxGame.Log.Error("While updating voxel storage '{0}' to new format: {1}", originalVoxFile, e.Message);
            }
        }

        public void Reset()
        {
            OnRangeChanged(Vector3I.Zero, Size - 1, MyStorageDataTypeFlags.All);
        }

        public virtual IMyStorage Copy()
        {
            return null;
        }

        public virtual void CloseInternal()
        {
            using (m_storageLock.AcquireExclusiveUsing())
            {
                Closed = true;
                if (RangeChanged != null)
                    foreach (var handler in RangeChanged.GetInvocationList())
                    {
                        RangeChanged -= (RangeChangedDelegate)handler;
                    }

                if (DataProvider != null)
                    DataProvider.Close();
            }

            if (CachedWrites)
                OperationsComponent.Remove(this);
        }

        public bool Closed { get; private set; }

        public void Close()
        {
            if (Pinned)
                MarkedForClose = true;
            else
                CloseInternal();
        }

        private volatile int m_pinCount = 0;

        private SpinLockRef m_pinLock = new SpinLockRef();

        public StoragePin Pin()
        {
            using (m_pinLock.Acquire())
            {
                if (Closed) return default(StoragePin);

                m_pinCount++;
                return new StoragePin(this);
            }
        }

        public void Unpin()
        {
            using (m_pinLock.Acquire())
            {
                m_pinCount--;
                if (m_pinCount < 1 && MarkedForClose)
                    CloseInternal();
            }
        }
    }
}