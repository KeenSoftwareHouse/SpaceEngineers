using System.Diagnostics;
using System.Linq;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using VRage.Render11.Resources;
using VRageMath;
using VRageRender;

namespace VRage.Render11.RenderContext.Internal
{
    class MyRenderContextState
    {
        DeviceContext m_deviceContext;
        MyRenderContextStatistics m_statistics;


        #region Cache fields

        InputLayout m_inputLayout;
        PrimitiveTopology m_primitiveTopology;

        IIndexBuffer m_indexBufferRef;
        MyIndexBufferFormat m_indexBufferFormat;
        int m_indexBufferOffset;

        readonly IVertexBuffer[] m_vertexBuffers = new IVertexBuffer[8];
        readonly int[] m_vertexBuffersStrides = new int[8];
        readonly int[] m_vertexBuffersByteOffset = new int[8];

        #endregion

        #region OutputMerger
        BlendState m_blendState;
        int m_stencilRef;
        DepthStencilState m_depthStencilState;
        int m_rtvsCount;
        RenderTargetView[] m_rtvs = new RenderTargetView[8];
        DepthStencilView m_dsv;
        #endregion

        #region Rasterizer
        RasterizerState m_rasterizerState;
        Vector2I m_scissorLeftTop, m_scissorRightBottom;
        RawViewportF m_viewport;
        #endregion

        #region StreamOutput
        Buffer m_targetBuffer;
        int m_targetOffsets;
        #endregion

        internal void Init(DeviceContext deviceContext, MyRenderContextStatistics statistics)
        {
            m_deviceContext = deviceContext;
            m_statistics = statistics;
        }

        internal void Clear()
        {
            if (m_deviceContext != null)
                m_deviceContext.ClearState();

            m_inputLayout = null;
            m_primitiveTopology = PrimitiveTopology.Undefined;
            m_indexBufferRef = null;
            m_indexBufferFormat = 0;
            m_indexBufferOffset = 0;
            for (int i = 0; i < m_vertexBuffers.Length; i++)
                m_vertexBuffers[i] = null;
            for (int i = 0; i < m_vertexBuffersStrides.Length; i++)
                m_vertexBuffersStrides[i] = 0;
            for (int i = 0; i < m_vertexBuffersByteOffset.Length; i++)
                m_vertexBuffersByteOffset[i] = 0;

            m_blendState = null;
            m_stencilRef = 0;
            m_depthStencilState = null;
            m_rtvsCount = 0;
            for (int i = 0; i < m_rtvs.Length; i++)
                m_rtvs[i] = null;
            m_dsv = null;

            m_rasterizerState = null;
            m_scissorLeftTop = new Vector2I(-1, -1);
            m_scissorRightBottom = new Vector2I(-1, -1);
            m_viewport = default(RawViewportF);

            m_targetBuffer = null;
            m_targetOffsets = 0;

            if (m_statistics != null)
                m_statistics.ClearStates++;
        }

        internal void SetInputLayout(InputLayout il)
        {
            if (il == m_inputLayout)
                return;

            m_inputLayout = il;
            m_deviceContext.InputAssembler.InputLayout = il;
            m_statistics.SetInputLayout++;
        }

        internal void SetPrimitiveTopology(PrimitiveTopology pt)
        {
            if (pt == m_primitiveTopology)
                return;

            m_primitiveTopology = pt;

            m_deviceContext.InputAssembler.PrimitiveTopology = pt;
            m_statistics.SetPrimitiveTopologies++;
        }

        internal void SetIndexBuffer(IIndexBuffer ib, MyIndexBufferFormat format, int offset)
        {
            if (ib == m_indexBufferRef
                && format == m_indexBufferFormat
                && offset == m_indexBufferOffset)
                return;

            m_indexBufferRef = ib;
            m_indexBufferFormat = format;
            m_indexBufferOffset = offset;
            m_deviceContext.InputAssembler.SetIndexBuffer(ib.Buffer, (Format)format, offset);
            m_statistics.SetIndexBuffers++;
        }

