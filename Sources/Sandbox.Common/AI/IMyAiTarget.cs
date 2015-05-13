using Sandbox.Common.ObjectBuilders.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Common.AI
{
    public interface IMyAiTarget 
    {
        void Init(MyObjectBuilder_AiTarget builder);
        MyObjectBuilder_AiTarget GetObjectBuilder();
        void UnsetTarget();
        void DebugDraw();
        void Cleanup();
        void Update();
    }
}
