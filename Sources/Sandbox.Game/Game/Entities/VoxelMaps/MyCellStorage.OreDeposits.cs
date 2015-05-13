using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.Debugging;
using Sandbox.Game.Voxels;
using VRage;
using VRageMath;
using VRage.Collections;

namespace Sandbox.Game.Entities.VoxelMaps
{
    partial class MyCellStorage
    {
        private Vector3 m_sizeInMeters;

        private Vector3I m_oreDepositCellsCount;

        public void ClearOreDeposits()
        {
            OreDepositsMutable.Clear();
        }

        public override void RecomputeOreDeposits()
        {
            long start = MyPerformanceCounter.ElapsedTicks;

            DepositCell deposit = null;
            ClearOreDeposits();
            for (int cellIndex = 0; cellIndex < m_materialCells.Length; ++cellIndex)
            {
                if (deposit == null)
                    deposit = new DepositCell(this);
                deposit.Recompute(cellIndex);
                if (deposit.TotalRareOreContent > 0)
                {
                    AddDepositCell(deposit);
                    deposit = null;
                }
            }

            long end = MyPerformanceCounter.ElapsedTicks;
            MySandboxGame.Log.WriteLine(string.Format("MyOreDeposits.Recompute: {0} ms", MyPerformanceCounter.TicksToMs(end - start)));
            AssertCheckOreDeposits();
        }

        [Conditional("DEBUG")]
        private void AssertCheckOreDeposits()
        {
            foreach (var oreDeposit in OreDepositsMutable)
            {
                Debug.Assert(oreDeposit.Value.TotalRareOreContent > 0);
                Debug.Assert(oreDeposit.Value.GetOreWithContent().Count > 0);
            }

            Dictionary<byte, int> materialsByIndex = new Dictionary<byte, int>();
            for (int i = 0; i < m_materialCells.Length; ++i)
            {
                var contentCell = m_contentCells[i];
                var materialCell = m_materialCells[i];
                if ((contentCell != null && contentCell.CellType == MyVoxelRangeType.EMPTY) ||
                    !materialCell.ContainsRareMaterial)
                {
                    Debug.Assert(!OreDepositsMutable.ContainsKey(i));
                }
                else
                {
                    for (int j = 0; j < MaterialCell.voxelsInCell; ++j)
                    {
                        var material = materialCell.GetMaterial(j);
                        var content = (contentCell == null) ? MyVoxelConstants.VOXEL_CONTENT_FULL : contentCell.GetContent(j);
                        if (material.IsRare && content > 0)
                        {
                            int contentPresent;
                            materialsByIndex.TryGetValue(material.Index, out contentPresent);
                            contentPresent += content;
                            materialsByIndex[material.Index] = contentPresent;
                        }
                    }
                    if (materialsByIndex.Count > 0)
                    {
                        IMyDepositCell deposit;
                        if (OreDepositsMutable.TryGetValue(i, out deposit))
                        {
                            foreach (var entry in materialsByIndex)
                            {
                                var material = MyDefinitionManager.Static.GetVoxelMaterialDefinition(entry.Key);
                                Debug.Assert(deposit.GetOreWithContent().Contains(material));
                            }
                        }
                        else
                        {
                            Debug.Fail("Missing deposit cell when there is rare ore.");
                        }
                    }
                    else
                    {
                        Debug.Assert(!OreDepositsMutable.ContainsKey(i));
                    }
                    materialsByIndex.Clear();
                }
            }
        }

        public void ChangeOreDepositCellContent(byte oldContent, byte newContent, ref Vector3I voxelCoord)
        {
            // if contents are same, then do nothing
            if (oldContent == newContent)
            {
                return;
            }

            MyVoxelMaterialDefinition material = GetMaterial(ref voxelCoord);

            // we change content only if material is ore
            if (!material.IsRare)
                return;

            Vector3I cellCoord;
            ComputeCellCoord(ref voxelCoord, out cellCoord);
            var oreDepositCell = GetOreDepositCell(ref cellCoord, createIfNeeded: true) as DepositCell;
            Debug.Assert(oreDepositCell.Storage != null);

            int content = newContent - oldContent;
            oreDepositCell.AddOreContent(material, content);
            if (oreDepositCell.TotalRareOreContent == 0)
            {
                RemoveOreDepositCell(ComputeCellIndex(ref cellCoord));
            }
        }

