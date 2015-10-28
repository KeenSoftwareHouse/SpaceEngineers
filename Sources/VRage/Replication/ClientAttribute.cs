using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Network
{
    /// <summary>
    /// Client method. Decorated method is be called by server on client.
    /// Clients always trust server and does not perform any validation.
    /// When used together with Server attribute, server validates data, invokes the method on server and then sends it to client who invoked it on server.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class ClientAttribute : Attribute
    {
    }
}
