using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VRage.Network;
using VRage.Plugins;
using VRageMath;

namespace Server
{
    class Program
    {
        public static Task<string> ReadConsoleAsync()
        {
            return Task.Factory.StartNew<string>(() => Console.ReadLine());
        }

        static MyRakNetServer m_server;

        private static bool IsRunning = true;

        static void Main(string[] args)
        {
            MyPlugins.Load();

            var asyncInput = ReadConsoleAsync();
            Console.CancelKeyPress += delegate(object sender, ConsoleCancelEventArgs eventArgs)
            {
                eventArgs.Cancel = true;
                IsRunning = false;
            };

            m_server = new MyRakNetServer(0);

            m_server.Startup(32, 27025, null);

            new MyRakNetSyncLayer().LoadData(m_server, typeof(Program).Assembly);

            //for (ulong i = 1; i < 512; i++)
            //{
            //    var eek = new Foo();
            //    MyRakNetSyncLayer.Replicate(eek);
            //}

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

            RegisterEvents(m_server);

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            Foo foo = null;

            bool sin = false;
            bool str = false;

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
                        else if (cmd == "+")
                        {
                            foo = new Foo();
                            foo.EntityID = 1337;
                            MyRakNetSyncLayer.Replicate(foo);
                        }
                        else if (cmd == "-")
                        {
                            if (foo != null)
                            {
                                MyRakNetSyncLayer.Destroy(foo);
                                foo = null;
                            }
                        }
                        else if (cmd == "sin")
                        {
                            if (foo != null)
                            {
                                sin = !sin;
                            }
                        }
                        else if (cmd == "str")
                        {
                            if (foo != null)
                            {
                                str = !str;
                            }
                        }
                        else
                        {
                            m_server.SendChatMessage(cmd);
                        }
                    }
                    asyncInput = ReadConsoleAsync();
                }

                if (foo != null)
                {
                    if (sin)
                    {
                        foo.Position.Set(new Vector3D(Math.Sin((double)stopWatch.ElapsedMilliseconds / 1000.0)));
                    }
                    if (str)
                    {
                        char c = (char)('a' + (byte)(stopWatch.ElapsedMilliseconds / 1000.0) % 26);
                        foo.Name.Set(String.Concat(Enumerable.Repeat(c, 100)));
                    }
                }

                //I.Set((int)(stopWatch.ElapsedMilliseconds / 1000.0));

                //F.Set((float)Math.Sin((double)stopWatch.ElapsedMilliseconds / 1000.0));
                //I.Set((int)(stopWatch.ElapsedMilliseconds/10));
                //V3.Set(new Vector3(F, F, F));

                m_server.Update();
                MyRakNetSyncLayer.Static.Update();

                //Console.Out.WriteLine(m_server.GetStatsToString());

                Thread.Sleep(16);
            }
            m_server.Dispose();
            MyRakNetSyncLayer.Static.UnloadData();
            MyPlugins.Unload();
        }

        private static void RegisterEvents(MyRakNetServer server)
        {
            server.OnChatMessage += server_OnChatMessage;
            server.OnClientJoined += server_OnClientJoined;
            server.OnClientLeft += server_OnClientLeft;
            server.OnClientReady += server_OnClientReady;
            server.OnConnectionLost += server_OnConnectionLost;
            server.OnRequestStateData += server_OnRequestStateData;
        }

        static void server_OnRequestStateData(ulong steamID)
        {
            Console.Out.WriteLine("RequestStateData {0}", steamID);
        }

        static void server_OnConnectionLost(ulong steamID)
        {
            Console.Out.WriteLine("ConnectionLost {0}", steamID);
        }

        static void server_OnClientReady(ulong steamID)
        {
            Console.Out.WriteLine("ClientReady {0}", steamID);
        }

        static void server_OnClientLeft(ulong steamID)
        {
            Console.Out.WriteLine("ClientLeft {0}", steamID);
        }

        static void server_OnClientJoined(ulong steamID)
        {
            Console.Out.WriteLine("ClientJoined {0}", steamID);
        }

        static void server_OnChatMessage(ulong steamID, string message)
        {
            Console.Out.WriteLine("ChatMessage {0}: {1}", steamID, message);
        }
    }
}
