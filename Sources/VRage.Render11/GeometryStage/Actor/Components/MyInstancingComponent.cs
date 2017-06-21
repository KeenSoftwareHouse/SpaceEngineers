using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage;
using VRage.Render11.Common;
using VRage.Render11.Resources;
using VRage.Utils;
using VRageMath.PackedVector;
using VRageRender.Messages;
using VRageRender.Vertex;


namespace VRageRender
{
    class MyInstancingComponent : MyActorComponent
    {
        private string m_debugName;

        // one-to-many component type -> generalize if more
        List<MyActor> m_owners;
        MyIDTracker<MyInstancingComponent> m_ID;
        internal IVertexBuffer VB;
        private MyVertexInputLayout m_input;
        private int m_stride;
        private MyRenderInstanceBufferType m_type;

        int m_capacity;

        internal override void Construct()
        {
            base.Construct();
            Type = MyActorComponentEnum.Instancing;

            MyUtils.Init(ref m_owners);
            m_owners.Clear();

            MyUtils.Init(ref m_ID);
            m_ID.Clear();

            VB = null;
            m_input = MyVertexInputLayout.Empty;
            m_stride = -1;
            m_type = MyRenderInstanceBufferType.Invalid;
            m_capacity = -1;
        }

        internal override void Destruct()
        {
            m_ID.Deregister();
            Dispose();

            base.Destruct();
        }

        internal override void Assign(MyActor owner)
        {
            Debug.Assert(m_owners.Find(x => x == owner) == null);
            base.Assign(owner);

            m_owners.Add(owner);
            owner.MarkRenderDirty();
        }

        internal unsafe void Init(MyRenderInstanceBufferType type)
        {
            Debug.Assert(type != MyRenderInstanceBufferType.Invalid, "Invalid instance buffer type!");
            if (type == MyRenderInstanceBufferType.Cube)
            {
                m_input = m_input.Append(MyVertexInputComponentType.CUBE_INSTANCE, 2, MyVertexInputComponentFreq.PER_INSTANCE);
                m_stride = sizeof(MyVertexFormatCubeInstance);
            }
            else if (type == MyRenderInstanceBufferType.Generic)
            {
                m_input = m_input.Append(MyVertexInputComponentType.GENERIC_INSTANCE, 2, MyVertexInputComponentFreq.PER_INSTANCE);
                m_stride = sizeof(MyVertexFormatGenericInstance);
            }

            m_type = type;
        }

        internal void Dispose()
        {
            if (VB != null)
                MyManagers.Buffers.Dispose(VB); VB = null;
            
            m_capacity = 0;
        }

        internal unsafe void UpdateGeneric(List<MyInstanceData> instanceData, int capacity)
        {
            Debug.Assert(m_type == MyRenderInstanceBufferType.Generic);

            fixed (MyInstanceData* dataPtr = instanceData.GetInternalArray())
            {
                m_capacity = Math.Max(instanceData.Count, capacity);

                // TODO: This class must allocate VB through MyManagers.Buffers before resize
                // New name was: m_debugName + " instances buffer"
                MyManagers.Buffers.Resize(VB, m_capacity, sizeof(MyVertexFormatGenericInstance), new IntPtr(dataPtr));
            }

            BumpRenderable();
        }

        internal void BumpRenderable()
        {
            foreach(var owner in m_owners)
            {
                owner.MarkRenderDirty();
            }
        }

        internal unsafe void UpdateCube(List<MyCubeInstanceData> instanceData, int capacity)
        {
            Debug.Assert(m_type == MyRenderInstanceBufferType.Cube);

            m_capacity = Math.Max(instanceData.Count, capacity);

            var rawBuffer = new MyVertexFormatCubeInstance[m_capacity];
            for (int instanceIndex = 0; instanceIndex < m_capacity; instanceIndex++)
            {
                fixed (byte* pSource = instanceData[instanceIndex].RawBones(), pTarget = rawBuffer[instanceIndex].bones)
                {
                    for (int boneIndex = 0; boneIndex < MyRender11Constants.CUBE_INSTANCE_BONES_NUM * 4; ++boneIndex)
                        pTarget[boneIndex] = pSource[boneIndex];
                }
                rawBuffer[instanceIndex].translationRotation = instanceData[instanceIndex].m_translationAndRot;
                rawBuffer[instanceIndex].colorMaskHSV = instanceData[instanceIndex].ColorMaskHSV;
            }

            fixed (MyVertexFormatCubeInstance* dataPtr = rawBuffer)
            {
                // TODO: This class must allocate VB through MyManagers.Buffers before resize
                // New name was: m_debugName + " instances buffer"
                MyManagers.Buffers.Resize(
                    VB, m_capacity, sizeof(MyVertexFormatCubeInstance), 
                    new IntPtr(dataPtr)); 
            }

            BumpRenderable();
        }

        internal void SetDebugName(string name)
        {
            m_debugName = name;
        }

        internal void SetID(uint id)
        {
            m_ID.Register(id, this);
        }

        internal override void OnRemove(MyActor owner)
        {
            base.OnRemove(owner);

            m_owners.Remove(owner);
        }
    }
}
