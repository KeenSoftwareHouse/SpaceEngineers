using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sandbox.Game.AI.Pathfinding
{
    public static class MyCestmirPathfindingShorts
    {
        public static MyPathfinding Pathfinding
        {
            get
            {
                return MyAIComponent.Static.Pathfinding as MyPathfinding;
            }
        }
    }
}
