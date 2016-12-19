using System;
using System.Runtime.InteropServices;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using VRage.Render11.Common;
using VRage.Render11.RenderContext;
using VRage.Render11.Resources;
using VRage.Render11.Tools;
using VRageMath;
using VRageRender;

namespace VRage.Render11.LightingStage.Shadows
{
    class MyPostprocessMarkCascades : IManagerDevice
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct MyMarkerConstants
        {
            public Matrix ShadowSpaceToDepthMapSpace;
        }

        IConstantBuffer m_markerConstantBuffer;
        PixelShaderId m_psMarker = PixelShaderId.NULL;
        VertexShaderId m_vsMarker = VertexShaderId.NULL;

        PixelShaderId m_psDrawCoverage = PixelShaderId.NULL;

        IIndexBuffer m_indexBuffer;
        IVertexBuffer m_vertexBuffer;
        InputLayoutId m_inputLayout = InputLayoutId.NULL;

        unsafe IVertexBuffer CreateVertexBuffer()
        {
            Vector3* vertices = stackalloc Vector3[8];
            vertices[0] = new Vector3(-1, -1, 0);
            vertices[1] = new Vector3(-1, 1, 0);
            vertices[2] = new Vector3(1, 1, 0);
            vertices[3] = new Vector3(1, -1, 0);
            vertices[4] = new Vector3(-1, -1, 1);
            vertices[5] = new Vector3(-1, 1, 1);
            vertices[6] = new Vector3(1, 1, 1);
            vertices[7] = new Vector3(1, -1, 1);
            return MyManagers.Buffers.CreateVertexBuffer(
                "MyPostprocessMarkCascades.VertexBuffer", 8, sizeof(Vector3), new IntPtr(vertices),
                ResourceUsage.Dynamic);
        }

        unsafe IIndexBuffer CreateIndexBuffer()
        {
            const int indexCount = 36;
            ushort* indices = stackalloc ushort[indexCount];
            indices[0] = 0; indices[1] = 1; indices[2] = 2;
            indices[3] = 0; indices[4] = 2; indices[5] = 3;
            indices[6] = 1; indices[7] = 5; indices[8] = 6;
            indices[9] = 1; indices[10] = 6; indices[11] = 2;
            indices[12] = 5; indices[13] = 4; indices[14] = 7;
            indices[15] = 5; indices[16] = 7; indices[17] = 6;
            indices[18] = 4; indices[19] = 0; indices[20] = 3;
            indices[21] = 4; indices[22] = 3; indices[23] = 7;
            indices[24] = 3; indices[25] = 2; indices[26] = 6;
            indices[27] = 3; indices[28] = 6; indices[29] = 7;
            indices[30] = 1; indices[31] = 0; indices[32] = 4;
            indices[33] = 1; indices[34] = 4; indices[35] = 5;

            return MyManagers.Buffers.CreateIndexBuffer(
                "MyPostprocessMarkCascades.IndexBuffer", indexCount, new IntPtr(indices),
                MyIndexBufferFormat.UShort, ResourceUsage.Immutable);
        }

        unsafe void IManagerDevice.OnDeviceInit()
        {
            if (m_markerConstantBuffer == null)
                m_markerConstantBuffer = MyManagers.Buffers.CreateConstantBuffer("MyPostprocessMarkCascades.MarkerConstantBuffer", sizeof(MyMarkerConstants), usage: ResourceUsage.Dynamic);

            if (m_psMarker == PixelShaderId.NULL)
                m_psMarker = MyShaders.CreatePs("Shadows\\StencilMarker.hlsl");
            if (m_vsMarker == VertexShaderId.NULL)
                m_vsMarker = MyShaders.CreateVs("Shadows\\StencilMarker.hlsl");
            if (m_psDrawCoverage == PixelShaderId.NULL)
                m_psDrawCoverage = MyShaders.CreatePs("Shadows\\CascadeCoverage.hlsl");
            if (m_inputLayout == InputLayoutId.NULL)
                m_inputLayout = MyShaders.CreateIL(m_vsMarker.BytecodeId, MyVertexLayouts.GetLayout(MyVertexInputComponentType.POSITION3));

            m_vertexBuffer = CreateVertexBuffer();
            m_indexBuffer = CreateIndexBuffer();
        }

        void IManagerDevice.OnDeviceReset()
        {
            MyManagers.Buffers.Dispose(m_vertexBuffer);
            m_vertexBuffer = CreateVertexBuffer();

            MyManagers.Buffers.Dispose(m_indexBuffer);
            m_indexBuffer = CreateIndexBuffer();
        }

        void IManagerDevice.OnDeviceEnd()
        {
            MyManagers.Buffers.Dispose(m_indexBuffer);
            MyManagers.Buffers.Dispose(m_vertexBuffer);
        }

        public void MarkOneCascade(int numCascade, IDepthStencil depthStencil, Matrix worldToProjection,
            ICascadeShadowMapSlice slice)
        {
            MyRenderContext RC = MyRender11.RC;
            RC.SetVertexBuffer(0, m_vertexBuffer);
            RC.SetIndexBuffer(m_indexBuffer);
            RC.SetInputLayout(m_inputLayout);
            RC.SetViewport(0, 0, MyRender11.ResolutionI.X, MyRender11.ResolutionI.Y);
            RC.SetDepthStencilState(MyDepthStencilStateManager.MarkIfInsideCascadeOld[numCascade], 0xf - numCascade);
            RC.SetRasterizerState(MyRasterizerStateManager.NocullRasterizerState);
            RC.SetPrimitiveTopology(PrimitiveTopology.TriangleList);

            MyMapping mapping = MyMapping.MapDiscard(m_markerConstantBuffer);
            Matrix shadowToProjection = slice.MatrixShadowToWorldAt0Space * worldToProjection;
            shadowToProjection = Matrix.Transpose(shadowToProjection);
            mapping.WriteAndPosition(ref shadowToProjection);
            mapping.Unmap();
            RC.VertexShader.SetConstantBuffer(6, m_markerConstantBuffer);
            RC.VertexShader.Set(m_vsMarker);

            RC.PixelShader.SetSrv(0, depthStencil.SrvDepth);
            //RC.PixelShader.SetSrv(1, depthStencil.SrvStencil);              
            RC.PixelShader.Set(m_psMarker);

            RC.SetRtv(depthStencil, MyDepthStencilAccess.DepthReadOnly);

            RC.DrawIndexed(36, 0, 0);
        }

        public void MarkAllCascades(IDepthStencil depthStencil, Matrix worldToProjection, ICascadeShadowMap csm)
        {
            MyRenderContext RC = MyRender11.RC;

            RC.ClearDsv(depthStencil, DepthStencilClearFlags.Stencil, 0, 0);

            for (int i = 0; i < csm.SlicesCount; i++)
            {
                MarkOneCascade(i, depthStencil, worldToProjection, csm.GetSlice(i));
            }
        }

        public void DrawCoverage(IRtvTexture outTex, IDepthStencil depthStencil)
        {
            MyRenderContext RC = MyRender11.RC;
            RC.SetBlendState(null);
            RC.SetRtv(outTex);

            RC.PixelShader.Set(m_psDrawCoverage);
            RC.PixelShader.SetSrv(1, depthStencil.SrvStencil);

            MyScreenPass.DrawFullscreenQuad();
            RC.ResetTargets();
        }
    }
}
