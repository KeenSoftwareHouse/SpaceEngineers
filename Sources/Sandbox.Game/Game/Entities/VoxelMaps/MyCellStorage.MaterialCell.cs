using System;
using System.Collections.Generic;
using System.Diagnostics;
using Sandbox.Common;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using SysUtils.Utils;
using VRage.Common;
using VRage.Common.Utils;
using VRageMath;

//  This class represents materials for cell of voxels (e.g. 8x8x8). Mapping is always one-to-one.
//  But detail 3D array is allocated and used only if all materials in this cell aren't same - so we save a lot of memory in areas where are same materials.
//  This cell is also used as a source for 'average data cell material'
//  Size of this cell is same as voxel data cell size.
//  This class doesn't support deallocating/disposing of 3D array if it's not anymore needed. Reason is that situation should happen or if, then it's very rare.
//  IndestructibleContents - it's content/scalar value of a voxel that tell us its minimum possible value, we can't set smaller content value. Used for indestructible materials only.

namespace Sandbox.Game.Entities.VoxelMaps
{
    partial class MyCellStorage
    {
        class MaterialCell
        {
            const byte INVALID_MATERIAL = 0xff;
            public static readonly int voxelsInCell = MyVoxelConstants.DATA_CELL_SIZE_IN_VOXELS * MyVoxelConstants.DATA_CELL_SIZE_IN_VOXELS * MyVoxelConstants.DATA_CELL_SIZE_IN_VOXELS;
            public static readonly int xStep = MyVoxelConstants.DATA_CELL_SIZE_IN_VOXELS * MyVoxelConstants.DATA_CELL_SIZE_IN_VOXELS;
            public static readonly int yStep = MyVoxelConstants.DATA_CELL_SIZE_IN_VOXELS;
            public static readonly int zStep = 1;
            static Dictionary<byte, int> m_cellMaterialCounts = new Dictionary<byte, int>();

            byte[] m_materials;
            byte m_singleMaterial;
            byte m_averageCellMaterial = INVALID_MATERIAL;
            byte m_averageNonRareCellMaterial = INVALID_MATERIAL;

            public bool IsSingleMaterial
            {
                get;
                private set;
            }

            public bool ContainsRareMaterial
            {
                get
                {
                    if (IsSingleMaterial)
                    {
                        return MyDefinitionManager.Static.GetVoxelMaterialDefinition(m_singleMaterial).IsRare;
                    }
                    else
                    {
                        for (int i = 0; i < m_materials.Length; ++i)
                        {
                            if (MyDefinitionManager.Static.GetVoxelMaterialDefinition(m_materials[i]).IsRare)
                                return true;
                        }
                        return false;
                    }
                }
            }

            public MyVoxelMaterialDefinition AverageMaterial
            {
                get
                {
                    if (MyFakes.SINGLE_VOXEL_MATERIAL != null)
                        return MyDefinitionManager.Static.GetVoxelMaterialDefinition(MyFakes.SINGLE_VOXEL_MATERIAL);

                    if (m_averageCellMaterial == INVALID_MATERIAL)
                        CalcAverageCellMaterial();

                    return MyDefinitionManager.Static.GetVoxelMaterialDefinition(m_averageCellMaterial);
                }
            }

            public MyVoxelMaterialDefinition AverageNonRareMaterial
            {
                get
                {
                    if (MyFakes.SINGLE_VOXEL_MATERIAL != null)
                        return MyDefinitionManager.Static.GetVoxelMaterialDefinition(MyFakes.SINGLE_VOXEL_MATERIAL);

                    if (m_averageNonRareCellMaterial == INVALID_MATERIAL)
                        CalcAverageNonRareCellMaterial();

                    var material = MyDefinitionManager.Static.GetVoxelMaterialDefinition(m_averageNonRareCellMaterial);
                    // When cell is single material and that material is rare, this will return rare material instead of non-rare.
                    //Debug.Assert(!material.IsRare);
                    return material;
                }
            }

            public MaterialCell(MyVoxelMaterialDefinition defaultMaterial)
            {
                //  By default cell contains only one single material
                Reset(defaultMaterial);
            }

            //  Use when you want to change whole cell to one single material
            public void Reset(MyVoxelMaterialDefinition defaultMaterial)
            {
                IsSingleMaterial             = true;
                m_singleMaterial             = defaultMaterial.Index;
                m_averageCellMaterial        = m_singleMaterial;
                m_averageNonRareCellMaterial = m_singleMaterial; // might be rare, but we have no way of knowing what non-rare material to use
                m_materials                  = null;
            }

            //  Change material for specified voxel
            //  If this material is single material for whole cell, we do nothing. Otherwise we allocate 3D arrays and start using them.
            public void SetMaterial(MyVoxelMaterialDefinition material, ref Vector3I voxelCoordInCell)
            {
                SetMaterial(material.Index, ref voxelCoordInCell);
            }

            public void SetMaterial(byte materialIndex, ref Vector3I voxelCoordInCell)
            {
                CheckInitArrays(materialIndex);

                if (!IsSingleMaterial)
                {
                    m_materials[ComputeVoxelIndexInCell(ref voxelCoordInCell)] = materialIndex;
                }
            }

