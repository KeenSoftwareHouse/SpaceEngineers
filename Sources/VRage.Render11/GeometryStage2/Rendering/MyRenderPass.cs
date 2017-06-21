using System.Collections.Generic;
using System.Runtime.InteropServices;
using ParallelTasks;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using VRage.Profiler;
using VRage.Render11.Common;
using VRage.Render11.GeometryStage2.Common;
using VRage.Render11.GeometryStage2.Instancing;
using VRage.Render11.GeometryStage2.Model;
using VRage.Render11.RenderContext;
using VRage.Render11.Resources;
using VRageMath.PackedVector;
using VRageRender;
using Matrix = VRageMath.Matrix;
using Vector3 = VRageMath.Vector3;
using Vector4 = VRageMath.Vector4;

namespace VRage.Render11.GeometryStage2.Rendering
{
    abstract class MyRenderPass : IPrioritizedWork
    {
        MyRenderContext m_RC;

        string m_debugName;
        int m_frustumIndex;
        MyFrustumEnum m_frustumType;
        protected MyPassStats m_stats;
        protected IGeometrySrvStrategy SrvStrategy { get; private set; }

        public int PassId { get; private set; }

        protected abstract void Draw(MyRenderContext RC, List<MyInstanceComponent> visibleInstances);
        
        List<MyInstanceComponent> m_visibleInstancesForDoWork;

        protected void Init(int passId, string debugName, MyFrustumEnum frustumType, int frustumIndex)
        {
            PassId = passId;
            m_debugName = debugName + "N";
            m_frustumType = frustumType;
            m_frustumIndex = frustumIndex;
            m_stats = new MyPassStats();
        }

        // complex implemtation because of the IPrioritizedWork, the simple way is to call Draw directly...
        public void InitWork(List<MyInstanceComponent> visibleInstances, IGeometrySrvStrategy srvStrategy)
        {
            MyRenderProxy.Assert(m_visibleInstancesForDoWork == null, "It is needed to call DoWork() after InitWork()");
            m_visibleInstancesForDoWork = visibleInstances;
            m_RC = MyManagers.DeferredRCs.AcquireRC();
            m_stats.Clear();
            SrvStrategy = srvStrategy;
        }

        // complex implemtation because of the IPrioritizedWork
        public void DoWork(WorkData workData = null)
        {
            MyRenderProxy.Assert(m_visibleInstancesForDoWork != null, "It is needed to call InitWork() before DoWork()");
            Draw(m_RC, m_visibleInstancesForDoWork);
            m_visibleInstancesForDoWork = null;
        }

        public void PostprocessWork()
        {
            CommandList commandList = m_RC.FinishCommandList(false);

            Profiler.MyGpuProfiler.IC_BeginBlock(m_debugName + m_frustumIndex);
            MyRender11.RC.ExecuteCommandList(commandList, false);
            Profiler.MyGpuProfiler.IC_EndBlock();

            commandList.Dispose();
            MyManagers.DeferredRCs.FreeRC(m_RC);
            
            int passHash = ((int)m_frustumType) << 10 | m_frustumIndex;
            MyRender11.GatherPassStats(passHash, m_debugName, m_stats);
        }

        public WorkPriority Priority
        {
            get { return WorkPriority.VeryHigh; }
        }

        public WorkOptions Options
        {
            get { return Parallel.DefaultOptions; }
        }

        // the method returns the same constant buffer as the parameter
        protected void FillConstantBuffer<T>(MyRenderContext RC, IConstantBuffer cb, T data) where T : struct
        {
            var mapping = MyMapping.MapDiscard(RC, MyCommon.ProjectionConstants);
            mapping.WriteAndPosition(ref data);
            mapping.Unmap();
        }

        protected static unsafe IConstantBuffer GetPlaceholderObjectCB(MyRenderContext RC, uint lod)
        {
            int cbSize = sizeof(MyObjectDataCommon);
            cbSize += sizeof(MyObjectDataNonVoxel);

            IConstantBuffer cb = MyCommon.GetObjectCB(cbSize);
            var mapping = MyMapping.MapDiscard(RC, cb);
            MyObjectDataNonVoxel nonVoxelData = new MyObjectDataNonVoxel();
            mapping.WriteAndPosition(ref nonVoxelData);
            MyObjectDataCommon commonData = new MyObjectDataCommon();
            commonData.LocalMatrix = Matrix.Identity;
            commonData.ColorMul = Vector3.One;
            commonData.KeyColor = new Vector3(0, 0f, 0f);
            commonData.LOD = lod;
            mapping.WriteAndPosition(ref commonData);
            mapping.Unmap();
            return cb;
        }
    }

