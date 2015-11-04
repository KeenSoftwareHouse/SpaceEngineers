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
        private struct MySectorInstanceData
        {
            public int LocalId;
            public MyInstanceData InstanceData;
        }

        private class MyModelInstanceData
        {
            public readonly MyStringHash SubtypeId;
            public readonly MyInstanceFlagsEnum Flags = MyInstanceFlagsEnum.ShowLod1 | MyInstanceFlagsEnum.CastShadows | MyInstanceFlagsEnum.EnableColorMask;
            public readonly float MaxViewDistance = float.MaxValue;
            public readonly List<MySectorInstanceData> InstanceData = new List<MySectorInstanceData>();
            public readonly Queue<int> FreeInstances = new Queue<int>();
            public readonly BoundingBox ModelBox;

            public MyModelInstanceData(MyStringHash subtypeId, MyInstanceFlagsEnum flags, float maxViewDistance, BoundingBox modelBox)
            {
                SubtypeId = subtypeId;
                Flags = flags;
                MaxViewDistance = maxViewDistance;
                ModelBox = modelBox;
            }

            public int AddInstanceData(ref MySectorInstanceData instanceData)
            {
                if (FreeInstances.Count > 0)
                {
                    var idx = FreeInstances.Dequeue();
                    InstanceData[idx] = instanceData;
                    return idx;
                }
                else
                {
                InstanceData.Add(instanceData);
                return InstanceData.Count - 1;
            }
        }
        }

        private readonly Vector3I m_id;
        private MatrixD m_sectorMatrix;
        private MatrixD m_sectorInvMatrix;

        FastResourceLock m_instancePartsLock = new FastResourceLock();
        private Dictionary<ModelId, MyModelInstanceData> m_instanceParts = new Dictionary<ModelId, MyModelInstanceData>();

        private uint m_instanceBufferId = MyRenderProxy.RENDER_ID_UNASSIGNED;
        private List<MyInstanceData> m_tmpInstanceData = new List<MyInstanceData>();
        private Dictionary<ModelId, MyRenderInstanceInfo> m_instanceInfo = new Dictionary<ModelId, MyRenderInstanceInfo>();
        private Dictionary<ModelId, uint> m_instanceGroupRenderObjects = new Dictionary<ModelId, uint>();
        private BoundingBox m_AABB = BoundingBox.CreateInvalid();
        private bool m_invalidateAABB = false;

        public MatrixD SectorMatrix { get { return m_sectorMatrix; } }

        public bool IsValid
        {
            get { return m_sectorItemCount > 0; }
        }

        public BoundingBox SectorBox
        {
            get 
            {
                if (m_invalidateAABB)
                {
                    m_invalidateAABB = false;
                    m_AABB = GetSectorBoundingBox();
                }
                return m_AABB; 
            }
        }

        public BoundingBoxD SectorWorldBox
        {
            get
            {
                var worldAABB = SectorBox.Transform(m_sectorMatrix);
                return worldAABB;
            }
        }

        private int m_sectorItemCount;
        public int SectorItemCount
        {
            get { return m_sectorItemCount; }
        }

        public MyEnvironmentSector(Vector3I id, Vector3D sectorOffset)
        {
            m_id = id;
            m_sectorMatrix = MatrixD.CreateTranslation(sectorOffset);
            m_sectorInvMatrix = MatrixD.Invert(m_sectorMatrix);
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

            using (m_instancePartsLock.AcquireExclusiveUsing())
            {
                foreach (var item in m_instanceParts)
                {
                    item.Value.InstanceData.Clear();
                }
            }
        }

        /// <summary>
        /// Adds instance of the given model. Local matrix specified might be changed internally for renderer (must be used for removing instances).
        /// </summary>
        /// <param name="subtypeId"></param>
        /// <param name="localMatrix">Local transformation matrix. Changed to internal matrix.</param>
        /// <param name="colorMaskHsv"></param>
        public int AddInstance(
            MyStringHash subtypeId, 
            ModelId modelId,
            int localId,
            ref Matrix localMatrix, 
            BoundingBox localAabb, 
            MyInstanceFlagsEnum instanceFlags, 
            float maxViewDistance,
            Vector4 colorMaskHsv = default(Vector4),
            Vector2I uvOffset = default(Vector2I))
        {
            MyModelInstanceData builderInstanceData;

            using (m_instancePartsLock.AcquireExclusiveUsing())
            {
                if (!m_instanceParts.TryGetValue(modelId, out builderInstanceData))
                {
                    builderInstanceData = new MyModelInstanceData(subtypeId, instanceFlags, maxViewDistance, localAabb);
                    m_instanceParts.Add(modelId, builderInstanceData);
                }
            }


            uvOffset = new Vector2I(MyUtils.GetRandomInt(2), MyUtils.GetRandomInt(2));
            Color green = Color.Green;
            Vector3 hsv = green.ColorToHSVDX11();
            hsv.Y = MyUtils.GetRandomFloat(0.0f, 1.0f);
            colorMaskHsv = new Vector4(hsv, 0);

            MySectorInstanceData newInstance = new MySectorInstanceData()
            {
                LocalId = localId,
                InstanceData = new MyInstanceData()
                {
                    ColorMaskHSV = new VRageMath.PackedVector.HalfVector4(colorMaskHsv),
                    LocalMatrix = localMatrix,
                    UVOffset = new VRageMath.PackedVector.HalfVector2(uvOffset)
                }
            };
            int sectorInstanceId = builderInstanceData.AddInstanceData(ref newInstance);

            // Matrix has been changed due to packing.
            localMatrix = builderInstanceData.InstanceData[sectorInstanceId].InstanceData.LocalMatrix;
            Debug.Assert(builderInstanceData.InstanceData[sectorInstanceId].InstanceData.LocalMatrix == localMatrix, "Bad matrix");

            m_AABB = m_AABB.Include(localAabb.Transform(localMatrix));
            m_sectorItemCount++;
            m_invalidateAABB = true;

            return sectorInstanceId;
        }

        public bool DisableInstance(int sectorInstanceId, ModelId modelId)
        {
            MyModelInstanceData instanceData = null;
            m_instanceParts.TryGetValue(modelId, out instanceData);
            Debug.Assert(instanceData != null, "Could not find instance data in a sector for model " + modelId.ToString());
            if (instanceData == null) return false;

            Debug.Assert(instanceData.InstanceData.Count > sectorInstanceId, "Disabling invalid instance in environment item sector!");
            if (instanceData.InstanceData.Count <= sectorInstanceId) return false;

            var data = instanceData.InstanceData[sectorInstanceId];
            data.InstanceData.LocalMatrix = Matrix.Zero;
            instanceData.InstanceData[sectorInstanceId] = data;

            instanceData.FreeInstances.Enqueue(sectorInstanceId);
            m_sectorItemCount--;
            m_invalidateAABB = true;

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
            using (m_instancePartsLock.AcquireSharedUsing())
            {
                foreach (var part in m_instanceParts)
                {
                    m_instanceInfo.Add(part.Key, new MyRenderInstanceInfo(m_instanceBufferId, m_tmpInstanceData.Count, part.Value.InstanceData.Count, part.Value.MaxViewDistance, part.Value.Flags));
                    var instanceData = part.Value.InstanceData;
                    if (m_tmpInstanceData.Count + instanceData.Count > m_tmpInstanceData.Capacity)
                        m_tmpInstanceData.Capacity = m_tmpInstanceData.Count + instanceData.Count;
                    foreach (var instance in instanceData)
                        m_tmpInstanceData.Add(instance.InstanceData);
                }
            }

            if (m_tmpInstanceData.Count > 0)
            {
                MyRenderProxy.UpdateRenderInstanceBuffer(m_instanceBufferId, m_tmpInstanceData, (int)(m_tmpInstanceData.Count * 1.2f));
            }
            else
            {
                MyRenderProxy.RemoveRenderObject(m_instanceBufferId);
                foreach (var renderObjectId in m_instanceGroupRenderObjects.Values)
                    MyRenderProxy.RemoveRenderObject(renderObjectId);
                m_instanceGroupRenderObjects.Clear();
            }
            m_tmpInstanceData.Clear();
        }

        public void UpdateRenderEntitiesData(MatrixD worldMatrixD, bool useTransparency = false, float transparency = 0.0f)
        {
            // Create/Remove/Update render objects
            foreach (var item in m_instanceInfo)
            {
                uint renderObjectId;
                ModelId modelId = item.Key;
                bool exists = m_instanceGroupRenderObjects.TryGetValue(item.Key, out renderObjectId);
                bool hasAnyInstances = item.Value.InstanceCount > 0;

                RenderFlags flags = item.Value.CastShadows ? RenderFlags.CastShadows : (RenderFlags)0;
                flags |= RenderFlags.Visible;

                if (!exists && hasAnyInstances)
                {
                    var model = MyModel.GetById(modelId);

                    renderObjectId = VRageRender.MyRenderProxy.CreateRenderEntity(
                        "Instance parts, part: " + modelId,
                        model,
                        m_sectorMatrix,
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
                    MyRenderProxy.SetInstanceBuffer(renderObjectId, item.Value.InstanceBufferId, item.Value.InstanceStart, item.Value.InstanceCount, SectorBox);
                    MyRenderProxy.UpdateRenderEntity(renderObjectId, Vector3.One, Vector3.Zero, useTransparency ? transparency : 0);
                    MyRenderProxy.UpdateRenderObject(renderObjectId, ref m_sectorMatrix, false);
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
            using (m_instancePartsLock.AcquireSharedUsing())
            {
                foreach (var part in m_instanceParts.Values)
                {
                    foreach (var data in part.InstanceData)
                    {
                        var itemWorldPosition = Vector3D.Transform(data.InstanceData.LocalMatrix.Translation, m_sectorMatrix);

                        var dist = (itemWorldPosition - Sandbox.Game.World.MySector.MainCamera.Position).Length();
                         if (dist < 30)
                        MyRenderProxy.DebugDrawText3D(itemWorldPosition, part.SubtypeId.ToString(), Color.White, 0.7f, true);


                    }
                }
            }
            
            //BoundingBoxD bb = new BoundingBoxD(sectorPos * sectorSize, (sectorPos + Vector3I.One) * sectorSize);
            //BoundingBoxD bb2 = new BoundingBoxD(m_AABB.Min, m_AABB.Max);
            //bb2.Min = Vector3D.Max(bb2.Min, bb.Min);
            //bb2.Max = Vector3D.Min(bb2.Max, bb.Max);
            //MyRenderProxy.DebugDrawAABB(bb, Color.Orange, 1.0f, 1.0f, true);
            //MyRenderProxy.DebugDrawAABB(bb2, Color.OrangeRed, 1.0f, 1.0f, true);

            MyRenderProxy.DebugDrawAABB(SectorWorldBox, Color.OrangeRed, 1.0f, 1.0f, true);

        }

        internal void GetItems(List<Vector3D> output)
        {
            foreach (var part in m_instanceParts)
            {
                var list = part.Value.InstanceData;
                foreach (var item in list)
                {
                    output.Add(Vector3D.Transform(item.InstanceData.LocalMatrix.Translation, m_sectorMatrix));
                }
            }
        }

        internal void GetItemsInRadius(Vector3D position, float radius, List<Vector3D> output)
        {
            var local = Vector3D.Transform(position, m_sectorInvMatrix);
            foreach (var part in m_instanceParts)
            {
                var list = part.Value.InstanceData;
                foreach (var item in list)
                {
                    if (Vector3D.DistanceSquared(item.InstanceData.LocalMatrix.Translation, local) < radius * radius)
                        output.Add(Vector3D.Transform(item.InstanceData.LocalMatrix.Translation, m_sectorMatrix));
                }
            }
        }

        internal void GetItemsInRadius(Vector3 position, float radius, List<MyEnvironmentItems.ItemInfo> output)
        {
            double sqRadius = radius * radius;
            foreach (var part in m_instanceParts)
            {
                var list = part.Value.InstanceData;
                foreach (var item in list)
                {
                    var itemWorldPosition = Vector3.Transform(item.InstanceData.LocalMatrix.Translation, m_sectorMatrix);
                    if ((itemWorldPosition - position).LengthSquared() < sqRadius)
                    {
                        output.Add(new MyEnvironmentItems.ItemInfo()
                        {
                            LocalId = item.LocalId,
                            SubtypeId = part.Value.SubtypeId,
                            Transform = new MyTransformD(itemWorldPosition)
                        });
                    }
                }
            }
        }

        internal void GetItems(List<MyEnvironmentItems.ItemInfo> output)
        {
            foreach (var part in m_instanceParts)
            {
                var list = part.Value.InstanceData;
                foreach (var item in list)
                {
                    var mat = item.InstanceData.LocalMatrix;
                    if (!mat.EqualsFast(ref Matrix.Zero))
                    {
                        var itemWorldPosition = Vector3.Transform(mat.Translation, m_sectorMatrix);
                        output.Add(new MyEnvironmentItems.ItemInfo()
                        {
                            LocalId = item.LocalId,
                            SubtypeId = part.Value.SubtypeId,
                            Transform = new MyTransformD(itemWorldPosition)
                        });
                    }
                }
            }
        }

        private BoundingBox GetSectorBoundingBox()
        {
            if (!IsValid)
                return new BoundingBox(Vector3.Zero, Vector3.Zero);

            BoundingBox output = BoundingBox.CreateInvalid();
            using (m_instancePartsLock.AcquireSharedUsing())
            {
                foreach (var modelData in m_instanceParts)
                {
                    var modelBox = modelData.Value.ModelBox;
                    foreach (var instance in modelData.Value.InstanceData)
                    {
                        var mat = instance.InstanceData.LocalMatrix;
                        if (!mat.EqualsFast(ref Matrix.Zero))
                            output.Include(modelBox.Transform(instance.InstanceData.LocalMatrix));
                    }
                }
            }

            return output;
        }

    }
}
