using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using VRage.Generics;
using VRage.Utils;
using VRageRender.Resources;
using Matrix = VRageMath.Matrix;

namespace VRageRender
{
    // stores preallocated data
    class MyPassLocals
    {
        internal MyMaterialProxyId matTexturesID;
        internal int matConstantsID;

        internal void Clear()
        {
            matTexturesID = MyMaterialProxyId.NULL;
            matConstantsID = -1;
        }
    }

    struct MyPassStats
    {
        internal int Meshes;
        internal int Submeshes;
        internal int Billboards;
        internal int Instances;
        internal int Triangles;
        internal int ObjectConstantsChanges;
        internal int MaterialConstantsChanges;

        internal void Clear()
        {
            Meshes = 0;
            Submeshes = 0;
            Billboards = 0;
            ObjectConstantsChanges = 0;
            MaterialConstantsChanges = 0;
            Instances = 0;
            Triangles = 0;
        }

        internal void Gather(MyPassStats other)
        {
            Meshes += other.Meshes;
            Submeshes += other.Submeshes;
            Billboards += other.Billboards;
            Instances += other.Instances;
            Triangles += other.Triangles;
            ObjectConstantsChanges += other.ObjectConstantsChanges;
            MaterialConstantsChanges += other.MaterialConstantsChanges;
        }
    }

    struct MyMergeInstancingConstants
    {
        public static readonly MyMergeInstancingConstants Default;

        static MyMergeInstancingConstants()
        {
            Default = new MyMergeInstancingConstants() { InstanceIndex = -1 };
        }

        public int InstanceIndex;
        public int StartIndex;
    }

    abstract class MyRenderingPass
    {
        #region Local
        internal MyRenderContext m_RC;
        internal MyPassLocals Locals;
        internal MyPassStats Stats;
        internal bool m_joined;

        int m_currentProfilingBlock_renderableType = -1;
        string m_currentProfilingBlock_renderableMaterial = string.Empty;
        #endregion

        #region Shared
        bool m_isImmediate;
        internal Matrix ViewProjection;
        internal MyViewport Viewport;
        internal string DebugName;
        #endregion

        internal DeviceContext Context { get { return RC.DeviceContext; } }
        internal int ProcessingMask { get; set; }

        internal MyRenderContext RC
        {
            get
            {
                Debug.Assert(!m_isImmediate || (MyRenderProxy.RenderThread.SystemThread == Thread.CurrentThread));
                return m_isImmediate ? MyImmediateRC.RC : m_RC;
            }
        }

        internal void SetImmediate(bool isImmediate)
        {
            m_isImmediate = isImmediate;
        }

        internal void SetContext(MyRenderContext rc)
        {
            Debug.Assert(!m_isImmediate && m_RC == null);
            m_RC = rc;
        }

        internal virtual void Cleanup()
        {
            m_RC = null;
            if(Locals != null)
                Locals.Clear();
            Stats.Clear();
            m_joined = false;

            m_currentProfilingBlock_renderableType = -1;
            m_currentProfilingBlock_renderableMaterial = string.Empty;

            m_isImmediate = false;
            ViewProjection = default(Matrix);
            Viewport = default(MyViewport);
            DebugName = string.Empty;
            ProcessingMask = 0;
        }

        internal virtual void PerFrame()
        {
        }

        internal virtual void Begin()
        {
            MyUtils.Init(ref Locals);
            Locals.Clear();

            //if (!m_isImmediate)
            //{
            //    //Debug.Assert(m_RC == null);
            //    //m_RC = MyRenderContextPool.AcquireRC();
            //}

            var viewProjTranspose = Matrix.Transpose(ViewProjection);
            var mapping = MyMapping.MapDiscard(RC.DeviceContext, MyCommon.ProjectionConstants);
            mapping.WriteAndPosition(ref viewProjTranspose);
            mapping.Unmap();

            // common settings
            Context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            Context.Rasterizer.SetViewport(Viewport.OffsetX, Viewport.OffsetY, Viewport.Width, Viewport.Height);

            Context.PixelShader.SetSamplers(0, SamplerStates.StandardSamplers);

            RC.SetCB(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);
            RC.SetCB(MyCommon.PROJECTION_SLOT, MyCommon.ProjectionConstants);
            RC.SetCB(MyCommon.ALPHAMASK_VIEWS_SLOT, MyCommon.AlphamaskViewsConstants);

            Context.PixelShader.SetShaderResource(MyCommon.DITHER_8X8_SLOT, MyTextures.Views[MyTextures.Dithering8x8TexId.Index]);

            if (MyBigMeshTable.Table.m_IB != StructuredBufferId.NULL)
            {
                var slotcounter = MyCommon.BIG_TABLE_INDICES;
                RC.VSBindRawSRV(slotcounter++, MyBigMeshTable.Table.m_IB);
                RC.VSBindRawSRV(slotcounter++, MyBigMeshTable.Table.m_VB_positions);
                RC.VSBindRawSRV(slotcounter++, MyBigMeshTable.Table.m_VB_rest);
            }
        }

