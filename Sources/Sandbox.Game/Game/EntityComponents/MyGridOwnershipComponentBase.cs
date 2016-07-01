using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Components;

namespace Sandbox.Game.EntityComponents
{
    public abstract class MyGridOwnershipComponentBase : MyEntityComponentBase
    {
        /// <summary>
        /// Returns the identity id of the block's owner
        /// </summary>
        public abstract long GetBlockOwnerId(MySlimBlock block);

        public override string ComponentTypeDebugString
        {
            get { return "Ownership"; }
        }
    }
}
