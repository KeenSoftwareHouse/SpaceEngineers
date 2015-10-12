using Sandbox.Common.ObjectBuilders.Voxels;
using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using VRage;
using VRage.Collections;
using VRage.FileSystem;
using VRage.Library.Utils;
using VRage.Plugins;
using VRage.Utils;
using VRage.Voxels;
using VRageMath;

namespace Sandbox.Engine.Voxels
{
    public abstract partial class MyStorageBase : IMyStorage
    {
        protected const string STORAGE_TYPE_NAME_CELL = "Cell";
        protected const int STORAGE_TYPE_VERSION_CELL = 2;
        protected const string STORAGE_TYPE_NAME_OCTREE = "Octree";
        protected const int STORAGE_TYPE_VERSION_OCTREE = 1;

        private byte[] m_compressedData;
        private readonly MyVoxelGeometry m_geometry = new MyVoxelGeometry();
        private readonly FastResourceLock m_lock = new FastResourceLock();
        protected byte m_defaultMaterial = MyDefinitionManager.Static.GetDefaultVoxelMaterialDefinition().Index;

        public abstract IMyStorageDataProvider DataProvider { get; set; }

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
        }

        public static MyStorageBase LoadFromFile(string absoluteFilePath)
        {
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
      
            MyStorageBase result =  Load(compressedData);

            return result;
        }

        public static MyStorageBase Load(string name)
        {
            Debug.Assert(name != null, "Name shouldn't be null");

            MyStorageBase result;
            //If there are some voxels from multiplayer, use them (because it appears that we changed to server from client)
            if (MyMultiplayer.Static == null || MyMultiplayer.Static.IsServer)
            {
                var filePath = Path.Combine(MySession.Static.CurrentPath, name + MyVoxelConstants.FILE_EXTENSION);
                result = LoadFromFile(filePath);
            }
            else
            {
                result = Load(MyMultiplayer.Static.VoxelMapData[name]);
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
                    m_compressedData = ms.ToArray();
                }

                outCompressedData = m_compressedData;
            }
            finally
            {
                ProfilerShort.End();
            }
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

        public void WriteRange(MyStorageDataCache source, MyStorageDataTypeFlags dataToWrite, ref Vector3I voxelRangeMin, ref Vector3I voxelRangeMax)
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

        public void ReadRange(MyStorageDataCache target, MyStorageDataTypeFlags dataToRead, int lodIndex, ref Vector3I lodVoxelRangeMin, ref Vector3I lodVoxelRangeMax)
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
                if ((dataToRead & MyStorageDataTypeFlags.Material) != 0)
                {
                    //target.ClearMaterials(m_defaultMaterial);
                }

                if (rangeSize.X <= threshold.X &&
                    rangeSize.Y <= threshold.Y &&
                    rangeSize.Z <= threshold.Z)
                {
                    using (m_lock.AcquireSharedUsing())
                    {
                        ReadRangeInternal(target, ref Vector3I.Zero, dataToRead, lodIndex, ref lodVoxelRangeMin, ref lodVoxelRangeMax);
                    }
                }
                else
                {
                    // splitting to smaller ranges to make sure the lock is not held for too long, preventing write on update thread
                    // subranges could be aligned to multiple of their size for possibly better performance
                    var steps = (rangeSize - 1) >> SUBRANGE_SIZE_SHIFT;
                    for (var it = new Vector3I.RangeIterator(ref Vector3I.Zero, ref steps); it.IsValid(); it.MoveNext())
                    {
                        var offset = it.Current << SUBRANGE_SIZE_SHIFT;
                        var min = lodVoxelRangeMin + offset;
                        var max = min + SUBRANGE_SIZE - 1;
                        Vector3I.Min(ref max, ref lodVoxelRangeMax, out max);
                        Debug.Assert(min.IsInsideInclusive(ref lodVoxelRangeMin, ref lodVoxelRangeMax));
                        Debug.Assert(max.IsInsideInclusive(ref lodVoxelRangeMin, ref lodVoxelRangeMax));
                        using (m_lock.AcquireSharedUsing())
                        {
                            ReadRangeInternal(target, ref offset, dataToRead, lodIndex, ref min, ref max);
                        }
                    }
                }
            }
            finally
            {
                ProfilerShort.End();
            }
        }

        protected abstract void LoadInternal(int fileVersion, Stream stream, ref bool isOldFormat);
        protected abstract void SaveInternal(Stream stream);

        protected abstract void ResetInternal(MyStorageDataTypeFlags dataToReset);
        protected abstract void OverwriteAllMaterialsInternal(MyVoxelMaterialDefinition material);
        protected abstract void WriteRangeInternal(MyStorageDataCache source, MyStorageDataTypeFlags dataToWrite, ref Vector3I voxelRangeMin, ref Vector3I voxelRangeMax);

        protected abstract void ReadRangeInternal(MyStorageDataCache target, ref Vector3I targetWriteRange, MyStorageDataTypeFlags dataToRead, int lodIndex, ref Vector3I lodVoxelRangeMin, ref Vector3I lodVoxelRangeMax);

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
    }
}