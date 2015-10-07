﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Profiler;
using VRageMath;
using VRageMath.PackedVector;
using VRageRender.Vertex;

namespace VRageRender.Profiler
{
    class MyRenderProfilerDX11 : MyRenderProfilerRendering
    {
        private MyRenderProfilerLineBatch m_lineBatch;

        public override Vector2 ViewportSize
        {
            get { return MyRender11.ViewportResolution; }
        }

        public override Vector2 MeasureText(StringBuilder text, float scale)
        {
            return MySpritesRenderer.MeasureText(text, scale);
        }

        public override float DrawText(Vector2 screenCoord, StringBuilder text, Color color, float scale)
        {
            return MySpritesRenderer.DrawText(screenCoord, text, color, scale);
        }

        public override float DrawTextShadow(Vector2 screenCoord, StringBuilder text, Color color, float scale)
        {
            return MySpritesRenderer.DrawTextShadow(screenCoord, text, color, scale);
        }

        public override void DrawOnScreenLine(Vector3 v0, Vector3 v1, Color color)
        {
            m_lineBatch.DrawOnScreenLine(v0, v1, color);
        }

        public override void Init()
        {
            m_lineBatch = new MyRenderProfilerLineBatch(Matrix.Identity, Matrix.CreateOrthographicOffCenter(0, ViewportSize.X, ViewportSize.Y, 0, 0, -1), 50000);
        }

        public override void BeginLineBatch()
        {
            m_lineBatch.Begin();
        }

        public override void EndLineBatch()
        {
            //VRageRender.Graphics.BlendState.Opaque.Apply();
            m_lineBatch.End();

            //GetRenderProfiler().StartProfilingBlock("MySpritesRenderer.Draw");
            MyLinesRenderer.Draw(null);
            MyCommon.UpdateFrameConstants();
            MySpritesRenderer.Draw(MyRender11.Backbuffer.m_RTV, new MyViewport(ViewportSize.X, ViewportSize.Y));
            //GetRenderProfiler().EndProfilingBlock();

            //GetRenderProfiler().StartProfilingBlock("MyLinesRenderer.Draw");

            //GetRenderProfiler().EndProfilingBlock();
        }
    }
}