using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Sandbox.Common;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using VRage.Common;
using VRage.Common.Utils;
using VRageMath;

namespace Sandbox.Game.Entities.VoxelMaps
{
    partial class MyCellStorage
    {
        public class DepositCell : IMyDepositCell
        {
            private MyCellStorage m_storage;
            private int m_cellIndex;
            private int m_totalOreContent;
            private bool m_positionIsDirty;

            private Dictionary<int, int> m_allMaterialsContent;
            private List<MyVoxelMaterialDefinition> m_oreWithContent;
            private Dictionary<int, Vector3?> m_allMaterialsPositions;
            private Dictionary<int, byte> m_helpersMaxContentForMaterial;

            public Vector3 WorldCenter
            {
                get
                {
                    Vector3 worldCenter;
                    m_storage.ComputeWorldCenter(m_cellIndex, out worldCenter);
                    return worldCenter;
                }
            }

            public int CellIndex
            {
                get { return m_cellIndex; }
            }

            public MyCellStorage Storage
            {
                get { return m_storage; }
            }

            public DepositCell(MyCellStorage storage)
            {
                Debug.Assert(storage != null);
                m_storage = storage;
                AssertCheckPosition();
                m_positionIsDirty = true;
                m_totalOreContent = 0;

                m_oreWithContent = new List<MyVoxelMaterialDefinition>(MyDefinitionManager.Static.VoxelMaterialRareCount);

                m_allMaterialsContent = new Dictionary<int, int>();
                m_allMaterialsPositions = new Dictionary<int, Vector3?>();
                m_helpersMaxContentForMaterial = new Dictionary<int, byte>();
            }

            [Conditional("DEBUG")]
            private void AssertCheckPosition()
            {
                Vector3I voxelStartCoord;
                m_storage.ComputeCellCoord(m_cellIndex, out voxelStartCoord);
                MyCellStorage.ComputeVoxelCoordOfCell(ref voxelStartCoord, out voxelStartCoord);
                Vector3I voxelEndCoord = voxelStartCoord + MyVoxelConstants.DATA_CELL_SIZE_IN_VOXELS - 1;
                Debug.Assert(m_storage.IsInside(ref voxelStartCoord), "Incorrect position of deposit cell.");
                Debug.Assert(m_storage.IsInside(ref voxelEndCoord), "Incorrect position of deposit cell.");
            }

            public void Recompute(int cellIndex)
            {
                m_cellIndex = cellIndex;
                m_totalOreContent = 0;
                m_positionIsDirty = true;
                m_allMaterialsContent.Clear();
                m_oreWithContent.Clear();
                m_allMaterialsPositions.Clear();
                m_helpersMaxContentForMaterial.Clear();

                var matCell = m_storage.m_materialCells[cellIndex];
                var contentCell = m_storage.m_contentCells[cellIndex];

                if (matCell.IsSingleMaterial)
                {
                    var material = matCell.AverageMaterial;
                    if (material.IsRare)
                    {
                        int content = (contentCell == null) ? MyVoxelConstants.DATA_CELL_CONTENT_SUM_TOTAL : contentCell.GetVoxelContentSum();
                        if (content > 0)
                            AddOreContent(material, content);
                    }
                }
                else
                {
                    // buffer one rare material
                    // IMPORTANT: the default value must be a rare material
                    MyVoxelMaterialDefinition bufferedMaterial = MyDefinitionManager.Static.GetDefaultVoxelMaterialDefinition(rare: true);
                    int bufferedContent = 0;

                    for (int voxelIndex = 0; voxelIndex < MyVoxelConstants.DATA_CELL_SIZE_IN_VOXELS_TOTAL; ++voxelIndex)
                    {
                        var material = matCell.GetMaterial(voxelIndex);
                        if (material == bufferedMaterial)  // do we buffer this material?
                        {
                            int content = (contentCell == null) ? MyVoxelConstants.VOXEL_CONTENT_FULL : contentCell.GetContent(voxelIndex);
                            bufferedContent += content;  // yes: just update the content
                        }
                        else if (material.IsRare)  // skip non-rare materials
                        {
                            int content = (contentCell == null) ? MyVoxelConstants.VOXEL_CONTENT_FULL : contentCell.GetContent(voxelIndex);
                            if (content > 0)  // skip empty cells
                            {
                                // new rare material: if there's any old buffered content, add it to the deposit
                                if (bufferedContent > 0)
                                {
                                    AddOreContent(bufferedMaterial, bufferedContent);
                                }

                                bufferedMaterial = material;
                                bufferedContent = content;
                            }
                        }
                    }

                    if (bufferedContent > 0)  // if there's any buffered content left, add it to the deposit
                    {
                        AddOreContent(bufferedMaterial, bufferedContent);
                    }
                }

            }

