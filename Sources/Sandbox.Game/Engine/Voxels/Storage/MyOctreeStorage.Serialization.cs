using Sandbox.Definitions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using VRage.Plugins;
using VRage.Voxels;
using VRageMath;

#if XB1 // XB1_ALLINONEASSEMBLY
using VRage.Utils;
#endif // XB1

namespace Sandbox.Engine.Voxels
{
    partial class MyOctreeStorage
    {
#if XB1 // XB1_ALLINONEASSEMBLY
        private static bool m_registered = false;
#endif // XB1

        private static readonly Dictionary<int, MyStorageDataProviderAttribute> m_attributesById = new Dictionary<int, MyStorageDataProviderAttribute>();
        private static readonly Dictionary<Type, MyStorageDataProviderAttribute> m_attributesByType = new Dictionary<Type, MyStorageDataProviderAttribute>();

        static MyOctreeStorage()
        {
#if XB1 // XB1_ALLINONEASSEMBLY
            RegisterTypes(MyAssembly.AllInOneAssembly);
#else // !XB1
            RegisterTypes(Assembly.GetExecutingAssembly());
            RegisterTypes(MyPlugins.GameAssembly);
            RegisterTypes(MyPlugins.SandboxAssembly);
            RegisterTypes(MyPlugins.UserAssembly);
#endif // !XB1
        }

        private static void RegisterTypes(Assembly assembly)
        {
            if (assembly == null)
                return;

#if XB1 // XB1_ALLINONEASSEMBLY
            System.Diagnostics.Debug.Assert(m_registered == false);
            if (m_registered == true)
                return;
            m_registered = true;
            foreach (var type in MyAssembly.GetTypes())
#else // !XB1
            foreach (var type in assembly.GetTypes())
#endif // !XB1
            {
                var attributes = type.GetCustomAttributes(typeof(MyStorageDataProviderAttribute), false);
                if (attributes == null || attributes.Length == 0)
                    continue;

                Debug.Assert(typeof(IMyStorageDataProvider).IsAssignableFrom(type));
                var attribute = (MyStorageDataProviderAttribute)attributes[0];
                Debug.Assert(type.GetConstructor(System.Type.EmptyTypes) != null, "Storage data provider must have parameterless constructor defined.");
                attribute.ProviderType = type;

                m_attributesById.Add(attribute.ProviderTypeId, attribute);
                m_attributesByType.Add(attribute.ProviderType, attribute);
            }
        }

        private void WriteStorageMetaData(Stream stream)
        {
            new ChunkHeader()
            {
                ChunkType = ChunkTypeEnum.StorageMetaData,
                Version = 1,
                Size = sizeof(Int32) * 4 + 1,
            }.WriteTo(stream);

            stream.WriteNoAlloc(LeafLodCount);
            stream.WriteNoAlloc(Size.X);
            stream.WriteNoAlloc(Size.Y);
            stream.WriteNoAlloc(Size.Z);
            stream.WriteNoAlloc(m_defaultMaterial);
        }

        private void ReadStorageMetaData(Stream stream, ChunkHeader header, ref bool isOldFormat)
        {
            int leafLodCount = stream.ReadInt32();
            Debug.Assert(leafLodCount == LeafLodCount);
            Vector3I size;
            size.X = stream.ReadInt32();
            size.Y = stream.ReadInt32();
            size.Z = stream.ReadInt32();
            Size = size;
            m_defaultMaterial = stream.ReadByteNoAlloc();

            InitTreeHeight();
        }

        const int VERSION_OCTREE_NODES_32BIT_KEY = 1;
        const int CURRENT_VERSION_OCTREE_NODES = 2;
        private static unsafe void WriteOctreeNodes(Stream stream, ChunkTypeEnum type, Dictionary<UInt64, MyOctreeNode> nodes)
        {
            new ChunkHeader()
            {
                ChunkType = type,
                Version = CURRENT_VERSION_OCTREE_NODES,
                Size = nodes.Count * (sizeof(UInt64) + MyOctreeNode.SERIALIZED_SIZE)
            }.WriteTo(stream);

            foreach (var entry in nodes)
            {
                stream.WriteNoAlloc(entry.Key);
                var node = entry.Value;
                stream.WriteNoAlloc(node.ChildMask);
                stream.WriteNoAlloc(node.Data, 0, MyOctreeNode.CHILD_COUNT);
            }
        }

