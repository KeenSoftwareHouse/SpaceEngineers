using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;
using SharpDX.Direct3D9;
using System.Runtime.InteropServices;
using System.Diagnostics;
using VRageRender.Textures;
using System.Reflection;
using VRageRender.RenderObjects;
using VRage.Stats;
using VRageMath.PackedVector;

namespace VRageRender
{
    /// <summary>
    /// Instance group - group of objects, locally dependent.
    /// </summary>
    class MyRenderInstanceBuffer : MyRenderObject
    {
        // practically union of these two
        MyInstanceData[] m_instances;
        MyCubeInstanceData[] m_cubeInstances;

        VertexBuffer m_instanceBuffer;
        public MyRenderInstanceBufferType Type { get; private set; }

        public readonly List<VertexElement> VertexElements;
        public readonly int Stride;

        public unsafe MyRenderInstanceBuffer(uint id, string debugName, MyRenderInstanceBufferType type)
            : base(id, debugName, (RenderFlags)0)
        {
            Type = type;

            if(type == MyRenderInstanceBufferType.Cube)
            {
                VertexElements = new List<VertexElement>()
                {
                    new VertexElement(1, sizeof(byte) * 0, DeclarationType.UByte4N, DeclarationMethod.Default, DeclarationUsage.TextureCoordinate, 1),
                    new VertexElement(1, sizeof(byte) * 4, DeclarationType.UByte4N, DeclarationMethod.Default, DeclarationUsage.TextureCoordinate, 2),
                    new VertexElement(1, sizeof(byte) * 8, DeclarationType.UByte4N, DeclarationMethod.Default, DeclarationUsage.TextureCoordinate, 3),
                    new VertexElement(1, sizeof(byte) * 12, DeclarationType.UByte4N, DeclarationMethod.Default, DeclarationUsage.TextureCoordinate, 4),
                    new VertexElement(1, sizeof(byte) * 16, DeclarationType.UByte4N, DeclarationMethod.Default, DeclarationUsage.TextureCoordinate, 5),
                    new VertexElement(1, sizeof(byte) * 20, DeclarationType.UByte4N, DeclarationMethod.Default, DeclarationUsage.TextureCoordinate, 6),
                    new VertexElement(1, sizeof(byte) * 24, DeclarationType.UByte4N, DeclarationMethod.Default, DeclarationUsage.TextureCoordinate, 7),
                    new VertexElement(1, sizeof(byte) * 28, DeclarationType.UByte4N, DeclarationMethod.Default, DeclarationUsage.TextureCoordinate, 8),
                    new VertexElement(1, sizeof(byte) * 32, DeclarationType.Float4, DeclarationMethod.Default, DeclarationUsage.TextureCoordinate, 9),
                    new VertexElement(1, sizeof(byte) * 32 + sizeof(float) * 4, DeclarationType.Float4, DeclarationMethod.Default, DeclarationUsage.TextureCoordinate, 10),
                };

                Stride = Marshal.SizeOf(typeof(MyCubeInstanceData));
                Debug.Assert(Stride == 64, "Instance data stride has unexpected size");
            }
            else
            {
                VertexElements = new List<VertexElement>()
                {
                    new VertexElement(1, (short)(sizeof(HalfVector4) * 0), DeclarationType.HalfFour, DeclarationMethod.Default, DeclarationUsage.TextureCoordinate, 1),
                    new VertexElement(1, (short)(sizeof(HalfVector4) * 1), DeclarationType.HalfFour, DeclarationMethod.Default, DeclarationUsage.TextureCoordinate, 2),
                    new VertexElement(1, (short)(sizeof(HalfVector4) * 2), DeclarationType.HalfFour, DeclarationMethod.Default, DeclarationUsage.TextureCoordinate, 3),
                    new VertexElement(1, (short)(sizeof(HalfVector4) * 3), DeclarationType.HalfFour, DeclarationMethod.Default, DeclarationUsage.TextureCoordinate, 4),
                    new VertexElement(1, (short)(sizeof(HalfVector4) * 4), DeclarationType.HalfTwo, DeclarationMethod.Default, DeclarationUsage.TextureCoordinate, 5),
                };

                Stride = Marshal.SizeOf(typeof(MyInstanceData));
                Debug.Assert(Stride == 40, "Instance data stride has unexpected size");
            }
            
        }

