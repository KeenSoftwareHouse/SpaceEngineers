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
using Sandbox.Game.World;
using VRageRender.Import;
using VRageRender.Messages;

#endregion

namespace Sandbox.Game.Entities.EnvironmentItems
{
    using ModelId = System.Int32;
    using VRage.Utils;
    using VRage.Library.Utils;
    using VRage.Game.Models;

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
            // Sector containing this instance data.
            public MyEnvironmentSector Parent;

            public ModelId Model;

            public readonly MyStringHash SubtypeId;
            public readonly MyInstanceFlagsEnum Flags = MyInstanceFlagsEnum.ShowLod1 | MyInstanceFlagsEnum.CastShadows | MyInstanceFlagsEnum.EnableColorMask;
            public readonly float MaxViewDistance = float.MaxValue;

            // Instance data.
            public readonly Dictionary<int, MyInstanceData> InstanceData = new Dictionary<int, MyInstanceData>(); 
            public readonly Dictionary<int, int> InstanceIds = new Dictionary<int, int>(); // MyInstanceData ID to Instance Id
            private int m_keyIndex;

            public readonly BoundingBox ModelBox;

            public uint InstanceBuffer = MyRenderProxy.RENDER_ID_UNASSIGNED;
            public uint RenderObjectId = MyRenderProxy.RENDER_ID_UNASSIGNED;

            public FastResourceLock InstanceBufferLock = new FastResourceLock();

            private bool m_changed = false;

            public int InstanceCount { get { return InstanceData.Count; } }

            public MyModelInstanceData(MyEnvironmentSector parent, MyStringHash subtypeId, ModelId model, MyInstanceFlagsEnum flags, float maxViewDistance, BoundingBox modelBox)
            {
                Parent = parent;
                SubtypeId = subtypeId;
                Flags = flags;
                MaxViewDistance = maxViewDistance;
                ModelBox = modelBox;
                Model = model;
            }

            public int AddInstanceData(ref MySectorInstanceData instanceData)
            {
                using (InstanceBufferLock.AcquireExclusiveUsing())
                {
                    while (InstanceData.ContainsKey(m_keyIndex) && InstanceData.Count < int.MaxValue)
                    {
                        m_keyIndex++;
                    }

                    if (!InstanceData.ContainsKey(m_keyIndex))
                    {
                        InstanceData.Add(m_keyIndex, instanceData.InstanceData);
                        InstanceIds.Add(m_keyIndex, instanceData.LocalId);
                        return m_keyIndex;
                    }
                    else
                    {
                        throw new Exception("No available keys to add new instance data to sector!");
                    }
                }
            }

            public void UnloadRenderObjects()
            {
                if (InstanceBuffer != MyRenderProxy.RENDER_ID_UNASSIGNED)
                {
                    MyRenderProxy.RemoveRenderObject(InstanceBuffer);
                    InstanceBuffer = MyRenderProxy.RENDER_ID_UNASSIGNED;
                }
                if (RenderObjectId != MyRenderProxy.RENDER_ID_UNASSIGNED)
                {
                    MyRenderProxy.RemoveRenderObject(RenderObjectId);
                    RenderObjectId = MyRenderProxy.RENDER_ID_UNASSIGNED;
                }
            }

            public void UpdateRenderInstanceData()
            {
                if (InstanceData.Count == 0) return;

                if (InstanceBuffer == MyRenderProxy.RENDER_ID_UNASSIGNED)
                {
                    InstanceBuffer = MyRenderProxy.CreateRenderInstanceBuffer(string.Format("EnvironmentSector{0} - {1}", Parent.SectorId, SubtypeId),
                        MyRenderInstanceBufferType.Generic);
                }

                MyRenderProxy.UpdateRenderInstanceBufferRange(InstanceBuffer, InstanceData.Values.ToArray());
            }

            public bool DisableInstance(int sectorInstanceId)
            {
                using (InstanceBufferLock.AcquireExclusiveUsing())
                {
                    //Debug.Assert(InstanceData.Count > sectorInstanceId, "Disabling invalid instance in environment item sector!");
                    if (!InstanceData.ContainsKey(sectorInstanceId))
                    {
                        if (MyFakes.ENABLE_FLORA_COMPONENT_DEBUG)
                        {
                            System.Diagnostics.Debug.Fail("Instance with the passed id wasn't found in the sector!");
                        }
                        return false;
                    }
                                        
                    InstanceData.Remove(sectorInstanceId);
                    InstanceIds.Remove(sectorInstanceId);
                }
                return true;
            }

