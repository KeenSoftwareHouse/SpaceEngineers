using System;
using System.Text;
using System.Collections.Generic;
using VRageMath;
using System.Diagnostics;
using System.Threading;
using VRage.Profiler;
using System.Runtime.CompilerServices;
using VRage;

namespace VRageRender.Profiler
{
#if !XB1 // XB1_NOPROFILER
    public class MyRenderProfilerDX9 : MyRenderProfilerRendering
    {
        MyLineBatch m_lineBatch;

        public override Vector2 ViewportSize
        {
            get { return new Vector2(MyRender.GraphicsDevice.Viewport.Width, MyRender.GraphicsDevice.Viewport.Height); }
        }

        public override Vector2 MeasureText(StringBuilder text, float scale)
        {
            return MyRender.MeasureText(text, scale);
        }

        public override float DrawText(Vector2 screenCoord, StringBuilder text, Color color, float scale)
        {
            return MyRender.DrawText(screenCoord, text, color, scale);
        }

        public override float DrawTextShadow(Vector2 screenCoord, StringBuilder text, Color color, float scale)
        {
            return MyRender.DrawTextShadow(screenCoord, text, color, scale);
        }

        public override void DrawOnScreenLine(Vector3 v0, Vector3 v1, Color color)
        {
            m_lineBatch.DrawOnScreenLine(v0, v1, color);
        }

        public override void Init()
        {
            SharpDX.Direct3D9.Device device = MyRender.GraphicsDevice;
            m_lineBatch = new MyLineBatch(Matrix.Identity, Matrix.CreateOrthographicOffCenter(0, device.Viewport.Width, device.Viewport.Height, 0, 0, -1), 50000);
        }

        public override void BeginLineBatch()
        {
            m_lineBatch.Begin();
        }

        public override void EndLineBatch()
        {
            VRageRender.Graphics.BlendState.Opaque.Apply();
            m_lineBatch.End();
        }
    }
#else // XB1
    public class MyRenderProfilerDX9 : MyRenderProfilerRendering
    {
    }
#endif // XB1
}
