using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Network
{
    /// <summary>
    /// Marks class which should be never replicated.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class NotReplicableAttribute : Attribute
    {
    }
}
