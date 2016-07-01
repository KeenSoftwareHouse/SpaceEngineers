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
using VRageRender.Resources;
using VRageRender.Vertex;
using Buffer = SharpDX.Direct3D11.Buffer;
using Matrix = VRageMath.Matrix;
using Vector3 = VRageMath.Vector3;
using BoundingBox = VRageMath.BoundingBox;
using BoundingFrustum = VRageMath.BoundingFrustum;
using VRage.Collections;
using VRage.OpenVRWrapper;


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

        public static void PSBindRawCB_FrameConstants(MyRenderContext RC)
        {
            if (MyStereoRender.RenderRegion == MyStereoRegion.FULLSCREEN)
                RC.DeviceContext.PixelShader.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);
            else if (MyStereoRender.RenderRegion == MyStereoRegion.LEFT)
                RC.DeviceContext.PixelShader.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstantsStereoLeftEye);
            else if (MyStereoRender.RenderRegion == MyStereoRegion.RIGHT)
                RC.DeviceContext.PixelShader.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstantsStereoRightEye);
        }

        public static void CSBindRawCB_FrameConstants(MyRenderContext RC)
        {
            if (MyStereoRender.RenderRegion == MyStereoRegion.FULLSCREEN)
                RC.DeviceContext.ComputeShader.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);
            else if (MyStereoRender.RenderRegion == MyStereoRegion.LEFT)
                RC.DeviceContext.ComputeShader.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstantsStereoLeftEye);
            else if (MyStereoRender.RenderRegion == MyStereoRegion.RIGHT)
                RC.DeviceContext.ComputeShader.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstantsStereoRightEye);
        }

        public static void VSBindRawCB_FrameConstants(MyRenderContext RC)
        {
            if (MyStereoRender.RenderRegion == MyStereoRegion.FULLSCREEN)
                RC.DeviceContext.VertexShader.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);
            else if (MyStereoRender.RenderRegion == MyStereoRegion.LEFT)
                RC.DeviceContext.VertexShader.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstantsStereoLeftEye);
            else if (MyStereoRender.RenderRegion == MyStereoRegion.RIGHT)
                RC.DeviceContext.VertexShader.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstantsStereoRightEye);
        }
        
        public static void BindRawCB_FrameConstants(MyRenderContext RC)
        {
            if (MyStereoRender.RenderRegion == MyStereoRegion.FULLSCREEN)
            {
                RC.DeviceContext.PixelShader.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);
                RC.DeviceContext.ComputeShader.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);
                RC.DeviceContext.VertexShader.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);
            }
            else if (MyStereoRender.RenderRegion == MyStereoRegion.LEFT)
            {
                RC.DeviceContext.PixelShader.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstantsStereoLeftEye);
                RC.DeviceContext.ComputeShader.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstantsStereoLeftEye);
                RC.DeviceContext.VertexShader.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstantsStereoLeftEye);
            }
            else if (MyStereoRender.RenderRegion == MyStereoRegion.RIGHT)
            {
                RC.DeviceContext.PixelShader.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstantsStereoRightEye);
                RC.DeviceContext.ComputeShader.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstantsStereoRightEye);
                RC.DeviceContext.VertexShader.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstantsStereoRightEye);
            }
        }

        public static void SetViewport(MyRenderContext RC, MyStereoRegion region)
        {
            SharpDX.ViewportF viewport = new SharpDX.ViewportF(0, 0, MyRender11.ViewportResolution.X, MyRender11.ViewportResolution.Y);
            if (region == MyStereoRegion.LEFT)
                viewport = new SharpDX.ViewportF(viewport.X, viewport.Y, viewport.Width / 2, viewport.Height);
            else if (region == MyStereoRegion.RIGHT)
                viewport = new SharpDX.ViewportF(viewport.X + viewport.Width / 2, viewport.Y, viewport.Width / 2, viewport.Height);
            RC.DeviceContext.Rasterizer.SetViewport(viewport);
        }

        // this method set viewport that is related to the current MyStereoRender.RenderRegion flag
        public static void SetViewport(MyRenderContext RC)
        {
            SetViewport(RC, MyStereoRender.RenderRegion);
        }
       
        private static void BeginDrawGBufferPass(MyRenderContext RC)
        {
            SetViewport(RC, MyStereoRegion.LEFT);

            var viewProjTranspose = Matrix.Transpose(EnvMatricesLeftEye.ViewProjectionAt0);
            var mapping = MyMapping.MapDiscard(RC.DeviceContext, MyCommon.ProjectionConstants);
            mapping.WriteAndPosition(ref viewProjTranspose);
            mapping.Unmap();
            RC.SetCB(MyCommon.PROJECTION_SLOT, MyCommon.ProjectionConstants);
            //RC.SetCB(MyCommon.FRAME_SLOT, MyCommon.VRFrameConstantsLeftEye);
        }

        private static void SwitchDrawGBufferPass(MyRenderContext RC)
        {
            SetViewport(RC, MyStereoRegion.RIGHT);

            var viewProjTranspose = Matrix.Transpose(EnvMatricesRightEye.ViewProjectionAt0);
            var mapping = MyMapping.MapDiscard(RC.DeviceContext, MyCommon.ProjectionConstants);
            mapping.WriteAndPosition(ref viewProjTranspose);
            mapping.Unmap();
            RC.SetCB(MyCommon.PROJECTION_SLOT, MyCommon.ProjectionConstants);
            //RC.SetCB(MyCommon.FRAME_SLOT, MyCommon.VRFrameConstantsRightEye);
        }

        private static void EndDrawGBufferPass(MyRenderContext RC)
        {
            SetViewport(RC, MyStereoRegion.FULLSCREEN);

            var viewProjTranspose = Matrix.Transpose(MyRender11.Environment.ViewProjectionAt0);
            var mapping = MyMapping.MapDiscard(RC.DeviceContext, MyCommon.ProjectionConstants);
            mapping.WriteAndPosition(ref viewProjTranspose);
            mapping.Unmap();
            RC.SetCB(MyCommon.PROJECTION_SLOT, MyCommon.ProjectionConstants);
            //RC.SetCB(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);
        }

        internal static void DrawGBufferPass(MyRenderContext RC, int vertexCount, int startVertexLocation)
        {
            BeginDrawGBufferPass(RC);
            RC.DeviceContext.Draw(vertexCount, startVertexLocation);
            SwitchDrawGBufferPass(RC);
            RC.DeviceContext.Draw(vertexCount, startVertexLocation);
            EndDrawGBufferPass(RC);
        }

        internal static void DrawIndexedGBufferPass(MyRenderContext RC, int indexCount, int startIndexLocation, int baseVertexLocation)
        {
            BeginDrawGBufferPass(RC);
            RC.DeviceContext.DrawIndexed(indexCount, startIndexLocation, baseVertexLocation);
            SwitchDrawGBufferPass(RC);
            RC.DeviceContext.DrawIndexed(indexCount, startIndexLocation, baseVertexLocation);
            EndDrawGBufferPass(RC);
        }

        internal static void DrawInstancedGBufferPass(MyRenderContext RC, int vertexCountPerInstance, int instanceCount, int startVertexLocation, int startInstanceLocation)
        {
            BeginDrawGBufferPass(RC);
            RC.DeviceContext.DrawInstanced(vertexCountPerInstance, instanceCount, startVertexLocation, startInstanceLocation);
            SwitchDrawGBufferPass(RC);
            RC.DeviceContext.DrawInstanced(vertexCountPerInstance, instanceCount, startVertexLocation, startInstanceLocation);
            EndDrawGBufferPass(RC);
        }

        internal static void DrawIndexedInstancedGBufferPass(MyRenderContext RC, int indexCountPerInstance, int instanceCount, int startIndexLocation, int baseVertexLocation, int startInstanceLocation)
        {
            BeginDrawGBufferPass(RC);
            RC.DeviceContext.DrawIndexedInstanced(indexCountPerInstance, instanceCount, startIndexLocation, baseVertexLocation, startInstanceLocation);
            SwitchDrawGBufferPass(RC);
            RC.DeviceContext.DrawIndexedInstanced(indexCountPerInstance, instanceCount, startIndexLocation, baseVertexLocation, startInstanceLocation);
            EndDrawGBufferPass(RC);
        }

        internal static void DrawIndexedInstancedIndirectGBufferPass(MyRenderContext RC, Buffer bufferForArgsRef, int alignedByteOffsetForArgs)
        {
            BeginDrawGBufferPass(RC);
            RC.DeviceContext.DrawIndexedInstancedIndirect(bufferForArgsRef, alignedByteOffsetForArgs);
            SwitchDrawGBufferPass(RC);
            RC.DeviceContext.DrawIndexedInstancedIndirect(bufferForArgsRef, alignedByteOffsetForArgs);
            EndDrawGBufferPass(RC);
        }

        internal static void DrawIndexedBillboards(MyRenderContext RC, int indexCount, int startIndexLocation, int baseVertexLocation)
        {
            MyStereoRender.SetViewport(RC, MyStereoRegion.LEFT);
            RC.SetCB(MyCommon.FRAME_SLOT, MyCommon.FrameConstantsStereoLeftEye);
            RC.DeviceContext.DrawIndexed(indexCount, startIndexLocation, baseVertexLocation);
            MyStereoRender.SetViewport(RC, MyStereoRegion.RIGHT);
            RC.SetCB(MyCommon.FRAME_SLOT, MyCommon.FrameConstantsStereoRightEye);
            RC.DeviceContext.DrawIndexed(indexCount, startIndexLocation, baseVertexLocation);
            MyStereoRender.SetViewport(RC, MyStereoRegion.FULLSCREEN);
            RC.SetCB(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);
        }

        internal static void DrawIndexedInstancedIndirectGPUParticles(MyRenderContext RC, Buffer bufferForArgsRef, int alignedByteOffsetForArgs)
        {
            RC.SetCB(MyCommon.FRAME_SLOT, MyCommon.FrameConstantsStereoLeftEye);
            SetViewport(RC, MyStereoRegion.LEFT);
            RC.DeviceContext.DrawIndexedInstancedIndirect(bufferForArgsRef, alignedByteOffsetForArgs);
            RC.SetCB(MyCommon.FRAME_SLOT, MyCommon.FrameConstantsStereoRightEye);
            SetViewport(RC, MyStereoRegion.RIGHT);
            RC.DeviceContext.DrawIndexedInstancedIndirect(bufferForArgsRef, alignedByteOffsetForArgs);
            RC.SetCB(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);
            SetViewport(RC, MyStereoRegion.FULLSCREEN);
        }
    }
}
