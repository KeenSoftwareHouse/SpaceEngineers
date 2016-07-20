using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using VRageRender.Resources;
using Vector2 = VRageMath.Vector2;
using Color = VRageMath.Color;
using Matrix = VRageMath.Matrix;
using VRageRender.Vertex;
using VRageMath.PackedVector;
using VRage;
using VRage.Utils;
using System.Diagnostics;
using System;
using VRage.OpenVRWrapper;

namespace VRageRender
{

    class MyStereoStencilMask : MyImmediateRC
    {
        static Vector2[] m_VBdata;
        static VertexBufferId m_VB = VertexBufferId.NULL;

        static VertexShaderId m_vs;
        static PixelShaderId m_ps;
        static InputLayoutId m_il;

        private static void InitInternal(Vector2[] vertsForMask)
        {
            m_VB = MyHwBuffers.CreateVertexBuffer(vertsForMask.Length, MyVertexFormat2DPosition.STRIDE, BindFlags.VertexBuffer, ResourceUsage.Dynamic, null, "MyStereoStencilMask.VB");
            MyMapping mapping = MyMapping.MapDiscard(RC.DeviceContext, m_VB.Buffer);
            mapping.WriteAndPosition(vertsForMask, 0, vertsForMask.Length);
            mapping.Unmap();

            m_vs = MyShaders.CreateVs("stereo_stencil_mask.hlsl");
            m_ps = MyShaders.CreatePs("stereo_stencil_mask.hlsl");

            m_il = MyShaders.CreateIL(m_vs.BytecodeId, MyVertexLayouts.GetLayout(MyVertexInputComponentType.POSITION2));
        }

        static Vector2[] m_tmpInitUndefinedMaskVerts = new Vector2 [6];
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
            RC.BindGBufferForWrite(MyGBuffer.Main, DepthStencilAccess.ReadWrite);
            RC.SetupScreenViewport();
            RC.DeviceContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            RC.SetVB(0, m_VB.Buffer, m_VB.Stride);
            RC.SetIL(m_il);

            RC.SetRS(MyRender11.m_nocullRasterizerState);
            RC.SetDS(MyDepthStencilState.StereoStereoStencilMask, MyDepthStencilState.GetStereoMask());

            RC.SetVS(m_vs);
            RC.SetPS(m_ps);

            RC.DeviceContext.Draw(m_VBdata.Length, 0);
        }
    }
}
