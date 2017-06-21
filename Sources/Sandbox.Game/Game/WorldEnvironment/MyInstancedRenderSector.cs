using System;
using System.Collections.Generic;
using Sandbox.Definitions;
using VRage;
using VRage.Game.Models;
using VRage.Import;
using VRageMath;
using VRageRender;
using VRageRender.Import;
using VRageRender.Messages;

namespace Sandbox.Game.WorldEnvironment
{

    public class MyInstancedRenderSector
    {
        public string Name { get; private set; }

        private const bool ENABLE_SEPARATE_INSTANCE_LOD = false;

        public int Lod
        {
            get { return m_lod; }
            set
            {
                if (m_lod != value && value != -1)
                    foreach (var buffer in m_instancedModels.Values)
                    {
                        buffer.SetPerInstanceLod(value == 0);
                    }
                m_lod = value;
            }
        }

        public MatrixD WorldMatrix;

        public MyInstancedRenderSector(string name, MatrixD worldMatrix)
        {
            Name = name;
            WorldMatrix = worldMatrix;
        }

        #region Instance Control

        private readonly Dictionary<int, InstancedModelBuffer> m_instancedModels = new Dictionary<int, InstancedModelBuffer>();

        private readonly HashSet<int> m_changedBuffers = new HashSet<int>();
        private int m_lod;

        // Manager of buffer size expansion policy.
        private int GetExpandedSize(int size)
        {
            return size + 5;
        }

        private class InstancedModelBuffer
        {
            public MyInstanceData[] Instances = new MyInstanceData[4];
            public uint[] InstanceOIDs; // Per lod instancing (lod 0 only).
            public Queue<short> UnusedSlots = new Queue<short>(); 

            public uint InstanceBufferId = MyRenderProxy.RENDER_ID_UNASSIGNED;
            public uint RenderObjectId = MyRenderProxy.RENDER_ID_UNASSIGNED;

            public BoundingBox Bounds = BoundingBox.CreateInvalid();

            public Int32 Model = 0;

            private readonly MyInstancedRenderSector m_parent;

            public readonly BoundingBox ModelBb;

            public InstancedModelBuffer(MyInstancedRenderSector parent, int modelId)
            {
                m_parent = parent;
                Model = modelId;

                MyModel modelData = MyModels.GetModelOnlyData(MyModel.GetById(Model));
                ModelBb = modelData.BoundingBox;
            }

            public void UpdateRenderObjects()
            {
                var modelName = MyModel.GetById(Model);

                if (InstanceOIDs == null)
                {
                    var bounds = Bounds;
                    bounds.Translate(m_parent.WorldMatrix.Translation);

                    if (RenderObjectId == MyRenderProxy.RENDER_ID_UNASSIGNED)
                    {
                        RenderObjectId = MyRenderProxy.CreateRenderEntity(
                            string.Format("RO::{0}: {1}", m_parent.Name, modelName),
                            modelName,
                            m_parent.WorldMatrix,
                            MyMeshDrawTechnique.MESH,
                            RenderFlags.Visible | RenderFlags.CastShadows,
                            CullingOptions.Default,
                            Vector3.One, colorMaskHsv: Vector3.Zero, dithering: 0, maxViewDistance: 100000
                            );

                        MyRenderProxy.UpdateRenderEntity(RenderObjectId, Vector3.One, Vector3.Zero, 0);
                    }

                    if (InstanceBufferId == MyRenderProxy.RENDER_ID_UNASSIGNED)
                    {
                        InstanceBufferId = MyRenderProxy.CreateRenderInstanceBuffer(string.Format("IB::{0}: {1}", m_parent.Name, modelName),
                            MyRenderInstanceBufferType.Generic);
                    }

                    MyRenderProxy.UpdateRenderInstanceBufferRange(InstanceBufferId, Instances);
                    MyRenderProxy.UpdateRenderInstanceBufferSettings(InstanceBufferId, m_parent.Lod == 0);
                    MyRenderProxy.SetInstanceBuffer(RenderObjectId, InstanceBufferId, 0, Instances.Length, bounds);

                    MyRenderProxy.UpdateRenderObject(RenderObjectId, ref m_parent.WorldMatrix, false, (BoundingBoxD)bounds);
                }
                else
                unsafe
                {
                    if (InstanceOIDs.Length != Instances.Length)
                        ResizeActorBuffer();

                    fixed (MyInstanceData* instances = Instances)
                    for (int i = 0; i < InstanceOIDs.Length; ++i)
                        if (InstanceOIDs[i] == MyRenderProxy.RENDER_ID_UNASSIGNED && instances[i].m_row0.PackedValue != 0)
                        {
                            MatrixD matrix = instances[i].LocalMatrix * m_parent.WorldMatrix;
                            var bounds = (BoundingBoxD)ModelBb;
                            bounds = bounds.TransformFast(ref matrix);

                            //bounds.Translate(m_parent.WorldMatrix.Translation);

                            var obj = MyRenderProxy.CreateRenderEntity(
                                string.Format("RO::{0}: {1}", m_parent.Name, modelName),
                                modelName,
                                matrix,
                                MyMeshDrawTechnique.MESH,
                                RenderFlags.Visible | RenderFlags.CastShadows,
                                CullingOptions.Default,
                                Vector3.One, colorMaskHsv: Vector3.Zero, dithering: 0, maxViewDistance: 100000
                            );

                            MyRenderProxy.UpdateRenderObject(obj, ref matrix, false, (BoundingBoxD)bounds);

                            InstanceOIDs[i] = obj;
                        }
                }
            }

