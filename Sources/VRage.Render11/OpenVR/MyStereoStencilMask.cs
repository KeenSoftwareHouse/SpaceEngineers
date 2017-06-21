using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using Vector2 = VRageMath.Vector2;
using VRageRender.Vertex;
using VRage.OpenVRWrapper;
using VRage.Render11.Common;
using VRage.Render11.RenderContext;
using VRage.Render11.Resources;

namespace VRageRender
{

    class MyStereoStencilMask : MyImmediateRC
    {
        static Vector2[] m_VBdata;
        static IVertexBuffer m_VB;

        static VertexShaderId m_vs;
        static PixelShaderId m_ps;
        static InputLayoutId m_il;

        private static void InitInternal(Vector2[] vertsForMask)
        {
            m_VB = MyManagers.Buffers.CreateVertexBuffer(
                "MyStereoStencilMask.VB", vertsForMask.Length, MyVertexFormat2DPosition.STRIDE, 
                usage: ResourceUsage.Dynamic);
            MyMapping mapping = MyMapping.MapDiscard(RC, m_VB);
            mapping.WriteAndPosition(vertsForMask, vertsForMask.Length);
            mapping.Unmap();

            m_vs = MyShaders.CreateVs("Stereo/StereoStencilMask.hlsl");
            m_ps = MyShaders.CreatePs("Stereo/StereoStencilMask.hlsl");

            m_il = MyShaders.CreateIL(m_vs.BytecodeId, MyVertexLayouts.GetLayout(MyVertexInputComponentType.POSITION2));
        }

        static readonly Vector2[] m_tmpInitUndefinedMaskVerts = new Vector2 [6];
        private static Vector2[] GetUndefinedMask()
        {
            // this offset represents percent of the screen, that are not displayed in the OpenVR
            float offset = 0.17f;
            Vector2 topLeft = new Vector2(-offset, 1);
            Vector2 topRight = new Vector2(offset, 1);
            Vector2 bottomLeft = new Vector2(-offset, -1);
            Vector2 bottomRight = new Vector2(offset, -1);

            Vector2[] verts = m_tmpInitUndefinedMaskVerts; 
            verts[0] = topLeft;
            verts[1] = topRight;
            verts[2] = bottomLeft;
            verts[3] = bottomLeft;
            verts[4] = topRight;
            verts[5] = bottomRight;

            return verts;
        }

        // This function should be used only for debug
        internal static void InitUsingUndefinedMask()
        {
            m_VBdata = GetUndefinedMask();
            InitInternal(m_VBdata);
        }

        internal static void InitUsingOpenVR()
        {
            m_VBdata = MyOpenVR.GetStencilMask();
            InitInternal(m_VBdata);
        }
        
        internal static void Draw()
        {
            RC.SetRtvs(MyGBuffer.Main, MyDepthStencilAccess.ReadWrite);
            RC.SetScreenViewport();
            RC.SetPrimitiveTopology(PrimitiveTopology.TriangleList);
            RC.SetVertexBuffer(0, m_VB);
            RC.SetInputLayout(m_il);

            RC.SetRasterizerState(MyRasterizerStateManager.NocullRasterizerState);
            RC.SetDepthStencilState(MyDepthStencilStateManager.StereoStencilMask, MyDepthStencilStateManager.GetStereoMask());

            RC.VertexShader.Set(m_vs);
            RC.PixelShader.Set(m_ps);

            RC.Draw(m_VBdata.Length, 0);
        }
    }
}
