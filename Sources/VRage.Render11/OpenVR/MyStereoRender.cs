using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Library.Collections;
using VRage.Utils;
using VRage.Generics;
using VRageMath;
using VRageMath.PackedVector;

using VRageRender.Vertex;
using Buffer = SharpDX.Direct3D11.Buffer;
using Matrix = VRageMath.Matrix;
using Vector3 = VRageMath.Vector3;
using BoundingBox = VRageMath.BoundingBox;
using BoundingFrustum = VRageMath.BoundingFrustum;
using VRage.Collections;
using VRage.OpenVRWrapper;
using VRage.Render11.RenderContext;
using VRage.Render11.Resources;


namespace VRageRender
{
    internal enum MyStereoRegion
    {
        FULLSCREEN,
        LEFT,
        RIGHT
    }
      
    class MyStereoRender
    {
        internal static bool Enable { get { return MyRender11.DeviceSettings.UseStereoRendering; } }
        internal static bool EnableUsingStencilMask { get { return true; } }

        internal static MyEnvironmentMatrices EnvMatricesLeftEye = new MyEnvironmentMatrices();
        internal static MyEnvironmentMatrices EnvMatricesRightEye = new MyEnvironmentMatrices();

        internal static MyStereoRegion RenderRegion = MyStereoRegion.FULLSCREEN;

