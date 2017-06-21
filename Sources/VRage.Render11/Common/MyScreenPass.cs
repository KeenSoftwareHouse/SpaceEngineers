using SharpDX.Direct3D11;
using System.Diagnostics;
using VRage.Render11.Common;
using VRage.Render11.RenderContext;
using VRage.Render11.Resources;
using VRageMath;

namespace VRageRender
{
    internal delegate void OnSettingsChangedDelegate();

    class MyScreenPass : MyImmediateRC
    {
        static VertexShaderId m_VSCopy;
        static InputLayoutId m_IL = InputLayoutId.NULL;
        static IVertexBuffer m_VBFullscreen;
        static IVertexBuffer m_VBLeftPart;
        static IVertexBuffer m_VBRightPart;

        static VRageRender.Vertex.MyVertexFormatPositionTextureH[] m_vbData = new VRageRender.Vertex.MyVertexFormatPositionTextureH[4];

        internal static void Init()
        {
            m_VSCopy = MyShaders.CreateVs("Postprocess/PostprocessCopy.hlsl");

            {
                m_VBFullscreen = MyManagers.Buffers.CreateVertexBuffer(
                    "MyScreenPass.VBFullscreen", 4, VRageRender.Vertex.MyVertexFormatPositionTextureH.STRIDE,
                    usage: ResourceUsage.Dynamic);
                m_vbData[0] = new VRageRender.Vertex.MyVertexFormatPositionTextureH(new Vector3(-1, -1, 0),
                    new VRageMath.PackedVector.HalfVector2(0, 1f));
                m_vbData[1] = new VRageRender.Vertex.MyVertexFormatPositionTextureH(new Vector3(-1, 1, 0),
                    new VRageMath.PackedVector.HalfVector2(0, 0));
                m_vbData[2] = new VRageRender.Vertex.MyVertexFormatPositionTextureH(new Vector3(1, -1, 0),
                    new VRageMath.PackedVector.HalfVector2(1, 1f));
                m_vbData[3] = new VRageRender.Vertex.MyVertexFormatPositionTextureH(new Vector3(1, 1, 0),
                    new VRageMath.PackedVector.HalfVector2(1, 0f));
                MyMapping mapping = MyMapping.MapDiscard(RC, m_VBFullscreen);
                mapping.WriteAndPosition(m_vbData, 4);
                mapping.Unmap();
            }

            {
                m_VBLeftPart = MyManagers.Buffers.CreateVertexBuffer(
                    "MyVRScreenPass.VBLeftPart", 4, VRageRender.Vertex.MyVertexFormatPositionTextureH.STRIDE,
                    usage: ResourceUsage.Dynamic);
                m_vbData[0] = new VRageRender.Vertex.MyVertexFormatPositionTextureH(new Vector3(-1, -1, 0),
                    new VRageMath.PackedVector.HalfVector2(0, 1));
                m_vbData[1] = new VRageRender.Vertex.MyVertexFormatPositionTextureH(new Vector3(-1, 1, 0),
                    new VRageMath.PackedVector.HalfVector2(0, 0));
                m_vbData[2] = new VRageRender.Vertex.MyVertexFormatPositionTextureH(new Vector3(0, -1, 0),
                    new VRageMath.PackedVector.HalfVector2(0.5f, 1));
                m_vbData[3] = new VRageRender.Vertex.MyVertexFormatPositionTextureH(new Vector3(0, 1, 0),
                    new VRageMath.PackedVector.HalfVector2(0.5f, 0f));
                MyMapping mapping = MyMapping.MapDiscard(RC, m_VBLeftPart);
                mapping.WriteAndPosition(m_vbData, 4);
                mapping.Unmap();
            }

            {
                m_VBRightPart = MyManagers.Buffers.CreateVertexBuffer(
                    "MyVRScreenPass.VBRightPart", 4, VRageRender.Vertex.MyVertexFormatPositionTextureH.STRIDE,
                    usage: ResourceUsage.Dynamic);
                m_vbData[0] = new VRageRender.Vertex.MyVertexFormatPositionTextureH(new Vector3(0, -1, 0),
                    new VRageMath.PackedVector.HalfVector2(0.5f, 1));
                m_vbData[1] = new VRageRender.Vertex.MyVertexFormatPositionTextureH(new Vector3(0, 1, 0),
                    new VRageMath.PackedVector.HalfVector2(0.5f, 0));
                m_vbData[2] = new VRageRender.Vertex.MyVertexFormatPositionTextureH(new Vector3(1, -1, 0),
                    new VRageMath.PackedVector.HalfVector2(1, 1));
                m_vbData[3] = new VRageRender.Vertex.MyVertexFormatPositionTextureH(new Vector3(1, 1, 0),
                    new VRageMath.PackedVector.HalfVector2(1, 0));
                MyMapping mapping = MyMapping.MapDiscard(RC, m_VBRightPart);
                mapping.WriteAndPosition(m_vbData, 4);
                mapping.Unmap();
            }

            // just some shader bytecode is selected
            m_IL = MyShaders.CreateIL(m_VSCopy.BytecodeId, MyVertexLayouts.GetLayout(MyVertexInputComponentType.POSITION3, MyVertexInputComponentType.TEXCOORD0_H));

        }

