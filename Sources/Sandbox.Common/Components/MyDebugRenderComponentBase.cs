using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;
using VRageRender;
using Sandbox.Engine;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRage;

namespace Sandbox.Common.Components
{
    public abstract class MyDebugRenderComponentBase
    {
        public virtual void PrepareForDraw() { }
        public abstract bool DebugDraw();
        public abstract void DebugDrawInvalidTriangles();
    }
}
