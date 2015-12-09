using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.Game.AI.Navigation
{
    public abstract class MySteeringBase
    {
        public float Weight { get; protected set; }
        public MyBotNavigation Parent { get; private set; }

        public MySteeringBase(MyBotNavigation parent, float weight)
        {
            Weight = weight;
            Parent = parent;
        }

        public abstract void AccumulateCorrection(ref Vector3 correction, ref float weight);
        public virtual void Update() { }
        public virtual void Cleanup() { }

        public abstract string GetName();

        [Conditional("DEBUG")]
        public virtual void DebugDraw() { }
    }
}
