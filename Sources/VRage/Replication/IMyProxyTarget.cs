using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Network
{
    public interface IMyProxyTarget : IMyNetObject
    {
        /// <summary>
        /// Gets target object.
        /// </summary>
        IMyEventProxy Target { get; }
    }
}
