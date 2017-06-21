using System;
using System.Diagnostics;
using System.Linq;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.Mathematics.Interop;
using VRage.Profiler;
using VRage.Render11.Profiler;
using VRage.Render11.RenderContext.Internal;
using VRage.Render11.Resources;
using MapFlags = SharpDX.Direct3D11.MapFlags;
using Resource = SharpDX.Direct3D11.Resource;
using VRageRender;


namespace VRage.Render11.RenderContext
{
    enum MyDepthStencilAccess
    {
        ReadWrite,
        DepthReadOnly,
        StencilReadOnly,
        ReadOnly
    }


    internal class MyRenderContext : System.IDisposable
    {
        #region Member variables

        SharpDX.Direct3D11.DeviceContext m_deviceContext;
        MyVertexStage m_vertexShaderStage = new MyVertexStage();
        MyGeometryStage m_geometryShaderStage = new MyGeometryStage();
        MyPixelStage m_pixelShaderStage = new MyPixelStage();
        MyComputeStage m_computeShaderStage = new MyComputeStage();
        MyAllShaderStages m_allShaderStages;

        SharpDX.Direct3D11.UserDefinedAnnotation m_annotations;
        MyFrameProfilingContext m_profilingQueries = new MyFrameProfilingContext();

        bool m_isDeferred;
        MyRenderContextStatistics m_statistics = new MyRenderContextStatistics();
        MyRenderContextState m_state = new MyRenderContextState();
        #endregion

        [ThreadStatic]
        static RenderTargetView[] m_tmpRtvs;

        #region Properties

        internal MyVertexStage VertexShader
        {
            get { return m_vertexShaderStage; }
        }

        internal MyGeometryStage GeometryShader
        {
            get { return m_geometryShaderStage; }
        }

        internal MyPixelStage PixelShader
        {
            get { return m_pixelShaderStage; }
        }

        internal MyComputeStage ComputeShader
        {
            get { return m_computeShaderStage; }
        }

        internal MyAllShaderStages AllShaderStages
        {
            get { return m_allShaderStages; }
        }

        internal MyFrameProfilingContext ProfilingQueries
        {
            get { return m_profilingQueries; }
        }

        #endregion

        #region Initialize/Dispose

        public MyRenderContext()
        {
            m_allShaderStages = new MyAllShaderStages(m_vertexShaderStage, m_geometryShaderStage, m_pixelShaderStage, m_computeShaderStage);
        }

        internal void Initialize(DeviceContext context = null)
        {
            MyRenderProxy.Assert(m_deviceContext == null, "Initialize is called to already initialized object. Whether initialization has been performed or not, check by the method 'IsInitialized()'");

            if (context == null)
            {
                context = new DeviceContext(MyRender11.Device);
                m_isDeferred = true;
            }
            else
            {
                m_isDeferred = false;
            }

            m_deviceContext = context;
            m_vertexShaderStage.Init(m_deviceContext, m_deviceContext.VertexShader, m_statistics);
            m_geometryShaderStage.Init(m_deviceContext, m_deviceContext.GeometryShader, m_statistics);
            m_pixelShaderStage.Init(m_deviceContext, m_deviceContext.PixelShader, m_statistics);
            m_computeShaderStage.Init(m_deviceContext, m_deviceContext.ComputeShader, m_statistics);

            m_state.Init(m_deviceContext, m_statistics);

            if (m_annotations == null)
                m_annotations = m_deviceContext.QueryInterface<SharpDX.Direct3D11.UserDefinedAnnotation>();

            m_statistics.Clear();
        }

        bool m_disposed = false;

        public void Dispose()
        {
            Dispose(true);
            System.GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (m_disposed)
                return;

            if (disposing)
            {
                // Dispose of shaders stage will be handled by DeviceContext.Dispose(), now we want to disable access to them:
                m_vertexShaderStage = null;
                m_geometryShaderStage = null;
                m_pixelShaderStage = null;
                m_computeShaderStage = null;
            }
            if (m_isDeferred)
            {
                try
                {
                    m_deviceContext.Dispose();
                }
                catch (Exception ex)
                {
                    MyRender11.Log.Log(Utils.MyLogSeverity.Error, "Exception disposing device context: {0}", ex.ToString());
                    MyRender11.Log.Flush();
                }
            }

            m_disposed = true;
        }

        #endregion

        #region Utils
        internal MyRenderContextStatistics GetStatistics()
        {
            return m_statistics;
        }

        internal void ClearStatistics()
        {
            m_statistics.Clear();
        }

