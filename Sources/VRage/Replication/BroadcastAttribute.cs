using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Network
{
    /// <summary>
    /// Client method. Decorated method is be called by server on all clients.
    /// Clients always trust server and does not perform any validation.
    /// When used together with Server attribute, server (optionally) validates data, invokes the method on server and then sends it to all clients.
    /// If the data is found invalid, it can be marked so by calling MyEventContext.ValidationFailed() on the server. The broadcasts will then not be performed.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class BroadcastAttribute : Attribute
    {
    }
}
