#if !XB1
using Sandbox.Game.Entities;
using Sandbox.Game.Replication;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using VRage.Network;

namespace Sandbox.Engine.Multiplayer
{
    [PreloadRequired]
    public class MyServerDebugCommands
    {
        static char[] m_separators = new char[] { ' ', '\r', '\n' };
        static Dictionary<string, Action<string[]>> m_commands = new Dictionary<string, Action<string[]>>(StringComparer.InvariantCultureIgnoreCase);
        static ulong m_commandAuthor;

        static MyReplicationServer Replication { get { return (MyReplicationServer)MyMultiplayer.Static.ReplicationLayer; } }

        static MyServerDebugCommands()
        {
            foreach (var method in typeof(MyServerDebugCommands).GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                var attr = method.GetCustomAttribute<DisplayNameAttribute>();
                var par = method.GetParameters();
                if (attr != null && method.ReturnType == typeof(void) && par.Length == 1 && par[0].ParameterType == typeof(string[]))
                {
                    m_commands[attr.DisplayName] = method.CreateDelegate<Action<string[]>>();
                }
            }
        }

        public static bool Process(string msg, ulong author)
        {
            m_commandAuthor = author;
            var parts = msg.Split(m_separators, StringSplitOptions.RemoveEmptyEntries);
            Action<string[]> handler;
            if (parts.Length > 0 && m_commands.TryGetValue(parts[0], out handler))
            {
                handler(parts.Skip(1).ToArray());
                return true;
            }
            return false;
        }

        [DisplayName("+resetinv")]
        static void ResetInventory(string[] args)
        {
            foreach (var inventory in Replication.NetworkObjects.OfType<MyInventoryReplicable>().ToArray())
            {
                Replication.ResetForClients(inventory);
            }
        }
    }
}
#endif // !XB1