        [Conditional("DEBUG")]
        internal void CheckErrors()
        {
            if (!m_isDeferred)
                MyRender11.ProcessDebugOutput();
        }
        #endregion

        #region Profiling
        [Conditional("DEBUG")]
        // DxAnnotations are visible in NSight frame debugger
        internal void BeginDxAnnotationBlock(string tag)
        {
            m_annotations.BeginEvent(tag);
        }

        [Conditional("DEBUG")]
        internal void EndDxAnnotationBlock()
        {
            m_annotations.EndEvent();
        }

        [Conditional("DEBUG")]
        internal void SetDxAnnotationMarker(string tag)
        {
            m_annotations.SetMarker(tag);
        }

        [Conditional(ProfilerShort.PerformanceProfilingSymbol)]
        internal void BeginProfilingBlock(string tag)
        {
            var q = MyQueryFactory.CreateTimestampQuery();
            End(q);
            var info = new MyIssuedQuery(q, tag, MyIssuedQueryEnum.BlockStart);

            if (m_isDeferred)
            {
                m_profilingQueries.m_issued.Enqueue(info);
            }
            else
            {
                MyGpuProfiler.IC_Enqueue(info);
            }
        }

        [Conditional(ProfilerShort.PerformanceProfilingSymbol)]
        internal void EndProfilingBlock()
        {
            var q = MyQueryFactory.CreateTimestampQuery();
            End(q);
            var info = new MyIssuedQuery(q, "", MyIssuedQueryEnum.BlockEnd);

            if (m_isDeferred)
            {
                m_profilingQueries.m_issued.Enqueue(info);
            }
            else
            {
                MyGpuProfiler.IC_Enqueue(info);
            }
        }

        internal void BeginProfilingBlockAlways(string tag)
        {
            var q = MyQueryFactory.CreateTimestampQuery();
            End(q);
            var info = new MyIssuedQuery(q, tag, MyIssuedQueryEnum.BlockStart);

            if (m_isDeferred)
            {
                m_profilingQueries.m_issued.Enqueue(info);
            }
            else
            {
                MyGpuProfiler.IC_Enqueue(info);
            }
        }

        internal void EndProfilingBlockAlways()
        {
            var q = MyQueryFactory.CreateTimestampQuery();
            End(q);
            var info = new MyIssuedQuery(q, "", MyIssuedQueryEnum.BlockEnd);

            if (m_isDeferred)
            {
                m_profilingQueries.m_issued.Enqueue(info);
            }
            else
            {
                MyGpuProfiler.IC_Enqueue(info);
            }
        }
        #endregion

        #region DeviceContext

        internal void Begin(Asynchronous asyncRef)
        {
            m_deviceContext.Begin(asyncRef);
        }

        internal void ClearDsv(IDepthStencil ds, DepthStencilClearFlags clearFlags,
            float depth, byte stencil)
        {
            IDepthStencilInternal dsInternal = (IDepthStencilInternal)ds;
            m_deviceContext.ClearDepthStencilView(dsInternal.Dsv, clearFlags, depth, stencil);
            CheckErrors();
        }

        internal void ClearDsv(IDsvBindable dsv, DepthStencilClearFlags clearFlags,
           float depth, byte stencil)
        {
            m_deviceContext.ClearDepthStencilView(dsv.Dsv, clearFlags, depth, stencil);
            CheckErrors();
        }

        internal void ClearRtv(IRtvBindable rtv, RawColor4 colorRGBA)
        {
            m_deviceContext.ClearRenderTargetView(rtv.Rtv, colorRGBA);
            CheckErrors();
        }

        internal void ClearState()
        {
            m_state.Clear();

            m_vertexShaderStage.ClearState();
            m_geometryShaderStage.ClearState();
            m_pixelShaderStage.ClearState();
            m_computeShaderStage.ClearState();
            CheckErrors();
        }

        internal void ClearUav(IUavBindable uav, RawInt4 values)
        {
            m_deviceContext.ClearUnorderedAccessView(uav.Uav, values);
            CheckErrors();
        }

        // TODO: Code that uses temporary resources (that calls this method) should be changed to use Managers and their interfaces
        internal void CopyResource(IResource source, Resource destination)
        {
            m_deviceContext.CopyResource(source.Resource, destination);
            CheckErrors();
        }

        internal void CopyResource(IResource source, IResource destination)
        {
            CopyResource(source, destination.Resource);
        }

        internal void CopyStructureCount(IBuffer dstBufferRef, int dstAlignedByteOffset, IUavBindable srcViewRef)
        {
            m_deviceContext.CopyStructureCount(dstBufferRef.Buffer, dstAlignedByteOffset, srcViewRef.Uav);
            CheckErrors();
        }

