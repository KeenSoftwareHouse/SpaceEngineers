using Sandbox.Definitions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using VRage.Voxels;
using VRageMath;

namespace Sandbox.Engine.Voxels
{
    partial class MyStorageBase
    {
        private const int MAX_ENCODED_NAME_LENGTH = 256;
        private static readonly byte[] m_encodedNameBuffer = new byte[MAX_ENCODED_NAME_LENGTH];

        private static MyStorageBase Compatibility_LoadCellStorage(int fileVersion, Stream stream)
        {
            //  Size of this voxel map (in voxels)
            Vector3I tmpSize;
            tmpSize.X = stream.ReadInt32();
            tmpSize.Y = stream.ReadInt32();
            tmpSize.Z = stream.ReadInt32();
            var storage = new MyOctreeStorage(null, tmpSize);

            //  Size of data cell in voxels. Has to be the same as current size specified by our constants.
            Vector3I cellSize;
            cellSize.X = stream.ReadInt32();
            cellSize.Y = stream.ReadInt32();
            cellSize.Z = stream.ReadInt32();
#if !XB1
            Trace.Assert(cellSize.X == MyVoxelConstants.DATA_CELL_SIZE_IN_VOXELS &&
                         cellSize.Y == MyVoxelConstants.DATA_CELL_SIZE_IN_VOXELS &&
                         cellSize.Z == MyVoxelConstants.DATA_CELL_SIZE_IN_VOXELS);
#else // XB1
            System.Diagnostics.Debug.Assert(cellSize.X == MyVoxelConstants.DATA_CELL_SIZE_IN_VOXELS &&
                         cellSize.Y == MyVoxelConstants.DATA_CELL_SIZE_IN_VOXELS &&
                         cellSize.Z == MyVoxelConstants.DATA_CELL_SIZE_IN_VOXELS);
#endif // XB1

            Vector3I cellsCount = tmpSize / cellSize;

            Dictionary<byte, MyVoxelMaterialDefinition> mappingTable = null;
            if (fileVersion == STORAGE_TYPE_VERSION_CELL)
            {
                // loading material names->index mappings
                mappingTable = Compatibility_LoadMaterialIndexMapping(stream);
            }
            else if (fileVersion == 1)
            {
                // material name->index mappings were not saved in this version
            }

            var startCoord = Vector3I.Zero;
            var endCoord = new Vector3I(MyVoxelConstants.DATA_CELL_SIZE_IN_VOXELS - 1);

            var cache = new MyStorageData();
            cache.Resize(Vector3I.Zero, endCoord);
            Vector3I cellCoord;
            for (cellCoord.X = 0; cellCoord.X < cellsCount.X; cellCoord.X++)
            {
                for (cellCoord.Y = 0; cellCoord.Y < cellsCount.Y; cellCoord.Y++)
                {
                    for (cellCoord.Z = 0; cellCoord.Z < cellsCount.Z; cellCoord.Z++)
                    {
                        MyVoxelRangeType cellType = (MyVoxelRangeType)stream.ReadByteNoAlloc();

                        //  Cell's are FULL by default, therefore we don't need to change them
                        switch (cellType)
                        {
                            case MyVoxelRangeType.EMPTY: cache.ClearContent(MyVoxelConstants.VOXEL_CONTENT_EMPTY); break;
                            case MyVoxelRangeType.FULL: cache.ClearContent(MyVoxelConstants.VOXEL_CONTENT_FULL); break;
                            case MyVoxelRangeType.MIXED:
                                Vector3I v;
                                for (v.X = 0; v.X < MyVoxelConstants.DATA_CELL_SIZE_IN_VOXELS; v.X++)
                                {
                                    for (v.Y = 0; v.Y < MyVoxelConstants.DATA_CELL_SIZE_IN_VOXELS; v.Y++)
                                    {
                                        for (v.Z = 0; v.Z < MyVoxelConstants.DATA_CELL_SIZE_IN_VOXELS; v.Z++)
                                        {
                                            cache.Content(ref v, stream.ReadByteNoAlloc());
                                        }
                                    }
                                }
                                break;
                        }
                        startCoord = cellCoord * MyVoxelConstants.DATA_CELL_SIZE_IN_VOXELS;
                        endCoord = startCoord + (MyVoxelConstants.DATA_CELL_SIZE_IN_VOXELS - 1);
                        storage.WriteRange(cache, MyStorageDataTypeFlags.Content, ref startCoord, ref endCoord);
                    }
                }
            }

            try
            { // In case materials are not saved, catch any exceptions caused by this.
                // Read materials and indestructible
                for (cellCoord.X = 0; cellCoord.X < cellsCount.X; cellCoord.X++)
                {
                    for (cellCoord.Y = 0; cellCoord.Y < cellsCount.Y; cellCoord.Y++)
                    {
                        for (cellCoord.Z = 0; cellCoord.Z < cellsCount.Z; cellCoord.Z++)
                        {
                            bool isSingleMaterial = stream.ReadByteNoAlloc() == 1;
                            MyVoxelMaterialDefinition material = null;

                            if (isSingleMaterial)
                            {
                                material = Compatibility_LoadCellVoxelMaterial(stream, mappingTable);
                                cache.ClearMaterials(material.Index);
                            }
                            else
                            {
                                byte indestructibleContent;
                                Vector3I voxelCoordInCell;
                                for (voxelCoordInCell.X = 0; voxelCoordInCell.X < MyVoxelConstants.DATA_CELL_SIZE_IN_VOXELS; voxelCoordInCell.X++)
                                {
                                    for (voxelCoordInCell.Y = 0; voxelCoordInCell.Y < MyVoxelConstants.DATA_CELL_SIZE_IN_VOXELS; voxelCoordInCell.Y++)
                                    {
                                        for (voxelCoordInCell.Z = 0; voxelCoordInCell.Z < MyVoxelConstants.DATA_CELL_SIZE_IN_VOXELS; voxelCoordInCell.Z++)
                                        {
                                            material = Compatibility_LoadCellVoxelMaterial(stream, mappingTable);
                                            indestructibleContent = stream.ReadByteNoAlloc();
                                            cache.Material(ref voxelCoordInCell, material.Index);
                                        }
                                    }
                                }
                            }
                            startCoord = cellCoord * MyVoxelConstants.DATA_CELL_SIZE_IN_VOXELS;
                            endCoord = startCoord + (MyVoxelConstants.DATA_CELL_SIZE_IN_VOXELS - 1);
                            storage.WriteRange(cache, MyStorageDataTypeFlags.Material, ref startCoord, ref endCoord);
                        }
                    }
                }
            }
            catch (EndOfStreamException ex)
            {
                MySandboxGame.Log.WriteLine(ex);
            }

            return storage;
        }

