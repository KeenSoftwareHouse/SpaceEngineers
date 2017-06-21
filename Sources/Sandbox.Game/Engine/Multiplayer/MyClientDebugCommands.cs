#if !XB1
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Replication;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using VRage;
using VRage.Network;
using VRageMath;

namespace Sandbox.Engine.Multiplayer
{
    [PreloadRequired]
    public class MyClientDebugCommands
    {
        static char[] m_separators = new char[] { ' ', '\r', '\n' };
        static Dictionary<string, Action<string[]>> m_commands = new Dictionary<string, Action<string[]>>(StringComparer.InvariantCultureIgnoreCase);
        static ulong m_commandAuthor;

        static MyClientDebugCommands()
        {
            foreach (var method in typeof(MyClientDebugCommands).GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
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

        [DisplayName("+stress")]
        static void StressTest(string[] args)
        {
            if (args.Length > 1)
            {
                if (args[0] == MySession.Static.LocalHumanPlayer.DisplayName || args[0] == "all" || args[0] == "clients")
                {
                    if (args.Length > 3)
                    {
                        MyReplicationClient.StressSleep.X = Convert.ToInt32(args[1]);
                        MyReplicationClient.StressSleep.Y = Convert.ToInt32(args[2]);
                        MyReplicationClient.StressSleep.Z = Convert.ToInt32(args[3]);
                    }
                    else if (args.Length > 2)
                    {
                        MyReplicationClient.StressSleep.X = Convert.ToInt32(args[1]);
                        MyReplicationClient.StressSleep.Y = Convert.ToInt32(args[2]);
                        MyReplicationClient.StressSleep.Z = 0;
                    }
                    else
                    {
                        MyReplicationClient.StressSleep.Y = Convert.ToInt32(args[1]);
                        MyReplicationClient.StressSleep.X = MyReplicationClient.StressSleep.Y;
                        MyReplicationClient.StressSleep.Z = 0;
                    }
                }
            }
            else
            {
                MyReplicationClient.StressSleep.X = 0;
                MyReplicationClient.StressSleep.Y = 0;
            }
        }

        [DisplayName("+resetplayers")]
        static void ResetPlayers(string[] args)
        {         
            ((MyReplicationClient)MyMultiplayer.Static.ReplicationLayer).ResetClientTimes();

            MySpectator.Static.Position = MySpectator.Static.ThirdPersonCameraDelta;
            MySpectator.Static.Target = Vector3D.Zero;
            MySession.Static.SetCameraController(VRage.Game.MyCameraControllerEnum.SpectatorDelta);

            if (MyEntities.GetEntities().Count > 0)
                MySpectatorCameraController.Static.TrackedEntity = MyEntities.GetEntities().First().EntityId;
        }      

    }
}
#endif // !XB1