        // TODO: Code that uses temporary resources (that calls this method) should be changed to use Managers and their interfaces
        internal void CopySubresourceRegion(IResource source, int sourceSubresource, ResourceRegion? sourceRegion,
            Resource destination, int destinationSubResource, int dstX = 0, int dstY = 0, int dstZ = 0)
        {
            m_deviceContext.CopySubresourceRegion(source.Resource, sourceSubresource, sourceRegion, destination, destinationSubResource, dstX, dstY, dstZ);
            CheckErrors();
        }

        internal void CopySubresourceRegion(IResource source, int sourceSubresource, ResourceRegion? sourceRegion,
            IResource destination, int destinationSubResource, int dstX = 0, int dstY = 0, int dstZ = 0)
        {
            CopySubresourceRegion(source, sourceSubresource, sourceRegion, destination.Resource, destinationSubResource, dstX, dstY, dstZ);
        }

        internal void Draw(int vertexCount, int startVertexLocation)
        {
            m_deviceContext.Draw(vertexCount, startVertexLocation);
            m_statistics.Draws++;
            CheckErrors();
        }

        internal void DrawAuto()
        {
            m_deviceContext.DrawAuto();
            m_statistics.Draws++;
            CheckErrors();
        }

        internal void DrawIndexed(int indexCount, int startIndexLocation, int baseVertexLocation)
        {
            m_deviceContext.DrawIndexed(indexCount, startIndexLocation, baseVertexLocation);
            m_statistics.Draws++;
            CheckErrors();
        }

        internal void DrawInstanced(int vertexCountPerInstance, int instanceCount, int startVertexLocation,
            int startInstanceLocation)
        {
            m_deviceContext.DrawInstanced(vertexCountPerInstance, instanceCount, startVertexLocation,
                startInstanceLocation);
            m_statistics.Draws++;
            CheckErrors();
        }

        internal void DrawIndexedInstanced(int indexCountPerInstance, int instanceCount, int startIndexLocation,
            int baseVertexLocation, int startInstanceLocation)
        {
            m_deviceContext.DrawIndexedInstanced(indexCountPerInstance, instanceCount, startIndexLocation,
                baseVertexLocation, startInstanceLocation);
            m_statistics.Draws++;
            CheckErrors();
        }

        internal void DrawIndexedInstancedIndirect(IBuffer bufferForArgsRef, int alignedByteOffsetForArgs)
        {
            m_deviceContext.DrawIndexedInstancedIndirect(bufferForArgsRef.Buffer, alignedByteOffsetForArgs);
            m_statistics.Draws++;
            CheckErrors();
        }

        internal void Dispatch(int threadGroupCountX, int threadGroupCountY, int threadGroupCountZ)
        {
            m_deviceContext.Dispatch(threadGroupCountX, threadGroupCountY, threadGroupCountZ);
            m_statistics.Dispatches++;
            CheckErrors();
        }

        internal void End(Asynchronous asyncRef)
        {
            m_deviceContext.End(asyncRef);
            CheckErrors();
        }

        internal void ExecuteCommandList(CommandList commandListRef, RawBool restoreContextState)
        {
            m_deviceContext.ExecuteCommandList(commandListRef, restoreContextState);
            CheckErrors();
        }

        internal CommandList FinishCommandList(bool restoreState)
        {
            return m_deviceContext.FinishCommandList(restoreState);
            CheckErrors();
        }

        internal void GenerateMips(ISrvBindable srv)
        {
            m_deviceContext.GenerateMips(srv.Srv);
            CheckErrors();
        }

        internal T GetData<T>(Asynchronous data, AsynchronousFlags flags) where T : struct
        {
            return m_deviceContext.GetData<T>(data, flags);
        }

        internal bool GetData<T>(Asynchronous data, out T result) where T : struct
        {
            return m_deviceContext.GetData<T>(data, out result);
        }

        internal bool GetData<T>(Asynchronous data, AsynchronousFlags flags, out T result) where T : struct
        {
            return m_deviceContext.GetData<T>(data, flags, out result);
        }

        internal bool IsDataAvailable(Asynchronous data)
        {
            return m_deviceContext.IsDataAvailable(data);
            CheckErrors();
        }

        internal bool IsDataAvailable(Asynchronous data, AsynchronousFlags flags)
        {
            return m_deviceContext.IsDataAvailable(data, flags);
            CheckErrors();
        }