        private static MyVoxelMaterialDefinition Compatibility_LoadCellVoxelMaterial(Stream stream, Dictionary<byte, MyVoxelMaterialDefinition> mapping)
        {
            MyVoxelMaterialDefinition retVal = null;

            byte materialIdx = stream.ReadByteNoAlloc();
            if (materialIdx != 0xFF)
            {
                if (mapping != null)
                {
                    // version 2
                    retVal = mapping[materialIdx];
                }
                else
                {
                    // version 1 from miner wars, possibly broken
                    retVal = MyDefinitionManager.Static.GetVoxelMaterialDefinition(materialIdx);
                }
                Debug.Assert(retVal != null);
            }
            else
            {
                // version 1 from space engineers
                var encoding = Encoding.UTF8;
                byte encodedNameLength = stream.ReadByteNoAlloc();
                stream.Read(m_encodedNameBuffer, 0, encodedNameLength);
                string materialName = encoding.GetString(m_encodedNameBuffer, 0, encodedNameLength);
                retVal = MyDefinitionManager.Static.GetVoxelMaterialDefinition(materialName);
                Debug.Assert(retVal != null || materialName == "Helium_01" || materialName == "Helium_02" || materialName == "Ice_01", "Unknown material: " + materialName);
            }

            if (retVal == null)
                retVal = MyDefinitionManager.Static.GetDefaultVoxelMaterialDefinition();

            return retVal;
        }

        private static Dictionary<byte, MyVoxelMaterialDefinition> Compatibility_LoadMaterialIndexMapping(Stream stream)
        {
            int materialCount = stream.Read7BitEncodedInt();

            var result = new Dictionary<byte, MyVoxelMaterialDefinition>(materialCount);
            for (int i = 0; i < materialCount; ++i)
            {
                byte index = stream.ReadByteNoAlloc();
                string name = stream.ReadString();

                MyVoxelMaterialDefinition definition;
                if (!MyDefinitionManager.Static.TryGetVoxelMaterialDefinition(name, out definition))
                {
                    definition = MyDefinitionManager.Static.GetDefaultVoxelMaterialDefinition();
                }
                result.Add(index, definition);
            }
            return result;
        }
    }
}
