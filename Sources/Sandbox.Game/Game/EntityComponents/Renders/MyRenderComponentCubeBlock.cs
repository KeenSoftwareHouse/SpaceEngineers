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

        public override void AddRenderObjects()
        {
            this.CalculateBlockDepthBias(m_cubeBlock);

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