        public VertexBuffer InstanceBuffer
        {
            get { return m_instanceBuffer; }
        }

        /// <summary>
        /// Updates instances in instance buffer
        /// </summary>
        /// <param name="capacity">Only used when buffer in not large enough and will be resized.</param>
        public unsafe void UpdateCube(List<MyCubeInstanceData> instanceData, int capacity = 0)
        {
            Debug.Assert(Type == MyRenderInstanceBufferType.Cube);

            // Preallocate array and instance buffer (when loaded)
            if (m_cubeInstances == null || m_cubeInstances.Length < instanceData.Count)
            {
                int newSize = Math.Max(instanceData.Count, capacity);
                m_cubeInstances = new MyCubeInstanceData[newSize];
                if (m_instanceBuffer != null)
                {
                    m_instanceBuffer.Dispose();
                }
                m_instanceBuffer = new VertexBuffer(MyRender.GraphicsDevice, Stride * newSize, Usage.Dynamic | Usage.WriteOnly, VertexFormat.None, Pool.Default);
            }

            // Copy locally and to instance buffer (when loaded)
            instanceData.CopyTo(m_cubeInstances);
            if (m_instanceBuffer != null)
            {
                using (MyRenderStats.Generic.Measure("render instance buffer rebuild", MyStatTypeEnum.CurrentValue))
                {
                    m_instanceBuffer.SetData(m_cubeInstances, LockFlags.Discard, instanceData.Count); // Copy only used instances
                }
            }
        }

        public unsafe void Update(List<MyInstanceData> instanceData, int capacity = 0)
        {
            Debug.Assert(Type == MyRenderInstanceBufferType.Generic);

            // Preallocate array and instance buffer (when loaded)
            if (m_instances == null || m_instances.Length < instanceData.Count)
            {
                int newSize = Math.Max(instanceData.Count, capacity);
                m_instances = new MyInstanceData[newSize];
                if (m_instanceBuffer != null)
                {
                    m_instanceBuffer.Dispose();
                }
                m_instanceBuffer = new VertexBuffer(MyRender.GraphicsDevice, Stride * newSize, Usage.Dynamic | Usage.WriteOnly, VertexFormat.None, Pool.Default);
            }

            // Copy locally and to instance buffer (when loaded)
            instanceData.CopyTo(m_instances);
            if (m_instanceBuffer != null)
            {
                using (MyRenderStats.Generic.Measure("render instance buffer rebuild", MyStatTypeEnum.CurrentValue))
                {
                    m_instanceBuffer.SetData(m_instances, LockFlags.Discard, instanceData.Count); // Copy only used instances
                }
            }
        }

        public override void LoadContent()
        {
            if (m_cubeInstances != null)
            {
                // Copy whole array (even unused instances, it's not problem when called only on DeviceLost)
                m_instanceBuffer = new VertexBuffer(MyRender.GraphicsDevice, Stride * m_cubeInstances.Length, Usage.Dynamic | Usage.WriteOnly, VertexFormat.None, Pool.Default);
                m_instanceBuffer.SetData(m_cubeInstances, LockFlags.Discard);
            }
            else if(m_instances != null)
            {
                m_instanceBuffer = new VertexBuffer(MyRender.GraphicsDevice, Stride * m_instances.Length, Usage.Dynamic | Usage.WriteOnly, VertexFormat.None, Pool.Default);
                m_instanceBuffer.SetData(m_instances, LockFlags.Discard);
            }
        }

        public override void UnloadContent()
        {
            if (m_instanceBuffer != null)
            {
                m_instanceBuffer.Dispose();
                m_instanceBuffer = null;
            }
        }

        public override void GetRenderElements(MyLodTypeEnum lodTypeEnum, List<MyRender.MyRenderElement> elements, List<MyRender.MyRenderElement> transparentElements)
        {
        }

        public override void GetRenderElementsForShadowmap(MyLodTypeEnum lodTypeEnum, List<MyRender.MyRenderElement> renderElements, List<MyRender.MyRenderElement> transparentRenderElements)
        {
        }

        public override void DebugDraw()
        {
        }
    }
}
