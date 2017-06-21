using Havok;

using Sandbox.Engine.Physics;
using Sandbox.Game.Components;
using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage.Import;
using VRageMath;
using VRageMath.PackedVector;
using VRageRender;
using VRageRender.Import;

namespace Sandbox.Game.Components
{
    public class MyRenderComponentFracturedPiece : MyRenderComponent
    {
        private const string EMPTY_MODEL = @"Models\Debug\Error.mwm";

        struct ModelInfo
        {
            public String Name;
            public MatrixD LocalTransform;
        }

        readonly List<ModelInfo> Models = new List<ModelInfo>();

        public void AddPiece(string modelName, MatrixD localTransform)
        {
            if (string.IsNullOrEmpty(modelName))
                modelName = EMPTY_MODEL;
            Models.Add(new ModelInfo() { Name = modelName, LocalTransform = localTransform });
        }

        public void RemovePiece(string modelName)
        {
            if (string.IsNullOrEmpty(modelName))
                modelName = EMPTY_MODEL;
            Models.RemoveAll(m => m.Name == modelName);
        }

        public override void InvalidateRenderObjects(bool sortIntoCullobjects = false)
        {
            // Update only cull object
            var m = Container.Entity.PositionComp.WorldMatrix;
            if ((Container.Entity.Visible || Container.Entity.CastShadows) && Container.Entity.InScene && Container.Entity.InvalidateOnMove && m_renderObjectIDs.Length > 0)
            {
                VRageRender.MyRenderProxy.UpdateRenderObject(m_renderObjectIDs[0], ref m, sortIntoCullobjects);
            }
        }

        public override void AddRenderObjects()
        {
            if (Models.Count == 0)
                return;

            var block = base.Container.Entity as MyCubeBlock;
            if (block != null)
            {
                this.CalculateBlockDepthBias(block);
            }

            m_renderObjectIDs = new uint[Models.Count + 1];

            m_renderObjectIDs[0] = VRageRender.MyRenderProxy.RENDER_ID_UNASSIGNED;
            SetRenderObjectID(0, MyRenderProxy.CreateManualCullObject(Container.Entity.Name ?? "Fracture", Container.Entity.PositionComp.WorldMatrix));

            for (int i = 0; i < Models.Count; ++i)
            {
                m_renderObjectIDs[i + 1] = VRageRender.MyRenderProxy.RENDER_ID_UNASSIGNED;
                SetRenderObjectID(i + 1, MyRenderProxy.CreateRenderEntity
                (
                    "Fractured piece " + i.ToString() + " " + Container.Entity.EntityId.ToString(),
                    Models[i].Name,
                    Models[i].LocalTransform,
                    MyMeshDrawTechnique.MESH,
                    GetRenderFlags(),
                    GetRenderCullingOptions(),
                    m_diffuseColor,
                    m_colorMaskHsv,
                    depthBias: DepthBias
                ));

                MyRenderProxy.SetParentCullObject(m_renderObjectIDs[i + 1], m_renderObjectIDs[0], Models[i].LocalTransform);
            }
        }

        public void ClearModels()
        {
            Models.Clear();
        }
    }
}
