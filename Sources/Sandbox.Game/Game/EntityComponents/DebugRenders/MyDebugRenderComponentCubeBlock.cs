using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;
using VRageRender;
using Sandbox.ModAPI;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Engine.Voxels;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System.Diagnostics;
using System.Threading;

using Sandbox.Game.Entities;

using Sandbox.Graphics;
using VRage.Game;

namespace Sandbox.Game.Components
{
    public class MyDebugRenderComponentCubeBlock : MyDebugRenderComponent
    {
        MyCubeBlock m_cubeBlock = null;

        public MyDebugRenderComponentCubeBlock(MyCubeBlock cubeBlock):base(cubeBlock)
        {
            m_cubeBlock = cubeBlock;
        }
        public override void DebugDraw()
        {
            // Duplicit. Use MyFakes.DEBUG_DRAW_MODEL_DUMMIES;
            /*if (m_model != null)
            {
                foreach (var d in m_model.Dummies)
                {
                    VRageRender.MyRenderProxy.DebugDrawOBB(d.Value.Matrix * WorldMatrix, Color.Blue.ToVector3(), 1.0f, false, false);
                    VRageRender.MyRenderProxy.DebugDrawAxis(d.Value.Matrix * WorldMatrix, 1, false);
                }
            }*/

            if (MyDebugDrawSettings.DEBUG_DRAW_CUBE_BLOCK_AABBS)
            {
                Color color = Color.Red;
                Color green = Color.Green;

                var centerGrid = m_cubeBlock.BlockDefinition.Center;

                var min = (m_cubeBlock.Min * m_cubeBlock.CubeGrid.GridSize) - new Vector3(m_cubeBlock.CubeGrid.GridSize / 2.0f);
                var max = (m_cubeBlock.Max * m_cubeBlock.CubeGrid.GridSize) + new Vector3(m_cubeBlock.CubeGrid.GridSize / 2.0f);
                BoundingBoxD bbox = new BoundingBoxD(min, max);

                var worldMat = m_cubeBlock.CubeGrid.WorldMatrix;
                MySimpleObjectDraw.DrawTransparentBox(ref worldMat, ref bbox, ref color, MySimpleObjectRasterizer.Wireframe, 1, 0.01f);
            }
        }
    }
}