    struct MyInstancesCounters
    {
        int[] m_instancesCounters;
        int m_instancesCountersCount;
        int m_maxLodId;

        public int GetDirectOffset(int lodId, MyInstanceLodState state)
        {
            int stateId = (int)state;
            MyRenderProxy.Assert(lodId <= (int)m_maxLodId);
            MyRenderProxy.Assert(stateId < (int)MyInstanceLodState.StatesCount);
            int stride = m_maxLodId + 1;
            return stride * stateId + lodId;
        }

        public void PrepareByZeroes(int maxLodId)
        {
            m_maxLodId = maxLodId;

            m_instancesCountersCount = (m_maxLodId + 1) * ((int)MyInstanceLodState.StatesCount);

            if (m_instancesCounters == null || m_instancesCounters.Length < m_instancesCountersCount)
                m_instancesCounters = new int[m_instancesCountersCount];

            for (int i = 0; i < m_instancesCountersCount; i++)
                m_instancesCounters[i] = 0;
        }

        public void PrepareByNegativeOnes(int maxLodId)
        {
            m_maxLodId = maxLodId;

            m_instancesCountersCount = (m_maxLodId + 1) * ((int)MyInstanceLodState.StatesCount);

            if (m_instancesCounters == null || m_instancesCounters.Length < m_instancesCountersCount)
                m_instancesCounters = new int[m_instancesCountersCount];

            for (int i = 0; i < m_instancesCountersCount; i++)
                m_instancesCounters[i] = -1;
        }

        public bool IsEmpty(int lodId, MyInstanceLodState state)
        {
            int offset = GetDirectOffset(lodId, state);
            return m_instancesCounters[offset] == 0;
        }

        public int At(int lodId, MyInstanceLodState state)
        {
            int offset = GetDirectOffset(lodId, state);
            return m_instancesCounters[offset];            
        }

        public void SetAt(int lodId, MyInstanceLodState state, int value)
        {
            int offset = GetDirectOffset(lodId, state);
            m_instancesCounters[offset] = value;
        }
        
        public void Add(int lodId, MyInstanceLodState state, int value)
        {
            int offset = GetDirectOffset(lodId, state);
            m_instancesCounters[offset] += value;
        }

        public int AtByDirectOffset(int directOffset)
        {
            MyRenderProxy.Assert(directOffset < m_instancesCountersCount);
            return m_instancesCounters[directOffset];
        }

        public int GetDirectOffsetsCount()
        {
            return m_instancesCountersCount;
        }

        public void GetDetailsFromDirectOffset(int directOffset, out int lodId, out MyInstanceLodState state)
        {
            MyRenderProxy.Assert(directOffset < m_instancesCountersCount);
            lodId = directOffset % (m_maxLodId+1);
            state = (MyInstanceLodState) (directOffset / (m_maxLodId+1));
        }
    }

    interface IDrawableGroupStrategy
    {
        void Init(int elementsCount);

        void Fill(int bufferOffset, MyInstanceComponent instance, MyLod lod, int multiTransformI, int instanceMaterialOffsetInData, MyInstanceLodState state, float stateData);
    }

    class MyDrawableGroupGBufferStrategy : IDrawableGroupStrategy
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct MyVbConstantElement
        {
            public Vector4 WorldMatrixRow0;
            public Vector4 WorldMatrixRow1;
            public Vector4 WorldMatrixRow2;
            public HalfVector4 KeyColorDithering;
            public HalfVector4 ColorMultEmissivity;
        };

        IVertexBuffer m_vbInstances;
        MyVbConstantElement[] m_vbData;
        int m_validElements;

        // input vb can be removed, always after update use GetResultVB
        public void Prepare(IVertexBuffer vbInstances)
        {
            m_vbInstances = vbInstances;
        }