        public static string ToString(MyMaterialType materialType)
        {
            switch(materialType)
            {
                case MyMaterialType.ALPHA_MASKED:
                    return "ALPHA_MASKED";
                case MyMaterialType.FORWARD:
                    return "FORWARD";
                case MyMaterialType.OPAQUE:
                    return "OPAQUE";
                case MyMaterialType.TRANSPARENT:
                    return "TRANSPARENT";
                default:
                    return "ERROR";
            }
        }

        [Conditional(VRage.ProfilerShort.PerformanceProfilingSymbol)]
        internal void FeedProfiler(ulong nextSortingKey)
        {
            int type = (int)MyRenderableComponent.ExtractTypeFromSortingKey(nextSortingKey);
            var material = MyRenderableComponent.ExtractMaterialNameFromSortingKey(nextSortingKey);

            if (type != m_currentProfilingBlock_renderableType)
            {
                if (m_currentProfilingBlock_renderableType != -1)
                {
                    // close material
                    RC.EndProfilingBlock();
                    // close type
                    RC.EndProfilingBlock();
                }

                // start type
                RC.BeginProfilingBlock(ToString((MyMaterialType)type));
                // start material
                RC.BeginProfilingBlock(material);

                m_currentProfilingBlock_renderableType = type;
                m_currentProfilingBlock_renderableMaterial = material;
            }

            if (material != m_currentProfilingBlock_renderableMaterial)
            {
                if (m_currentProfilingBlock_renderableMaterial != "")
                {
                    // close material
                    RC.EndProfilingBlock();
                }
                // start material
                RC.BeginProfilingBlock(material);

                m_currentProfilingBlock_renderableMaterial = material;
            }
        }

        internal void RecordCommands(MyRenderableProxy proxy)
        {
            RecordCommandsInternal(proxy);
        }

        protected virtual void RecordCommandsInternal(MyRenderableProxy proxy)
        {
        }

        internal void RecordCommands(ref MyRenderableProxy_2 proxy, int instance = -1, int section = -1)
        {
            RecordCommandsInternal(ref proxy, instance, section);
        }

        protected virtual void RecordCommandsInternal(ref MyRenderableProxy_2 proxy, int instance, int section) { }

        internal virtual void End()
        {
            if (VRage.MyCompilationSymbols.PerformanceProfiling)
            {
                if (m_currentProfilingBlock_renderableType != -1)
                {
                    // close material
                    RC.EndProfilingBlock();
                    // close type
                    RC.EndProfilingBlock();
                }
            }
        }

        protected unsafe void SetProxyConstants(ref MyRenderableProxy_2 proxy, MyMergeInstancingConstants? arg = null)
        {
            MyMergeInstancingConstants constants = arg ?? MyMergeInstancingConstants.Default;

            int version = constants.GetHashCode();
            if (constants.GetHashCode() != proxy.ObjectConstants.Version)
            {
                int size = sizeof(MyMergeInstancingConstants);
                var buffer = new byte[sizeof(MyMergeInstancingConstants)];

                proxy.ObjectConstants = new MyConstantsPack()
                {
                    BindFlag = MyBindFlag.BIND_VS,
                    CB = MyCommon.GetObjectCB(size),
                    Version = version,
                    Data = buffer
                };


                fixed (byte* dstPtr = buffer)
                {
#if XB1
                    SharpDX.Utilities.CopyMemory(new IntPtr(dstPtr), new IntPtr(&constants), size);
#else // !XB1
                    MyMemory.CopyMemory(new IntPtr(dstPtr), new IntPtr(&constants), (uint)size);
#endif // !XB1
                }
            }

            RC.MoveConstants(ref proxy.ObjectConstants);
            RC.SetConstants(ref proxy.ObjectConstants, MyCommon.OBJECT_SLOT);

            ++Stats.ObjectConstantsChanges;
        }

