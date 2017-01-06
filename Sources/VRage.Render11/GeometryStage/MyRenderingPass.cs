using SharpDX.Direct3D;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using VRage.Profiler;
using VRage.Render11.Common;
using VRage.Render11.RenderContext;
using VRage.Render11.Resources;
using VRage.Utils;
using VRageRender.Import;
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
        internal int Draws;
        internal int Billboards;
        internal int Instances;
        internal int Triangles;
        internal int ObjectConstantsChanges;
        internal int MaterialConstantsChanges;

        internal void Clear()
        {
            Draws = 0;
            Billboards = 0;
            ObjectConstantsChanges = 0;
            MaterialConstantsChanges = 0;
            Instances = 0;
            Triangles = 0;
        }

        internal void Gather(MyPassStats other)
        {
            Draws += other.Draws;
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
        private MyRenderContext m_rc;
        internal MyPassLocals Locals;
        internal MyPassStats Stats = new MyPassStats();
        internal bool m_joined;

        internal long Elapsed;

        int m_currentProfilingBlock_renderableType = -1;
        string m_currentProfilingBlock_renderableMaterial = string.Empty;
        #endregion

        #region Shared
        bool m_isImmediate;
        internal Matrix ViewProjection;
        internal MyViewport Viewport;
        internal string DebugName;
        internal int FrustumIndex;
        #endregion

        protected virtual MyFrustumEnum FrustumType
        {
            get { return MyFrustumEnum.Unassigned; }
        }

        internal int ProcessingMask { get; set; }

        internal MyRenderContext RC
        {
            get
            {
                Debug.Assert(!m_isImmediate || (MyRenderProxy.RenderThread.SystemThread == Thread.CurrentThread));
                return m_isImmediate ? MyImmediateRC.RC : m_rc;
            }
        }

        internal void SetImmediate(bool isImmediate)
        {
            m_isImmediate = isImmediate;
        }

        internal void SetContext(MyRenderContext rc)
        {
            Debug.Assert(!m_isImmediate && m_rc == null);
            m_rc = rc;
        }

        internal virtual void Cleanup()
        {
            m_rc = null;
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
            FrustumIndex = 0;
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
            var mapping = MyMapping.MapDiscard(RC, MyCommon.ProjectionConstants);
            mapping.WriteAndPosition(ref viewProjTranspose);
            mapping.Unmap();

            // common settings
            RC.SetPrimitiveTopology(PrimitiveTopology.TriangleList);
            RC.SetViewport(Viewport.OffsetX, Viewport.OffsetY, Viewport.Width, Viewport.Height);

            RC.PixelShader.SetSamplers(0, MySamplerStateManager.StandardSamplers);

            RC.AllShaderStages.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);
            RC.AllShaderStages.SetConstantBuffer(MyCommon.PROJECTION_SLOT, MyCommon.ProjectionConstants);
            RC.AllShaderStages.SetConstantBuffer(MyCommon.VOXELS_MATERIALS_LUT_SLOT, MyCommon.VoxelMaterialsConstants.Cb);
            RC.AllShaderStages.SetConstantBuffer(MyCommon.ALPHAMASK_VIEWS_SLOT, MyCommon.AlphamaskViewsConstants);

            RC.PixelShader.SetSrv(MyCommon.DITHER_8X8_SLOT, MyGeneratedTextureManager.Dithering8x8Tex);

            if (MyBigMeshTable.Table.m_IB != null)
            {
                var slotcounter = MyCommon.BIG_TABLE_INDICES;
                RC.VertexShader.SetSrv(slotcounter++, MyBigMeshTable.Table.m_IB);
                RC.VertexShader.SetSrv(slotcounter++, MyBigMeshTable.Table.m_VB_positions);
                RC.VertexShader.SetSrv(slotcounter++, MyBigMeshTable.Table.m_VB_rest);
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

        [Conditional(ProfilerShort.PerformanceProfilingSymbol)]
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
            bool draw = true;
            FilterRenderable(proxy, ref draw);
            if (!draw)
                return;

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

            MyRenderUtils.MoveConstants(RC, ref proxy.ObjectConstants);
            MyRenderUtils.SetConstants(RC, ref proxy.ObjectConstants, MyCommon.OBJECT_SLOT);

            ++Stats.ObjectConstantsChanges;
        }

        protected void SetProxyConstants(MyRenderableProxy proxy)
        {
            RC.AllShaderStages.SetConstantBuffer(MyCommon.OBJECT_SLOT, proxy.ObjectBuffer);

            FillBuffers(proxy, RC);

            ++Stats.ObjectConstantsChanges;
        }

        internal static unsafe void FillBuffers(MyRenderableProxy proxy, MyRenderContext rc)
        {
            MyMapping mapping;
            mapping = MyMapping.MapDiscard(rc, proxy.ObjectBuffer);
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
                    mapping.WriteAndPosition(proxy.SkinningMatrices, Math.Min(MyRender11Constants.SHADER_MAX_BONES, proxy.SkinningMatrices.Length));
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

        internal static void BindProxyGeometry(MyRenderableProxy proxy, MyRenderContext rc)
        {
            MyMeshBuffers buffers = proxy.Mesh.Buffers;

            rc.SetVertexBuffer(0, buffers.VB0);
            rc.SetVertexBuffer(1, buffers.VB1);
            
            if (proxy.InstancingEnabled && proxy.Instancing.VB != null)
            {
                rc.SetVertexBuffer(2, proxy.Instancing.VB);

            }
            rc.SetIndexBuffer(buffers.IB);
        }

        [Conditional("DEBUG")]
        void FilterRenderable(MyRenderableProxy proxy, ref bool draw)
        {
            if (proxy.Material == MyMeshMaterialId.NULL)
            {
                if (proxy.VoxelCommonObjectData.IsValid)
                    draw &= MyRender11.Settings.DrawVoxels;

                return;
            }

            switch (proxy.Material.Info.Technique)
            {
                case MyMeshDrawTechnique.MESH:
                {
                    if (proxy.InstanceCount == 0)
                        draw &= MyRender11.Settings.DrawMeshes;
                    else
                        draw &= MyRender11.Settings.DrawInstancedMeshes;
                    break;
                }
                case MyMeshDrawTechnique.ALPHA_MASKED:
                {
                    draw &= MyRender11.Settings.DrawAlphamasked;
                    if (proxy.Material.Info.Facing == MyFacingEnum.Impostor)
                        draw &= MyRender11.Settings.DrawImpostors;
                    break;
                }
            }
        }

        internal virtual MyRenderingPass Fork()
        {
            var renderPass = (MyRenderingPass)MyObjectPoolManager.Allocate(this.GetType());

            renderPass.m_rc = m_rc;
            if(renderPass.Locals != null)
                renderPass.Locals.Clear();
            renderPass.Stats = default(MyPassStats);
            renderPass.m_joined = m_joined;
            renderPass.m_currentProfilingBlock_renderableType = -1;
            renderPass.m_currentProfilingBlock_renderableMaterial = string.Empty;
            renderPass.m_isImmediate = m_isImmediate;
            renderPass.ViewProjection = ViewProjection;
            renderPass.Viewport = Viewport;
            renderPass.FrustumIndex = FrustumIndex;
            renderPass.DebugName = DebugName;
            renderPass.ProcessingMask = ProcessingMask;

            return renderPass;
        }

        internal void Join()
        {
            Debug.Assert(!m_joined);
            m_joined = true;
            int passHash = ((int)FrustumType) << 10 | FrustumIndex;
            MyRender11.GatherPassStats(passHash, DebugName, Stats);
        }
    }
}
