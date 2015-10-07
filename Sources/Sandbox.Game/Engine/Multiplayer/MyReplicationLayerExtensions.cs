using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using VRage.Network;
using VRage.Plugins;

namespace Sandbox.Engine.Multiplayer
{
    public static class MyReplicationLayerExtensions
    {
        public static void RegisterFromGameAssemblies(this MyReplicationLayerBase layer)
        {
            var assemblies = new Assembly[] { typeof(MySandboxGame).Assembly, MyPlugins.GameAssembly, MyPlugins.SandboxAssembly, MyPlugins.SandboxGameAssembly, MyPlugins.UserAssembly };
            layer.RegisterFromAssembly(assemblies.Where(s => s != null).Distinct());
        }
    }
}