        internal void SetVertexBuffer(int slot, IVertexBuffer vb, int stride, int byteOffset)
        {
            MyRenderProxy.Assert(slot < m_vertexBuffers.Length);

            if (vb == m_vertexBuffers[slot] && stride == m_vertexBuffersStrides[slot] && byteOffset == m_vertexBuffersByteOffset[slot])
                return;

            m_vertexBuffers[slot] = vb;
            m_vertexBuffersStrides[slot] = stride;
            m_vertexBuffersByteOffset[slot] = byteOffset;

            m_deviceContext.InputAssembler.SetVertexBuffers(slot, new VertexBufferBinding(vb != null ? vb.Buffer : null, stride, byteOffset));
            m_statistics.SetVertexBuffers++;
        }

        internal void SetVertexBuffers(int startSlot, IVertexBuffer[] vbs, int[] strides)
        {
            Debug.Assert(strides != null);
            MyRenderProxy.Assert(startSlot + vbs.Length < m_vertexBuffers.Length);

            for (int i = startSlot; i < startSlot + vbs.Length; i++)
                SetVertexBuffer(i, vbs[i], strides[i], 0);
        }

        internal void SetBlendState(BlendState bs)
        {
            if (bs == m_blendState)
                return;

            m_blendState = bs;
            m_deviceContext.OutputMerger.SetBlendState(bs);
            m_statistics.SetBlendStates++;
        }

        internal void SetDepthStencilState(DepthStencilState dss, int stencilRef)
        {
            if (dss == m_depthStencilState && stencilRef == m_stencilRef)
                return;

            m_depthStencilState = dss;
            m_stencilRef = stencilRef;

            m_deviceContext.OutputMerger.SetDepthStencilState(dss, stencilRef);
            m_statistics.SetDepthStencilStates++;
        }

        internal void SetTargets(DepthStencilView dsv, RenderTargetView[] rtvs, int rtvsCount)
        {
            if (dsv == m_dsv && rtvsCount == m_rtvsCount)
            {
                bool same = true;
                for (int i = 0; i < rtvsCount; i++)
                    if (rtvs[i] != m_rtvs[i])
                        same = false;

                if (same)
                    return;
            }

            m_dsv = dsv;
            m_rtvsCount = rtvsCount;
            for (int i = 0; i < m_rtvsCount; i++)
                m_rtvs[i] = rtvs[i];

            m_deviceContext.OutputMerger.SetTargets(dsv, rtvsCount, rtvs);
            m_statistics.SetTargets++;
        }

        internal void SetRasterizerState(RasterizerState rs)
        {
            if (rs == m_rasterizerState)
                return;

            m_rasterizerState = rs;
            m_deviceContext.Rasterizer.State = m_rasterizerState;
            m_statistics.SetRasterizerStates++;
        }

        internal void SetScissorRectangle(int left, int top, int right, int bottom)
        {
            Vector2I leftTop = new Vector2I(left, top);
            Vector2I rightBottom = new Vector2I(right, bottom);

            if (leftTop == m_scissorLeftTop && rightBottom == m_scissorRightBottom)
                return;

            m_scissorLeftTop = leftTop;
            m_scissorRightBottom = rightBottom;

            m_deviceContext.Rasterizer.SetScissorRectangle(m_scissorLeftTop.X, m_scissorLeftTop.Y,
                m_scissorRightBottom.X, m_scissorRightBottom.Y);
        }

        internal void SetViewport(RawViewportF viewport)
        {
            if (viewport.X != m_viewport.X
                || viewport.Y != m_viewport.Y
                || viewport.Width != m_viewport.Width
                || viewport.Height != m_viewport.Height
                || viewport.MinDepth != m_viewport.MinDepth
                || viewport.MaxDepth != m_viewport.MaxDepth)
            {
                m_viewport = viewport;

                m_deviceContext.Rasterizer.SetViewport(viewport);
                m_statistics.SetViewports++;
            }
        }

        internal void SetTarget(Buffer buffer, int offsets)
        {
            if (buffer == m_targetBuffer && offsets == m_targetOffsets)
                return;

            m_targetBuffer = buffer;
            m_targetOffsets = offsets;

            m_deviceContext.StreamOutput.SetTarget(buffer, offsets);
            m_statistics.SetTargets++;
        }
    }
}