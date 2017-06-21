#region Using

using Sandbox.Common;
using Sandbox.Game.World;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using VRage;
using VRage.Game.Models;
using VRage.Import;
using VRageMath;
using VRageRender;
using VRageRender.Import;
using VRageRender.Messages;
using ModelId = System.Int32;


#endregion

namespace Sandbox.Game.Entities.Cube
{
    /// <summary>
    /// Helper class for rendering additional grid models - roof tops, roof edges, etc). Instances must be rotated with ortho matrices only.
    /// </summary>
    public class MyGridPartsRenderData
    {
        private static uint m_idCounter = 0;

        private class MyModelInstanceData
        {
            public ModelId Model;
            public MyInstanceInfo InstanceInfo;
            public Dictionary<uint, MyCubeInstanceData> InstanceData = new Dictionary<uint, MyCubeInstanceData>();

            public MyModelInstanceData(MyInstanceFlagsEnum flags, float maxViewDistance)
            {
                InstanceInfo = new MyInstanceInfo(flags, maxViewDistance);
            }
        }

        private Dictionary<ModelId, MyModelInstanceData> m_instanceParts = new Dictionary<ModelId, MyModelInstanceData>();
        private uint m_instanceBufferId = MyRenderProxy.RENDER_ID_UNASSIGNED;
        private List<MyCubeInstanceData> m_tmpInstanceData = new List<MyCubeInstanceData>();
        private Dictionary<ModelId, MyRenderInstanceInfo> m_instanceInfo = new Dictionary<ModelId, MyRenderInstanceInfo>();
        private Dictionary<ModelId, uint> m_instanceGroupRenderObjects = new Dictionary<ModelId, uint>();
        private BoundingBox m_AABB = BoundingBox.CreateInvalid();

        private MyInstanceFlagsEnum m_instanceFlags;
        private float m_maxViewDistance;
        private float m_transparency;
        private string m_debugBufferName;


        public MyGridPartsRenderData(MyInstanceFlagsEnum flags, float maxViewDistance, float transparency = 0f, string debugBufferName = "Instance buffer")
        {
            m_instanceFlags = flags;
            m_maxViewDistance = maxViewDistance;
            m_transparency = transparency;
            m_debugBufferName = debugBufferName;
        }

        public void UnloadRenderObjects()
        {
            foreach (var renderObject in m_instanceGroupRenderObjects)
            {
                VRageRender.MyRenderProxy.RemoveRenderObject(renderObject.Value);
            }
            m_instanceGroupRenderObjects.Clear();

            if (m_instanceBufferId != MyRenderProxy.RENDER_ID_UNASSIGNED)
            {
                VRageRender.MyRenderProxy.RemoveRenderObject(m_instanceBufferId);
                m_instanceBufferId = MyRenderProxy.RENDER_ID_UNASSIGNED;
            }
        }

        public void ClearInstanceData()
        {
            m_tmpInstanceData.Clear();
            m_AABB = BoundingBox.CreateInvalid();

            foreach (var item in m_instanceParts)
            {
                item.Value.InstanceData.Clear();
            }
        }

        /// <summary>
        /// Adds instance of the given model and returns its internal id which can be used for removing the instance. Local matrix specified will be changed to internal packed matrix.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="localMatrix">Local transformation matrix. Changed to internal matrix.</param>
        /// <param name="colorMaskHsv"></param>
        public uint AddInstance(ModelId model, ref Matrix localMatrix, BoundingBox localAabb, Vector4 colorMaskHsv = default(Vector4))
        {
            MyModelInstanceData builderInstanceData;
            if (!m_instanceParts.TryGetValue(model, out builderInstanceData))
            {
                builderInstanceData = new MyModelInstanceData(m_instanceFlags, m_maxViewDistance);
                builderInstanceData.Model = model;
                m_instanceParts.Add(model, builderInstanceData);
            }

            uint instanceId = m_idCounter++;

            var instanceData = new MyCubeInstanceData()
            {
                ColorMaskHSV = colorMaskHsv,
                EnableSkinning = false,
                LocalMatrix = localMatrix
            };
            instanceData.SetColorMaskHSV(colorMaskHsv);

            builderInstanceData.InstanceData.Add(instanceId, instanceData);
            // Matrix has been changed due to packing.
            localMatrix = builderInstanceData.InstanceData[instanceId].LocalMatrix;

            m_AABB = m_AABB.Include(localAabb.Transform(localMatrix));

            return instanceId;
        }

