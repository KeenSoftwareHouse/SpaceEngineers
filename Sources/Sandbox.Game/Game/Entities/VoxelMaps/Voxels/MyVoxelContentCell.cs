using System.Collections.Generic;
using Sandbox.Engine.Utils;
using VRage.Common.Utils;
using SysUtils.Utils;
using VRageMath;
using System;
using Sandbox.Game.Entities;
using System.Diagnostics;
using Sandbox.Game.Entities.VoxelMaps;

//  This class holds data about voxel cell, but doesn't hold content of each voxel. That's in MyVoxelCellContent.

namespace Sandbox.Game.Voxels
{
    //  This enum tells us if cell is 100% empty, 100% full or mixed (some voxels are full, some empty, some are something between)
    public enum MyVoxelRangeType : byte
    {
        EMPTY,
        FULL,
        MIXED
    }

    class MyVoxelContentCell
    {
        //  Cell type. Default is FULL.
        public MyVoxelRangeType CellType { get; private set; }
        
        //  Reference to cell's content (array of voxel values). Only if cell type is MIXED.
        MyVoxelContentCellContent m_cellContent = null;

        //  Sums all voxel values. Default is summ of all full voxel in cell, so by subtracting we can switch cell from MIXED to EMPTY.
        int m_voxelContentSum;


        public MyVoxelContentCell()
        {
            //  Default cell is FULL
            CellType = MyVoxelRangeType.FULL;

            //  Sums all voxel values. Default is summ of all full voxel in cell, so be subtracting we can switch cell from MIXED to EMPTY.
            m_voxelContentSum = MyVoxelConstants.DATA_CELL_CONTENT_SUM_TOTAL;
        }

        //  Voxel at specified coordinate 'x, y, z' sets to value 'content'. Coordinates are relative to voxel cell
        //  IMPORTANT: Do not call this method directly! Always call it through MyVoxelMap.SetVoxelContent()
        public void SetVoxelContent(byte content, ref Vector3I voxelCoordInCell)
        {
            content = MyCellStorage.Quantizer.QuantizeValue(content);

            if (CellType == MyVoxelRangeType.FULL)
            {
                if (content == MyVoxelConstants.VOXEL_CONTENT_FULL)
                {
                    //  Nothing is changing
                    return;
                }
                else
                {
                    m_voxelContentSum -= (MyVoxelConstants.VOXEL_CONTENT_FULL - content);
                    CheckCellType();

                    //  If this cell is mixed, we change voxel's value in the cell content array, but first allocate the array
                    if (CellType == MyVoxelRangeType.MIXED)
                    {
                        m_cellContent = MyVoxelContentCellContents.Allocate();
                        if (m_cellContent != null)
                        {
                            m_cellContent.Reset(MyVoxelConstants.VOXEL_CONTENT_FULL);
                            m_cellContent.SetVoxelContent(content, ref voxelCoordInCell);
                        }
                    }
                }
            }
            else if (CellType == MyVoxelRangeType.EMPTY)
            {
                if (content == MyVoxelConstants.VOXEL_CONTENT_EMPTY)
                {
                    //  Nothing is changing
                    return;
                }
                else
                {
                    m_voxelContentSum += content;
                    CheckCellType();

                    //  If this cell is mixed, we change voxel's value in the cell content array, but first allocate the array
                    if (CellType == MyVoxelRangeType.MIXED)
                    {
                        m_cellContent = MyVoxelContentCellContents.Allocate();
                        if (m_cellContent != null)
                        {
                            m_cellContent.Reset(MyVoxelConstants.VOXEL_CONTENT_EMPTY);
                            m_cellContent.SetVoxelContent(content, ref voxelCoordInCell);
                        }
                    }
                }
            }
            else if (CellType == MyVoxelRangeType.MIXED)
            {
                if (m_cellContent == null)
                {
                    return;
                }
                //  Check for previous content value not only for optimisation, but because we need to know how much it changed
                //  for calculating whole cell content summary.
                byte previousContent = m_cellContent.GetVoxelContent(ref voxelCoordInCell);

                if (previousContent == content)
                {
                    //  New value is same as current, so nothing needs to be changed
                    return;
                }

                m_voxelContentSum -= previousContent - content;
                CheckCellType();

                //  If this cell is still mixed, we change voxel's value in the cell content array
                if (CellType == MyVoxelRangeType.MIXED)
                {
                    m_cellContent.SetVoxelContent(content, ref voxelCoordInCell);
                }
            }
            else
            {
                throw new InvalidBranchException();
            }
        }

        // Set voxel content for the whole cell.
        public void SetAllVoxelContents(byte[] buffer)
        {
            // quantize the buffer and compute sum
            m_voxelContentSum = 0;
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = MyCellStorage.Quantizer.QuantizeValue(buffer[i]);
                m_voxelContentSum += buffer[i];
            }

