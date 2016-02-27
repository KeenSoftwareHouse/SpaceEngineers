using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Network
{

    /// <summary>
    /// Indicates that event will be blocking all other events.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class BlockingAttribute : Attribute
    {
        /// <summary>
        /// Creates attribute that indicates that event will be blocking all other events until this is resolved.
        /// </summary>
        public BlockingAttribute()
        {
        }
    }
}