            //  Return material for specified voxel. If whole cell contain one single material, this one is returned. Otherwise material from 3D array is returned.
            public MyVoxelMaterialDefinition GetMaterial(ref Vector3I voxelCoordInCell)
            {
                if (!string.IsNullOrEmpty(MyFakes.SINGLE_VOXEL_MATERIAL))
                    return MyDefinitionManager.Static.GetVoxelMaterialDefinition(MyFakes.SINGLE_VOXEL_MATERIAL);

                byte idx;
                if (IsSingleMaterial)
                {
                    idx = m_singleMaterial;
                }
                else
                {
                    idx = m_materials[ComputeVoxelIndexInCell(ref voxelCoordInCell)];
                }
                return MyDefinitionManager.Static.GetVoxelMaterialDefinition(idx);
            }

            public MyVoxelMaterialDefinition GetMaterial(int voxelIndexInCell)
            {
                if (!string.IsNullOrEmpty(MyFakes.SINGLE_VOXEL_MATERIAL))
                    return MyDefinitionManager.Static.GetVoxelMaterialDefinition(MyFakes.SINGLE_VOXEL_MATERIAL);

                byte idx;
                if (IsSingleMaterial)
                {
                    idx = m_singleMaterial;
                }
                else
                {
                    idx = m_materials[voxelIndexInCell];
                }
                return MyDefinitionManager.Static.GetVoxelMaterialDefinition(idx);
            }

            internal byte GetMaterialIdx(ref Vector3I voxelCoordInCell)
            {
                if (!string.IsNullOrEmpty(MyFakes.SINGLE_VOXEL_MATERIAL))
                    return MyDefinitionManager.Static.GetVoxelMaterialDefinition(MyFakes.SINGLE_VOXEL_MATERIAL).Index;

                if (IsSingleMaterial)
                {
                    return m_singleMaterial;
                }
                else
                {
                    return m_materials[ComputeVoxelIndexInCell(ref voxelCoordInCell)];
                }
            }

            public static int ComputeVoxelIndexInCell(ref Vector3I voxelCoordInCell)
            {
                return voxelCoordInCell.X * xStep + voxelCoordInCell.Y * yStep + voxelCoordInCell.Z * zStep;
            }

            private void CalcAverageNonRareCellMaterial()
            {
                m_averageNonRareCellMaterial = MyDefinitionManager.Static.GetDefaultVoxelMaterialDefinition().Index;
                if (IsSingleMaterial)
                {
                    m_averageNonRareCellMaterial = m_singleMaterial;
                }
                else
                {
                    for (int xyz = 0; xyz < voxelsInCell; xyz++)
                    {
                        var material = m_materials[xyz];
                        int amount = 0;
                        m_cellMaterialCounts.TryGetValue(material, out amount);
                        m_cellMaterialCounts[material] = ++amount;
                    }

                    int maxNum = 0;
                    foreach (var entry in m_cellMaterialCounts)
                    {
                        var material = MyDefinitionManager.Static.GetVoxelMaterialDefinition(entry.Key);
                        if (entry.Value > maxNum && !material.IsRare)
                        {
                            maxNum = entry.Value;
                            m_averageNonRareCellMaterial = entry.Key;
                        }
                    }
                    m_cellMaterialCounts.Clear();
                }
            }

            private void CalcAverageCellMaterial()
            {
                if (IsSingleMaterial)
                {
                    m_averageCellMaterial = m_singleMaterial;
                }
                else
                {
                    for (int xyz = 0; xyz < voxelsInCell; xyz++)
                    {
                        var material = m_materials[xyz];
                        int amount = 0;
                        m_cellMaterialCounts.TryGetValue(material, out amount);
                        m_cellMaterialCounts[material] = ++amount;
                    }

                    int maxNum = 0;
                    foreach (var entry in m_cellMaterialCounts)
                    {
                        if (entry.Value > maxNum)
                        {
                            maxNum = entry.Value;
                            m_averageCellMaterial = entry.Key;
                        }
                    }
                    m_cellMaterialCounts.Clear();
                }
            }

            //  Check if we new material differs from one main material and if yes, we need to start using 3D arrays
            private void CheckInitArrays(byte materialIndex)
            {
                if (IsSingleMaterial && (m_singleMaterial != materialIndex))
                {
                    m_materials = new byte[voxelsInCell];
                    //  Fill with present cell values
                    for (int xyz = 0; xyz < voxelsInCell; xyz++)
                    {
                        m_materials[xyz] = m_singleMaterial;
                    }

                    //  From now, this cell contains more than one material
                    IsSingleMaterial = false;
                    m_averageCellMaterial = INVALID_MATERIAL;
                    m_averageNonRareCellMaterial = INVALID_MATERIAL;
                }
            }

            public void GetAllMaterialsPresent(HashSet<MyVoxelMaterialDefinition> outputMaterialSet)
            {
                if (MyFakes.SINGLE_VOXEL_MATERIAL != null)
                {
                    outputMaterialSet.Add(MyDefinitionManager.Static.GetVoxelMaterialDefinition(MyFakes.SINGLE_VOXEL_MATERIAL));
                    return;
                }

                if (IsSingleMaterial)
                {
                    outputMaterialSet.Add(MyDefinitionManager.Static.GetVoxelMaterialDefinition(m_singleMaterial));
                }
                else
                {
                    foreach (var material in m_materials)
                    {
                        outputMaterialSet.Add(MyDefinitionManager.Static.GetVoxelMaterialDefinition(material));
                    }
                }
            }
        }
    }
}