            internal void UpdateRenderEntitiesData(ref MatrixD worldMatrixD, bool useTransparency, float transparency)
            {
                ModelId modelId = Model;

                bool hasInstances = (InstanceCount > 0);
                bool hasEntity = RenderObjectId != MyRenderProxy.RENDER_ID_UNASSIGNED;

                // nothin' 
                if (!hasInstances)
                {
                    if (!hasEntity)
                        return;
                    else
                    {
                        UnloadRenderObjects();
                    }
                }
                else
                {
                    RenderFlags flags = RenderFlags.Visible | RenderFlags.CastShadows;

                    if (!hasEntity)
                    {
                        var model = MyModel.GetById(modelId);

                        RenderObjectId = VRageRender.MyRenderProxy.CreateRenderEntity(
                            "Instance parts, part: " + modelId,
                            model,
                            Parent.SectorMatrix,
                            MyMeshDrawTechnique.MESH,
                            flags,
                            CullingOptions.Default,
                            Vector3.One,
                            Vector3.Zero,
                            useTransparency ? transparency : 0,
                            MaxViewDistance
                        );
                    }

                    MyRenderProxy.SetInstanceBuffer(RenderObjectId, InstanceBuffer, 0, InstanceData.Count, Parent.SectorBox);
                    MyRenderProxy.UpdateRenderEntity(RenderObjectId, Vector3.One, Vector3.Zero, useTransparency ? transparency : 0);
                    var matrix = Parent.SectorMatrix;
                    MyRenderProxy.UpdateRenderObject(RenderObjectId, ref matrix, false);
                }
            }
        }

        public Vector3I SectorId
        {
            get { return m_id; }
        }

        private readonly Vector3I m_id;
        private MatrixD m_sectorMatrix;
        private MatrixD m_sectorInvMatrix;

        FastResourceLock m_instancePartsLock = new FastResourceLock();
        private Dictionary<ModelId, MyModelInstanceData> m_instanceParts = new Dictionary<ModelId, MyModelInstanceData>();

        private List<MyInstanceData> m_tmpInstanceData = new List<MyInstanceData>();
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
            using (m_instancePartsLock.AcquireExclusiveUsing())
            {
                foreach (var instance in m_instanceParts)
                {
                    instance.Value.UnloadRenderObjects();
                }
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
            Vector3 colorMaskHsv = default(Vector3))
        {
            MyModelInstanceData builderInstanceData;

            using (m_instancePartsLock.AcquireExclusiveUsing())
            {
                if (!m_instanceParts.TryGetValue(modelId, out builderInstanceData))
                {
                    builderInstanceData = new MyModelInstanceData(this, subtypeId, modelId, instanceFlags, maxViewDistance, localAabb);
                    m_instanceParts.Add(modelId, builderInstanceData);
                }
            }


            MySectorInstanceData newInstance = new MySectorInstanceData()
            {
                LocalId = localId,
                InstanceData = new MyInstanceData()
                {
                    ColorMaskHSV = new VRageMath.PackedVector.HalfVector4(colorMaskHsv.X, colorMaskHsv.Y, colorMaskHsv.Z, 0),
                    LocalMatrix = localMatrix,
                }
            };
            int sectorInstanceId = builderInstanceData.AddInstanceData(ref newInstance);

            // Matrix has been changed due to packing.
            localMatrix = builderInstanceData.InstanceData[sectorInstanceId].LocalMatrix;
            Debug.Assert(builderInstanceData.InstanceData[sectorInstanceId].LocalMatrix == localMatrix, "Bad matrix");

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
            if (instanceData == null)
            {
                return false;
            }

            if (instanceData.DisableInstance(sectorInstanceId))
            {
                m_sectorItemCount--;
                m_invalidateAABB = true;

                return true;
            }
            return false;
        }

        public void UpdateRenderInstanceData()
        {
            using (m_instancePartsLock.AcquireSharedUsing())
            {
                foreach (var part in m_instanceParts)
                {
                    part.Value.UpdateRenderInstanceData();
                }
            }
        }

        public void UpdateRenderInstanceData(ModelId modelId)
        {
            using (m_instancePartsLock.AcquireSharedUsing())
            {
                MyModelInstanceData instanceData = null;
                m_instanceParts.TryGetValue(modelId, out instanceData);
                Debug.Assert(instanceData != null, "Could not find instance data in a sector for model " + modelId.ToString());
                if (instanceData == null) return;

                instanceData.UpdateRenderInstanceData();
            }
        }

        public void UpdateRenderEntitiesData(MatrixD worldMatrixD, bool useTransparency = false, float transparency = 0.0f)
        {
            // Create/Remove/Update render objects
            foreach (var item in m_instanceParts.Values)
            {
                item.UpdateRenderEntitiesData(ref worldMatrixD, useTransparency, transparency);
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
                foreach (var idata in m_instanceParts.Values)
                {
                    using (idata.InstanceBufferLock.AcquireSharedUsing())
                    {
                        foreach (var entry in idata.InstanceData)
                        {
                            var ii = entry.Value;
                            var itemWorldPosition = Vector3D.Transform(ii.LocalMatrix.Translation, m_sectorMatrix);



                            //var dist = (itemWorldPosition - Sandbox.Game.World.MySector.MainCamera.Position).Length();
                            //if (dist < 30)

                            //BoundingBoxD bb = idata.ModelBox.Transform(ii.LocalMatrix);
                            MyRenderProxy.DebugDrawOBB(Matrix.Rescale(ii.LocalMatrix * m_sectorMatrix, idata.ModelBox.HalfExtents * 2), Color.OrangeRed, .5f, true, true);

                            var dist = Vector3D.Distance(MySector.MainCamera.Position, itemWorldPosition);
                            if (dist < 250)
                                MyRenderProxy.DebugDrawText3D(itemWorldPosition, idata.SubtypeId.ToString(), Color.White, 0.7f, true);
                        }
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
                var idata = part.Value;
                using (idata.InstanceBufferLock.AcquireSharedUsing())
                {
                    foreach (var entry in idata.InstanceData)
                    {
                        var ii = entry.Value;

                        var mat = ii.LocalMatrix;
                        if (!mat.EqualsFast(ref Matrix.Zero))
                        {
                            output.Add(Vector3D.Transform(ii.LocalMatrix.Translation, m_sectorMatrix));
                        }
                    }
                }
            }
        }

        internal void GetItemsInRadius(Vector3D position, float radius, List<Vector3D> output)
        {
            var local = Vector3D.Transform(position, m_sectorInvMatrix);
            foreach (var part in m_instanceParts)
            {
                using (part.Value.InstanceBufferLock.AcquireSharedUsing())
                {
                    var list = part.Value.InstanceData;
                    foreach (var item in list)
                    {
                        if (Vector3D.DistanceSquared(item.Value.LocalMatrix.Translation, local) < radius * radius)
                            output.Add(Vector3D.Transform(item.Value.LocalMatrix.Translation, m_sectorMatrix));
                    }
                }
            }
        }

        internal void GetItemsInRadius(Vector3 position, float radius, List<MyEnvironmentItems.ItemInfo> output)
        {
            double sqRadius = radius * radius;
            foreach (var part in m_instanceParts)
            {
                var idata = part.Value;
                using (idata.InstanceBufferLock.AcquireSharedUsing())
                {
                    foreach (var entry in idata.InstanceData)
                    {
                        var ii = entry.Value;

                        if (ii.LocalMatrix.EqualsFast(ref Matrix.Zero)) continue;

                        var itemWorldPosition = Vector3.Transform(ii.LocalMatrix.Translation, m_sectorMatrix);
                        if ((itemWorldPosition - position).LengthSquared() < sqRadius)
                        {
                            output.Add(new MyEnvironmentItems.ItemInfo()
                            {
                                LocalId = idata.InstanceIds[entry.Key],
                                SubtypeId = part.Value.SubtypeId,
                                Transform = new MyTransformD(itemWorldPosition)
                            });
                        }
                    }
                }
            }
        }

        internal void GetItems(List<MyEnvironmentItems.ItemInfo> output)
        {
            foreach (var part in m_instanceParts)
            {
                var idata = part.Value;
                using (idata.InstanceBufferLock.AcquireSharedUsing())
                {
                    foreach (var entry in idata.InstanceData)
                    {
                        var ii = entry.Value;

                        var mat = ii.LocalMatrix;
                        if (!mat.EqualsFast(ref Matrix.Zero))
                        {
                            var itemWorldPosition = Vector3.Transform(mat.Translation, m_sectorMatrix);
                            output.Add(new MyEnvironmentItems.ItemInfo()
                            {
                                LocalId = idata.InstanceIds[entry.Key],
                                SubtypeId = part.Value.SubtypeId,
                                Transform = new MyTransformD(itemWorldPosition)
                            });
                        }
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
                foreach (var part in m_instanceParts)
                {
                    var idata = part.Value;
                    using (idata.InstanceBufferLock.AcquireSharedUsing())
                    {
                        var modelBox = idata.ModelBox;
                        foreach (var entry in idata.InstanceData)
                        {
                            var ii = entry.Value;
                            var mat = ii.LocalMatrix;
                            if (!mat.EqualsFast(ref Matrix.Zero))
                                output.Include(modelBox.Transform(ii.LocalMatrix));
                        }
                    }
                }
            }

            return output;
        }

    }
}
