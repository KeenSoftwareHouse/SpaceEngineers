using Sandbox.Common.ObjectBuilders.Voxels;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.Voxels;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using VRage;
using VRage.Collections;
using VRage.Common.Plugins;
using VRage.Common.Utils;
using VRageMath;

namespace Sandbox.Game.Entities.VoxelMaps
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class MyVoxelStorageAttribute : System.Attribute
    {
        public string SerializedTypeName;
        public int SerializedVersion;
        public Type StorageType;

        public MyVoxelStorageAttribute(string typeName, int version)
        {
            SerializedTypeName = typeName;
            SerializedVersion = version;
        }
    }

    public abstract class MyStorageBase : IMyStorage
    {
        private static readonly Dictionary<string, MyVoxelStorageAttribute> m_attributesByName = new Dictionary<string, MyVoxelStorageAttribute>();
        private static readonly Dictionary<Type, MyVoxelStorageAttribute> m_attributesByType = new Dictionary<Type, MyVoxelStorageAttribute>();

        static MyStorageBase()
        {
            Debug.Assert(MyPlugins.Loaded);
            RegisterTypes(Assembly.GetExecutingAssembly());
            if (MyPlugins.PluginAssembly != null)
                RegisterTypes(MyPlugins.PluginAssembly);
        }

        private static void RegisterTypes(Assembly assembly)
        {
            foreach (var type in assembly.GetTypes())
            {
                var attributes = type.GetCustomAttributes(typeof(MyVoxelStorageAttribute), false);
                if (attributes == null || attributes.Length == 0)
                    continue;

                Debug.Assert(typeof(MyStorageBase).IsAssignableFrom(type));
                var attribute = (MyVoxelStorageAttribute)attributes[0];
                attribute.StorageType = type;

                m_attributesByName.Add(attribute.SerializedTypeName, attribute);
                m_attributesByType.Add(attribute.StorageType, attribute);
            }
        }

        protected readonly Dictionary<int, IMyDepositCell> OreDepositsMutable = new Dictionary<int, IMyDepositCell>();
        private byte[] m_compressedData;
        private MyStringId m_nameId;

        public MyVoxelMap VoxelMap { get; protected set; }

        public MyStringId NameId
        {
            get { return m_nameId; }
            set
            {
                if (m_nameId != value)
                {
                    var oldNameId = m_nameId;
                    m_nameId = value;
                    MySession.Static.VoxelMaps.OnStorageRenamed(this, oldNameId);
                }
            }
        }

        public string Name
        {
            get { return m_nameId.ToString(); }
            set
            {
                NameId = MyStringId.GetOrCompute(value);
            }
        }

        public Vector3I Size
        {
            get;
            protected set;
        }

        public DictionaryValuesReader<int, IMyDepositCell> OreDeposits
        {
            get { return OreDepositsMutable; }
        }

        public MyStorageBase(string name)
        {
            m_nameId = MyStringId.GetOrCompute(name);
            MySession.Static.VoxelMaps.AddStorage(this);
        }

        public static MyStorageBase LoadFromFile(MyVoxelMap voxelMap, string absoluteFilePath)
        {
            if (!MyFileSystem.FileExists(absoluteFilePath))
            {
                var oldPath = Path.ChangeExtension(absoluteFilePath, "vox");
                UpdateFileFormat(oldPath);
                Debug.Assert(MyFileSystem.FileExists(absoluteFilePath));
            }
            Debug.Assert(absoluteFilePath.EndsWith(MyVoxelConstants.FILE_EXTENSION));

            byte[] compressedData = null;
            using (var file = MyFileSystem.OpenRead(absoluteFilePath))
            {
                compressedData = new byte[file.Length];
                file.Read(compressedData, 0, compressedData.Length);
            }
            return Load(voxelMap, compressedData, Path.GetFileNameWithoutExtension(absoluteFilePath));
        }

        public static MyStorageBase Load(MyVoxelMap voxelMap, string name)
        {
            Debug.Assert(name != null, "Name shouldn't be null");

            MyStorageBase result;
            //If there are some voxels from multiplayer, use them (because it appears that we changed to server from client)
            if (!MySession.Static.VoxelMaps.TryGetStorage(name, out result))
            {
                if (Multiplayer.Sync.IsServer && (MyVoxelMaps.MultiplayerVoxelMaps == null || MyVoxelMaps.MultiplayerVoxelMaps.Count == 0))
                {
                    var filePath = Path.Combine(MySession.Static.CurrentPath, name + MyVoxelConstants.FILE_EXTENSION);
                    result = LoadFromFile(voxelMap, filePath);
                }
                else
                {
                    Debug.Assert(MyVoxelMaps.MultiplayerVoxelMaps != null);
                    Debug.Assert(MyVoxelMaps.MultiplayerVoxelMaps.ContainsKey(name));
                    result = Load(voxelMap, MyVoxelMaps.MultiplayerVoxelMaps[name], name);
                }
            }

            return result;
        }

        private static MyStorageBase Load(MyVoxelMap voxelMap, byte[] memoryBuffer, string name)
        {
            MyStorageBase storage;
            using (var ms = new MemoryStream(memoryBuffer, false))
            using (var gz = new GZipStream(ms, CompressionMode.Decompress))
            {
                storage = Load(voxelMap, gz, name);
            }
            storage.m_compressedData = memoryBuffer;
            return storage;
        }

        private static MyStorageBase Load(MyVoxelMap voxelMap, Stream stream, string name)
        {
            Profiler.Begin("MyStorageBase.Load");
            try
            {
                string storageType = stream.ReadString();
                int version = stream.Read7BitEncodedInt();
                MyStorageBase storage = null;
                MyVoxelStorageAttribute attr;
                if (!m_attributesByName.TryGetValue(storageType, out attr))
                {
                    Debug.Fail(string.Format("Encountered unknown storage type in voxel file: {0}", storageType));
                    return null;
                }
                storage = Activator.CreateInstance(attr.StorageType, name) as MyStorageBase;
                storage.VoxelMap = voxelMap;
                storage.LoadInternal(version, stream);

                return storage;
            }
            finally
            {
                Profiler.End();
            }
        }

        public void Save(out byte[] outCompressedData)
        {
            Profiler.Begin("MyStorageBase.Save");
            try
            {
                if (m_compressedData == null)
                {
                    MemoryStream ms;
                    using (ms = new MemoryStream(0x4000))
                    using (GZipStream gz = new GZipStream(ms, CompressionMode.Compress))
                    using (BufferedStream buf = new BufferedStream(gz, 0x4000))
                    {
                        var attr = m_attributesByType[GetType()];
                        buf.WriteNoAlloc(attr.SerializedTypeName);
                        buf.Write7BitEncodedInt(attr.SerializedVersion);
                        SaveInternal(buf);
                    }
                    m_compressedData = ms.ToArray();
                }

                outCompressedData = m_compressedData;
            }
            finally
            {
                Profiler.End();
            }
        }

        public void Close()
        {
            m_compressedData = null;
            CloseInternal();
        }

        // mk:TODO Remove
        public void MergeVoxelMaterials(MyMwcVoxelFilesEnum voxelFile, Vector3I voxelPosition, MyVoxelMaterialDefinition materialToSet)
        {
            m_compressedData = null;
            MergeVoxelMaterialsInternal(voxelFile, voxelPosition, materialToSet);
        }

        public void OverwriteAllMaterials(MyVoxelMaterialDefinition material)
        {
            m_compressedData = null;
            OverwriteAllMaterialsInternal(material);
        }

        // mk:TODO Remove
        public void SetSurfaceMaterial(MyVoxelMaterialDefinition material, int cellThickness)
        {
            m_compressedData = null;
            SetSurfaceMaterialInternal(material, cellThickness);
        }

        public void WriteRange(MyStorageDataCache source, bool writeContent, bool writeMaterials, ref Vector3I voxelRangeMin, ref Vector3I voxelRangeMax)
        {
            m_compressedData = null;
            WriteRangeInternal(source, writeContent, writeMaterials, ref voxelRangeMin, ref voxelRangeMax);
        }

        public MyStorageBase DeepCopy(string newName)
        {
            if (m_compressedData == null)
            {
                byte[] dummy;
                Save(out dummy); // this will update m_compressedData, so no need to pass it in
            }

            return Load(VoxelMap, m_compressedData, newName);
        }

        protected abstract void LoadInternal(int fileVersion, Stream stream);
        protected abstract void SaveInternal(Stream stream);
        protected abstract void CloseInternal();
        protected abstract void MergeVoxelMaterialsInternal(MyMwcVoxelFilesEnum voxelFile, Vector3I voxelPosition, MyVoxelMaterialDefinition materialToSet);
        protected abstract void OverwriteAllMaterialsInternal(MyVoxelMaterialDefinition material);
        protected abstract void SetSurfaceMaterialInternal(MyVoxelMaterialDefinition material, int cellThickness);
        protected abstract void WriteRangeInternal(MyStorageDataCache source, bool writeContent, bool writeMaterials, ref Vector3I voxelRangeMin, ref Vector3I voxelRangeMax);

        public abstract void GetAllMaterialsPresent(HashSet<MyVoxelMaterialDefinition> outputMaterialSet);
        public abstract void RecomputeOreDeposits();
        public abstract void ReadRange(MyStorageDataCache target, bool readContent, bool readMaterials, int lodIndex, ref Vector3I lodVoxelRangeMin, ref Vector3I lodVoxelRangeMax);
        public abstract MyVoxelRangeType GetRangeType(int lodIndex, ref Vector3I lodVoxelRangeMin, ref Vector3I lodVoxelRangeMax);

        public virtual void DebugDraw(MyVoxelDebugDrawMode mode, int modeArg) { }

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
                buffer.WriteNoAlloc(m_attributesByType[typeof(MyCellStorage)].SerializedTypeName);

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

    }
}