        unsafe void IDrawableGroupStrategy.Init(int elementsCount)
        {
            if (elementsCount != 0)
            {
                if (m_vbInstances == null || m_vbInstances.ElementCount < elementsCount)
                {
                    if (m_vbInstances != null)
                        MyManagers.Buffers.Dispose(m_vbInstances);
                    m_vbInstances = MyManagers.Buffers.CreateVertexBuffer("MyDrawableGroupGBufferStrategy.VbInstances",
                        elementsCount, sizeof(MyVbConstantElement), usage: ResourceUsage.Dynamic);
                }
                if (m_vbData == null || m_vbData.Length < elementsCount)
                    m_vbData = new MyVbConstantElement[elementsCount];
            }

            m_validElements = 0;
        }

        void IDrawableGroupStrategy.Fill(int bufferOffset, MyInstanceComponent instance, MyLod lod, int multiTransformI, int instanceMaterialOffsetInData, MyInstanceLodState state, float stateData)
        {
            HalfVector4 packedColorMultEmissivity = MyInstanceMaterial.Default.PackedColorMultEmissivity;
            if (instanceMaterialOffsetInData != -1) // if instance material is defined
            {
                MyInstanceMaterial instanceMaterial = instance.GetInstanceMaterial(instanceMaterialOffsetInData);
                packedColorMultEmissivity = instanceMaterial.PackedColorMultEmissivity;
            }
            else
                packedColorMultEmissivity = instance.GlobalColorMultEmissivity;

            HalfVector4 packedKeyColorDithering = instance.KeyColor.ToHalfVector4();
            HalfVector4 dithering = new HalfVector4();
            dithering.PackedValue = (ulong)HalfUtils.Pack(stateData);
            packedKeyColorDithering.PackedValue = packedKeyColorDithering.PackedValue | dithering.PackedValue << 48;

            MyVbConstantElement element = new MyVbConstantElement
            {
                //WorldMatrixRow0 = new Vector4(matrix.M11, matrix.M12, matrix.M13, matrix.M14),
                //WorldMatrixRow1 = new Vector4(matrix.M21, matrix.M22, matrix.M23, matrix.M24),
                //WorldMatrixRow2 = new Vector4(matrix.M31, matrix.M32, matrix.M33, matrix.M34),
                KeyColorDithering = packedKeyColorDithering,
                ColorMultEmissivity = packedColorMultEmissivity,
            };
            // much faster approach than naive:
            instance.GetMatrixCols(multiTransformI, 
                out element.WorldMatrixRow0,
                out element.WorldMatrixRow1,
                out element.WorldMatrixRow2); 

            m_vbData[bufferOffset] = element;

            m_validElements++;
        }

