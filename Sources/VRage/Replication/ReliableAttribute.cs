using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Network
{
    /// <summary>
    /// Event which is sent reliably, use with caution and only when necessary!
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class ReliableAttribute : Attribute
    {
    }
}