            public void ClearRenderObjects()
            {
                if (InstanceBufferId != MyRenderProxy.RENDER_ID_UNASSIGNED)
                {
                    MyRenderProxy.RemoveRenderObject(InstanceBufferId);
                    InstanceBufferId = MyRenderProxy.RENDER_ID_UNASSIGNED;
                }
                if (RenderObjectId != MyRenderProxy.RENDER_ID_UNASSIGNED)
                {
                    MyRenderProxy.RemoveRenderObject(RenderObjectId);
                    RenderObjectId = MyRenderProxy.RENDER_ID_UNASSIGNED;
                }

                if (InstanceOIDs != null)
                {
                    for (int i = 0; i < InstanceOIDs.Length; ++i)
                        if (InstanceOIDs[i] != MyRenderProxy.RENDER_ID_UNASSIGNED)
                        {
                            MyRenderProxy.RemoveRenderObject(InstanceOIDs[i]);
                            InstanceOIDs[i] = MyRenderProxy.RENDER_ID_UNASSIGNED;
                        }
                }
            }

            public void Close()
            {
                ClearRenderObjects();
                Bounds = BoundingBox.CreateInvalid();

                Instances = null;
                InstanceOIDs = null;

                UnusedSlots.Clear();
            }

            public void SetPerInstanceLod(bool value)
            {
                if (value != (InstanceOIDs != null) && Instances != null && ENABLE_SEPARATE_INSTANCE_LOD)
                {
                    ClearRenderObjects();
                    if (InstanceOIDs == null)
                        ResizeActorBuffer();
                    else
                        InstanceOIDs = null;
                    UpdateRenderObjects();
                }
            }

            private void ResizeActorBuffer()
            {
                int start = InstanceOIDs != null ? InstanceOIDs.Length : 0;

                Array.Resize(ref InstanceOIDs, Instances.Length);

                for (int i = start; i < InstanceOIDs.Length; ++i)
                {
                    InstanceOIDs[i] = MyRenderProxy.RENDER_ID_UNASSIGNED;
                }
            }
        }

        public unsafe int AddInstances(int model, List<MyInstanceData> instances)
        {
            InstancedModelBuffer buffer;
            if (!m_instancedModels.TryGetValue(model, out buffer))
            {
                buffer = new InstancedModelBuffer(this, model);
                buffer.SetPerInstanceLod(Lod == 0);
                m_instancedModels[model] = buffer;
            }

            buffer.Instances = instances.GetInternalArray();

            var count = instances.Count;

            fixed (MyInstanceData* data = buffer.Instances)
                for (int i = 0; i < count; i++)
                {
                    BoundingBox modelBox = buffer.ModelBb.Transform(data[i].LocalMatrix);
                    buffer.Bounds.Include(ref modelBox);
                }

            // Fix the unused slot lists
            buffer.UnusedSlots.Clear();
            for (int i = count; i < instances.Capacity; ++i)
            {
                buffer.UnusedSlots.Enqueue((short)(i));
            }

            m_changedBuffers.Add(model);

            return 0;
        }

        public void CommitChangesToRenderer()
        {
            foreach (var buffer in m_changedBuffers)
            {
                m_instancedModels[buffer].UpdateRenderObjects();
            }

            m_changedBuffers.Clear();
        }

        public void Close()
        {
            foreach (var buffer in m_instancedModels.Values)
            {
                buffer.Close();
            }
        }

        public bool HasChanges()
        {
            return m_changedBuffers.Count != 0;
        }

        #endregion

        public void DetachEnvironment(MyEnvironmentSector myEnvironmentSector)
        {
            Close();
        }

        public void RemoveInstance(int modelId, short index)
        {
            var buffer = m_instancedModels[modelId];
            buffer.Instances[index] = new MyInstanceData();
            buffer.UnusedSlots.Enqueue(index);

            m_changedBuffers.Add(modelId);
        }

        public short AddInstance(int modelId, ref MyInstanceData data)
        {
            InstancedModelBuffer buffer;
            if (!m_instancedModels.TryGetValue(modelId, out buffer))
            {
                buffer = new InstancedModelBuffer(this, modelId);
                buffer.SetPerInstanceLod(Lod == 0);
                m_instancedModels[modelId] = buffer;
            }

            short empty;
            if (buffer.UnusedSlots.TryDequeue(out empty))
            {
                buffer.Instances[empty] = data;
            }
            else
            {
                int prevSize = buffer.Instances != null ? buffer.Instances.Length : 0;

                int size = GetExpandedSize(prevSize);
                Array.Resize(ref buffer.Instances, size);

                int diff = size - prevSize;
                empty = (short)prevSize;

                buffer.Instances[empty] = data;

                for (int i = 1; i < diff; ++i)
                {
                    buffer.UnusedSlots.Enqueue((short)(i + empty));
                }
            }

            BoundingBox modelBox = buffer.ModelBb.Transform(data.LocalMatrix);
            buffer.Bounds.Include(ref modelBox);

            m_changedBuffers.Add(modelId);

            return empty;
        }

        public uint GetRenderEntity(int modelId)
        {
            InstancedModelBuffer buffer;
            if (m_instancedModels.TryGetValue(modelId, out buffer))
            {
                return buffer.RenderObjectId;
            }
            return MyRenderProxy.RENDER_ID_UNASSIGNED;
        }
    }
}
