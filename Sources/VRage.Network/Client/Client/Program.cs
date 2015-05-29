using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VRage.Network;
using VRage.Plugins;

namespace Client
{
    class Program
    {
        public static Task<string> ReadConsoleAsync()
        {
            return Task.Factory.StartNew<string>(() => Console.ReadLine());
        }

        static MyRakNetClient m_client;
        static ushort m_port = 27005;

        private static bool IsRunning = true;

        private static Server.Foo foo = null;
        private static double lastD = 0;
        private static float lastF = 0;
        private static int lastI = 0;
        private static string lastStr = String.Empty;

        static void Main(string[] args)
        {
            MyPlugins.Load();

            var asyncInput = ReadConsoleAsync();
            Console.CancelKeyPress += delegate(object sender, ConsoleCancelEventArgs eventArgs)
            {
                eventArgs.Cancel = true;
                IsRunning = false;
            };

            for (int i = 0; i < 10; i++)
            {
                m_client = new MyRakNetClient((ulong)m_port);
                switch (m_client.Startup(m_port))
                {
                    case StartupResultEnum.SOCKET_PORT_ALREADY_IN_USE:
                        m_port++;
                        m_client.Dispose();
                        continue;
                    default:
                        i = 10;
                        break;
                }
            }

            new MyRakNetSyncLayer().LoadData(m_client, typeof(Program).Assembly);

            //MySyncedClass mySyncedObject = new MySyncedClass();
            //MySyncedFloatSNorm F = new MySyncedFloatSNorm();
            //mySyncedObject.Add(F);
            //MySyncedInt I = new MySyncedInt();
            //mySyncedObject.Add(I);

            //MySyncedClass myInnerSyncedObject = new MySyncedClass();
            //MySyncedVector3 V3 = new MySyncedVector3();
            //mySyncedObject.Add(V3);
            //mySyncedObject.Add(myInnerSyncedObject);
            //MyRakNetSyncLayer.RegisterSynced(mySyncedObject);

            MyRakNetSyncLayer.Static.OnEntityCreated += Static_OnEntityCreated;
            MyRakNetSyncLayer.Static.OnEntityDestroyed += Static_OnEntityDestroyed;

            RegisterEvents(m_client);
            var result = m_client.Connect("127.0.0.1", 27025);

            while (IsRunning)
            {
                if (asyncInput.IsCompleted)
                {
                    var cmd = asyncInput.Result;
                    if (!String.IsNullOrEmpty(cmd))
                    {
                        if (cmd == "quit")
                        {
                            IsRunning = false;
                        }
                        else
                        {
                            m_client.SendChatMessage(cmd);
                        }
                    }
                    asyncInput = ReadConsoleAsync();
                }

                m_client.Update();

                if (foo != null)
                {
                    if (lastD != foo.Position.Get().X)
                    {
                        for (int i = 0; i < (foo.Position.Get().X + 1) / 2 * 115; i++)
                        {
                            Console.Out.Write("|");
                        }
                        Console.Out.WriteLine();
                        lastD = foo.Position.Get().X;
                    }

                    if (lastStr != foo.Name.Get())
                    {
                        Console.WriteLine(foo.Name.Get());
                        lastStr = foo.Name.Get();
                    }
                }

                //if (lastF != F)
                //{
                //    for (int i = 0; i < (F + 1) / 2 * 115; i++)
                //    {
                //        Console.Out.Write("|");
                //    }
                //    Console.Out.WriteLine();
                //    lastF = F;
                //}

                //if (lastI != I)
                //{
                //    for (int i = 0; i < I % 116; i++)
                //    {
                //        Console.Out.Write("-");
                //    }
                //    Console.Out.WriteLine();
                //    lastI = I;
                //}

                Thread.Sleep(16);
            }
            m_client.Dispose();
            MyRakNetSyncLayer.Static.UnloadData();
            MyPlugins.Unload();
        }

        static void Static_OnEntityCreated(object obj)
        {
            //Debug.Assert(foo == null);
            Console.Out.WriteLine("EntityCreated {0}", obj);
            foo = (Server.Foo)obj;
            lastD = double.NaN;
            lastStr = string.Empty;
        }

        static void Static_OnEntityDestroyed(object obj)
        {
            Console.Out.WriteLine("EntityDestroyed {0}", obj);
            if (foo != null)
            {
                if (foo == obj)
                {
                    foo = null;
                    lastD = double.NaN;
                    lastStr = string.Empty;
                }
            }
        }

        private static void RegisterEvents(MyRakNetClient client)
        {
            client.OnAlreadyConnected += client_OnAlreadyConnected;
            client.OnChatMessage += client_OnChatMessage;
            client.OnClientJoined += client_OnClientJoined;
            client.OnClientLeft += client_OnClientLeft;
            client.OnConnectionAttemptFailed += client_OnConnectionAttemptFailed;
            client.OnConnectionBanned += client_OnConnectionBanned;
            client.OnConnectionLost += client_OnConnectionLost;
            client.OnDisconnectionNotification += client_OnDisconnectionNotification;
            client.OnInvalidPassword += client_OnInvalidPassword;
            client.OnModListRecieved += client_OnModListRecieved;
            client.OnStateDataDownloadProgress += client_OnStateDataDownloadProgress;
        }

        static void client_OnStateDataDownloadProgress(uint progress, uint total, uint partLength)
        {
            Console.Out.WriteLine("StateDataDownloadProgress {0},{1},{2}", progress, total, partLength);
        }

        static void client_OnModListRecieved(List<ulong> mods)
        {
            Console.Out.WriteLine("ModListRecieved {0}", mods.Count);
            foreach (var mod in mods)
            {
                Console.Out.WriteLine("\t{0}", mod);
            }
        }

        static void client_OnInvalidPassword()
        {
            throw new NotImplementedException();
        }

        static void client_OnDisconnectionNotification()
        {
            Console.Out.WriteLine("DisconnectionNotification");
        }

        static void client_OnConnectionLost(ulong steamID)
        {
            Console.Out.WriteLine("ConnectionLost {0}", steamID);
        }

        static void client_OnConnectionBanned()
        {
            throw new NotImplementedException();
        }

        static void client_OnConnectionAttemptFailed()
        {
            Console.Out.WriteLine("ConnectionAttemptFailed");
        }

        static void client_OnClientLeft(ulong steamID)
        {
            Console.Out.WriteLine("ClientLeft {0}", steamID);
        }

        static void client_OnClientJoined(ulong steamID)
        {
            Console.Out.WriteLine("ClientJoined {0}", steamID);
        }

        static void client_OnChatMessage(ulong steamID, string message)
        {
            Console.Out.WriteLine("ChatMessage {0}: {1}", steamID, message);
        }

        static void client_OnAlreadyConnected()
        {
            Console.Out.WriteLine("AlreadyConnected");
        }
    }
}
