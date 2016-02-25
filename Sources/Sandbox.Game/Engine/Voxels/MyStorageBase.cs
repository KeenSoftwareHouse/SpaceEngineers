using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using ParallelTasks;
using VRage;
using VRage.Collections;
using VRage.FileSystem;
using VRage.Utils;
using VRage.Voxels;
using VRageMath;

namespace Sandbox.Engine.Voxels
{
    public abstract partial class MyStorageBase : IMyStorage
    {
        struct MyVoxelObjectDefinition
        {
            public string filePath;
            public byte[] materialChangeFrom;
            public byte[] materialChangeTo;

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = 17;
                    hash = hash * 486187739 + filePath.GetHashCode();
                    if (materialChangeFrom != null && materialChangeTo != null)
                    {
                        for (int i = 0; i < Math.Min(materialChangeFrom.Length, materialChangeTo.Length); i++)
                        {
                            hash = hash * 486187739 + materialChangeFrom[i].GetHashCode();
                            hash = hash * 486187739 + materialChangeTo[i].GetHashCode();
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
        private readonly FastResourceLock m_lock = new FastResourceLock();
        protected byte m_defaultMaterial = MyDefinitionManager.Static.GetDefaultVoxelMaterialDefinition().Index;

        public abstract IMyStorageDataProvider DataProvider { get; set; }

        public static bool UseStorageCache = true;
        static LRUCache<int, MyStorageBase> m_storageCache = new LRUCache<int, MyStorageBase>(16);
        public bool Shared { get; protected set; }

        public bool MarkedForClose { get; private set; }

        protected bool Pinned {
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

        public bool ChangeMaterials(byte[] materialChangeFrom, byte[] materialChangeTo)
        {
            int rewrites = 0;
            if (materialChangeFrom != null && materialChangeTo != null)
            {
                int maxIndex = Math.Min(materialChangeFrom.Length, materialChangeTo.Length);
                if (maxIndex > 0)
                {
                    Vector3I minCorner = Vector3I.Zero;
                    Vector3I maxCorner = this.Size-1;

                    MyStorageData cache = new MyStorageData();
                    cache.Resize(this.Size);
                    cache.ClearMaterials(0);

                    this.ReadRange(cache, MyStorageDataTypeFlags.Material, 0, ref minCorner, ref maxCorner);
                    byte[] data = cache[MyStorageDataTypeEnum.Material];
                    int i, j;
                    for (i = 0; i < data.Length; i++)
                    {
                        if (data[i] > 0)
                        {
                            for (j = 0; j < maxIndex; j++)
                            {
                                if (data[i] == materialChangeFrom[j])
                                {
                                    data[i] = materialChangeTo[j];
                                    rewrites++;
                                    break;
                                }
                            }
                        }
                    }
                    if (rewrites > 0) this.WriteRange(cache, MyStorageDataTypeFlags.Material, ref minCorner, ref maxCorner);
                }
            }
            return (rewrites > 0);
        }

        public static MyStorageBase LoadFromFile(string absoluteFilePath, byte[] materialChangeFrom = null, byte[] materialChangeTo = null)
        {
            //get hash code
            MyVoxelObjectDefinition definition;
            definition.filePath = absoluteFilePath;
            definition.materialChangeFrom = materialChangeFrom;
            definition.materialChangeTo = materialChangeTo;
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
                UpdateFileFormat(oldPath);
                Debug.Assert(MyFileSystem.FileExists(absoluteFilePath));
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
      
            result = Load(compressedData);

            //change materials
            result.ChangeMaterials(definition.materialChangeFrom,definition.materialChangeTo);

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

            using (m_lock.AcquireExclusiveUsing())
            {
                m_compressedData = null;
                ResetInternal(dataToReset);
            }
            OnRangeChanged(Vector3I.Zero, Size - 1, dataToReset);
        }

        public void OverwriteAllMaterials(MyVoxelMaterialDefinition material)
        {
            MyPrecalcComponent.AssertUpdateThread();

            using (m_lock.AcquireExclusiveUsing())
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
                using (m_lock.AcquireExclusiveUsing())
                {
                    m_compressedData = null;
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

        public void ReadRange(MyStorageData target, MyStorageDataTypeFlags dataToRead, int lodIndex, ref Vector3I lodVoxelRangeMin, ref Vector3I lodVoxelRangeMax, ref MyVoxelRequestFlags requestFlags)
        {
            ProfilerShort.Begin(GetType().Name + ".ReadRange");
            try
            {
                const int SUBRANGE_SIZE_SHIFT = 3;
                const int SUBRANGE_SIZE = 1 << SUBRANGE_SIZE_SHIFT;
                var threshold = new Vector3I(SUBRANGE_SIZE);
                var rangeSize = lodVoxelRangeMax - lodVoxelRangeMin + 1;
                if ((dataToRead & MyStorageDataTypeFlags.Content) != 0)
                {
                    target.ClearContent(0);
                }

                if ((rangeSize.X <= threshold.X &&
                    rangeSize.Y <= threshold.Y &&
                    rangeSize.Z <= threshold.Z) || !MyFakes.ENABLE_SPLIT_VOXEL_READ_QUERIES)
                {
                    using (m_lock.AcquireSharedUsing())
                    {
                        ReadRangeInternal(target, ref Vector3I.Zero, dataToRead, lodIndex, ref lodVoxelRangeMin, ref lodVoxelRangeMax, ref requestFlags);
                    }
                }
                else
                {
                    // These optimizations don't work when splitting the range.
                    requestFlags &= ~(MyVoxelRequestFlags.OneMaterial | MyVoxelRequestFlags.ContentChecked);
                    MyVoxelRequestFlags flags = requestFlags;

                    // splitting to smaller ranges to make sure the lock is not held for too long, preventing write on update thread
                    // subranges could be aligned to multiple of their size for possibly better performance
                    var steps = (rangeSize - 1) >> SUBRANGE_SIZE_SHIFT;
                    for (var it = new Vector3I.RangeIterator(ref Vector3I.Zero, ref steps); it.IsValid(); it.MoveNext())
                    {
                        flags = requestFlags;
                        var offset = it.Current << SUBRANGE_SIZE_SHIFT;
                        var min = lodVoxelRangeMin + offset;
                        var max = min + SUBRANGE_SIZE - 1;
                        Vector3I.Min(ref max, ref lodVoxelRangeMax, out max);
                        Debug.Assert(min.IsInsideInclusive(ref lodVoxelRangeMin, ref lodVoxelRangeMax));
                        Debug.Assert(max.IsInsideInclusive(ref lodVoxelRangeMin, ref lodVoxelRangeMax));
                        using (m_lock.AcquireSharedUsing())
                        {
                            ReadRangeInternal(target, ref offset, dataToRead, lodIndex, ref min, ref max, ref flags);
                        }
                    }

                    // If the storage is consistent this should be fine.
                    requestFlags = flags;
                }
            }
            finally
            {
                ProfilerShort.End();
            }
        }

        public ContainmentType Intersect(ref BoundingBox box, bool lazy)
        {
            using (m_lock.AcquireSharedUsing())
            {
                if (Closed) return ContainmentType.Disjoint;
                return IntersectInternal(ref box, lazy);
            }
        }

        public bool Intersect(ref LineD line)
        {
            using (m_lock.AcquireSharedUsing())
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

        public abstract void ResetOutsideBorders(MyVoxelBase voxelMap, BoundingBoxD worldAabb);

        public virtual void DebugDraw(MyVoxelBase voxelMap, MyVoxelDebugDrawMode mode) { }

        private static void UpdateFileFormat(string originalVoxFile)
        {
            var newFile = Path.ChangeExtension(originalVoxFile, MyVoxelConstants.FILE_EXTENSION);
            if (!File.Exists(originalVoxFile))
            {
                MySandboxGame.Log.WriteLine(string.Format("ERROR: Voxel file '{0}' does not exists!", originalVoxFile));
            }
            if (Path.GetExtension(originalVoxFile) != "vox")
            {
                MySandboxGame.Log.WriteLine(string.Format("ERROR: Unexpected voxel file extensions in path: '{0}'", originalVoxFile));
            }

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
            using (m_lock.AcquireExclusiveUsing())
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
        }

        public bool Closed { get; private set; }

        public void Close()
        {
            if (Pinned)
                MarkedForClose = true;
            else
                CloseInternal();
        }

        private class StoragePin : IDisposable
        {
            MyStorageBase m_storage;

            public StoragePin(MyStorageBase myStorageBase)
            {
                m_storage = myStorageBase;
            }

            public void Dispose()
            {
                m_storage.Unpin();
            }
        }

        private volatile int m_pinCount = 0;

        private SpinLockRef m_pinLock = new SpinLockRef();

        public IDisposable Pin()
        {
            using (m_pinLock.Acquire())
            {
                if (Closed) return null;

                m_pinCount++;
                return new StoragePin(this);
            }
        }

        private void Unpin()
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