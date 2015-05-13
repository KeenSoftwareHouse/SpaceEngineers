#region Using

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Engine.Models;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Character;
using Sandbox.Graphics;
using VRage;
using VRage.Import;
using VRage.Utils;
using VRageMath;
using VRageRender;
using Sandbox.Game.GUI;

#endregion

namespace Sandbox.Game.Entities.EnvironmentItems
{
    using ModelId = System.Int32;
    using VRage.Utils;
    using VRage.Library.Utils;

    /// <summary>
    /// Area of environment items where data is instanced.
    /// </summary>
    public class MyEnvironmentSector
    {
        private class MyModelInstanceData
        {
            public readonly MyStringId SubtypeId;
            public readonly MyInstanceFlagsEnum Flags = MyInstanceFlagsEnum.ShowLod1 | MyInstanceFlagsEnum.CastShadows | MyInstanceFlagsEnum.EnableColorMask;
            public readonly float MaxViewDistance = float.MaxValue;
            public readonly List<MyInstanceData> InstanceData = new List<MyInstanceData>();

            public MyModelInstanceData(MyStringId subtypeId, MyInstanceFlagsEnum flags, float maxViewDistance)
            {
                SubtypeId = subtypeId;
                Flags = flags;
                MaxViewDistance = maxViewDistance;
            }

            public int AddInstanceData(ref MyInstanceData instanceData)
            {
                InstanceData.Add(instanceData);
                return InstanceData.Count - 1;
            }
        }

        private readonly Vector3I m_id;

        private Dictionary<MyStringId, MyModelInstanceData> m_instanceParts = new Dictionary<MyStringId, MyModelInstanceData>();
        private uint m_instanceBufferId = MyRenderProxy.RENDER_ID_UNASSIGNED;
        private List<MyInstanceData> m_tmpInstanceData = new List<MyInstanceData>();
        private Dictionary<MyStringId, MyRenderInstanceInfo> m_instanceInfo = new Dictionary<MyStringId, MyRenderInstanceInfo>();
        private Dictionary<MyStringId, uint> m_instanceGroupRenderObjects = new Dictionary<MyStringId, uint>();
        private BoundingBox m_AABB = BoundingBox.CreateInvalid();

        public BoundingBox SectorBox
        {
            get { return m_AABB; }
        }

        private int m_sectorItemCount;
        public int SectorItemCount
        {
            get { return m_sectorItemCount; }
        }

        public MyEnvironmentSector(Vector3I id)
        {
            m_id = id;
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
            m_sectorItemCount = 0;

            foreach (var item in m_instanceParts)
            {
                item.Value.InstanceData.Clear();
            }
        }

        /// <summary>
        /// Adds instance of the given model. Local matrix specified might be changed internally for renderer (must be used for removing instances).
        /// </summary>
        /// <param name="subtypeId"></param>
        /// <param name="localMatrix">Local transformation matrix. Changed to internal matrix.</param>
        /// <param name="colorMaskHsv"></param>
        public int AddInstance(MyStringId subtypeId, ref Matrix localMatrix, BoundingBox localAabb, MyInstanceFlagsEnum instanceFlags, float maxViewDistance,
            Vector4 colorMaskHsv = default(Vector4))
        {
            MyModelInstanceData builderInstanceData;
            if (!m_instanceParts.TryGetValue(subtypeId, out builderInstanceData))
            {
                builderInstanceData = new MyModelInstanceData(subtypeId, instanceFlags, maxViewDistance);
                m_instanceParts.Add(subtypeId, builderInstanceData);
            }

            MyInstanceData newInstance = new MyInstanceData()
            {
                ColorMaskHSV = new VRageMath.PackedVector.HalfVector4(colorMaskHsv),
                LocalMatrix = localMatrix
            };
            int sectorInstanceId = builderInstanceData.AddInstanceData(ref newInstance);

            // Matrix has been changed due to packing.
            localMatrix = builderInstanceData.InstanceData[builderInstanceData.InstanceData.Count - 1].LocalMatrix;
            Debug.Assert(builderInstanceData.InstanceData[builderInstanceData.InstanceData.Count - 1].LocalMatrix == localMatrix, "Bad matrix");

            m_AABB = m_AABB.Include(localAabb.Transform(localMatrix));
            m_sectorItemCount++;

            return sectorInstanceId;
        }

        public bool DisableInstance(int sectorInstanceId, MyStringId subtypeId)
        {
            MyModelInstanceData instanceData = null;
            m_instanceParts.TryGetValue(subtypeId, out instanceData);
            Debug.Assert(instanceData != null, "Could not find instance data in a sector for subtype " + subtypeId.ToString());
            if (instanceData == null) return false;

            Debug.Assert(instanceData.InstanceData.Count > sectorInstanceId, "Disabling invalid instance in environment item sector!");
            if (instanceData.InstanceData.Count <= sectorInstanceId) return false;

            var data = instanceData.InstanceData[sectorInstanceId];
            data.LocalMatrix = Matrix.Zero;
            instanceData.InstanceData[sectorInstanceId] = data;

            return true;
        }

