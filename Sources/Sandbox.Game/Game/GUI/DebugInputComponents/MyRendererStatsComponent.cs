using Sandbox.Common;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Engine.Voxels;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Sandbox.Graphics;
using Sandbox.Graphics.GUI;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Utils;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.Gui
{
    public class MyRendererStatsComponent : MyDebugComponent
    {

        public override string GetName()
        {
            return "RendererStats";
        }

        static StringBuilder m_frameDebugText = new StringBuilder(1024);
        static StringBuilder m_frameDebugText2 = new StringBuilder(1024);

        public Vector2 GetScreenLeftTopPosition()
        {
            float deltaPixels = 25 * MyGuiManager.GetSafeScreenScale();
            Rectangle fullscreenRectangle = MyGuiManager.GetSafeFullscreenRectangle();
            return MyGuiManager.GetNormalizedCoordinateFromScreenCoordinate_FULLSCREEN(new Vector2(deltaPixels, deltaPixels));
        }

        public override void Draw()
        {
            //base.Draw();

            //m_frameDebugText.Clear();
            //m_frameDebugText2.Clear();

            //m_frameDebugText.AppendFormat("RenderableObjects: {0: #,0}", MyPerformanceCounter.PerCameraDraw11Read.RenderableObjectsNum);
            //m_frameDebugText.AppendLine();
            //m_frameDebugText.AppendFormat("ViewFrustumObjects: {0: #,0}", MyPerformanceCounter.PerCameraDraw11Read.ViewFrustumObjectsNum);
            //m_frameDebugText.AppendLine();

            //for (int cascadeIndex = 0; cascadeIndex < MyRenderProxy.Settings.ShadowCascadeCount; ++cascadeIndex)
            //{
            //    m_frameDebugText.AppendFormat("Cascade" + cascadeIndex.ToString() + " Objects: {0: #,0}", MyPerformanceCounter.PerCameraDraw11Read.ShadowCascadeObjectsNum[cascadeIndex]);
            //    m_frameDebugText.AppendLine();
            //}

            //m_frameDebugText.AppendFormat("MeshesDrawn: {0: #,0}", MyPerformanceCounter.PerCameraDraw11Read.MeshesDrawn);
            //m_frameDebugText.AppendLine();
            //m_frameDebugText.AppendFormat("SubmeshesDrawn: {0: #,0}", MyPerformanceCounter.PerCameraDraw11Read.SubmeshesDrawn);
            //m_frameDebugText.AppendLine();
            //m_frameDebugText.AppendFormat("BillboardsDrawn: {0: #,0}", MyPerformanceCounter.PerCameraDraw11Read.BillboardsDrawn);
            //m_frameDebugText.AppendLine();
            //m_frameDebugText.AppendFormat("ObjectConstantsChanges: {0: #,0}", MyPerformanceCounter.PerCameraDraw11Read.ObjectConstantsChanges);
            //m_frameDebugText.AppendLine();
            //m_frameDebugText.AppendFormat("MaterialConstantsChanges: {0: #,0}", MyPerformanceCounter.PerCameraDraw11Read.MaterialConstantsChanges);
            //m_frameDebugText.AppendLine();
            //m_frameDebugText.AppendFormat("Triangles: {0: #,0}", MyPerformanceCounter.PerCameraDraw11Read.TrianglesDrawn);
            //m_frameDebugText.AppendLine();
            //for (int cascadeIndex = 0; cascadeIndex < MyRenderProxy.Settings.ShadowCascadeCount; ++cascadeIndex)
            //{
            //    m_frameDebugText.AppendFormat("Cascade" + cascadeIndex.ToString() + " Triangles: {0: #,0}", MyPerformanceCounter.PerCameraDraw11Read.ShadowCascadeTriangles[cascadeIndex]);
            //    m_frameDebugText.AppendLine();
            //}
            //m_frameDebugText.AppendFormat("Other shadow Triangles: {0: #,0}", MyPerformanceCounter.PerCameraDraw11Read.OtherShadowTriangles);
            //m_frameDebugText.AppendLine();

            //m_frameDebugText.AppendFormat("Instances: {0: #,0}", MyPerformanceCounter.PerCameraDraw11Read.InstancesDrawn);

            //m_frameDebugText2.AppendFormat("DrawIndexed: {0}", MyPerformanceCounter.PerCameraDraw11Read.DrawIndexed);
            //m_frameDebugText2.AppendLine();
            //m_frameDebugText2.AppendFormat("DrawIndexedInstanced: {0: #,0}", MyPerformanceCounter.PerCameraDraw11Read.DrawIndexedInstanced);
            //m_frameDebugText2.AppendLine();
            //m_frameDebugText2.AppendFormat("DrawAuto: {0: #,0}", MyPerformanceCounter.PerCameraDraw11Read.DrawAuto);
            //m_frameDebugText2.AppendLine();
            //m_frameDebugText2.AppendFormat("Shadow DrawIndexed: {0: #,0}", MyPerformanceCounter.PerCameraDraw11Read.ShadowDrawIndexed);
            //m_frameDebugText2.AppendLine();
            //m_frameDebugText2.AppendFormat("Shadow DrawIndexedInstanced: {0: #,0}", MyPerformanceCounter.PerCameraDraw11Read.ShadowDrawIndexedInstanced);
            //m_frameDebugText2.AppendLine();
            //m_frameDebugText2.AppendFormat("Billboard DrawCalls: {0: #,0}", MyPerformanceCounter.PerCameraDraw11Read.BillboardDrawCalls);
            //m_frameDebugText2.AppendLine();
            //m_frameDebugText2.AppendFormat("SetVB: {0: #,0}", MyPerformanceCounter.PerCameraDraw11Read.SetVB);
            //m_frameDebugText2.AppendLine();
            //m_frameDebugText2.AppendFormat("SetIB: {0: #,0}", MyPerformanceCounter.PerCameraDraw11Read.SetIB);
            //m_frameDebugText2.AppendLine();
            //m_frameDebugText2.AppendFormat("BindShaderResources: {0: #,0}", MyPerformanceCounter.PerCameraDraw11Read.BindShaderResources);
            //m_frameDebugText2.AppendLine();
            //m_frameDebugText2.AppendFormat("SetIL: {0: #,0}", MyPerformanceCounter.PerCameraDraw11Read.SetIL);
            //m_frameDebugText2.AppendLine();
            //m_frameDebugText2.AppendFormat("SetVS: {0: #,0}", MyPerformanceCounter.PerCameraDraw11Read.SetVS);
            //m_frameDebugText2.AppendLine();
            //m_frameDebugText2.AppendFormat("SetPS: {0: #,0}", MyPerformanceCounter.PerCameraDraw11Read.SetPS);
            //m_frameDebugText2.AppendLine();
            //m_frameDebugText2.AppendFormat("SetCB: {0: #,0}", MyPerformanceCounter.PerCameraDraw11Read.SetCB);
            //m_frameDebugText2.AppendLine();
            //m_frameDebugText2.AppendFormat("SetPS: {0: #,0}", MyPerformanceCounter.PerCameraDraw11Read.SetPS);
            //m_frameDebugText2.AppendLine();
            //m_frameDebugText2.AppendFormat("SetRasterizerState: {0: #,0}", MyPerformanceCounter.PerCameraDraw11Read.SetRasterizerState);
            //m_frameDebugText2.AppendLine();
            //m_frameDebugText2.AppendFormat("SetBlendState: {0: #,0}", MyPerformanceCounter.PerCameraDraw11Read.SetBlendState);
            //m_frameDebugText2.AppendLine();

            //Vector2 origin = GetScreenLeftTopPosition();
            //Vector2 horizontalOffset = origin + new Vector2(0.5f, 0.0f);
            //MyGuiManager.DrawString(MyFontEnum.White, m_frameDebugText, origin, 1);
            //MyGuiManager.DrawString(MyFontEnum.White, m_frameDebugText2, origin + horizontalOffset, 1);
        }
    }
}
