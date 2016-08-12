using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.Components;

namespace Sandbox.Game.Entities.Cube
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class MyCubeGrids : MySessionComponentBase
    {
        public static event Action<MyCubeGrid, MySlimBlock> BlockBuilt;

        internal static void NotifyBlockBuilt(MyCubeGrid grid, MySlimBlock block)
        {
            /*if (BlockBuilt != null)
                BlockBuilt(grid, block);*/
        }

        protected override void UnloadData()
        {
            base.UnloadData();

            BlockBuilt = null;
        }
    }
}
