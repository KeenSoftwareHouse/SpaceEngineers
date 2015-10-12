using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;
using VRageRender;
using Sandbox.ModAPI;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Voxels;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Engine.Voxels;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System.Diagnostics;
using System.Threading;

using Sandbox.Game.Entities;
using Sandbox.Common.Components;
using VRage.Utils;
using VRage.Import;
using VRage.ModAPI;
using VRage.Components;

namespace Sandbox.Game.Components
{
    public class MyDebugRenderComponent : MyDebugRenderComponentBase
    {
        MyEntity m_entity = null;
        #region overrides
        public MyDebugRenderComponent(IMyEntity entity)
        {
            m_entity = (MyEntity)entity;
        }
        public override void DebugDrawInvalidTriangles()
        {
            if (m_entity == null)
            {
                return;
            }
            foreach (var child in this.m_entity.Hierarchy.Children)
            {
                child.Container.Entity.DebugDrawInvalidTriangles();
            }

            if (m_entity.Render.GetModel() != null)
            {
                int triCount = m_entity.Render.GetModel().GetTrianglesCount();
                for (int i = 0; i < triCount; ++i)
                {
                    var triangle = m_entity.Render.GetModel().GetTriangle(i);
                    if (MyUtils.IsWrongTriangle(m_entity.Render.GetModel().GetVertex(triangle.I0), m_entity.Render.GetModel().GetVertex(triangle.I1), m_entity.Render.GetModel().GetVertex(triangle.I2)))
                    {
                        Vector3 v0 = Vector3.Transform(m_entity.Render.GetModel().GetVertex(triangle.I0), m_entity.PositionComp.WorldMatrix);
                        Vector3 v1 = Vector3.Transform(m_entity.Render.GetModel().GetVertex(triangle.I1), m_entity.PositionComp.WorldMatrix);
                        Vector3 v2 = Vector3.Transform(m_entity.Render.GetModel().GetVertex(triangle.I2), m_entity.PositionComp.WorldMatrix);
                        VRageRender.MyRenderProxy.DebugDrawLine3D(v0, v1, Color.Purple, Color.Purple, false);
                        VRageRender.MyRenderProxy.DebugDrawLine3D(v1, v2, Color.Purple, Color.Purple, false);
                        VRageRender.MyRenderProxy.DebugDrawLine3D(v2, v0, Color.Purple, Color.Purple, false);
                        Vector3 center = (v0 + v1 + v2) / 3f;
                        VRageRender.MyRenderProxy.DebugDrawLine3D(center, center + Vector3.UnitX, Color.Yellow, Color.Yellow, false);
                        VRageRender.MyRenderProxy.DebugDrawLine3D(center, center + Vector3.UnitY, Color.Yellow, Color.Yellow, false);
                        VRageRender.MyRenderProxy.DebugDrawLine3D(center, center + Vector3.UnitZ, Color.Yellow, Color.Yellow, false);
                    }
                }
            }
        }
        public override bool DebugDraw()
        {
            if (MyDebugDrawSettings.DEBUG_DRAW_MODEL_DUMMIES)
            {
                DebugDrawDummies(m_entity.Render.GetModel());
            }

            if (MyDebugDrawSettings.DEBUG_DRAW_ENTITY_IDS)
            {
                if (this.m_entity.Parent == null || !MyDebugDrawSettings.DEBUG_DRAW_ENTITY_IDS_ONLY_ROOT)
                {
                    MyRenderProxy.DebugDrawText3D(m_entity.PositionComp.WorldMatrix.Translation, m_entity.EntityId.ToString("X16"), Color.White, 0.6f, false);
                }
            }

            return true;
        }
        #endregion

        protected void DebugDrawDummies(MyModel model)
        {
            if (model == null)
            {
                return;
            }

            foreach (var dummy in model.Dummies)
            {
                MyModelDummy modelDummy = dummy.Value;

                MatrixD worldMatrix = (MatrixD)modelDummy.Matrix * m_entity.PositionComp.WorldMatrix;
                VRageRender.MyRenderProxy.DebugDrawText3D(worldMatrix.Translation, dummy.Key, Color.White, 0.7f, false);
                VRageRender.MyRenderProxy.DebugDrawAxis(MatrixD.Normalize(worldMatrix), 0.1f, false);
                VRageRender.MyRenderProxy.DebugDrawOBB(worldMatrix, Vector3.One, 1, false, false);
            }

        }
    }
}
