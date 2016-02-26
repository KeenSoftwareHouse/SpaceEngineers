using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Network
{
    /// <summary>
    /// Interface which only marks class as event owner.
    /// Object itself must be replicated in network to allow raising events.
    /// If you considering to add this to object, it's probably wrong and you should use static events in most cases.
    /// This is commonly implemented only by entities which has it's external replicable.
    /// </summary>
    public interface IMyEventProxy : IMyEventOwner
    {
    }
}