        private static unsafe void ReadOctreeNodes(Stream stream, ChunkHeader header, ref bool isOldFormat, Dictionary<UInt64, MyOctreeNode> contentNodes)
        {
            switch (header.Version)
            {
                case VERSION_OCTREE_NODES_32BIT_KEY:
                    {
                        const int entrySize = sizeof(UInt32) + MyOctreeNode.SERIALIZED_SIZE;
                        Debug.Assert((header.Size % entrySize) == 0);
                        int nodesCount = header.Size / entrySize;
                        MyOctreeNode node;
                        MyCellCoord cell = new MyCellCoord();
                        for (int i = 0; i < nodesCount; i++)
                        {
                            cell.SetUnpack(stream.ReadUInt32());
                            node.ChildMask = stream.ReadByteNoAlloc();
                            stream.ReadNoAlloc(node.Data, 0, MyOctreeNode.CHILD_COUNT);
                            contentNodes.Add(cell.PackId64(), node);
                        }
                        isOldFormat = true;
                    }
                    break;

                case CURRENT_VERSION_OCTREE_NODES:
                    {
                        const int entrySize = sizeof(UInt64) + MyOctreeNode.SERIALIZED_SIZE;
                        Debug.Assert((header.Size % entrySize) == 0);
                        int nodesCount = header.Size / entrySize;
                        MyOctreeNode node;
                        UInt64 key;
                        for (int i = 0; i < nodesCount; i++)
                        {
                            key = stream.ReadUInt64();
                            node.ChildMask = stream.ReadByteNoAlloc();
                            stream.ReadNoAlloc(node.Data, 0, MyOctreeNode.CHILD_COUNT);
                            contentNodes.Add(key, node);
                        }
                    }
                    break;

                default:
                    throw new InvalidBranchException();
            }

        }

        private static void WriteMaterialTable(Stream stream)
        {
            var materials = MyDefinitionManager.Static.GetVoxelMaterialDefinitions();
            MemoryStream ms;
            using (ms = new MemoryStream(1024))
            {
                ms.WriteNoAlloc(materials.Count);
                foreach (var material in materials)
                {
                    ms.Write7BitEncodedInt(material.Index);
                    ms.WriteNoAlloc(material.Id.SubtypeName);
                }
                Debug.Assert(ms.Length < int.MaxValue);
            }
            var array = ms.ToArray();

            new ChunkHeader()
            {
                ChunkType = ChunkTypeEnum.MaterialIndexTable,
                Version = 1,
                Size = array.Length,
            }.WriteTo(stream);

            stream.Write(array, 0, array.Length);
        }

        private static Dictionary<byte, MyVoxelMaterialDefinition> ReadMaterialTable(Stream stream, ChunkHeader header, ref bool isOldFormat)
        {
            int materialCount = stream.ReadInt32();
            var res = new Dictionary<byte, MyVoxelMaterialDefinition>(materialCount);
            for (int i = 0; i < materialCount; ++i)
            {
                int index = stream.Read7BitEncodedInt();
                string name = stream.ReadString();
                Debug.Assert(index < byte.MaxValue);
                MyVoxelMaterialDefinition def = MyDefinitionManager.Static.GetVoxelMaterialDefinition(name);
                if (def == null)
                    def = MyDefinitionManager.Static.GetDefaultVoxelMaterialDefinition();
                res.Add((byte)index, def);
            }

            return res;
        }

        const int VERSION_OCTREE_LEAVES_32BIT_KEY = 2; // also version 1
        const int CURRENT_VERSION_OCTREE_LEAVES = 3;
        private static void WriteOctreeLeaves<TLeaf>(Stream stream, Dictionary<UInt64, TLeaf> leaves) where TLeaf : IMyOctreeLeafNode
        {
            foreach (var entry in leaves)
            {
                var header = new ChunkHeader()
                {
                    ChunkType = entry.Value.SerializedChunkType,
                    Size = entry.Value.SerializedChunkSize + sizeof(UInt64), // increase chunk size by the size of key (which is inserted before it)
                    Version = CURRENT_VERSION_OCTREE_LEAVES,
                };
                header.WriteTo(stream);

                stream.WriteNoAlloc(entry.Key);
                switch (header.ChunkType)
                {
                    case ChunkTypeEnum.ContentLeafOctree:
                        (entry.Value as MyMicroOctreeLeaf).WriteTo(stream);
                        break;

                    case ChunkTypeEnum.ContentLeafProvider:
                        Debug.Assert(header.Size == sizeof(UInt64), "Provider leaf should not serialize any data.");
                        break;

                    case ChunkTypeEnum.MaterialLeafOctree:
                        (entry.Value as MyMicroOctreeLeaf).WriteTo(stream);
                        break;

                    case ChunkTypeEnum.MaterialLeafProvider:
                        Debug.Assert(header.Size == sizeof(UInt64), "Provider leaf should not serialize any data.");
                        break;

                    default:
                        throw new InvalidBranchException();
                }
            }
        }