        internal DataBox MapSubresource(IResource resourceRef, int subresource, MapMode mapType,
            MapFlags mapFlags)
        {
            return m_deviceContext.MapSubresource(resourceRef.Resource, subresource, mapType, mapFlags);
            CheckErrors();
        }

        internal DataBox MapSubresource(IResource resource, int mipSlice, int arraySlice, MapMode mode, MapFlags flags, out int mipSize)
        {
            return m_deviceContext.MapSubresource(resource.Resource, mipSlice, arraySlice, mode, flags, out mipSize);
            CheckErrors();
        }

        internal DataBox MapSubresource(Texture1D resource, int mipSlice, int arraySlice, MapMode mode, MapFlags flags,
            out DataStream stream)
        {
            return m_deviceContext.MapSubresource(resource, mipSlice, arraySlice, mode, flags, out stream);
            CheckErrors();
        }

        internal DataBox MapSubresource(Texture2D resource, int mipSlice, int arraySlice, MapMode mode, MapFlags flags,
            out DataStream stream)
        {
            return m_deviceContext.MapSubresource(resource, mipSlice, arraySlice, mode, flags, out stream);
            CheckErrors();
        }

        internal DataBox MapSubresource(Texture3D resource, int mipSlice, int arraySlice, MapMode mode, MapFlags flags,
            out DataStream stream)
        {
            return m_deviceContext.MapSubresource(resource, mipSlice, arraySlice, mode, flags, out stream);
            CheckErrors();
        }

        // TODO: Code that uses temporary resources (that calls this method) should be changed to use Managers and their interfaces
        internal void UnmapSubresource(Resource resourceRef, int subresource)
        {
            m_deviceContext.UnmapSubresource(resourceRef, subresource);
            CheckErrors();
        }

        internal void UnmapSubresource(IResource resourceRef, int subresource)
        {
            UnmapSubresource(resourceRef.Resource, subresource);
        }

        internal void UpdateSubresource(DataBox source, IResource resource, int subresource = 0)
        {
            m_deviceContext.UpdateSubresource(source, resource.Resource, subresource);
            CheckErrors();
        }

        #endregion

        #region InputAssembler

        internal void SetInputLayout(InputLayout il)
        {
            m_state.SetInputLayout(il);
            CheckErrors();
        }

        internal void SetPrimitiveTopology(PrimitiveTopology pt)
        {
            m_state.SetPrimitiveTopology(pt);
            CheckErrors();
        }

        internal void SetIndexBuffer(IIndexBuffer indexBufferRef, int offset = 0)
        {
            m_state.SetIndexBuffer(indexBufferRef, indexBufferRef != null ? indexBufferRef.Format : 0, offset);
            CheckErrors();
        }

        internal void SetVertexBuffer(int slot, IVertexBuffer vb, int stride = -1, int byteOffset = 0)
        {
            if (vb != null && stride < 0)
                stride = vb.Description.StructureByteStride;

            m_state.SetVertexBuffer(slot, vb, stride, byteOffset);
            CheckErrors();
        }

        internal void SetVertexBuffers(int startSlot, IVertexBuffer[] vbs, int[] strides = null)
        {
            strides = strides ?? vbs.Select(vb => vb != null ? vb.Description.StructureByteStride : -1).ToArray();

            m_state.SetVertexBuffers(startSlot, vbs, strides);
            CheckErrors();
        }
        #endregion

        #region OutputMerger
        internal void SetBlendState(IBlendState bs)
        {
            BlendState dxstate = null;
            if (bs != null)
            {
                IBlendStateInternal bsInternal = (IBlendStateInternal)bs;
                dxstate = bsInternal.Resource;
            }
            m_state.SetBlendState(dxstate);
            CheckErrors();
        }

        internal void ResetTargets()
        {
            m_state.SetTargets(null, null, 0);
        }

        internal void SetDepthStencilState(IDepthStencilState dss, int stencilRef = 0)
        {
            DepthStencilState dxstate = null;
            if (dss != null)
            {
                IDepthStencilStateInternal dssInternal = (IDepthStencilStateInternal)dss;
                dxstate = dssInternal.Resource;
            }
            m_state.SetDepthStencilState(dxstate, stencilRef);
            CheckErrors();
        }

        internal void SetRtv(IRtvBindable rtv)
        {
            InternalSetRtvs(null, MyDepthStencilAccess.ReadOnly, rtv);
            CheckErrors();
        }

        internal void SetRtvs(params IRtvBindable[] rtvs)
        {
            InternalSetRtvs(null, MyDepthStencilAccess.ReadOnly, rtvs);
            CheckErrors();
        }

