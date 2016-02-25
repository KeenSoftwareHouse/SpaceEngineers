using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using System;
using System.Diagnostics;
using VRage.Game.Models;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.Components
{
    public class MyRenderComponentCubeBlock : MyRenderComponent
    {
        protected MyCubeBlock m_cubeBlock = null;

        #region overrides
        public override void OnAddedToContainer()
        {
            base.OnAddedToContainer();
            m_cubeBlock = Container.Entity as MyCubeBlock;
        }
        public override void InvalidateRenderObjects(bool sortIntoCullobjects = false)
        {
        }

        public unsafe override void AddRenderObjects()
        {
            if (m_cubeBlock.Hierarchy != null)
            {
                var parentCompound = m_cubeBlock.Hierarchy.Parent.Entity as MyCompoundCubeBlock;
                if (parentCompound != null)
                {
                    const int offsetCount = 64;
                    bool* offsets = stackalloc bool[offsetCount];
                    for (int offsetIndex = 0; offsetIndex < offsetCount; ++offsetIndex)
                        offsets[offsetIndex] = false;

                    foreach (var block in parentCompound.GetBlocks())
                    {
                        if (block.FatBlock != null && block.FatBlock != m_cubeBlock)
                        {
                            var cubeBlockRender = block.FatBlock.Render as MyRenderComponentCubeBlock;
                            if (cubeBlockRender != null)
                                offsets[cubeBlockRender.DepthBias] = true;
                        }
                    }

                    int preferedOffset = 0;
                    var modelStorage = ModelStorage as MyModel;
                    if (modelStorage != null)
                    {
                        Vector3 blockCenterLocal = modelStorage.BoundingSphere.Center;
                        MatrixI blockOrientation = new MatrixI(m_cubeBlock.SlimBlock.Orientation);
                        Vector3 blockCenter = new Vector3();
                        Vector3.Transform(ref blockCenterLocal, ref blockOrientation, out blockCenter);
                        if (blockCenter.LengthSquared() > 0.5f)
                        {
                            if (Math.Abs(blockCenter.X) > Math.Abs(blockCenter.Y))
                            {
                                if (Math.Abs(blockCenter.X) > Math.Abs(blockCenter.Z))
                                {
                                    preferedOffset = blockCenter.X > 0 ? 2 : 4;
                                }
                                else
                                {
                                    preferedOffset = blockCenter.Z > 0 ? 10 : 12;
                                }
                            }
                            else
                            {
                                if (Math.Abs(blockCenter.Z) > Math.Abs(blockCenter.Y))
                                {
                                    preferedOffset = blockCenter.Z > 0 ? 10 : 12;
                                }
                                else
                                {
                                    preferedOffset = blockCenter.Y > 0 ? 6 : 8;
                                }
                            }
                        }
                    }

                    for (int offsetIndex = preferedOffset; offsetIndex < offsetCount; ++offsetIndex)
                    {
                        if (!offsets[offsetIndex])
                        {
                            DepthBias = (byte)offsetIndex;
                            break;
                        }
                    }
                }
            }

            base.AddRenderObjects();

            if (MyFakes.MANUAL_CULL_OBJECTS)
            {
                var cell = m_cubeBlock.CubeGrid.RenderData.GetCell(m_cubeBlock.Position * m_cubeBlock.CubeGrid.GridSize);
                if (cell.ParentCullObject == MyRenderProxy.RENDER_ID_UNASSIGNED)
                {
                    cell.RebuildInstanceParts(GetRenderFlags());
                }
                Debug.Assert(cell.ParentCullObject != MyRenderProxy.RENDER_ID_UNASSIGNED, "Somethings wrong, parent cull object should have been created");

                foreach (var renderObjectId in m_renderObjectIDs)
                {
                    MyRenderProxy.SetParentCullObject(renderObjectId, cell.ParentCullObject);
                }
            }
        }
        #endregion
    }
}