        // returned value - is there anything to draw?
        public bool Finalize(MyRenderContext RC, out IVertexBuffer vbInstances)
        {
            vbInstances = m_vbInstances;
            if (m_validElements == 0)
                return false;

            MyMapping mappingVb = MyMapping.MapDiscard(RC, vbInstances);
            mappingVb.WriteAndPosition(m_vbData, m_validElements);
            mappingVb.Unmap();
            return true;
        }
    }

    class MyDrawableGroupDepthStrategy : IDrawableGroupStrategy
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct MyVbConstantElement
        {
            public Vector4 WorldMatrixRow0;
            public Vector4 WorldMatrixRow1;
            public Vector4 WorldMatrixRow2;
        };

        IVertexBuffer m_vbInstances;
        MyVbConstantElement[] m_vbData;
        int m_validElements;

        // input vb can be removed, always after update use GetResultVB
        public void Prepare(IVertexBuffer vbInstances)
        {
            m_vbInstances = vbInstances;
        }

        unsafe void IDrawableGroupStrategy.Init(int elementsCount)
        {
            if (elementsCount != 0)
            {
                if (m_vbInstances == null || m_vbInstances.ElementCount < elementsCount)
                {
                    if (m_vbInstances != null)
                        MyManagers.Buffers.Dispose(m_vbInstances);
                    m_vbInstances = MyManagers.Buffers.CreateVertexBuffer("MyDrawableGroupDepthStrategy.VbInstances",
                        elementsCount, sizeof(MyVbConstantElement), usage: ResourceUsage.Dynamic);
                }
                if (m_vbData == null || m_vbData.Length < elementsCount)
                    m_vbData = new MyVbConstantElement[elementsCount];
            }

            m_validElements = 0;
        }

        void IDrawableGroupStrategy.Fill(int bufferOffset, MyInstanceComponent instance, MyLod lod, int multiTransformI, int instanceMaterialOffsetInData, MyInstanceLodState state, float stateData)
        {
            MyVbConstantElement element = new MyVbConstantElement
            {
                //WorldMatrixRow0 = new Vector4(matrix.M11, matrix.M12, matrix.M13, matrix.M14),
                //WorldMatrixRow1 = new Vector4(matrix.M21, matrix.M22, matrix.M23, matrix.M24),
                //WorldMatrixRow2 = new Vector4(matrix.M31, matrix.M32, matrix.M33, matrix.M34),
            };
            //Much faster approach than naive:
            instance.GetMatrixCols(multiTransformI, 
                out element.WorldMatrixRow0,
                out element.WorldMatrixRow1,
                out element.WorldMatrixRow2);

            m_vbData[bufferOffset] = element;

            m_validElements++;
        }

        // returned value - is there anything to draw?
        public bool Finalize(MyRenderContext RC, out IVertexBuffer vbInstances)
        {
            vbInstances = m_vbInstances;
            if (m_validElements == 0)
                return false;

            MyMapping mappingVb = MyMapping.MapDiscard(RC, vbInstances);
            mappingVb.WriteAndPosition(m_vbData, m_validElements);
            mappingVb.Unmap();
            return true;
        }
    }

    struct MyRawDrawableGroup
    {
        public MyLod Lod;
        public MyInstanceLodState State;
        public int OffsetInInstanceBuffer;
        public int InstancesIncrement;
        public int InstancesCount;
        public int InstanceMaterialsCount;
    }

    class MyDrawableGroupFactory
    {
        List<MyRawDrawableGroup> m_rawDrawableGroups = new List<MyRawDrawableGroup>();
        MyInstancesCounters m_tmpInstancesCounts = new MyInstancesCounters();
        MyInstancesCounters m_tmpInstancesOffsets = new MyInstancesCounters();

        static void PrecomputeCounts(out int out_sumInstanceMaterials, out int out_drawnLodsCount, ref MyInstancesCounters out_counters, List<MyInstanceComponent> visibleInstances, int passId, int maxLodId)
        {
            out_drawnLodsCount = 0;
            out_sumInstanceMaterials = 0;
            out_counters.PrepareByZeroes(maxLodId);

            foreach (var instance in visibleInstances)
            {
                for (int i = 0; i < instance.GetLodsCount(passId); i++)
                {
                    MyLod lod;
                    MyInstanceLodState state;
                    float stateData;
                    instance.GetLod(passId, i, out lod, out state, out stateData);
                    int multiTransformCount = instance.GetMultiTransformCount();

                    out_counters.Add(lod.UniqueId, state, multiTransformCount);
                    out_sumInstanceMaterials += lod.InstanceMaterialsCount * multiTransformCount;
                    out_drawnLodsCount += multiTransformCount;
                }
            }
        }

        static void PrepareDrawableContainers(ref List<MyRawDrawableGroup> out_drawableGroups, ref MyInstancesCounters out_instancesGroupOffset,
            IDrawableGroupStrategy strategy, MyInstancesCounters precalcCounts, List<MyInstanceComponent> visibleInstances, int passId, int maxLodId)
        {
            MyRenderProxy.Assert(strategy != null);
            out_drawableGroups.Clear();
            out_instancesGroupOffset.PrepareByNegativeOnes(maxLodId);

            int groupsCount = 0;
            int instanceBufferOffset = 0;
            foreach (var instance in visibleInstances)
            {
                int lodsCount = instance.GetLodsCount(passId);
                for (int i = 0; i < lodsCount; i++)
                {
                    // get info about current instanced lod:
                    MyLod lod;
                    MyInstanceLodState state;
                    float stateData;
                    instance.GetLod(passId, i, out lod, out state, out stateData);
                    int lodId = lod.UniqueId;
                    int directOffset = out_instancesGroupOffset.GetDirectOffset(lodId, state);

                    // if the lod group is not added into the output, add it
                    if (out_instancesGroupOffset.At(lodId, state) == -1)
                    {
                        MyRenderProxy.Assert(groupsCount == out_drawableGroups.Count,
                            "The values are different, internal error");
                        out_instancesGroupOffset.SetAt(lodId, state, groupsCount);

                        int instancesCount = precalcCounts.At(lodId, state);
                        MyRawDrawableGroup rawDrawableGroup = new MyRawDrawableGroup
                        {
                            Lod = lod,
                            State = state,
                            OffsetInInstanceBuffer = instanceBufferOffset,
                            InstancesCount = instancesCount,
                            InstancesIncrement = 0,
                            InstanceMaterialsCount = lod.InstanceMaterialsCount,
                        };
                        out_drawableGroups.Add(rawDrawableGroup);

                        instanceBufferOffset += instancesCount * (1 + lod.InstanceMaterialsCount);
                        groupsCount++;
                    }

                    // Fill data:
                    int groupOffset = out_instancesGroupOffset.AtByDirectOffset(directOffset);
                    MyRawDrawableGroup group = out_drawableGroups[groupOffset];
                    int bufferOffset = group.OffsetInInstanceBuffer + group.InstancesIncrement;
                    int multiTransformCount = instance.GetMultiTransformCount();
                    for (int multiTransformI = 0; multiTransformI < multiTransformCount; multiTransformI++)
                    {
                        strategy.Fill(bufferOffset + multiTransformI, instance, lod, multiTransformI, -1, state,
                            stateData);
                    }
                    List<int> instanceMaterialDataOffsets = lod.GetInstanceMaterialDataOffsets();
                    for (int instanceMaterialOffsetInLod = 0;
                        instanceMaterialOffsetInLod < lod.InstanceMaterialsCount;
                        instanceMaterialOffsetInLod++)
                    {
                        bufferOffset = group.OffsetInInstanceBuffer + group.InstancesIncrement +
                                       group.InstancesCount * (instanceMaterialOffsetInLod + 1);
                        int instanceMaterialDataOffset = instanceMaterialDataOffsets[instanceMaterialOffsetInLod];
                        for (int multiTransformI = 0; multiTransformI < multiTransformCount; multiTransformI++)
                            strategy.Fill(bufferOffset + multiTransformI, instance, lod, multiTransformI,
                                instanceMaterialDataOffset, state, stateData);
                    }

                    // Update group data:
                    group.InstancesIncrement += multiTransformCount;
                    out_drawableGroups[groupOffset] = group;
                }
            }
        }

        public void Compute(IDrawableGroupStrategy drawableGroupStrategy, List<MyInstanceComponent> visibleInstances, int passId, int maxLodId)
        {
            int drawnLodsCount, sumInstanceMaterials;
            PrecomputeCounts(out sumInstanceMaterials, out drawnLodsCount, ref m_tmpInstancesCounts, visibleInstances, passId, maxLodId);
            drawableGroupStrategy.Init(drawnLodsCount + sumInstanceMaterials);
            PrepareDrawableContainers(ref m_rawDrawableGroups, ref m_tmpInstancesOffsets, drawableGroupStrategy,
                    m_tmpInstancesCounts, visibleInstances, passId, maxLodId);
        }

        public List<MyRawDrawableGroup> GetRawDrawableGroups()
        {
            return m_rawDrawableGroups;
        }
    }


    class MyGBufferPass : MyRenderPass
    {
        Matrix m_viewProjMatrix;
        MyViewport m_viewport;
        MyGBuffer m_gbuffer;

        IVertexBuffer m_vbInstances;
        readonly MyDrawableGroupGBufferStrategy m_drawableGroupGBufferStrategy = new MyDrawableGroupGBufferStrategy();
        readonly MyDrawableGroupFactory m_drawableGroupFactory = new MyDrawableGroupFactory();

        public void Init(int passId, Matrix viewProjMatrix, MyViewport viewport, MyGBuffer gbuffer)
        {
            m_viewProjMatrix = viewProjMatrix;
            m_viewport = viewport;
            m_gbuffer = gbuffer;
            Init(passId, "GBuffer", MyFrustumEnum.MainFrustum, MyPassIdResolver.GetGBufferPassIdx(PassId));
        }

        protected override void Draw(MyRenderContext RC, List<MyInstanceComponent> visibleInstances)
        {
            int maxLodId = MyManagers.IDGenerator.GBufferLods.GetHighestID();

            ProfilerShort.Begin("Preparation");
            m_drawableGroupGBufferStrategy.Prepare(m_vbInstances);
            m_drawableGroupFactory.Compute(m_drawableGroupGBufferStrategy, visibleInstances, 0, maxLodId);
            bool isEmpty = !m_drawableGroupGBufferStrategy.Finalize(RC, out m_vbInstances);
            if (!isEmpty)
            {
                RC.SetPrimitiveTopology(PrimitiveTopology.TriangleList);

                RC.SetViewport(m_viewport.OffsetX, m_viewport.OffsetY, m_viewport.Width, m_viewport.Height);
                RC.SetRtvs(m_gbuffer, MyDepthStencilAccess.ReadWrite);

                FillConstantBuffer(RC, MyCommon.ProjectionConstants, Matrix.Transpose(m_viewProjMatrix));

                RC.VertexShader.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);
                RC.VertexShader.SetConstantBuffer(MyCommon.PROJECTION_SLOT, MyCommon.ProjectionConstants);

                RC.PixelShader.SetSamplers(0, MySamplerStateManager.StandardSamplers);
                RC.PixelShader.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);
                RC.PixelShader.SetSrv(MyCommon.DITHER_8X8_SLOT, MyGeneratedTextureManager.Dithering8x8Tex);

                IConstantBuffer cbObjectData = GetPlaceholderObjectCB(RC, 255); // <- the lod value does not matter in this case
                RC.VertexShader.SetConstantBuffer(MyCommon.OBJECT_SLOT, cbObjectData);
                RC.PixelShader.SetConstantBuffer(MyCommon.OBJECT_SLOT, cbObjectData);

                ProfilerShort.BeginNextBlock("Recording commands");

                foreach (var itGroup in m_drawableGroupFactory.GetRawDrawableGroups())
                {
                    MyRenderProxy.Assert(itGroup.InstancesCount != 0);
                    MyRenderProxy.Assert(itGroup.InstancesCount == itGroup.InstancesIncrement);
                    foreach (var part in itGroup.Lod.Parts)
                    {
                        MyStandardMaterial material = part.StandardMaterial;
                        RC.SetVertexBuffer(0, part.Parent.VB0);
                        RC.SetVertexBuffer(1, part.Parent.VB1);
                        RC.SetVertexBuffer(2, m_vbInstances);
                        RC.SetIndexBuffer(part.Parent.IB);
                        if (MyRender11.Settings.Wireframe)
                        {
                            RC.SetDepthStencilState(MyDepthStencilStateManager.DepthTestWrite);
                            RC.SetBlendState(null);
                            RC.SetRasterizerState(MyRasterizerStateManager.NocullWireframeRasterizerState);
                        }
                        else
                        {
                            RC.SetDepthStencilState(part.StandardMaterial.DepthStencilState);
                            RC.SetRasterizerState(material.RasterizerState);
                            RC.SetBlendState(part.StandardMaterial.BlendState);
                        }

                        MyShaderBundle shaderBundle = part.GetShaderBundle(itGroup.State);
                        RC.SetInputLayout(shaderBundle.InputLayout);
                        RC.VertexShader.Set(shaderBundle.VertexShader);
                        RC.PixelShader.Set(shaderBundle.PixelShader);
                        RC.PixelShader.SetSrvs(0, SrvStrategy.GetSrvs(part));

                        int numInstances = itGroup.InstancesCount;
                        int ibOffset = itGroup.OffsetInInstanceBuffer + (part.InstanceMaterialOffsetInLod+1)* itGroup.InstancesCount;
                        RC.DrawIndexedInstanced(part.IndicesCount, numInstances, part.StartIndex,
                            part.StartVertex, ibOffset);

                        m_stats.Triangles += (part.IndicesCount / 3) * numInstances;
                        m_stats.Instances += numInstances;
                        m_stats.Draws++;
                    }
                }
            }

            ProfilerShort.End();
        }

        unsafe IConstantBuffer GetGlassCB(MyRenderContext RC, MyGlassMaterial material)
        {
            StaticGlassConstants glassConstants = new StaticGlassConstants();
            glassConstants.Color = material.Color;
            glassConstants.Reflective = material.Refraction;

            var glassCB = MyCommon.GetMaterialCB(sizeof(StaticGlassConstants));
            var mapping = MyMapping.MapDiscard(RC, glassCB);
            mapping.WriteAndPosition(ref glassConstants);
            mapping.Unmap();
            return glassCB;
        }

        // this is tricky call. The method assumes that Draw() has been called in this frame
        public void DrawGlass(MyRenderContext RC)
        {
            RC.SetPrimitiveTopology(PrimitiveTopology.TriangleList);

            RC.SetViewport(m_viewport.OffsetX, m_viewport.OffsetY, m_viewport.Width, m_viewport.Height);
            //RC.SetRtvs(m_gbuffer, MyDepthStencilAccess.ReadWrite); <- the rtv is set out of the pass...

            FillConstantBuffer(RC, MyCommon.ProjectionConstants, Matrix.Transpose(m_viewProjMatrix));

            RC.VertexShader.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);
            RC.VertexShader.SetConstantBuffer(MyCommon.PROJECTION_SLOT, MyCommon.ProjectionConstants);

            RC.PixelShader.SetSamplers(0, MySamplerStateManager.StandardSamplers);
            RC.PixelShader.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);
            RC.PixelShader.SetSrv(MyCommon.DITHER_8X8_SLOT, MyGeneratedTextureManager.Dithering8x8Tex);

            IConstantBuffer cbObjectData = GetPlaceholderObjectCB(RC, 255); // <- the lod value does not matter in this case
            RC.VertexShader.SetConstantBuffer(MyCommon.OBJECT_SLOT, cbObjectData);
            //RC.PixelShader.SetConstantBuffer(MyCommon.OBJECT_SLOT, cbObjectData);

            foreach (var itGroup in m_drawableGroupFactory.GetRawDrawableGroups())
            {
                MyRenderProxy.Assert(itGroup.InstancesCount != 0);
                MyRenderProxy.Assert(itGroup.InstancesCount == itGroup.InstancesIncrement);
                if (itGroup.Lod.GlassParts == null)
                    continue;
                foreach (var part in itGroup.Lod.GlassParts)
                {
                    MyGlassMaterial material = part.GlassMaterial;
                    RC.SetVertexBuffer(0, part.Parent.VB0);
                    RC.SetVertexBuffer(1, part.Parent.VB1);
                    RC.SetVertexBuffer(2, m_vbInstances);
                    RC.SetIndexBuffer(part.Parent.IB);

                    MyShaderBundle shaderBundle = part.GetShaderBundle(itGroup.State);
                    RC.SetInputLayout(shaderBundle.InputLayout);
                    RC.VertexShader.Set(shaderBundle.VertexShader);
                    RC.PixelShader.Set(shaderBundle.PixelShader);
                    RC.PixelShader.SetSrvs(0, material.Srvs);
                    RC.PixelShader.SetConstantBuffer(MyCommon.OBJECT_SLOT, GetGlassCB(RC, material));

                    int numInstances = itGroup.InstancesCount;
                    int ibOffset = itGroup.OffsetInInstanceBuffer + (part.InstanceMaterialOffsetInLod + 1) * itGroup.InstancesCount;
                    RC.DrawIndexedInstanced(part.IndicesCount, numInstances, part.StartIndex,
                        part.StartVertex, ibOffset);
                }
            }
        }
    }


    class MyDepthPass : MyRenderPass
    {
        Matrix m_viewProjMatrix;
        MyViewport m_viewport;
        IDsvBindable m_dsv;
        IVertexBuffer m_vbInstances;
        bool m_isCascade;

        readonly MyDrawableGroupDepthStrategy m_drawableGroupDepthStrategy = new MyDrawableGroupDepthStrategy();
        readonly MyDrawableGroupFactory m_drawableGroupFactory = new MyDrawableGroupFactory();

        public void Init(int passId, Matrix viewProjMatrix, MyViewport viewport, IDsvBindable dsv, bool isCascade, string debugName)
        {
            m_viewProjMatrix = viewProjMatrix;
            m_viewport = viewport;
            m_dsv = dsv;
            m_isCascade = isCascade;

            if (isCascade)
                Init(passId, debugName, MyFrustumEnum.ShadowCascade, MyPassIdResolver.GetCascadeDepthPassIdx(passId));
            else Init(passId, debugName, MyFrustumEnum.ShadowProjection, MyPassIdResolver.GetSingleDepthPassIdx(passId));
        }

        bool IsProfilingDoable()
        {
            // this is hotfix to get theprofiling info from depth pass. Good to refactor in the future
            if (!MyDebugGeometryStage2.EnableParallelRendering)
                return true;
            return !MyRender11.MultithreadedRenderingEnabled;
        }
        
        static IRasterizerState GetRasterizerState(bool isCascade)
        {
            return isCascade
                ? MyRasterizerStateManager.CascadesRasterizerStateOld
                : MyRasterizerStateManager.ShadowRasterizerState;
        }

        protected override void Draw(MyRenderContext RC, List<MyInstanceComponent> visibleInstances)
        {
            int maxLodId = MyManagers.IDGenerator.DepthLods.GetHighestID();

            if (IsProfilingDoable())
                ProfilerShort.Begin("Preparation");
            m_drawableGroupDepthStrategy.Prepare(m_vbInstances);
            m_drawableGroupFactory.Compute(m_drawableGroupDepthStrategy, visibleInstances, PassId, maxLodId);
            bool isEmpty = !m_drawableGroupDepthStrategy.Finalize(RC, out m_vbInstances);
            if (!isEmpty)
            {
                FillConstantBuffer(RC, MyCommon.ProjectionConstants, Matrix.Transpose(m_viewProjMatrix));

                RC.SetPrimitiveTopology(PrimitiveTopology.TriangleList);

                RC.SetViewport(m_viewport.OffsetX, m_viewport.OffsetY, m_viewport.Width, m_viewport.Height);
                RC.SetRtv(m_dsv, null);

                FillConstantBuffer(RC, MyCommon.ProjectionConstants, Matrix.Transpose(m_viewProjMatrix));

                RC.VertexShader.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);
                RC.VertexShader.SetConstantBuffer(MyCommon.PROJECTION_SLOT, MyCommon.ProjectionConstants);

                RC.PixelShader.SetSamplers(0, MySamplerStateManager.StandardSamplers);
                RC.PixelShader.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);
                RC.PixelShader.SetSrv(MyCommon.DITHER_8X8_SLOT, MyGeneratedTextureManager.Dithering8x8Tex);

                // Just some placeholder:
                IConstantBuffer cbObjectData = GetPlaceholderObjectCB(RC, 0);
                RC.VertexShader.SetConstantBuffer(MyCommon.OBJECT_SLOT, cbObjectData);
                RC.PixelShader.SetConstantBuffer(MyCommon.OBJECT_SLOT, cbObjectData);

                if (IsProfilingDoable())
                    ProfilerShort.BeginNextBlock("Recording commands");

                foreach (var itGroup in m_drawableGroupFactory.GetRawDrawableGroups())
                {
                    MyRenderProxy.Assert(itGroup.InstancesCount != 0);
                    MyRenderProxy.Assert(itGroup.InstancesCount == itGroup.InstancesIncrement);
                    foreach (var part in itGroup.Lod.Parts)
                    {
                        RC.SetVertexBuffer(0, part.Parent.VB0);
                        RC.SetVertexBuffer(2, m_vbInstances);
                        RC.SetIndexBuffer(part.Parent.IB);

                        RC.SetDepthStencilState(null);
                        RC.SetRasterizerState(GetRasterizerState(m_isCascade));
                        RC.SetBlendState(null);

                        MyShaderBundle shaderBundle = part.GetShaderBundle(itGroup.State);
                        RC.SetInputLayout(shaderBundle.InputLayout);
                        RC.VertexShader.Set(shaderBundle.VertexShader);
                        RC.PixelShader.Set(shaderBundle.PixelShader);

                        int numInstances = itGroup.InstancesCount;
                        int ibOffset = itGroup.OffsetInInstanceBuffer + (part.InstanceMaterialOffsetInLod + 1) * itGroup.InstancesCount;
                        RC.DrawIndexedInstanced(part.IndicesCount, numInstances, part.StartIndex,
                            part.StartVertex, ibOffset);

                        m_stats.Triangles += (part.IndicesCount / 3) * numInstances;
                        m_stats.Instances += numInstances;
                        m_stats.Draws++;
                    }
                }
            }

            if (IsProfilingDoable())
                ProfilerShort.End();
        }
    }
}
