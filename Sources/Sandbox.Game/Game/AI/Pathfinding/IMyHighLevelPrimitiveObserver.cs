using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Game.AI.Pathfinding
{
    public interface IMyHighLevelPrimitiveObserver
    {
        // Will be called when the observed object was changed or removed and thus this observer should not be valid anymore
        void Invalidate();
    }
}