        public void ChangeOreDepositMaterial(MyVoxelMaterialDefinition oldMaterial, MyVoxelMaterialDefinition newMaterial, ref Vector3I voxelCoord)
        {
            int content = GetContent(ref voxelCoord);

            // if there is no content in voxel, then do nothing
            if (content == MyVoxelConstants.VOXEL_CONTENT_EMPTY)
            {
                return;
            }

            Vector3I cellCoord;
            ComputeCellCoord(ref voxelCoord, out cellCoord);
            DepositCell oreDepositCell = GetOreDepositCell(ref cellCoord) as DepositCell;


            // if new material is ore, then we must add it to ore deposit cell
            if (newMaterial.IsRare)
            {
                if (oreDepositCell == null)
                    oreDepositCell = ComputeAndAddDepositCell(ref cellCoord);
                else
                    oreDepositCell.AddOreContent(newMaterial, content);
            }

            // if old material is ore, then we must remove it from ore deposit cell
            if (oldMaterial.IsRare && oreDepositCell != null)
            {
                oreDepositCell.AddOreContent(oldMaterial, -content);

                if (oreDepositCell.TotalRareOreContent <= 0)
                    RemoveOreDepositCell(ref cellCoord);
            }
        }

        public void RecalculateDeposits(ref Vector3I cellCoord)
        {
            (GetOreDepositCell(ref cellCoord, createIfNeeded: true) as DepositCell).RecalculateDeposits();
        }

        public void RemoveOreDepositCell(ref Vector3I cellCoord)
        {
            RemoveOreDepositCell(ComputeCellIndex(ref cellCoord));
        }

        private void RemoveOreDepositCell(int cellIndex)
        {
            IMyDepositCell oreDepositCell;
            if (OreDepositsMutable.TryGetValue(cellIndex, out oreDepositCell))
            {
                OreDepositsMutable.Remove(cellIndex);
            }
        }

        private IMyDepositCell GetOreDepositCell(ref Vector3I cellCoord, bool createIfNeeded = false)
        {
            int cellIndex = ComputeCellIndex(ref cellCoord);
            return GetOreDepositCell(cellIndex, createIfNeeded);
        }

        private IMyDepositCell GetOreDepositCell(int cellIndex, bool createIfNeeded = false)
        {
            IMyDepositCell cell;
            OreDepositsMutable.TryGetValue(cellIndex, out cell);
            if (createIfNeeded && cell == null)
            {
                cell = ComputeAndAddDepositCell(cellIndex);
            }
            return cell;
        }

        private DepositCell ComputeAndAddDepositCell(ref Vector3I cellCoord)
        {
            //  Adding or creating cell can be made only once
            int cellIndex = ComputeCellIndex(ref cellCoord);
            return ComputeAndAddDepositCell(cellIndex);
        }

        private DepositCell ComputeAndAddDepositCell(int cellIndex)
        {
            DepositCell ret = new DepositCell(this);
            ret.Recompute(cellIndex);
            AddDepositCell(ret);
            return ret;
        }

        private void AddDepositCell(DepositCell cell)
        {
            Debug.Assert(!OreDepositsMutable.ContainsKey(cell.CellIndex));
            OreDepositsMutable[cell.CellIndex] = cell;
        }

        private void ComputeWorldCenter(int cellIndex, out Vector3 worldCenter)
        {
            Vector3I cellCoord;
            ComputeCellCoord(cellIndex, out cellCoord);
            Vector3 cellSize = (m_sizeInMeters / m_dataCellsCount);
            Vector3 cellHalfExtents = cellSize * 0.5f;
            worldCenter = VoxelMap.PositionLeftBottomCorner + (cellCoord * cellSize) + cellHalfExtents;
        }

    }
}
