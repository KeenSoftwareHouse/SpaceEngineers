using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Game.GameSystems
{
    /// <summary>
    /// Sometimes multiple IMyOxygenConsumers and/or IMyOxygenProducers may be linked
    /// by a shared space (or other entity) from which they insert/extract oxygen. In such
    /// cases, they should all return the same instance of an object implementing this interface
    /// so that the common resource can be handled properly.
    /// 
    /// The quintessential example of this is air vents, which multiple can be attached
    /// to same room.
    /// </summary>
    public interface IMyOxygenSharedSpace
    {
        /// <summary>
        /// The maximum amount of oxygen this space can hold.
        /// </summary>
        double MaxCapacity
        {
            get;
        }

        /// <summary>
        /// The current amount of oxygen this space is holding.
        /// </summary>
        double CurrentFill
        {
            get;
        }
    }
}