            // mixed-->empty/full: deallocate
            // empty/full-->mixed: allocate
            // mixed: fill with values from buffer
            if (m_voxelContentSum == 0)
            {
                if (CellType == MyVoxelRangeType.MIXED) Deallocate();
                CellType = MyVoxelRangeType.EMPTY;
            }
            else if (m_voxelContentSum == MyVoxelConstants.DATA_CELL_CONTENT_SUM_TOTAL)
            {
                if (CellType == MyVoxelRangeType.MIXED) Deallocate();
                CellType = MyVoxelRangeType.FULL;
            }
            else
            {
                if (CellType == MyVoxelRangeType.FULL || CellType == MyVoxelRangeType.EMPTY) m_cellContent = MyVoxelContentCellContents.Allocate();
                if (m_cellContent != null)
                {
                    m_cellContent.SetAddVoxelContents(buffer);
                }
                CellType = MyVoxelRangeType.MIXED;
            }
        }

        //  Coordinates are relative to voxel cell
        //  IMPORTANT: Input variable 'voxelCoordInCell' is 'ref' only for optimization. Never change its value in the method!!!
        public byte GetContent(ref Vector3I voxelCoordInCell)
        {
            if (CellType == MyVoxelRangeType.EMPTY)
            {
                //  Cell is empty, therefore voxel must be empty too.
                return MyVoxelConstants.VOXEL_CONTENT_EMPTY;
            }
            else if (CellType == MyVoxelRangeType.FULL)
            {
                //  Cell is full, therefore voxel must be full too.
                return MyVoxelConstants.VOXEL_CONTENT_FULL;
            }
            else
            {
                //  If cell is mixed, get voxel's content from the cell's content.
                //  Content was allocated before, we don't need to do it now (or even check it).
                if (m_cellContent != null)
                {
                    return m_cellContent.GetVoxelContent(ref voxelCoordInCell);
                }

                return 0;
            }
        }

        public byte GetContent(int voxelIndexInCell)
        {
            if (CellType == MyVoxelRangeType.EMPTY)
            {
                return MyVoxelConstants.VOXEL_CONTENT_EMPTY;
            }
            else if (CellType == MyVoxelRangeType.FULL)
            {
                return MyVoxelConstants.VOXEL_CONTENT_FULL;
            }
            else
            {
                if (m_cellContent != null)
                {
                    return m_cellContent.GetVoxelContent(voxelIndexInCell);
                }

                return 0;
            }
        }

        //  This method helps us to maintain correct cell type even after removing or adding voxels from cell
        //  If all voxels were removed from this cell, we change its type to from MIXED to EMPTY.
        //  If voxels were added, we change its type to from EMPTY to MIXED.
        //  If voxels were added to full, we change its type to FULL.
        void CheckCellType()
        {
            //  Voxel cell content sum isn't in allowed range. Probably increased or descreased too much.
            System.Diagnostics.Debug.Assert((m_voxelContentSum >= 0) && (m_voxelContentSum <= MyVoxelConstants.DATA_CELL_CONTENT_SUM_TOTAL));

            if (m_voxelContentSum == 0)
            {
                CellType = MyVoxelRangeType.EMPTY;
            }
            else if (m_voxelContentSum == MyVoxelConstants.DATA_CELL_CONTENT_SUM_TOTAL)
            {
                CellType = MyVoxelRangeType.FULL;
            }
            else
            {
                CellType = MyVoxelRangeType.MIXED;
            }

            //  If cell changed from MIXED to EMPTY or FULL, we will release it's cell content because it's not needed any more
            if ((CellType == MyVoxelRangeType.EMPTY) || (CellType == MyVoxelRangeType.FULL))
            {
                Deallocate();
            }
        }

        public byte GetAverageContent()
        {
            return (byte)(m_voxelContentSum / MyVoxelConstants.DATA_CELL_SIZE_IN_VOXELS_TOTAL);
        }

        public int GetVoxelContentSum()
        {
            return m_voxelContentSum;
        }

        public void SetToEmpty()
        {
            CellType = MyVoxelRangeType.EMPTY;
            m_voxelContentSum = 0;

            CheckCellType();
        }

        public void SetToFull()
        {
            CellType = MyVoxelRangeType.FULL;
            m_voxelContentSum = MyVoxelConstants.DATA_CELL_CONTENT_SUM_TOTAL;

            CheckCellType();
        }
        
        public void Deallocate()
        {
            Debug.Assert(CellType == MyVoxelRangeType.FULL ||
                         CellType == MyVoxelRangeType.EMPTY ||
                         m_cellContent != null);
            if (m_cellContent != null)
            {
                //m_cellContent.Value.NeedReset = true;
                MyVoxelContentCellContents.Deallocate(m_cellContent);
                m_cellContent = null;
            }
        }
    }
}
