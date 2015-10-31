using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Network
{
    /// <summary>
    /// Base interface for networked object.
    /// Derived interfaces are so far IMyReplicable and IMyStateGroup.
    /// </summary>
    public interface IMyNetObject : IMyEventOwner
    {
    }
}