        internal void SetRtv(IDsvBindable dsvBind, IRtvBindable rtv)
        {
            DepthStencilView dsv = null;
            if (dsvBind != null)
                dsv = dsvBind.Dsv;
            InternalSetRtvs(dsv, rtv);
            CheckErrors();
        }

        internal void SetRtv(IDepthStencil ds, MyDepthStencilAccess access)
        {
            InternalSetRtvs(ds, access);
            CheckErrors();
        }

        internal void SetRtv(IDepthStencil ds, MyDepthStencilAccess access, IRtvBindable rtv)
        {
            InternalSetRtvs(ds, access, rtv);
            CheckErrors();
        }

        internal void SetRtvs(IDepthStencil ds, MyDepthStencilAccess access, params IRtvBindable[] rtvs)
        {
            InternalSetRtvs(ds, access, rtvs);
            CheckErrors();
        }

        internal void SetRtvs(MyGBuffer gbuffer, MyDepthStencilAccess access)
        {
            InternalSetRtvs(gbuffer.DepthStencil, access, gbuffer.GBuffer0, gbuffer.GBuffer1, gbuffer.GBuffer2);
            CheckErrors();
        }

        void InternalSetRtvs(IDepthStencil ds, MyDepthStencilAccess access, params IRtvBindable[] rtvs)
        {
            // Init DepthStencilView
            DepthStencilView dsv = null;
            if (ds == null)
                dsv = null;
            else
            {
                IDepthStencilInternal dsInternal = (IDepthStencilInternal)ds;
                switch (access)
                {
                    case MyDepthStencilAccess.ReadWrite:
                        dsv = dsInternal.Dsv;
                        break;
                    case MyDepthStencilAccess.DepthReadOnly:
                        dsv = dsInternal.Dsv_roDepth;
                        break;
                    case MyDepthStencilAccess.StencilReadOnly:
                        dsv = dsInternal.Dsv_roStencil;
                        break;
                    case MyDepthStencilAccess.ReadOnly:
                        dsv = dsInternal.Dsv_ro;
                        break;
                }
            }

            InternalSetRtvs(dsv, rtvs);
        }

        void InternalSetRtvs(DepthStencilView dsv, params IRtvBindable[] rtvs)
        {
            if (m_tmpRtvs == null)
                m_tmpRtvs = new RenderTargetView[8];

            // Init RenderTargetView-s
            MyRenderProxy.Assert(rtvs.Length <= m_tmpRtvs.Length);
            for (int i = 0; i < rtvs.Length; i++)
            {
                IRtvBindable rtvBindable = rtvs[i];

                RenderTargetView dxObject = null;
                if (rtvBindable != null)
                    dxObject = rtvBindable.Rtv;
                m_tmpRtvs[i] = dxObject;
            }
            for (int i = rtvs.Length; i < m_tmpRtvs.Length; i++)
                m_tmpRtvs[i] = null;

            m_state.SetTargets(dsv, m_tmpRtvs, rtvs.Length);
            CheckErrors();
        }

        #endregion

        #region Rasterizer

        internal void SetRasterizerState(IRasterizerState rs)
        {
            RasterizerState dxstate = null;
            if (rs != null)
            {
                IRasterizerStateInternal rsInternal = (IRasterizerStateInternal)rs;
                dxstate = rsInternal.Resource;
            }
            m_state.SetRasterizerState(dxstate);
            CheckErrors();
        }

        internal void SetScissorRectangle(int left, int top, int right, int bottom)
        {
            m_state.SetScissorRectangle(left, top, right, bottom);
            CheckErrors();
        }

        internal void SetViewport(SharpDX.Mathematics.Interop.RawViewportF viewport)
        {
            m_state.SetViewport(viewport);
            CheckErrors();
        }

        internal void SetViewport(float x, float y, float width, float height, float minZ = 0f, float maxZ = 1f)
        {
            RawViewportF viewport = new RawViewportF
            {
                X = x,
                Y = y,
                Width = width,
                Height = height,
                MinDepth = minZ,
                MaxDepth = maxZ
            };
            SetViewport(viewport);
            CheckErrors();
        }

        internal void SetScreenViewport()
        {
            SetViewport(new SharpDX.ViewportF(0, 0, MyRender11.ResolutionF.X, MyRender11.ResolutionF.Y));
            CheckErrors();
        }

        #endregion Rasterizer methods

        #region StreamOutput

        internal void SetTarget(IBuffer buffer, int offsets)
        {
            m_state.SetTarget(buffer.Buffer, offsets);
            CheckErrors();
        }

        #endregion
    }
}