        public bool RemoveInstance(ModelId model, uint instanceId)
        {
            MyModelInstanceData builderInstanceData;
            if (!m_instanceParts.TryGetValue(model, out builderInstanceData))
                return false;

            return builderInstanceData.InstanceData.Remove(instanceId);
        }

        public void UpdateRenderInstanceData()
        {
            if (m_instanceBufferId == MyRenderProxy.RENDER_ID_UNASSIGNED)
            {
                m_instanceBufferId = MyRenderProxy.CreateRenderInstanceBuffer(m_debugBufferName, MyRenderInstanceBufferType.Cube);
            }

            // Merge data into one buffer
            Debug.Assert(m_tmpInstanceData.Count == 0, "Instance data is not cleared");
            m_instanceInfo.Clear();
            foreach (var part in m_instanceParts)
            {
                m_instanceInfo.Add(part.Key, new MyRenderInstanceInfo(m_instanceBufferId, m_tmpInstanceData.Count, part.Value.InstanceData.Count, part.Value.InstanceInfo.MaxViewDistance, part.Value.InstanceInfo.Flags));

                m_tmpInstanceData.AddList(part.Value.InstanceData.Values.ToList());
            }

            if (m_tmpInstanceData.Count > 0)
            {
                MyRenderProxy.UpdateRenderCubeInstanceBuffer(m_instanceBufferId, m_tmpInstanceData, (int)(m_tmpInstanceData.Count * 1.2f));
            }
            m_tmpInstanceData.Clear();
        }

        public void UpdateRenderEntitiesData(MatrixD worldMatrix, bool useTransparency = false)
        {
            // Create/Remove/Update render objects
            foreach (var item in m_instanceInfo)
            {
                uint renderObjectId;
                bool exists = m_instanceGroupRenderObjects.TryGetValue(item.Key, out renderObjectId);
                bool hasAnyInstances = item.Value.InstanceCount > 0;

                RenderFlags flags = item.Value.CastShadows ? RenderFlags.CastShadows : (RenderFlags)0;
                flags |= RenderFlags.Visible;

                if (!exists && hasAnyInstances)
                {
                    var model = MyModel.GetById(item.Key);
                    renderObjectId = VRageRender.MyRenderProxy.CreateRenderEntity(
                        "Instance parts, part: " + item.Key,
                        model,
                        MatrixD.Identity,
                        MyMeshDrawTechnique.MESH,
                        flags,
                        CullingOptions.Default,
                        Vector3.One,
						MyPlayer.SelectedColor,
                        useTransparency ? m_transparency : 0,
                        item.Value.MaxViewDistance
                    );

                    m_instanceGroupRenderObjects[item.Key] = renderObjectId;
                }
                else if (exists && !hasAnyInstances)
                {
                    uint objectId = m_instanceGroupRenderObjects[item.Key];
                    VRageRender.MyRenderProxy.RemoveRenderObject(objectId);
                    m_instanceGroupRenderObjects.Remove(item.Key);
                    renderObjectId = MyRenderProxy.RENDER_ID_UNASSIGNED;
                    continue;
                }

                if (hasAnyInstances)
                {
					MyRenderProxy.UpdateRenderEntity(renderObjectId, Color.White, MyPlayer.SelectedColor, useTransparency ? m_transparency : 0);
                    MyRenderProxy.UpdateRenderObject(renderObjectId, ref worldMatrix, false);
                    MyRenderProxy.SetInstanceBuffer(renderObjectId, item.Value.InstanceBufferId, item.Value.InstanceStart, item.Value.InstanceCount, m_AABB);
                }
            }
        }



    }
}