        internal static void DrawFullscreenQuad(MyViewport? customViewport = null)
        {
            if (customViewport.HasValue)
                RC.SetViewport(customViewport.Value.OffsetX, customViewport.Value.OffsetY, customViewport.Value.Width, customViewport.Value.Height);
            else
                RC.SetScreenViewport();

            // set vertex buffer:
            if (!MyStereoRender.Enable || MyStereoRender.RenderRegion == MyStereoRegion.FULLSCREEN)
                RC.SetVertexBuffer(0, m_VBFullscreen);
            else if (MyStereoRender.RenderRegion == MyStereoRegion.LEFT)
                RC.SetVertexBuffer(0, m_VBLeftPart);
            else if (MyStereoRender.RenderRegion == MyStereoRegion.RIGHT)
                RC.SetVertexBuffer(0, m_VBRightPart);

            if (MyStereoRender.Enable)
                MyStereoRender.PSBindRawCB_FrameConstants(RC);

            RC.SetPrimitiveTopology(SharpDX.Direct3D.PrimitiveTopology.TriangleStrip);
            RC.SetInputLayout(m_IL);
            RC.VertexShader.Set(m_VSCopy);
            RC.Draw(4, 0);
            RC.SetPrimitiveTopology(SharpDX.Direct3D.PrimitiveTopology.TriangleList);

            if (MyStereoRender.Enable)
                RC.PixelShader.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);
        }

        internal static void RunFullscreenPixelFreq(IRtvBindable RT)
        {
            if (MyRender11.MultisamplingEnabled)
            {
                RC.SetDepthStencilState(MyDepthStencilStateManager.TestEdgeStencil, 0);
            }
            RC.SetRtvs(MyGBuffer.Main.DepthStencil, MyDepthStencilAccess.ReadOnly, RT);
            DrawFullscreenQuad();
            if (MyRender11.MultisamplingEnabled)
            {
                RC.SetDepthStencilState(MyDepthStencilStateManager.DefaultDepthState);
            }
        }

        internal static void RunFullscreenSampleFreq(IRtvBindable RT)
        {
            Debug.Assert(MyRender11.MultisamplingEnabled);
            RC.SetDepthStencilState(MyDepthStencilStateManager.TestEdgeStencil, 0x80);
            RC.SetRtv(MyGBuffer.Main.DepthStencil, MyDepthStencilAccess.ReadOnly, RT);
            DrawFullscreenQuad();
            RC.SetDepthStencilState(MyDepthStencilStateManager.DefaultDepthState);
        }
    }
}
