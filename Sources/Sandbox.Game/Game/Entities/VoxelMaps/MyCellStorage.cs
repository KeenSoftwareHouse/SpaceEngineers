using Sandbox.Common.ObjectBuilders.Voxels;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.Voxels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using VRage;
using VRage.Common.Utils;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.Entities.VoxelMaps
{
    [MyVoxelStorage("Cell", CURRENT_FILE_VERSION)]
    partial class MyCellStorage : MyStorageBase
    {
        private const int CURRENT_FILE_VERSION = 1;
        private const int MAX_ENCODED_NAME_LENGTH = 256;
        private static readonly byte[] m_encodedNameBuffer = new byte[MAX_ENCODED_NAME_LENGTH];
        private static byte[] m_buffer = new byte[MyVoxelConstants.DATA_CELL_SIZE_IN_VOXELS_TOTAL];
        public static readonly MyQuantizer Quantizer = new MyQuantizer(MyFakes.QUANTIZER_VALUE);

        private MyVoxelContentCell[] m_contentCells;
        private MaterialCell[] m_materialCells;
        private Vector3I m_dataCellsCount;

        public Vector3I DataCellsCount
        {
            get { return m_dataCellsCount; }
        }

        #region Construction, saving and loading

        // Constructor for deserialization only!
        public MyCellStorage(string name) : base(name) { }

        public MyCellStorage(string name, Vector3I size, MyVoxelMap voxelMap) : base(name)
        {
            base.VoxelMap = voxelMap;

            Size = size;
            m_sizeInMeters = Size * MyVoxelConstants.VOXEL_SIZE_IN_METRES;
            m_dataCellsCount = Size / MyVoxelConstants.DATA_CELL_SIZE_IN_VOXELS;
            m_oreDepositCellsCount = m_dataCellsCount;

            AllocateContents();
        }

        private void AllocateContents()
        {
            m_dataCellsCount = Size >> MyVoxelConstants.DATA_CELL_SIZE_IN_VOXELS_BITS;
            var cellsSize = m_dataCellsCount.Size();
            m_contentCells = new MyVoxelContentCell[cellsSize];
            m_materialCells = new MaterialCell[cellsSize];
            var defaultMaterial = MyDefinitionManager.Static.GetDefaultVoxelMaterialDefinition();
            for (int i = 0; i < cellsSize; ++i)
            {
                m_materialCells[i] = new MaterialCell(defaultMaterial);
            }
        }

        protected override void LoadInternal(int fileVersion, Stream stream)
        {
            //  Not supported file version
            Trace.Assert(fileVersion == CURRENT_FILE_VERSION);

            //  Size of this voxel map (in voxels)
            Vector3I tmpSize;
            tmpSize.X = stream.ReadInt32();
            tmpSize.Y = stream.ReadInt32();
            tmpSize.Z = stream.ReadInt32();
            Size = tmpSize;

            m_sizeInMeters = tmpSize * MyVoxelConstants.VOXEL_SIZE_IN_METRES;

            //  Size of data cell in voxels. Has to be the same as current size specified by our constants.
            Vector3I cellSize;
            cellSize.X = stream.ReadInt32();
            cellSize.Y = stream.ReadInt32();
            cellSize.Z = stream.ReadInt32();
            Trace.Assert(cellSize.X == MyVoxelConstants.DATA_CELL_SIZE_IN_VOXELS &&
                         cellSize.Y == MyVoxelConstants.DATA_CELL_SIZE_IN_VOXELS &&
                         cellSize.Z == MyVoxelConstants.DATA_CELL_SIZE_IN_VOXELS);

            Vector3I cellsCount = tmpSize / cellSize;
            m_oreDepositCellsCount = cellsCount;

            Profiler.BeginNextBlock("InitVoxelMap");

            //  Init this voxel map (arrays are allocated, sizes calculated). It must be called before we start reading and seting voxels.
            AllocateContents();

            Profiler.BeginNextBlock("Cells foreach");

            Vector3I cellCoord;
            for (cellCoord.X = 0; cellCoord.X < cellsCount.X; cellCoord.X++)
            {
                for (cellCoord.Y = 0; cellCoord.Y < cellsCount.Y; cellCoord.Y++)
                {
                    for (cellCoord.Z = 0; cellCoord.Z < cellsCount.Z; cellCoord.Z++)
                    {
                        MyVoxelRangeType cellType = (MyVoxelRangeType)stream.ReadByteNoAlloc();

                        //  Cell's are FULL by default, therefore we don't need to change them
                        if (cellType != MyVoxelRangeType.FULL)
                        {
                            MyVoxelContentCell newCell = AddCell(ref cellCoord);

                            //  If cell is empty we don't need to set all its voxels to empty. Just allocate cell and set its type to empty.
                            if (cellType == MyVoxelRangeType.EMPTY)
                            {
                                newCell.SetToEmpty();
                            }
                            else if (cellType == MyVoxelRangeType.MIXED)
                            {
                                stream.Read(m_buffer, 0, cellSize.Size());
                                newCell.SetAllVoxelContents(m_buffer);
                            }
                        }
                    }
                }
            }

            Profiler.BeginNextBlock("Materials + indestructible");

            try
            { // In case materials are not saved, catch any exceptions caused by this.
                // Read materials and indestructible
                for (cellCoord.X = 0; cellCoord.X < cellsCount.X; cellCoord.X++)
                {
                    for (cellCoord.Y = 0; cellCoord.Y < cellsCount.Y; cellCoord.Y++)
                    {
                        for (cellCoord.Z = 0; cellCoord.Z < cellsCount.Z; cellCoord.Z++)
                        {
                            var matCell = GetMaterialCell(ref cellCoord);

                            bool isSingleMaterial = stream.ReadByteNoAlloc() == 1;
                            MyVoxelMaterialDefinition material = MyDefinitionManager.Static.GetDefaultVoxelMaterialDefinition();

                            if (isSingleMaterial)
                            {
                                material = LoadVoxelMaterial(stream);
                                GetMaterialCell(ref cellCoord).Reset(material);
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
                                            material = LoadVoxelMaterial(stream);
                                            indestructibleContent = stream.ReadByteNoAlloc();
                                            matCell.SetMaterial(material, ref voxelCoordInCell);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (EndOfStreamException ex)
            {
                MySandboxGame.Log.WriteLine(ex);
            }

            Profiler.BeginNextBlock("Compute ore deposits");
            RecomputeOreDeposits();
            Profiler.End();
        }

        protected override void SaveInternal(Stream stream)
        {
            //  Size of this voxel map (in voxels)
            stream.WriteNoAlloc(Size.X);
            stream.WriteNoAlloc(Size.Y);
            stream.WriteNoAlloc(Size.Z);

            //  Size of data cell in voxels, doesn't have to be same as current size specified by our constants.
            stream.WriteNoAlloc(MyVoxelConstants.DATA_CELL_SIZE_IN_VOXELS);
            stream.WriteNoAlloc(MyVoxelConstants.DATA_CELL_SIZE_IN_VOXELS);
            stream.WriteNoAlloc(MyVoxelConstants.DATA_CELL_SIZE_IN_VOXELS);

            Vector3I cellCoord;
            for (cellCoord.X = 0; cellCoord.X < DataCellsCount.X; cellCoord.X++)
            {
                for (cellCoord.Y = 0; cellCoord.Y < DataCellsCount.Y; cellCoord.Y++)
                {
                    for (cellCoord.Z = 0; cellCoord.Z < DataCellsCount.Z; cellCoord.Z++)
                    {
                        MyVoxelContentCell voxelCell = GetContentCell(ref cellCoord);
                        if (voxelCell == null)
                        {
                            stream.WriteNoAlloc((byte)MyVoxelRangeType.FULL);
                        }
                        else
                        {
                            stream.WriteNoAlloc((byte)voxelCell.CellType);

                            //  If we are here, cell is empty or mixed. If empty, we don't need to save each individual voxel.
                            //  But if it is mixed, we will do it here.
                            if (voxelCell.CellType == MyVoxelRangeType.MIXED)
                            {
                                Vector3I voxelCoordInCell;
                                for (voxelCoordInCell.X = 0; voxelCoordInCell.X < MyVoxelConstants.DATA_CELL_SIZE_IN_VOXELS; voxelCoordInCell.X++)
                                {
                                    for (voxelCoordInCell.Y = 0; voxelCoordInCell.Y < MyVoxelConstants.DATA_CELL_SIZE_IN_VOXELS; voxelCoordInCell.Y++)
                                    {
                                        for (voxelCoordInCell.Z = 0; voxelCoordInCell.Z < MyVoxelConstants.DATA_CELL_SIZE_IN_VOXELS; voxelCoordInCell.Z++)
                                        {
                                            stream.WriteNoAlloc(voxelCell.GetContent(ref voxelCoordInCell));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // Save material cells
            for (cellCoord.X = 0; cellCoord.X < DataCellsCount.X; cellCoord.X++)
            {
                for (cellCoord.Y = 0; cellCoord.Y < DataCellsCount.Y; cellCoord.Y++)
                {
                    for (cellCoord.Z = 0; cellCoord.Z < DataCellsCount.Z; cellCoord.Z++)
                    {
                        var matCell = GetMaterialCell(ref cellCoord);

                        Vector3I voxelCoordInCell = new Vector3I(0, 0, 0);

                        bool isWholeMaterial = matCell.IsSingleMaterial;
                        stream.WriteNoAlloc((byte)(isWholeMaterial ? 1 : 0));
                        if (isWholeMaterial)
                        {
                            var cellMaterial = matCell.GetMaterial(ref voxelCoordInCell);
                            SaveVoxelMaterial(stream, cellMaterial);
                        }
                        else
                        {
                            const byte INDESTRUCTIBLE_CONTENT = 0;
                            for (voxelCoordInCell.X = 0; voxelCoordInCell.X < MyVoxelConstants.DATA_CELL_SIZE_IN_VOXELS; voxelCoordInCell.X++)
                            {
                                for (voxelCoordInCell.Y = 0; voxelCoordInCell.Y < MyVoxelConstants.DATA_CELL_SIZE_IN_VOXELS; voxelCoordInCell.Y++)
                                {
                                    for (voxelCoordInCell.Z = 0; voxelCoordInCell.Z < MyVoxelConstants.DATA_CELL_SIZE_IN_VOXELS; voxelCoordInCell.Z++)
                                    {
                                        var cellMaterial = matCell.GetMaterial(ref voxelCoordInCell);
                                        SaveVoxelMaterial(stream, cellMaterial);
                                        stream.WriteNoAlloc(INDESTRUCTIBLE_CONTENT);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void SaveVoxelMaterial(Stream stream, MyVoxelMaterialDefinition cellMaterial)
        {
            var encoding = Encoding.UTF8;

            // Old file format stored material index as byte. Replaces old index with 0xFF, followed by number of bytes for name and then name itself.
            // Names are limited to 256 bytes of UTF8 text.
            //compressFile.Add((byte)cellMaterial.Index);
            int byteLength = encoding.GetByteCount(cellMaterial.Id.SubtypeName);
            Trace.Assert(0 < byteLength && byteLength < MAX_ENCODED_NAME_LENGTH, "Length of encoded voxel material name must fit inside single byte.");
            int written = encoding.GetBytes(cellMaterial.Id.SubtypeName, 0, cellMaterial.Id.SubtypeName.Length, m_encodedNameBuffer, 0);
            Debug.Assert(written == byteLength);

            stream.WriteNoAlloc((byte)0xFF); // special value replacing old index.
            stream.WriteNoAlloc((byte)byteLength);
            stream.Write(m_encodedNameBuffer, 0, byteLength);
        }

        private MyVoxelMaterialDefinition LoadVoxelMaterial(Stream stream)
        {
            MyVoxelMaterialDefinition retVal = null;

            byte materialIdx = stream.ReadByteNoAlloc();
            if (materialIdx != 0xFF)
            {
                // old format with material index stored in file
                retVal = MyDefinitionManager.Static.GetVoxelMaterialDefinition(materialIdx);
                Debug.Assert(retVal != null);
            }
            else
            {
                // new format with name stored in file
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

        #endregion

        #region Non-const methods

        protected override void OverwriteAllMaterialsInternal(MyVoxelMaterialDefinition material)
        {
            for (int i = 0; i < m_materialCells.Length; ++i)
            {
                m_materialCells[i].Reset(material);
            }

            RecomputeOreDeposits();
        }

        public void SetMaterial(MyVoxelMaterialDefinition material, ref Vector3I voxelCoord)
        {
            Vector3I cellCoord, voxelCoordInCell;
            ComputeCellCoord(ref voxelCoord, out cellCoord);
            ComputeVoxelCoordInCell(ref voxelCoord, out voxelCoordInCell);
            var materialCell = GetMaterialCell(ref cellCoord);
            var oldMaterial = materialCell.GetMaterial(ref voxelCoordInCell);
            if (oldMaterial == material)
                return;
            materialCell.SetMaterial(material, ref voxelCoordInCell);

            ChangeOreDepositMaterial(oldMaterial, material, ref voxelCoord);
        }

        public void ResetCellMaterial(ref Vector3I cellCoord, MyVoxelMaterialDefinition material)
        {
            GetMaterialCell(ref cellCoord).Reset(material);

            if (material.IsRare)
                RecalculateDeposits(ref cellCoord);
            else
                RemoveOreDepositCell(ref cellCoord);

        }

        public void SetVoxelContent(byte content, ref Vector3I voxelCoord)
        {
            //  We don't change voxel if it's a border voxel and it would be an empty voxel (not full). Because that would make voxel map with wrong/missing edges.
            if ((content > 0) && (IsVoxelAtBorder(ref voxelCoord)))
                return;

            //try { Profiler.Begin("SetVoxelContent");

            Vector3I cellCoord, voxelCoordInCell;
            ComputeCellCoord(ref voxelCoord, out cellCoord);
            ComputeVoxelCoordInCell(ref voxelCoord, out voxelCoordInCell);
            var voxelCell = GetContentCell(ref cellCoord);
            if (voxelCell == null)
            { // cell is null when it is full
                if (content == MyVoxelConstants.VOXEL_CONTENT_FULL)
                {
                    return;
                }
                else
                {
                    //  We are switching cell from type FULL to EMPTY or MIXED, therefore we need to allocate new cell
                    MyVoxelContentCell newCell = AddCell(ref cellCoord);
                    newCell.SetVoxelContent(content, ref voxelCoordInCell);

                    //  We change ore deposit content from full to new content
                    ChangeOreDepositCellContent(MyVoxelConstants.VOXEL_CONTENT_FULL, Quantizer.QuantizeValue(content), ref voxelCoord);
                }
            }
            else if (voxelCell.CellType == MyVoxelRangeType.FULL)
            {
                // Shouldn't full cells be null? This assertion is triggered occasionally.
                //Debug.Fail("Invalid branch.");
                if (content == MyVoxelConstants.VOXEL_CONTENT_FULL)
                {
                    return;
                }
                else
                {
                    voxelCell.SetVoxelContent(content, ref voxelCoordInCell);
                    CheckIfCellChangedToFull(voxelCell, ref cellCoord);

                    //  We change ore deposit content from full to new content
                    ChangeOreDepositCellContent(MyVoxelConstants.VOXEL_CONTENT_FULL, Quantizer.QuantizeValue(content), ref voxelCoord);
                }
            }
            else if (voxelCell.CellType == MyVoxelRangeType.EMPTY)
            {
                if (content == MyVoxelConstants.VOXEL_CONTENT_EMPTY)
                {
                    return;
                }
                else
                {
                    voxelCell.SetVoxelContent(content, ref voxelCoordInCell);
                    CheckIfCellChangedToFull(voxelCell, ref cellCoord);

                    //  We change ore deposit content from empty to new content
                    ChangeOreDepositCellContent(MyVoxelConstants.VOXEL_CONTENT_EMPTY, MyCellStorage.Quantizer.QuantizeValue(content), ref voxelCoord);
                }
            }
            else if (voxelCell.CellType == MyVoxelRangeType.MIXED)
            {
                byte oldContent = voxelCell.GetContent(ref voxelCoordInCell);
                voxelCell.SetVoxelContent(content, ref voxelCoordInCell);
                CheckIfCellChangedToFull(voxelCell, ref cellCoord);

                byte newContent = voxelCell.GetContent(ref voxelCoordInCell);
                //  We change ore deposit content from old to new
                ChangeOreDepositCellContent(oldContent, newContent, ref voxelCoord);
            }
            else
            {
                throw new InvalidBranchException();
            }

            //} finally { Profiler.End(); }
        }

        protected override void CloseInternal()
        {
            ClearOreDeposits();
            var defaultMaterial = MyDefinitionManager.Static.GetDefaultVoxelMaterialDefinition();
            for (int i = 0; i < m_contentCells.Length; ++i)
            {
                MyVoxelContentCell cell = null;
                MyUtils.Swap(ref cell, ref m_contentCells[i]);
                if (cell != null)
                    cell.Deallocate();
                m_materialCells[i].Reset(defaultMaterial);
            }
        }

        #endregion

        #region Const methods

        private int GetContentSumInCell(ref Vector3I dataCellCoord)
        {
            var cell = GetContentCell(ref dataCellCoord);
            if (cell != null)
                return cell.GetVoxelContentSum();
            else
                return MyVoxelConstants.DATA_CELL_CONTENT_SUM_TOTAL;
        }

        public MyVoxelMaterialDefinition GetMaterial(ref Vector3I voxelCoord)
        {
            Vector3I cellCoord, voxelCoordInCell;
            ComputeCellCoord(ref voxelCoord, out cellCoord);
            ComputeVoxelCoordInCell(ref voxelCoord, out voxelCoordInCell);
            return GetMaterialCell(ref cellCoord).GetMaterial(ref voxelCoordInCell);
        }

        public byte GetMaterialIdx(ref Vector3I voxelCoord)
        {
            Vector3I cellCoord, voxelCoordInCell;
            ComputeCellCoord(ref voxelCoord, out cellCoord);
            ComputeVoxelCoordInCell(ref voxelCoord, out voxelCoordInCell);
            return GetMaterialCell(ref cellCoord).GetMaterialIdx(ref voxelCoordInCell);
        }

        public byte GetContent(ref Vector3I voxelCoord)
        {
            Vector3I cellCoord, voxelCoordInCell;
            ComputeCellCoord(ref voxelCoord, out cellCoord);
            ComputeVoxelCoordInCell(ref voxelCoord, out voxelCoordInCell);
            var contentCell = GetContentCell(ref cellCoord);
            if (contentCell != null)
                return contentCell.GetContent(ref voxelCoordInCell);
            else
                return MyVoxelConstants.VOXEL_CONTENT_FULL;
        }

        private MyVoxelContentCell GetContentCell(ref Vector3I cellCoord)
        {
            return m_contentCells[ComputeCellIndex(ref cellCoord)];
        }

        private MaterialCell GetMaterialCell(ref Vector3I cellCoord)
        {
            return m_materialCells[ComputeCellIndex(ref cellCoord)];
        }

        public MyVoxelMaterialDefinition GetDataCellAverageNonRareMaterial(ref Vector3I cellCoord)
        {
            return GetMaterialCell(ref cellCoord).AverageNonRareMaterial;
        }

        public MyVoxelContentCell TryGetContentCell(ref Vector3I cellCoord)
        {
            if (IsValidCellCoord(ref cellCoord))
                return GetContentCell(ref cellCoord);
            else
                return null;
        }

        private MaterialCell TryGetMaterialCell(ref Vector3I cellCoord)
        {
            if (IsValidCellCoord(ref cellCoord))
                return GetMaterialCell(ref cellCoord);
            else
                return null;
        }

        public MyVoxelRangeType GetCellType(ref Vector3I cellCoord)
        {
            Debug.Assert(IsValidCellCoord(ref cellCoord));
            var cell = GetContentCell(ref cellCoord);
            if (cell != null)
                return cell.CellType;
            else
                return MyVoxelRangeType.FULL;
        }

        public byte GetDataCellAverageContent(ref Vector3I cellCoord)
        {
            MyVoxelContentCell cell = TryGetContentCell(ref cellCoord);

            if (cell == null)
            {
                //  Cell wasn't found in cell dictionary, therefore cell must be full
                return MyVoxelConstants.VOXEL_CONTENT_FULL;
            }
            else
            {
                if (cell.CellType == MyVoxelRangeType.EMPTY)
                {
                    return MyVoxelConstants.VOXEL_CONTENT_EMPTY;
                }
                else
                {
                    return cell.GetAverageContent();
                }
            }
        }

        #endregion

        #region Static helper methods

        public static void ComputeCellCoord(ref Vector3I voxelCoord, out Vector3I cellCoord)
        {
            cellCoord.X = voxelCoord.X >> MyVoxelConstants.DATA_CELL_SIZE_IN_VOXELS_BITS;
            cellCoord.Y = voxelCoord.Y >> MyVoxelConstants.DATA_CELL_SIZE_IN_VOXELS_BITS;
            cellCoord.Z = voxelCoord.Z >> MyVoxelConstants.DATA_CELL_SIZE_IN_VOXELS_BITS;
        }

        private static void ComputeVoxelCoordInCell(ref Vector3I voxelCoord, out Vector3I voxelCoordInCell)
        {
            voxelCoordInCell.X = voxelCoord.X & MyVoxelConstants.DATA_CELL_SIZE_IN_VOXELS_MASK;
            voxelCoordInCell.Y = voxelCoord.Y & MyVoxelConstants.DATA_CELL_SIZE_IN_VOXELS_MASK;
            voxelCoordInCell.Z = voxelCoord.Z & MyVoxelConstants.DATA_CELL_SIZE_IN_VOXELS_MASK;
        }

        private static void ComputeVoxelCoordOfCell(ref Vector3I cellCoord, out Vector3I voxelCoordOfCell)
        {
            voxelCoordOfCell.X = cellCoord.X << MyVoxelConstants.DATA_CELL_SIZE_IN_VOXELS_BITS;
            voxelCoordOfCell.Y = cellCoord.Y << MyVoxelConstants.DATA_CELL_SIZE_IN_VOXELS_BITS;
            voxelCoordOfCell.Z = cellCoord.Z << MyVoxelConstants.DATA_CELL_SIZE_IN_VOXELS_BITS;
        }

        #endregion

        #region Settings surface material

        protected override void SetSurfaceMaterialInternal(MyVoxelMaterialDefinition material, int cellThickness)
        {
            SetSurfaceMaterial(material, cellThickness, DataCellsCount, CheckAndChangeSurfaceCell);
        }

        private delegate void CheckAndChangeSurfaceDelegate(MyVoxelMaterialDefinition material, ref Vector3I cellCoord, ref int thickness);

        private void CheckAndChangeSurfaceCell(MyVoxelMaterialDefinition material, ref Vector3I cellCoord, ref int thickness)
        {
            var cell = TryGetContentCell(ref cellCoord);
            if (cell == null || cell.CellType != MyVoxelRangeType.EMPTY)
            {
                ResetCellMaterial(ref cellCoord, material);
                ++thickness;
            }
        }

        private void CheckAndChangeSurfaceVoxel(MyVoxelMaterialDefinition material, ref Vector3I voxelCoord, ref int thickness)
        {
            if (GetContent(ref voxelCoord) != MyVoxelConstants.VOXEL_CONTENT_EMPTY)
            {
                SetMaterial(material, ref voxelCoord);
                ++thickness;
            }
        }

        private void SetSurfaceMaterial(MyVoxelMaterialDefinition material,
            int cellThickness,
            Vector3I max,
            CheckAndChangeSurfaceDelegate checkAndChange)
        {
            Profiler.Begin("MyVoxelMap.SetSurfaceMaterial");

            Debug.Assert(cellThickness >= 1);
            if (cellThickness < 1)
                cellThickness = 1;

            int currentThickness = 0;
            Vector3I coord = Vector3I.Zero;
            for (coord.X = 0; coord.X < max.X; ++coord.X)
                for (coord.Y = 0; coord.Y < max.Y; ++coord.Y)
                {
                    currentThickness = 0;
                    for (coord.Z = 0; coord.Z < max.Z; ++coord.Z)
                    {
                        checkAndChange(material, ref coord, ref currentThickness);
                        if (currentThickness == cellThickness)
                            break;
                    }

                    currentThickness = 0;
                    for (coord.Z = max.Z - 1; coord.Z >= 0; --coord.Z)
                    {
                        checkAndChange(material, ref coord, ref currentThickness);
                        if (currentThickness == cellThickness)
                            break;
                    }
                }


            for (coord.X = 0; coord.X < max.X; ++coord.X)
                for (coord.Z = 0; coord.Z < max.Z; ++coord.Z)
                {
                    currentThickness = 0;
                    for (coord.Y = 0; coord.Y < max.Y; ++coord.Y)
                    {
                        checkAndChange(material, ref coord, ref currentThickness);
                        if (currentThickness == cellThickness)
                            break;
                    }

                    currentThickness = 0;
                    for (coord.Y = max.Y - 1; coord.Y >= 0; --coord.Y)
                    {
                        checkAndChange(material, ref coord, ref currentThickness);
                        if (currentThickness == cellThickness)
                            break;
                    }
                }

            for (coord.Y = 0; coord.Y < max.Y; ++coord.Y)
                for (coord.Z = 0; coord.Z < max.Z; ++coord.Z)
                {
                    currentThickness = 0;
                    for (coord.X = 0; coord.X < max.X; ++coord.X)
                    {
                        checkAndChange(material, ref coord, ref currentThickness);
                        if (currentThickness == cellThickness)
                            break;
                    }

                    currentThickness = 0;
                    for (coord.X = max.X - 1; coord.X >= 0; --coord.X)
                    {
                        checkAndChange(material, ref coord, ref currentThickness);
                        if (currentThickness == cellThickness)
                            break;
                    }
                }

            Profiler.End();
        }

        #endregion

        #region Material merging

        //  Merges specified materials (from file) into our actual voxel map - overwriting materials only.
        //  We are using a regular voxel map to define areas where we want to set a specified material. Empty voxels are ignored and 
        //  only mixed/full voxels are used to tell us that that voxel will contain new material - 'materialToSet'.
        //  If we are seting indestructible material, voxel content values from merged voxel map will be used to define indestructible content.
        //  Parameter 'voxelPosition' - place where we will place merged voxel map withing actual voxel map. It's in voxel coords.
        //  IMPORTANT: THIS METHOD WILL WORK ONLY IF WE PLACE THE MAP THAT WE TRY TO MERGE FROM IN VOXEL COORDINATES THAT ARE MULTIPLY OF DATA CELL SIZE
        //  This method is used to load small material areas, overwriting actual material only if value from file is 1. Zeros are ignored (it's empty space).
        //  This method is quite fast, even on large maps - 512x512x512, so we can do more overwrites.
        //  Parameter 'materialToSet' tells us what material to set at places which are full in file. Empty are ignored - so stay as they were before this method was called.
        //  IMPORTANT: THIS MERGE MATERIAL CAN BE CALLED ONLY AFTER ALL VOXEL CONTENTS ARE LOADED. THAT'S BECAUSE WE NEED TO KNOW THEM FOR MIN CONTENT / INDESTRUCTIBLE CONTENT.
        //  Voxel map we are trying to merge into existing voxel map can be bigger or outside of area of existing voxel map. This method will just ignore those parts.
        // mk:TODO move to Data storage, hide behind interface and make sure this does not overwrite empty area material (in target voxel map)
        protected override void MergeVoxelMaterialsInternal(MyMwcVoxelFilesEnum voxelFile, Vector3I voxelPosition, MyVoxelMaterialDefinition materialToSet)
        {
            Profiler.Begin("MyVoxelMap.MergeVoxelMaterials");

            using (var fileStream = File.OpenRead(MyVoxelFiles.Get(voxelFile).GetVoxFilePath()))
            using (var gzip = new GZipStream(fileStream, CompressionMode.Decompress))
            {
                var storage = gzip.ReadString();
                Debug.Assert(storage == "Cell");

                //  Version of a VOX file
                int fileVersion = gzip.Read7BitEncodedInt();

                //  Not supported VOX file version
                Debug.Assert(fileVersion == CURRENT_FILE_VERSION);

                //  Size of this voxel map (in voxels)
                int sizeX = gzip.ReadInt32();
                int sizeY = gzip.ReadInt32();
                int sizeZ = gzip.ReadInt32();

                //  Size of data cell in voxels, doesn't have to be same as current size specified by our constants.
                int cellSizeX = gzip.ReadInt32();
                int cellSizeY = gzip.ReadInt32();
                int cellSizeZ = gzip.ReadInt32();

                int cellsCountX = sizeX / cellSizeX;
                int cellsCountY = sizeY / cellSizeY;
                int cellsCountZ = sizeZ / cellSizeZ;

                //  This method will work only if we place the map that we try to merge from in voxel coordinates that are multiply of data cell size
                Debug.Assert((voxelPosition.X & MyVoxelConstants.DATA_CELL_SIZE_IN_VOXELS_MASK) == 0);
                Debug.Assert((voxelPosition.Y & MyVoxelConstants.DATA_CELL_SIZE_IN_VOXELS_MASK) == 0);
                Debug.Assert((voxelPosition.Z & MyVoxelConstants.DATA_CELL_SIZE_IN_VOXELS_MASK) == 0);
                Vector3I cellFullForVoxelPosition;
                MyCellStorage.ComputeCellCoord(ref voxelPosition, out cellFullForVoxelPosition);

                Vector3I cellCoord;
                for (cellCoord.X = 0; cellCoord.X < cellsCountX; cellCoord.X++)
                {
                    for (cellCoord.Y = 0; cellCoord.Y < cellsCountY; cellCoord.Y++)
                    {
                        for (cellCoord.Z = 0; cellCoord.Z < cellsCountZ; cellCoord.Z++)
                        {
                            MyVoxelRangeType cellType = (MyVoxelRangeType)gzip.ReadByteNoAlloc();

                            //  We can do "continue" here, becase we need to read this file properly, even if we will ignore that data
                            var tmp = new Vector3I(
                                    cellFullForVoxelPosition.X + cellCoord.X,
                                    cellFullForVoxelPosition.Y + cellCoord.Y,
                                    cellFullForVoxelPosition.Z + cellCoord.Z);
                            bool isDataCellInVoxelMap = IsValidCellCoord(ref tmp);

                            if (cellType == MyVoxelRangeType.EMPTY)
                            {
                                //  If merged cell is empty, there is nothing to overwrite, so we can skip this cell
                                continue;
                            }
                            else if (cellType == MyVoxelRangeType.FULL)
                            {
                                //  If merged cell is full, than we reset whole material cell to 'materialToSet'
                                if (isDataCellInVoxelMap)
                                {

                                    var coord = cellFullForVoxelPosition + cellCoord;
                                    ResetCellMaterial(ref coord, materialToSet);
                                }
                            }
                            else
                            {
                                //Vector3I cellCoordInVoxels = GetVoxelCoordinatesOfDataCell(ref cellCoord);
                                Vector3I cellCoordInVoxels;
                                MyCellStorage.ComputeVoxelCoordOfCell(ref cellCoord, out cellCoordInVoxels);

                                Vector3I voxelCoordRelative;
                                voxelCoordRelative.X = voxelPosition.X + cellCoordInVoxels.X;
                                voxelCoordRelative.Y = voxelPosition.Y + cellCoordInVoxels.Y;
                                voxelCoordRelative.Z = voxelPosition.Z + cellCoordInVoxels.Z;

                                Vector3I voxelCoordInCell;
                                for (voxelCoordInCell.X = 0; voxelCoordInCell.X < cellSizeX; voxelCoordInCell.X++)
                                {
                                    for (voxelCoordInCell.Y = 0; voxelCoordInCell.Y < cellSizeY; voxelCoordInCell.Y++)
                                    {
                                        for (voxelCoordInCell.Z = 0; voxelCoordInCell.Z < cellSizeZ; voxelCoordInCell.Z++)
                                        {
                                            byte voxelFromFile = gzip.ReadByteNoAlloc();

                                            if (isDataCellInVoxelMap)
                                            {
                                                //  Ignore empty voxels, but use mixed/full for seting the material
                                                if (voxelFromFile > MyVoxelConstants.VOXEL_CONTENT_EMPTY)
                                                {
                                                    Vector3I voxelCoord = new Vector3I(
                                                        voxelCoordRelative.X + voxelCoordInCell.X,
                                                        voxelCoordRelative.Y + voxelCoordInCell.Y,
                                                        voxelCoordRelative.Z + voxelCoordInCell.Z);

                                                    //  Actual voxel content
                                                    byte voxelContent = GetContent(ref voxelCoord);

                                                    SetMaterial(materialToSet, ref voxelCoord);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            Profiler.End();
        }

        #endregion

        private MyVoxelContentCell AddCell(ref Vector3I cellCoord)
        {
            //  Adding or creating cell can be made only once
            Debug.Assert(GetContentCell(ref cellCoord) == null);

            MyVoxelContentCell ret = new MyVoxelContentCell();
            m_contentCells[ComputeCellIndex(ref cellCoord)] = ret;

            return ret;
        }

        public bool IsInside(ref Vector3I voxelCoord)
        {
            return 0 <= voxelCoord.X && voxelCoord.X < Size.X &&
                   0 <= voxelCoord.Y && voxelCoord.Y < Size.Y &&
                   0 <= voxelCoord.Z && voxelCoord.Z < Size.Z;
        }

        //  Return true if this voxel is on voxel map border
        private bool IsVoxelAtBorder(ref Vector3I voxelCoord)
        {
            Debug.Assert(IsInside(ref voxelCoord));
            return 0 == voxelCoord.X || voxelCoord.X == (Size.X - 1) ||
                   0 == voxelCoord.Y || voxelCoord.Y == (Size.Y - 1) ||
                   0 == voxelCoord.Z || voxelCoord.Z == (Size.Z - 1);
        }

        private bool IsCellAtBorder(ref Vector3I cellCoord)
        {
            return 0 == cellCoord.X || cellCoord.X == (DataCellsCount.X - 1) ||
                   0 == cellCoord.Y || cellCoord.Y == (DataCellsCount.Y - 1) ||
                   0 == cellCoord.Z || cellCoord.Z == (DataCellsCount.Z - 1);
        }

        //  Checks if cell didn't change to FULL and if is, we set it to null
        private void CheckIfCellChangedToFull(MyVoxelContentCell voxelCell, ref Vector3I cellCoord)
        {
            if (voxelCell.CellType == MyVoxelRangeType.FULL)
            {
                m_contentCells[ComputeCellIndex(ref cellCoord)] = null;
            }
        }

        public bool IsValidCellCoord(ref Vector3I cellCoord)
        {
            return 0 <= cellCoord.X && cellCoord.X < DataCellsCount.X &&
                   0 <= cellCoord.Y && cellCoord.Y < DataCellsCount.Y &&
                   0 <= cellCoord.Z && cellCoord.Z < DataCellsCount.Z;
        }

        private int ComputeCellIndex(ref Vector3I cellCoord)
        {
            return cellCoord.X + m_dataCellsCount.X * (cellCoord.Y + m_dataCellsCount.Y * cellCoord.Z);
        }

        private void ComputeCellCoord(int index, out Vector3I cellCoord)
        {
            int xyMultiplied = DataCellsCount.X * DataCellsCount.Y;
            cellCoord.Z = index / xyMultiplied;
            index %= xyMultiplied;
            cellCoord.Y = index / DataCellsCount.X;
            index %= DataCellsCount.X;
            cellCoord.X = index;
        }

        public void ForEachMaterial(MaterialLeafCallback materialLeafCallback, bool skipEmptyContent, bool rareMaterialOnly)
        {
            Vector3I cellCoord;
            var cellSize = new Vector3I(MyVoxelConstants.DATA_CELL_SIZE_IN_VOXELS);
            MaterialLeaf leaf = new MaterialLeaf(ref cellSize);
            var defaultMaterial = MyDefinitionManager.Static.GetDefaultVoxelMaterialDefinition();
            for (int i = 0; i < m_materialCells.Length; ++i)
            {
                ComputeCellCoord(i, out cellCoord);
                ComputeVoxelCoordOfCell(ref cellCoord, out leaf.VoxelCoordStart);

                var contentCell = m_contentCells[i];
                var materialCell = m_materialCells[i];
                if (contentCell != null && contentCell.CellType == MyVoxelRangeType.EMPTY)
                {
                    if (skipEmptyContent || rareMaterialOnly)
                        continue;
                    leaf.Material = defaultMaterial;
                    leaf.Content = 0;
                    materialLeafCallback(ref leaf);
                }
                else if (materialCell.IsSingleMaterial)
                {
                    leaf.Material = materialCell.AverageMaterial;
                    if (rareMaterialOnly && !leaf.Material.IsRare)
                        continue;
                    leaf.Content = (contentCell == null) ? MyVoxelConstants.DATA_CELL_CONTENT_SUM_TOTAL : contentCell.GetVoxelContentSum();
                    materialLeafCallback(ref leaf);
                }
                else
                {
                    MaterialLeaf subLeaf = new MaterialLeaf(ref Vector3I.One);
                    Vector3I voxelCoordInCell;
                    for (voxelCoordInCell.X = 0; voxelCoordInCell.X < leaf.Size.X; ++voxelCoordInCell.X)
                    {
                        for (voxelCoordInCell.Y = 0; voxelCoordInCell.Y < leaf.Size.Y; ++voxelCoordInCell.Y)
                        {
                            for (voxelCoordInCell.Z = 0; voxelCoordInCell.Z < leaf.Size.Z; ++voxelCoordInCell.Z)
                            {
                                subLeaf.Material = materialCell.GetMaterial(ref voxelCoordInCell);
                                if (rareMaterialOnly && !subLeaf.Material.IsRare)
                                    continue;

                                subLeaf.Content = (contentCell != null) ? contentCell.GetContent(ref voxelCoordInCell) : MyVoxelConstants.VOXEL_CONTENT_FULL;
                                if (skipEmptyContent && subLeaf.Content == MyVoxelConstants.VOXEL_CONTENT_EMPTY)
                                    continue;

                                subLeaf.VoxelCoordStart = voxelCoordInCell + leaf.VoxelCoordStart;
                                materialLeafCallback(ref subLeaf);
                                Debug.Assert(subLeaf.Size == Vector3I.One, "Callback changed size of the cell. This must not happen (it is const ref)!");
                            }
                        }
                    }
                }

                Debug.Assert(leaf.Size == cellSize, "Callback changed size of the cell. This must not happen (it is const ref)!");
            }
        }

        public struct MaterialLeaf
        {
            public Vector3I VoxelCoordStart;
            public Vector3I Size;
            public MyVoxelMaterialDefinition Material;
            public int Content;

            public MaterialLeaf(ref Vector3I size)
            {
                Size = size;
                VoxelCoordStart = Vector3I.Zero;
                Material = null;
                Content = 0;
            }
        }

        public delegate void MaterialLeafCallback(ref MaterialLeaf leaf);

        public override void GetAllMaterialsPresent(HashSet<MyVoxelMaterialDefinition> outputMaterialSet)
        {
            for (int i = 0; i < m_contentCells.Length; ++i)
            {
                var contentCell = m_contentCells[i];
                if (contentCell == null || contentCell.GetVoxelContentSum() > 0)
                    m_materialCells[i].GetAllMaterialsPresent(outputMaterialSet);
            }
        }

        private Vector3 GetVoxelPositionAbsolute(ref Vector3I voxelCoord)
        {
            return VoxelMap.GetVoxelPositionAbsolute(ref voxelCoord);
        }

        public override void DebugDraw(MyVoxelDebugDrawMode mode, int modeArg)
        {
            switch (mode)
            {
                case MyVoxelDebugDrawMode.EmptyCells: DebugDrawCells(MyVoxelRangeType.EMPTY); break;
                case MyVoxelDebugDrawMode.FullCells: DebugDrawCells(MyVoxelRangeType.FULL); break;
                case MyVoxelDebugDrawMode.MixedCells: DebugDrawCells(MyVoxelRangeType.MIXED); break;
            }
        }

        internal void DebugDrawCells(MyVoxelRangeType type)
        {
            var color = Color.NavajoWhite;
            color.A = 25;
            using (var batch = VRageRender.MyRenderProxy.DebugDrawBatchAABB(Matrix.Identity, color, true, true))
            {
                for (int cellIdx = 0; cellIdx < m_contentCells.Length; ++cellIdx)
                {
                    var cell = m_contentCells[cellIdx];
                    if ((cell != null && cell.CellType == type) ||
                        (cell == null && type == MyVoxelRangeType.FULL))
                    {
                        Vector3I coord;
                        ComputeCellCoord(cellIdx, out coord);
                        ComputeVoxelCoordOfCell(ref coord, out coord);
                        Vector3 min = (coord + new Vector3(0.5f)) * MyVoxelConstants.VOXEL_SIZE_IN_METRES + VoxelMap.PositionLeftBottomCorner;
                        Vector3 max = min + MyVoxelConstants.DATA_CELL_SIZE_IN_METRES;
                        var bb = new BoundingBox(min, max);
                        batch.Add(ref bb);
                    }
                }
            }
        }

        public override MyVoxelRangeType GetRangeType(int lodIndex, ref Vector3I lodVoxelRangeMin, ref Vector3I lodVoxelRangeMax)
        {
            const int SHIFT = MyVoxelConstants.DATA_CELL_SIZE_IN_VOXELS_BITS;
            const int CELL_SIZE_MINUS_ONE = MyVoxelConstants.DATA_CELL_SIZE_IN_VOXELS - 1;

            var voxelRangeMin = lodVoxelRangeMin << lodIndex;
            var voxelRangeMax = lodVoxelRangeMax << lodIndex;
            var cellStart = voxelRangeMin >> SHIFT;
            var cellEnd = voxelRangeMax >> SHIFT;
            Vector3I cell, cellInVoxels, startInCell, endInCell;
            bool containsCellX, containsCellY, containsCellZ;
            bool foundFull = false, foundEmpty = false;
            for (cell.Z = cellStart.Z; cell.Z <= cellEnd.Z; ++cell.Z)
            {
                cellInVoxels.Z = cell.Z << SHIFT;
                startInCell.Z = Math.Max(voxelRangeMin.Z - cellInVoxels.Z, 0);
                endInCell.Z = Math.Min(voxelRangeMax.Z - cellInVoxels.Z, CELL_SIZE_MINUS_ONE);
                containsCellZ = startInCell.Z == 0 && endInCell.Z == CELL_SIZE_MINUS_ONE;

                for (cell.Y = cellStart.Y; cell.Y <= cellEnd.Y; ++cell.Y)
                {
                    cellInVoxels.Y = cell.Y << SHIFT;
                    startInCell.Y = Math.Max(voxelRangeMin.Y - cellInVoxels.Y, 0);
                    endInCell.Y = Math.Min(voxelRangeMax.Y - cellInVoxels.Y, CELL_SIZE_MINUS_ONE);
                    containsCellY = startInCell.Y == 0 && endInCell.Y == CELL_SIZE_MINUS_ONE;

                    for (cell.X = cellStart.X; cell.X <= cellEnd.X; ++cell.X)
                    {
                        cellInVoxels.X = cell.X << SHIFT;
                        startInCell.X = Math.Max(voxelRangeMin.X - cellInVoxels.X, 0);
                        endInCell.X = Math.Min(voxelRangeMax.X - cellInVoxels.X, CELL_SIZE_MINUS_ONE);
                        containsCellX = startInCell.X == 0 && endInCell.X == CELL_SIZE_MINUS_ONE;
                        MyVoxelRangeType type;
                        if (containsCellX && containsCellY && containsCellZ)
                        { // fully contained
                            type = GetCellType(ref cell);
                        }
                        else
                        { // partial overlap
                            var contentCell = TryGetContentCell(ref cell);
                            if (contentCell == null)
                            {
                                type = MyVoxelRangeType.FULL;
                            }
                            else if (contentCell.CellType != MyVoxelRangeType.MIXED)
                            {
                                type = contentCell.CellType;
                            }
                            else
                            {
                                type = GetRangeTypeInCell(contentCell, ref startInCell, ref endInCell);
                            }
                        }
                        switch (type)
                        {
                            case MyVoxelRangeType.FULL: foundFull = true; break;
                            case MyVoxelRangeType.EMPTY: foundEmpty = true; break;
                            case MyVoxelRangeType.MIXED: return MyVoxelRangeType.MIXED; break;
                            default:
                                throw new InvalidBranchException();
                                break;
                        }
                        if (foundFull && foundEmpty)
                            return MyVoxelRangeType.MIXED;
                    }
                }
            }

            return foundFull ? MyVoxelRangeType.FULL
                : foundEmpty ? MyVoxelRangeType.EMPTY
                : MyVoxelRangeType.MIXED;
        }

        private MyVoxelRangeType GetRangeTypeInCell(MyVoxelContentCell contentCell, ref Vector3I startInCell, ref Vector3I endInCell)
        {
            Vector3I c = Vector3I.Zero;
            bool foundFull = false, foundEmpty = false;
            for (c.Z = startInCell.Z; c.Z <= endInCell.Z; ++c.Z)
            {
                for (c.Y = startInCell.Y; c.Y <= endInCell.Y; ++c.Y)
                {
                    for (c.X = startInCell.X; c.X <= endInCell.X; ++c.X)
                    {
                        var content = contentCell.GetContent(ref c);
                        if (content >= MyVoxelConstants.VOXEL_CONTENT_FULL)
                            foundFull = true;
                        else if (content <= MyVoxelConstants.VOXEL_CONTENT_EMPTY)
                            foundEmpty = true;
                        else
                            return MyVoxelRangeType.MIXED;

                        if (foundFull && foundEmpty)
                            return MyVoxelRangeType.MIXED;
                    }
                }
            }
            Debug.Assert(foundFull != foundEmpty);
            return foundFull ? MyVoxelRangeType.FULL : MyVoxelRangeType.EMPTY;
        }

        #region Reading and writing data

        public override void ReadRange(MyStorageDataCache target, bool readContent, bool readMaterials, int lodIndex, ref Vector3I voxelCoordStart, ref Vector3I voxelCoordEnd)
        {
            Debug.Assert(lodIndex == 0 || lodIndex == MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_VOXELS_BITS);
            if (lodIndex == 0)
            {
                Debug.Assert(readContent || readMaterials);
                const int SHIFT = MyVoxelConstants.DATA_CELL_SIZE_IN_VOXELS_BITS;
                const int CELL_SIZE = MyVoxelConstants.DATA_CELL_SIZE_IN_VOXELS;
                const int CELL_SIZE_MINUS_ONE = CELL_SIZE - 1;

                var cellStart = voxelCoordStart >> SHIFT;
                var cellEnd = voxelCoordEnd >> SHIFT;
                var baseWriteOffset = (cellStart << SHIFT) - voxelCoordStart;

                Vector3I cell, cellInVoxels, writeOffset;
                Vector3I startInCell, endInCell;
                for (cell.Z = cellStart.Z, writeOffset.Z = baseWriteOffset.Z; cell.Z <= cellEnd.Z; ++cell.Z, writeOffset.Z += CELL_SIZE)
                {
                    cellInVoxels.Z = cell.Z << SHIFT;
                    startInCell.Z = Math.Max(voxelCoordStart.Z - cellInVoxels.Z, 0);
                    endInCell.Z = Math.Min(voxelCoordEnd.Z - cellInVoxels.Z, CELL_SIZE_MINUS_ONE);

                    for (cell.Y = cellStart.Y, writeOffset.Y = baseWriteOffset.Y; cell.Y <= cellEnd.Y; ++cell.Y, writeOffset.Y += CELL_SIZE)
                    {
                        cellInVoxels.Y = cell.Y << SHIFT;
                        startInCell.Y = Math.Max(voxelCoordStart.Y - cellInVoxels.Y, 0);
                        endInCell.Y = Math.Min(voxelCoordEnd.Y - cellInVoxels.Y, CELL_SIZE_MINUS_ONE);

                        for (cell.X = cellStart.X, writeOffset.X = baseWriteOffset.X; cell.X <= cellEnd.X; ++cell.X, writeOffset.X += CELL_SIZE)
                        {
                            cellInVoxels.X = cell.X << SHIFT;
                            startInCell.X = Math.Max(voxelCoordStart.X - cellInVoxels.X, 0);
                            endInCell.X = Math.Min(voxelCoordEnd.X - cellInVoxels.X, CELL_SIZE_MINUS_ONE);

                            if (readContent)
                                ReadRange(target, TryGetContentCell(ref cell), ref startInCell, ref endInCell, ref writeOffset);
                            if (readMaterials)
                                ReadRange(target, TryGetMaterialCell(ref cell), ref startInCell, ref endInCell, ref writeOffset);
                        }
                    }
                }
            }
            else
            {
                byte defaultMaterial = MyDefinitionManager.Static.GetDefaultVoxelMaterialDefinition().Index;
                Vector3I cellP, p;
                for (cellP.Z = voxelCoordStart.Z; cellP.Z <= voxelCoordEnd.Z; ++cellP.Z)
                {
                    p.Z = cellP.Z - voxelCoordStart.Z;
                    for (cellP.Y = voxelCoordStart.Y; cellP.Y <= voxelCoordEnd.Y; ++cellP.Y)
                    {
                        p.Y = cellP.Y - voxelCoordStart.Y;
                        for (cellP.X = voxelCoordStart.X; cellP.X <= voxelCoordEnd.X; ++cellP.X)
                        {
                            p.X = cellP.X - voxelCoordStart.X;

                            if (readContent)
                            {
                                var cell = TryGetContentCell(ref cellP);
                                byte content = (cell != null) ? cell.GetAverageContent() : MyVoxelConstants.VOXEL_CONTENT_FULL;
                                target.Content(ref p, content);
                            }

                            if (readMaterials)
                            {
                                var cell = TryGetMaterialCell(ref cellP);
                                target.Material(ref p, (cell != null) ? cell.AverageNonRareMaterial.Index : defaultMaterial);
                            }
                        }
                    }
                }
            }
        }

        private void ReadRange(
            MyStorageDataCache target,
            MyVoxelContentCell cell,
            ref Vector3I startInCell,
            ref Vector3I endInCell,
            ref Vector3I writeOffset)
        {
            Vector3I coordInCell;
            Vector3I writeCoord;
            byte? cellValue = null;
            if (cell == null || cell.CellType == MyVoxelRangeType.FULL)
            {
                cellValue = MyVoxelConstants.VOXEL_CONTENT_FULL;
            }
            else if (cell != null && cell.CellType == MyVoxelRangeType.EMPTY)
            {
                cellValue = MyVoxelConstants.VOXEL_CONTENT_EMPTY;
            }
            var offset = writeOffset + startInCell;

            if (cellValue.HasValue)
            { // write same value everywhere
                var value = cellValue.Value;
                for (coordInCell.Z = startInCell.Z, writeCoord.Z = offset.Z; coordInCell.Z <= endInCell.Z; ++coordInCell.Z, ++writeCoord.Z)
                {
                    for (coordInCell.Y = startInCell.Y, writeCoord.Y = offset.Y; coordInCell.Y <= endInCell.Y; ++coordInCell.Y, ++writeCoord.Y)
                    {
                        for (coordInCell.X = startInCell.X, writeCoord.X = offset.X; coordInCell.X <= endInCell.X; ++coordInCell.X, ++writeCoord.X)
                        {
                            target.Content(ref writeCoord, value);
                        }
                    }
                }
            }
            else
            { // read value for each voxel
                for (coordInCell.Z = startInCell.Z, writeCoord.Z = offset.Z; coordInCell.Z <= endInCell.Z; ++coordInCell.Z, ++writeCoord.Z)
                {
                    for (coordInCell.Y = startInCell.Y, writeCoord.Y = offset.Y; coordInCell.Y <= endInCell.Y; ++coordInCell.Y, ++writeCoord.Y)
                    {
                        for (coordInCell.X = startInCell.X, writeCoord.X = offset.X; coordInCell.X <= endInCell.X; ++coordInCell.X, ++writeCoord.X)
                        {
                            target.Content(ref writeCoord, cell.GetContent(ref coordInCell));
                        }
                    }
                }
            }
        }

        private void ReadRange(
            MyStorageDataCache target,
            MaterialCell cell,
            ref Vector3I startInCell,
            ref Vector3I endInCell,
            ref Vector3I writeOffset)
        {
            Vector3I coordInCell;
            Vector3I writeCoord;
            Vector3I offset = writeOffset + startInCell;
            if (cell == null)
            {
                byte material = MyDefinitionManager.Static.GetDefaultVoxelMaterialDefinition().Index;
                for (coordInCell.Z = startInCell.Z, writeCoord.Z = offset.Z; coordInCell.Z <= endInCell.Z; ++coordInCell.Z, ++writeCoord.Z)
                {
                    for (coordInCell.Y = startInCell.Y, writeCoord.Y = offset.Y; coordInCell.Y <= endInCell.Y; ++coordInCell.Y, ++writeCoord.Y)
                    {
                        for (coordInCell.X = startInCell.X, writeCoord.X = offset.X; coordInCell.X <= endInCell.X; ++coordInCell.X, ++writeCoord.X)
                        {
                            target.Material(ref writeCoord, material);
                        }
                    }
                }
            }
            else
            {
                for (coordInCell.Z = startInCell.Z, writeCoord.Z = offset.Z; coordInCell.Z <= endInCell.Z; ++coordInCell.Z, ++writeCoord.Z)
                {
                    for (coordInCell.Y = startInCell.Y, writeCoord.Y = offset.Y; coordInCell.Y <= endInCell.Y; ++coordInCell.Y, ++writeCoord.Y)
                    {
                        for (coordInCell.X = startInCell.X, writeCoord.X = offset.X; coordInCell.X <= endInCell.X; ++coordInCell.X, ++writeCoord.X)
                        {
                            target.Material(ref writeCoord, cell.GetMaterialIdx(ref coordInCell));
                        }
                    }
                }
            }
        }

        protected override void WriteRangeInternal(MyStorageDataCache source, bool writeContent, bool writeMaterials, ref Vector3I voxelCoordStart, ref Vector3I voxelCoordEnd)
        {
            Vector3I voxel, read;
            for (voxel.Z = voxelCoordStart.Z, read.Z = 0; voxel.Z <= voxelCoordEnd.Z; ++voxel.Z, ++read.Z)
            {
                for (voxel.Y = voxelCoordStart.Y, read.Y = 0; voxel.Y <= voxelCoordEnd.Y; ++voxel.Y, ++read.Y)
                {
                    for (voxel.X = voxelCoordStart.X, read.X = 0; voxel.X <= voxelCoordEnd.X; ++voxel.X, ++read.X)
                    {
                        if (writeContent)
                            SetVoxelContent(source.Content(ref read), ref voxel);
                        if (writeMaterials)
                            SetMaterial(MyDefinitionManager.Static.GetVoxelMaterialDefinition(source.Material(ref read)), ref voxel);
                    }
                }
            }
        }
        #endregion
    }
}