        private void ReadOctreeLeaf(Stream stream, ChunkHeader header, ref bool isOldFormat, MyStorageDataTypeEnum dataType, out UInt64 key, out MyMicroOctreeLeaf contentLeaf)
        {
            Debug.Assert(
                header.ChunkType == ChunkTypeEnum.ContentLeafOctree ||
                header.ChunkType == ChunkTypeEnum.MaterialLeafOctree);

            MyCellCoord cellCoord = new MyCellCoord();
            if (header.Version <= VERSION_OCTREE_LEAVES_32BIT_KEY)
            {
                UInt32 oldKey = stream.ReadUInt32();
                cellCoord.SetUnpack(oldKey);
                key = cellCoord.PackId64();
                header.Size -= sizeof(UInt32);
                isOldFormat = true;
            }
            else
            {
                Debug.Assert(header.Version == CURRENT_VERSION_OCTREE_LEAVES);
                key = stream.ReadUInt64();
                cellCoord.SetUnpack(key);
                header.Size -= sizeof(UInt64);
            }
            contentLeaf = new MyMicroOctreeLeaf(dataType, LeafLodCount, cellCoord.CoordInLod << (cellCoord.Lod + LeafLodCount));
            contentLeaf.ReadFrom(header, stream);
        }

        private void ReadProviderLeaf(Stream stream, ChunkHeader header, ref bool isOldFormat, HashSet<UInt64> outKeySet)
        {
            Debug.Assert(
                header.ChunkType == ChunkTypeEnum.MaterialLeafProvider ||
                header.ChunkType == ChunkTypeEnum.ContentLeafProvider);

            UInt64 key;
            if (header.Version <= VERSION_OCTREE_LEAVES_32BIT_KEY)
            {
                UInt32 oldKey = stream.ReadUInt32();
                MyCellCoord cell = new MyCellCoord();
                cell.SetUnpack(oldKey);
                key = cell.PackId64();
                header.Size -= sizeof(UInt32);
                isOldFormat = true;
            }
            else
            {
                Debug.Assert(header.Version == CURRENT_VERSION_OCTREE_LEAVES);
                key = stream.ReadUInt64();
                header.Size -= sizeof(UInt64);
            }

            Debug.Assert(!outKeySet.Contains(key));
            outKeySet.Add(key);

            Debug.Assert(header.Size == 0 || header.Version == 1,
                "All versions except 1 should have 0 size of provider leaf chunk.");
            stream.SkipBytes(header.Size);
        }

        private static void WriteDataProvider(Stream stream, IMyStorageDataProvider provider)
        {
            if (provider == null)
                return;

            ChunkHeader header = new ChunkHeader()
            {
                ChunkType = ChunkTypeEnum.DataProvider,
                Version = 2,
                Size = provider.SerializedSize + sizeof(Int32),
            };
            header.WriteTo(stream);
            stream.WriteNoAlloc(m_attributesByType[provider.GetType()].ProviderTypeId);
            provider.WriteTo(stream);
        }

        private static void ReadDataProvider(Stream stream, ChunkHeader header, ref bool isOldFormat, out IMyStorageDataProvider provider)
        {
            const int TERRAIN_DATA_ID = 1;
            switch (header.Version)
            {
                case 2: // this could be any provider
                    {
                        Int32 providerTypeId = stream.ReadInt32();
                        provider = (IMyStorageDataProvider)Activator.CreateInstance(m_attributesById[providerTypeId].ProviderType);
                        header.Size -= sizeof(Int32);
                        provider.ReadFrom(ref header, stream, ref isOldFormat);
                    }
                    break;

                case 1: // Version 1 is only used for terrain data and nothing else can be there. Terrain data has ID 1
                    provider = (IMyStorageDataProvider)Activator.CreateInstance(m_attributesById[TERRAIN_DATA_ID].ProviderType);
                    provider.ReadFrom(ref header, stream, ref isOldFormat);
                    isOldFormat = true;
                    break;

                default:
                    throw new InvalidBranchException();
            }
        }

    }
}
