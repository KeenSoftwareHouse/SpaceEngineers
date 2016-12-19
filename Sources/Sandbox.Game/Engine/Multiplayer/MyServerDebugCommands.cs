#if !XB1
using Sandbox.Game.Entities;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Replication;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using VRage.Game.Entity;
using VRage.Network;
using VRageMath;

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

        [DisplayName("+stress")]
        static void StressTest(string[] args)
        {
            if (args.Length > 1)
            {
                if ((args[0] == "server") || (args[0] == "all"))
                {
                    if (args.Length > 3)
                    {
                        MyReplicationServer.StressSleep.X = Convert.ToInt32(args[1]);
                        MyReplicationServer.StressSleep.Y = Convert.ToInt32(args[2]);
                        MyReplicationServer.StressSleep.Z = Convert.ToInt32(args[3]);
                    }
                    else if (args.Length > 2)
                    {
                        MyReplicationServer.StressSleep.X = Convert.ToInt32(args[1]);
                        MyReplicationServer.StressSleep.Y = Convert.ToInt32(args[2]);
                        MyReplicationServer.StressSleep.Z = 0;
                    }
                    else
                    {
                        MyReplicationServer.StressSleep.X = Convert.ToInt32(args[1]);
                        MyReplicationServer.StressSleep.Y = MyReplicationServer.StressSleep.X;
                        MyReplicationServer.StressSleep.Z = 0;
                    }
                }
            }
            else
            {
                MyReplicationServer.StressSleep.X = 0;
                MyReplicationServer.StressSleep.Y = 0;
            }
        }

        [DisplayName("+dump")]
        static void Dump(string[] args)
        {
            MySession.InitiateDump();
        }

        [DisplayName("+save")]
        static void Save(string[] args)
        {
            MySession.Static.Save();
        }

        [DisplayName("+unban")]
        static void Unban(string[] args)
        {
            if (args.Length > 0)
            {
                ulong user = 0;
                if (ulong.TryParse(args[0], out user))
                {
                    MyMultiplayer.Static.BanClient(user, false);
                }
            }
        }

        [DisplayName("+resetplayers")]
        static void ResetPlayers(string[] args)
        {
            Vector3D pos = Vector3D.Zero;
            //foreach (var controledEntity in Sync.Players.ControlledEntities)
            //{
            //    MyEntity entity;
            //    if (MyEntities.TryGetEntityById(controledEntity.Key, out entity))
            //    {
            //        pos.X += ce * 20;
            //        entity.PositionComp.SetPosition(pos);
            //        ce++;
            //    }
            //}

            foreach (var entity in MyEntities.GetEntities())
            {
                MatrixD worldMatrix = MatrixD.CreateTranslation(pos);
                entity.PositionComp.SetWorldMatrix(worldMatrix);
                entity.Physics.LinearVelocity = Vector3D.Forward;
                pos.X += 50;
            }
        }

        [DisplayName("+forcereorder")]
        static void ForceReorder(string[] args)
        {
            Physics.MyPhysics.ForceClustersReorder();
        }
    }
}
#endif // !XB1
