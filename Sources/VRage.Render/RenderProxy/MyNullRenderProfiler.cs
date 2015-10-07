using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageRender.Profiler;

namespace VRageRender.RenderProxy
{
    class MyNullRenderProfiler : MyRenderProfiler
    {
        protected override void Draw(VRage.Profiler.MyProfiler drawProfiler, int lastFrameIndex, int frameToDraw)
        {
        }
    }
}