        public void UpdateRenderInstanceData()
        {
            if (m_instanceBufferId == MyRenderProxy.RENDER_ID_UNASSIGNED)
            {
                m_instanceBufferId = MyRenderProxy.CreateRenderInstanceBuffer("Environment items sector " + string.Format("{0} {1} {2}", m_id.X, m_id.Y, m_id.Z),
                    MyRenderInstanceBufferType.Generic);
            }

            // Merge data into one buffer
            Debug.Assert(m_tmpInstanceData.Count == 0, "Instance data is not cleared");
            m_instanceInfo.Clear();
            foreach (var part in m_instanceParts)
            {
                m_instanceInfo.Add(part.Key, new MyRenderInstanceInfo(m_instanceBufferId, m_tmpInstanceData.Count, part.Value.InstanceData.Count, part.Value.MaxViewDistance, part.Value.Flags));

                m_tmpInstanceData.AddList(part.Value.InstanceData);
            }

            if (m_tmpInstanceData.Count > 0)
            {
                MyRenderProxy.UpdateRenderInstanceBuffer(m_instanceBufferId, m_tmpInstanceData, (int)(m_tmpInstanceData.Count * 1.2f));
            }
            m_tmpInstanceData.Clear();
        }

        public void UpdateRenderEntitiesData(MatrixD worldMatrixD, Dictionary<MyStringId, ModelId> modelMappings, bool useTransparency = false, float transparency = 0.0f)
        {
            // Create/Remove/Update render objects
            foreach (var item in m_instanceInfo)
            {
                uint renderObjectId;
                ModelId modelId;
                bool exists = m_instanceGroupRenderObjects.TryGetValue(item.Key, out renderObjectId);
                bool hasAnyInstances = item.Value.InstanceCount > 0;
                bool hasModel = modelMappings.TryGetValue(item.Key, out modelId);

                RenderFlags flags = item.Value.CastShadows ? RenderFlags.CastShadows : (RenderFlags)0;
                flags |= RenderFlags.Visible;

                if (!exists && hasAnyInstances && hasModel)
                {
                    var model = MyModel.GetById(modelId);

                    renderObjectId = VRageRender.MyRenderProxy.CreateRenderEntity(
                        "Instance parts, part: " + modelId,
                        model,
                        MatrixD.Identity,
                        MyMeshDrawTechnique.MESH,
                        flags,
                        CullingOptions.Default,
                        Vector3.One,
                        Vector3.Zero,
                        useTransparency ? transparency : 0,
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
                    MyRenderProxy.UpdateRenderEntity(renderObjectId, Vector3.One, Vector3.Zero, useTransparency ? transparency : 0);
                    MyRenderProxy.UpdateRenderObject(renderObjectId, ref worldMatrixD, false);
                    MyRenderProxy.SetInstanceBuffer(renderObjectId, item.Value.InstanceBufferId, item.Value.InstanceStart, item.Value.InstanceCount, m_AABB);

                    //MyMedievalDebugDrawHelper.Static.AddAabb(m_AABB);
                }
            }

        }

        public static Vector3I GetSectorId(Vector3D position, float sectorSize)
        {
            return Vector3I.Floor(position / sectorSize);
        }

        internal void DebugDraw(Vector3I sectorPos, float sectorSize)
        {
            foreach (var part in m_instanceParts.Values)
            {
                foreach (var data in part.InstanceData)
                {
                    var dist = (data.LocalMatrix.Translation - Sandbox.Game.World.MySector.MainCamera.Position).Length();
                    if (dist < 30)
                        MyRenderProxy.DebugDrawText3D(data.LocalMatrix.Translation, part.SubtypeId.ToString(), Color.Red, (float)(7.0 / dist), true);
                }
            }

            BoundingBoxD bb = new BoundingBoxD(sectorPos * sectorSize, (sectorPos + Vector3I.One) * sectorSize);
            BoundingBoxD bb2 = new BoundingBoxD(m_AABB.Min, m_AABB.Max);
            bb2.Min = Vector3D.Max(bb2.Min, bb.Min);
            bb2.Max = Vector3D.Min(bb2.Max, bb.Max);
            MyRenderProxy.DebugDrawAABB(bb, Color.Orange, 1.0f, 1.0f, true);
            MyRenderProxy.DebugDrawAABB(bb2, Color.OrangeRed, 1.0f, 1.0f, true);
        }

        internal void GetItems(MatrixD worldMatrix, List<Vector3D> output)
        {
            foreach (var part in m_instanceParts)
            {
                var list = part.Value.InstanceData;
                foreach (var item in list)
                {
                    output.Add(Vector3D.Transform(item.LocalMatrix.Translation, worldMatrix));
                }
            }
        }
    }
}
