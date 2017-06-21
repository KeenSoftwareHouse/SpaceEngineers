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

using VRage.Utils;
using VRage.Import;
using VRage.ModAPI;
using VRage.Game.Components;
using VRage.Game.Models;
using VRage.Game.Entity;
using VRageRender.Import;

namespace Sandbox.Game.Components
{
    public class MyDebugRenderComponent : MyDebugRenderComponentBase
    {
        protected MyEntity Entity = null;
        #region overrides
        public MyDebugRenderComponent(IMyEntity entity)
        {
            Entity = (MyEntity)entity;
        }
        public override void DebugDrawInvalidTriangles()
        {
            if (Entity == null)
            {
                return;
            }
            foreach (var child in this.Entity.Hierarchy.Children)
            {
                child.Container.Entity.DebugDrawInvalidTriangles();
            }

            if (Entity.Render.GetModel() != null)
            {
                int triCount = Entity.Render.GetModel().GetTrianglesCount();
                for (int i = 0; i < triCount; ++i)
                {
                    var triangle = Entity.Render.GetModel().GetTriangle(i);
                    if (MyUtils.IsWrongTriangle(Entity.Render.GetModel().GetVertex(triangle.I0), Entity.Render.GetModel().GetVertex(triangle.I1), Entity.Render.GetModel().GetVertex(triangle.I2)))
                    {
                        Vector3 v0 = Vector3.Transform(Entity.Render.GetModel().GetVertex(triangle.I0), Entity.PositionComp.WorldMatrix);
                        Vector3 v1 = Vector3.Transform(Entity.Render.GetModel().GetVertex(triangle.I1), Entity.PositionComp.WorldMatrix);
                        Vector3 v2 = Vector3.Transform(Entity.Render.GetModel().GetVertex(triangle.I2), Entity.PositionComp.WorldMatrix);
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
        public override void DebugDraw()
        {
            // Probably a base entity, render its dummy
            if (Entity.Render.RenderObjectIDs[0] == UInt32.MaxValue)
            {
                MyRenderProxy.DebugDrawSphere(Entity.PositionComp.WorldMatrix.Translation, 0.2f, Color.Orange, 0.5f, true);
                MyRenderProxy.DebugDrawAxis(Entity.PositionComp.WorldMatrix, 1, true);
            }

            if (MyDebugDrawSettings.DEBUG_DRAW_MODEL_DUMMIES)
            {
                DebugDrawDummies(Entity.Render.GetModel());
            }

            if (MyDebugDrawSettings.DEBUG_DRAW_ENTITY_IDS)
            {
                if (this.Entity.Parent == null || !MyDebugDrawSettings.DEBUG_DRAW_ENTITY_IDS_ONLY_ROOT)
                {
                    MyRenderProxy.DebugDrawText3D(Entity.PositionComp.WorldMatrix.Translation, Entity.EntityId.ToString("X16"), Color.White, 0.6f, false);
                }
            }
        }
        #endregion

        protected void DebugDrawDummies(MyModel model)
        {
            if (model == null)
            {
                return;
            }

            var distanceSquared = 0f;
            var cameraPos = Vector3D.Zero;
            if (MySector.MainCamera != null) 
            {
                distanceSquared = MyDebugDrawSettings.DEBUG_DRAW_MODEL_DUMMIES_DISTANCE * MyDebugDrawSettings.DEBUG_DRAW_MODEL_DUMMIES_DISTANCE;
                cameraPos = MySector.MainCamera.WorldMatrix.Translation;
            }

            foreach (var dummy in model.Dummies)
            {
                MyModelDummy modelDummy = dummy.Value;

                MatrixD worldMatrix = (MatrixD)modelDummy.Matrix * Entity.PositionComp.WorldMatrix;
                if (distanceSquared != 0f && Vector3D.DistanceSquared(cameraPos, worldMatrix.Translation) > distanceSquared)
                    continue;

                VRageRender.MyRenderProxy.DebugDrawText3D(worldMatrix.Translation, dummy.Key, Color.White, 0.7f, false);
                VRageRender.MyRenderProxy.DebugDrawAxis(MatrixD.Normalize(worldMatrix), 0.1f, false);
                VRageRender.MyRenderProxy.DebugDrawOBB(worldMatrix, Vector3.One, 0.1f, false, false);
            }
        }
    }
}
