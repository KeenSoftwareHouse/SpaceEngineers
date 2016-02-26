using System;
using System.Collections.Generic;
using VRageRender.Vertex;
using VRage.Generics;

namespace VRageRender
{
    partial class MyInstanceBuffer
    {
        private string m_debugName;

        internal VertexBufferId VertexBuffer;
        internal MyVertexInputLayout m_input;
        internal int m_stride;
        internal MyRenderInstanceBufferType m_type;

        int m_capacity;

        internal unsafe void Construct(MyRenderInstanceBufferType type)
        {
            m_capacity = 0;

            m_input = MyVertexInputLayout.Empty;

            if(type == MyRenderInstanceBufferType.Cube)
            { 
                m_input.Append(MyVertexInputComponentType.CUBE_INSTANCE, 2, MyVertexInputComponentFreq.PER_INSTANCE);
                m_stride = sizeof(MyVertexFormatCubeInstance);
            }
            else if (type == MyRenderInstanceBufferType.Generic)
            {
                m_input.Append(MyVertexInputComponentType.GENERIC_INSTANCE, 2, MyVertexInputComponentFreq.PER_INSTANCE);
                m_stride = sizeof(MyVertexFormatGenericInstance);
            }

            m_type = type;
            VertexBuffer = VertexBufferId.NULL;
        }

        internal void Dispose()
        {
            if(VertexBuffer != VertexBufferId.NULL) 
            {
                MyHwBuffers.Destroy(VertexBuffer);
                VertexBuffer = VertexBufferId.NULL;
            }

            m_capacity = 0;
        }

        internal unsafe void UpdateGeneric(List<MyInstanceData> instanceData, int capacity)
        {
            var instancesNum = instanceData.Count;
            if (m_capacity < instancesNum)
                m_capacity = Math.Max(instancesNum, capacity);
            fixed (MyInstanceData* dataPtr = instanceData.ToArray())
            {
                MyHwBuffers.ResizeAndUpdateStaticVertexBuffer(ref VertexBuffer, m_capacity, sizeof(MyVertexFormatGenericInstance), new IntPtr(dataPtr), m_debugName + " instances buffer");
            }
        }

        internal unsafe void UpdateCube(List<MyCubeInstanceData> instanceData, int capacity)
        {
            var instancesNum = instanceData.Count;
            if (m_capacity < instancesNum)
                m_capacity = Math.Max(instancesNum, capacity);

            var rawBuffer = new MyVertexFormatCubeInstance[m_capacity];
            for (int i = 0; i < instancesNum; i++)
            {
                fixed (byte* pSource = instanceData[i].RawBones(), pTarget = rawBuffer[i].bones)
                {
                    for (int j = 0; j < MyRender11Constants.CUBE_INSTANCE_BONES_NUM * 4; j++)
                        pTarget[j] = pSource[j];
                }
                rawBuffer[i].translationRotation = instanceData[i].m_translationAndRot;
                rawBuffer[i].colorMaskHSV = (instanceData[i].ColorMaskHSV);
            }

            fixed (MyVertexFormatCubeInstance* dataPtr = rawBuffer)
            {
                MyHwBuffers.ResizeAndUpdateStaticVertexBuffer(ref VertexBuffer, m_capacity, sizeof(MyVertexFormatCubeInstance), new IntPtr(dataPtr), m_debugName + " instances buffer");
            }
        }
    };

    partial class MyInstanceBuffer
    {
        static MyObjectsPool<MyInstanceBuffer> m_classPool = new MyObjectsPool<MyInstanceBuffer>(128);
        static Dictionary<uint, MyInstanceBuffer> m_byID = new Dictionary<uint, MyInstanceBuffer>();

        internal static MyInstanceBuffer Create(uint ID, MyRenderInstanceBufferType type, string debugName)
        {
            MyInstanceBuffer instanceBuffer;
            m_classPool.AllocateOrCreate(out instanceBuffer);
            m_byID[ID] = instanceBuffer;
            instanceBuffer.Construct(type);
            instanceBuffer.m_debugName = debugName;

            return instanceBuffer;
        }

        internal static MyInstanceBuffer ByID(uint ID)
        {
            MyInstanceBuffer instanceBuffer;
            m_byID.TryGetValue(ID, out instanceBuffer);
            return instanceBuffer;
        }

        internal static void RemoveAll()
        {
            foreach(var buffer in m_byID.Values)
            {
                buffer.Dispose();
            }

            m_classPool.DeallocateAll();
            m_byID.Clear();
        }
    }
}