        public static void PSBindRawCB_FrameConstants(MyRenderContext rc)
        {
            if (MyStereoRender.RenderRegion == MyStereoRegion.FULLSCREEN)
                rc.PixelShader.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);
            else if (MyStereoRender.RenderRegion == MyStereoRegion.LEFT)
                rc.PixelShader.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstantsStereoLeftEye);
            else if (MyStereoRender.RenderRegion == MyStereoRegion.RIGHT)
                rc.PixelShader.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstantsStereoRightEye);
        }

        public static void CSBindRawCB_FrameConstants(MyRenderContext rc)
        {
            if (MyStereoRender.RenderRegion == MyStereoRegion.FULLSCREEN)
                rc.ComputeShader.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);
            else if (MyStereoRender.RenderRegion == MyStereoRegion.LEFT)
                rc.ComputeShader.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstantsStereoLeftEye);
            else if (MyStereoRender.RenderRegion == MyStereoRegion.RIGHT)
                rc.ComputeShader.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstantsStereoRightEye);
        }

        public static void VSBindRawCB_FrameConstants(MyRenderContext rc)
        {
            if (MyStereoRender.RenderRegion == MyStereoRegion.FULLSCREEN)
                rc.VertexShader.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);
            else if (MyStereoRender.RenderRegion == MyStereoRegion.LEFT)
                rc.VertexShader.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstantsStereoLeftEye);
            else if (MyStereoRender.RenderRegion == MyStereoRegion.RIGHT)
                rc.VertexShader.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstantsStereoRightEye);
        }
        
        public static void BindRawCB_FrameConstants(MyRenderContext rc)
        {
            if (MyStereoRender.RenderRegion == MyStereoRegion.FULLSCREEN)
            {
                rc.PixelShader.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);
                rc.ComputeShader.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);
                rc.VertexShader.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);
            }
            else if (MyStereoRender.RenderRegion == MyStereoRegion.LEFT)
            {
                rc.PixelShader.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstantsStereoLeftEye);
                rc.ComputeShader.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstantsStereoLeftEye);
                rc.VertexShader.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstantsStereoLeftEye);
            }
            else if (MyStereoRender.RenderRegion == MyStereoRegion.RIGHT)
            {
                rc.PixelShader.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstantsStereoRightEye);
                rc.ComputeShader.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstantsStereoRightEye);
                rc.VertexShader.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstantsStereoRightEye);
            }
        }

        public static void SetViewport(MyRenderContext rc, MyStereoRegion region)
        {
            SharpDX.ViewportF viewport = new SharpDX.ViewportF(0, 0, MyRender11.ViewportResolution.X, MyRender11.ViewportResolution.Y);
            if (region == MyStereoRegion.LEFT)
                viewport = new SharpDX.ViewportF(viewport.X, viewport.Y, viewport.Width / 2, viewport.Height);
            else if (region == MyStereoRegion.RIGHT)
                viewport = new SharpDX.ViewportF(viewport.X + viewport.Width / 2, viewport.Y, viewport.Width / 2, viewport.Height);
            rc.SetViewport(viewport);
        }

        // this method set viewport that is related to the current MyStereoRender.RenderRegion flag
        public static void SetViewport(MyRenderContext rc)
        {
            SetViewport(rc, MyStereoRender.RenderRegion);
        }
       
        private static void BeginDrawGBufferPass(MyRenderContext rc)
        {
            SetViewport(rc, MyStereoRegion.LEFT);

            var viewProjTranspose = Matrix.Transpose(EnvMatricesLeftEye.ViewProjectionAt0);
            var mapping = MyMapping.MapDiscard(rc, MyCommon.ProjectionConstants);
            mapping.WriteAndPosition(ref viewProjTranspose);
            mapping.Unmap();
            rc.AllShaderStages.SetConstantBuffer(MyCommon.PROJECTION_SLOT, MyCommon.ProjectionConstants);
            //RC.AllShaderStages.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.VRFrameConstantsLeftEye);
        }

        private static void SwitchDrawGBufferPass(MyRenderContext rc)
        {
            SetViewport(rc, MyStereoRegion.RIGHT);

            var viewProjTranspose = Matrix.Transpose(EnvMatricesRightEye.ViewProjectionAt0);
            var mapping = MyMapping.MapDiscard(rc, MyCommon.ProjectionConstants);
            mapping.WriteAndPosition(ref viewProjTranspose);
            mapping.Unmap();
            rc.AllShaderStages.SetConstantBuffer(MyCommon.PROJECTION_SLOT, MyCommon.ProjectionConstants);
            //RC.AllShaderStages.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.VRFrameConstantsRightEye);
        }

        private static void EndDrawGBufferPass(MyRenderContext rc)
        {
            SetViewport(rc, MyStereoRegion.FULLSCREEN);

            var viewProjTranspose = Matrix.Transpose(MyRender11.Environment.Matrices.ViewProjectionAt0);
            var mapping = MyMapping.MapDiscard(rc, MyCommon.ProjectionConstants);
            mapping.WriteAndPosition(ref viewProjTranspose);
            mapping.Unmap();
            rc.AllShaderStages.SetConstantBuffer(MyCommon.PROJECTION_SLOT, MyCommon.ProjectionConstants);
            //RC.AllShaderStages.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);
        }

        internal static void DrawGBufferPass(MyRenderContext rc, int vertexCount, int startVertexLocation)
        {
            BeginDrawGBufferPass(rc);
            rc.Draw(vertexCount, startVertexLocation);
            SwitchDrawGBufferPass(rc);
            rc.Draw(vertexCount, startVertexLocation);
            EndDrawGBufferPass(rc);
        }

        internal static void DrawIndexedGBufferPass(MyRenderContext rc, int indexCount, int startIndexLocation, int baseVertexLocation)
        {
            BeginDrawGBufferPass(rc);
            rc.DrawIndexed(indexCount, startIndexLocation, baseVertexLocation);
            SwitchDrawGBufferPass(rc);
            rc.DrawIndexed(indexCount, startIndexLocation, baseVertexLocation);
            EndDrawGBufferPass(rc);
        }

        internal static void DrawInstancedGBufferPass(MyRenderContext rc, int vertexCountPerInstance, int instanceCount, int startVertexLocation, int startInstanceLocation)
        {
            BeginDrawGBufferPass(rc);
            rc.DrawInstanced(vertexCountPerInstance, instanceCount, startVertexLocation, startInstanceLocation);
            SwitchDrawGBufferPass(rc);
            rc.DrawInstanced(vertexCountPerInstance, instanceCount, startVertexLocation, startInstanceLocation);
            EndDrawGBufferPass(rc);
        }

        internal static void DrawIndexedInstancedGBufferPass(MyRenderContext rc, int indexCountPerInstance, int instanceCount, int startIndexLocation, int baseVertexLocation, int startInstanceLocation)
        {
            BeginDrawGBufferPass(rc);
            rc.DrawIndexedInstanced(indexCountPerInstance, instanceCount, startIndexLocation, baseVertexLocation, startInstanceLocation);
            SwitchDrawGBufferPass(rc);
            rc.DrawIndexedInstanced(indexCountPerInstance, instanceCount, startIndexLocation, baseVertexLocation, startInstanceLocation);
            EndDrawGBufferPass(rc);
        }

        internal static void DrawIndexedInstancedIndirectGBufferPass(MyRenderContext rc, IBuffer bufferForArgsRef, int alignedByteOffsetForArgs)
        {
            BeginDrawGBufferPass(rc);
            rc.DrawIndexedInstancedIndirect(bufferForArgsRef, alignedByteOffsetForArgs);
            SwitchDrawGBufferPass(rc);
            rc.DrawIndexedInstancedIndirect(bufferForArgsRef, alignedByteOffsetForArgs);
            EndDrawGBufferPass(rc);
        }

        internal static void DrawIndexedBillboards(MyRenderContext rc, int indexCount, int startIndexLocation, int baseVertexLocation)
        {
            MyStereoRender.SetViewport(rc, MyStereoRegion.LEFT);
            rc.AllShaderStages.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstantsStereoLeftEye);
            rc.DrawIndexed(indexCount, startIndexLocation, baseVertexLocation);
            MyStereoRender.SetViewport(rc, MyStereoRegion.RIGHT);
            rc.AllShaderStages.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstantsStereoRightEye);
            rc.DrawIndexed(indexCount, startIndexLocation, baseVertexLocation);
            MyStereoRender.SetViewport(rc, MyStereoRegion.FULLSCREEN);
            rc.AllShaderStages.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);
        }

        internal static void DrawIndexedInstancedIndirectGPUParticles(MyRenderContext rc, IBuffer bufferForArgsRef, int alignedByteOffsetForArgs)
        {
            rc.AllShaderStages.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstantsStereoLeftEye);
            SetViewport(rc, MyStereoRegion.LEFT);
            rc.DrawIndexedInstancedIndirect(bufferForArgsRef, alignedByteOffsetForArgs);
            rc.AllShaderStages.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstantsStereoRightEye);
            SetViewport(rc, MyStereoRegion.RIGHT);
            rc.DrawIndexedInstancedIndirect(bufferForArgsRef, alignedByteOffsetForArgs);
            rc.AllShaderStages.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);
            SetViewport(rc, MyStereoRegion.FULLSCREEN);
        }
    }
}
