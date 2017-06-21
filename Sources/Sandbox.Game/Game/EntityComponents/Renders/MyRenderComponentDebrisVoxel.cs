using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageRender;
using VRageMath;

using Sandbox.Game.Entities.Debris;

using VRage.Utils;

namespace Sandbox.Game.Components
{
    class MyRenderComponentDebrisVoxel : MyRenderComponent
    {
        public float TexCoordOffset { get; set; }
        public float TexCoordScale { get; set; }
        public byte VoxelMaterialIndex { get; set; }

        public override void AddRenderObjects()
        {
            if (m_renderObjectIDs[0] != VRageRender.MyRenderProxy.RENDER_ID_UNASSIGNED)
                return;

            MyDebug.AssertDebug(Model != null && !string.IsNullOrEmpty(Model.AssetName), "Missing model for Voxel Debris!");

            SetRenderObjectID(0, MyRenderProxy.CreateRenderVoxelDebris(
                "Voxel debris",
                Model.AssetName,
                (Matrix)Container.Entity.PositionComp.WorldMatrix,
                TexCoordOffset,
                TexCoordScale,
                1.0f,
                VoxelMaterialIndex));
        }
    }
}
