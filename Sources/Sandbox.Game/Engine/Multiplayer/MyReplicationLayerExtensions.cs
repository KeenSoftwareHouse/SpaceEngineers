using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using VRage.Network;
using VRage.Plugins;
#if XB1 // XB1_ALLINONEASSEMBLY
using VRage.Utils;
#endif // XB1

namespace Sandbox.Engine.Multiplayer
{
    public static class MyReplicationLayerExtensions
    {
        public static void RegisterFromGameAssemblies(this MyReplicationLayerBase layer)
        {
#if XB1 // XB1_ALLINONEASSEMBLY
            layer.RegisterFromAssembly(MyAssembly.AllInOneAssembly);
#else // !XB1
            var assemblies = new Assembly[] { typeof(MySandboxGame).Assembly, MyPlugins.GameAssembly, MyPlugins.SandboxAssembly, MyPlugins.SandboxGameAssembly, MyPlugins.UserAssembly };
            layer.RegisterFromAssembly(assemblies.Where(s => s != null).Distinct());
#endif // !XB1
        }
    }
}
