using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;
using VRageRender;
using VRage;

namespace VRage.Components
{
    public abstract class MyDebugRenderComponentBase
    {
        public virtual void PrepareForDraw() { }
        public abstract bool DebugDraw();
        public abstract void DebugDrawInvalidTriangles();
    }
}
