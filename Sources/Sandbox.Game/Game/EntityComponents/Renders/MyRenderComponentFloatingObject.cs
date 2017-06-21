using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageRender;
using VRageMath;

using Sandbox.Game.Entities;

using VRage.Utils;
using VRage.Game.Components;

namespace Sandbox.Game.Components
{
    class MyRenderComponentFloatingObject : MyRenderComponent
    {
        MyFloatingObject m_floatingObject = null;
        #region overrides
        public override void OnAddedToContainer()
        {
            base.OnAddedToContainer();
            m_floatingObject = Container.Entity as MyFloatingObject;
        }
        public override void AddRenderObjects()
        {
            if (m_floatingObject.VoxelMaterial == null)
            {
                base.AddRenderObjects();
                return;
            }

            if (m_renderObjectIDs[0] != VRageRender.MyRenderProxy.RENDER_ID_UNASSIGNED)
                return;

            MyDebug.AssertDebug(Model != null && !string.IsNullOrEmpty(Model.AssetName), "Missing model for Voxel Debris!");

            SetRenderObjectID(0, MyRenderProxy.CreateRenderVoxelDebris(
                "Voxel debris",
                Model.AssetName,
                (Matrix)Container.Entity.PositionComp.WorldMatrix,
                5,
                8,
                1.0f,
                m_floatingObject.VoxelMaterial.Index));
        }
        #endregion
    }
}
