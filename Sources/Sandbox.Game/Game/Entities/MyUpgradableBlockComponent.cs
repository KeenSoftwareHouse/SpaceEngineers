using System.Collections.Generic;
using System.Diagnostics;
using Sandbox.Game.GameSystems.Conveyors;

namespace Sandbox.Game.Entities
{
    public class MyUpgradableBlockComponent
    {
        public HashSet<ConveyorLinePosition> ConnectionPositions
        {
            get;
            private set;
        }

        public MyUpgradableBlockComponent(MyCubeBlock parent)
        {
            Debug.Assert(parent != null);

            ConnectionPositions = new HashSet<ConveyorLinePosition>();
            Refresh(parent);
        }

        public void Refresh(MyCubeBlock parent)
        {
            if (parent.BlockDefinition.Model == null)
            {
                return;
            }

            ConnectionPositions.Clear();
            var positions = MyMultilineConveyorEndpoint.GetLinePositions(parent, "detector_upgrade");
            foreach (var position in positions)
            {
                ConnectionPositions.Add(MyMultilineConveyorEndpoint.PositionToGridCoords(position, parent));
            }
        }
    }
}