            public void AddOreContent(MyVoxelMaterialDefinition ore, int content)
            {
                if (content == 0 || !ore.IsRare)
                {
                    return;
                }

                int oldTotalOreContent = m_totalOreContent;

                int existingContent = 0;
                m_allMaterialsContent.TryGetValue(ore.Index, out existingContent);

                int contentToAdd = content;
                if (content < 0)
                {
                    contentToAdd = Math.Max(content, -existingContent);
                }

                if (!m_allMaterialsContent.ContainsKey(ore.Index))
                    m_allMaterialsContent.Add(ore.Index, 0);
                m_allMaterialsContent[ore.Index] += contentToAdd;
                m_totalOreContent += contentToAdd;

                // this ore hasn't any content before, so we add it to oreWithContent collection
                if (contentToAdd > 0 && existingContent == 0)
                {
                    m_oreWithContent.Add(ore);
                }
                // this ore has no content now, so we remove it from oreWithContent collection
                else if (contentToAdd < 0 && m_allMaterialsContent[(int)ore.Index] == 0)
                {
                    m_oreWithContent.Remove(ore);
                }

                m_positionIsDirty = true;
            }

            public List<MyVoxelMaterialDefinition> GetOreWithContent()
            {
                return m_oreWithContent;
            }

            public int TotalRareOreContent
            {
                get { return m_totalOreContent; }
            }

            public Vector3? GetPosition(MyVoxelMaterialDefinition material)
            {
                if (m_positionIsDirty)
                {
                    RecalculateDeposits();
                    m_positionIsDirty = false;
                }

                Vector3? pos;
                m_allMaterialsPositions.TryGetValue(material.Index, out pos);
                return pos;
            }

            private void ClearPositionsAndMaxContent()
            {
                m_allMaterialsPositions.Clear();
                m_helpersMaxContentForMaterial.Clear();
            }

            internal void RecalculateDeposits()
            {
                ClearPositionsAndMaxContent();
                int sizeInVoxels = MyVoxelConstants.DATA_CELL_SIZE_IN_VOXELS;

                Vector3I cellCoord;
                m_storage.ComputeCellCoord(m_cellIndex, out cellCoord);
                Vector3I voxelStartCoord = cellCoord * sizeInVoxels;
                Vector3I voxelEndCoord = voxelStartCoord + sizeInVoxels - 1;
                Debug.Assert(m_storage.IsInside(ref voxelStartCoord), "Incorrect position of deposit cell.");
                Debug.Assert(m_storage.IsInside(ref voxelEndCoord), "Incorrect position of deposit cell.");
                Vector3I voxelCoord;
                for (voxelCoord.X = voxelStartCoord.X; voxelCoord.X <= voxelEndCoord.X; voxelCoord.X++)
                {
                    for (voxelCoord.Y = voxelStartCoord.Y; voxelCoord.Y <= voxelEndCoord.Y; voxelCoord.Y++)
                    {
                        for (voxelCoord.Z = voxelStartCoord.Z; voxelCoord.Z <= voxelEndCoord.Z; voxelCoord.Z++)
                        {
                            byte content = m_storage.GetContent(ref voxelCoord);
                            if (content >= MyVoxelConstants.VOXEL_ISO_LEVEL)
                            {
                                MyVoxelMaterialDefinition material = m_storage.GetMaterial(ref voxelCoord);
                                byte maxContent;
                                m_helpersMaxContentForMaterial.TryGetValue((int)material.Index, out maxContent);

                                if (!m_allMaterialsPositions.ContainsKey((int)material.Index) || content > maxContent)
                                {
                                    if (!m_allMaterialsPositions.ContainsKey((int)material.Index))
                                        m_allMaterialsPositions.Add((int)material.Index, m_storage.GetVoxelPositionAbsolute(ref voxelCoord));
                                    else
                                        m_allMaterialsPositions[(int)material.Index] = m_storage.GetVoxelPositionAbsolute(ref voxelCoord);

                                    if (!m_helpersMaxContentForMaterial.ContainsKey((int)material.Index))
                                        m_helpersMaxContentForMaterial.Add((int)material.Index, content);
                                    else
                                        m_helpersMaxContentForMaterial[(int)material.Index] = content;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