        protected void SetProxyConstants(MyRenderableProxy proxy)
        {
            RC.SetCB(MyCommon.OBJECT_SLOT, proxy.ObjectBuffer);

            FillBuffers(proxy, RC.DeviceContext);

            ++Stats.ObjectConstantsChanges;
        }

        internal static unsafe void FillBuffers(MyRenderableProxy proxy, DeviceContext deviceContext)
        {
            MyMapping mapping;
            mapping = MyMapping.MapDiscard(deviceContext, proxy.ObjectBuffer);
            if(proxy.NonVoxelObjectData.IsValid)
            {
                mapping.WriteAndPosition(ref proxy.NonVoxelObjectData);
            }
            else if (proxy.VoxelCommonObjectData.IsValid)
            {
                mapping.WriteAndPosition(ref proxy.VoxelCommonObjectData);
            }
            mapping.WriteAndPosition(ref proxy.CommonObjectData);

            if (proxy.SkinningMatrices != null)
            {
                if (proxy.DrawSubmesh.BonesMapping == null)
                {
                    mapping.WriteAndPosition(proxy.SkinningMatrices, 0, Math.Min(MyRender11Constants.SHADER_MAX_BONES, proxy.SkinningMatrices.Length));
                }
                else
                {
                    for (int j = 0; j < proxy.DrawSubmesh.BonesMapping.Length; j++)
                    {
                        mapping.WriteAndPosition(ref proxy.SkinningMatrices[proxy.DrawSubmesh.BonesMapping[j]]);
                    }
                }
            }
            mapping.Unmap();
        }

        internal static void BindProxyGeometry(MyRenderableProxy proxy, MyRenderContext renderContext)
        {
            MyMeshBuffers buffers;
            
            if(proxy.Mesh != LodMeshId.NULL)
                buffers = proxy.Mesh.Buffers;
            else
                buffers = proxy.MergedMesh.Buffers;

            bool firstChanged = renderContext.UpdateVB(0, buffers.VB0.Buffer, buffers.VB0.Stride);
            bool secondChanged = renderContext.UpdateVB(1, buffers.VB1.Buffer, buffers.VB1.Stride);
            
            if (firstChanged && secondChanged)
            {
                renderContext.DeviceContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(buffers.VB0.Buffer, buffers.VB0.Stride, 0));
                renderContext.DeviceContext.InputAssembler.SetVertexBuffers(1, new VertexBufferBinding(buffers.VB1.Buffer, buffers.VB1.Stride, 0));
                renderContext.Stats.SetVB++;
            }
            else if (firstChanged)
            {
                renderContext.DeviceContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(buffers.VB0.Buffer, buffers.VB0.Stride, 0));
                renderContext.Stats.SetVB++;
            }
            else if (secondChanged)
            {
                renderContext.DeviceContext.InputAssembler.SetVertexBuffers(1, new VertexBufferBinding(buffers.VB1.Buffer, buffers.VB1.Stride, 0));
                renderContext.Stats.SetVB++;
            }
            
            if (proxy.InstancingEnabled && proxy.Instancing.VB.Index != -1)
            {
                renderContext.DeviceContext.InputAssembler.SetVertexBuffers(2, new VertexBufferBinding(proxy.Instancing.VB.Buffer, proxy.Instancing.VB.Stride, 0));
                renderContext.Stats.SetVB++;
            }
            renderContext.SetIB(buffers.IB.Buffer, buffers.IB.Format);
        }

        internal virtual MyRenderingPass Fork()
        {
            var renderPass = (MyRenderingPass)MyObjectPoolManager.Allocate(this.GetType());

            renderPass.m_RC = m_RC;
            if(renderPass.Locals != null)
                renderPass.Locals.Clear();
            renderPass.Stats = default(MyPassStats);
            renderPass.m_joined = m_joined;
            renderPass.m_currentProfilingBlock_renderableType = -1;
            renderPass.m_currentProfilingBlock_renderableMaterial = string.Empty;
            renderPass.m_isImmediate = m_isImmediate;
            renderPass.ViewProjection = ViewProjection;
            renderPass.Viewport = Viewport;
            renderPass.DebugName = DebugName;
            renderPass.ProcessingMask = ProcessingMask;

            return renderPass;
        }

        internal void Join()
        {
            Debug.Assert(!m_joined);
            m_joined = true;

            MyRender11.GatherStats(Stats);
        }
    }
}
