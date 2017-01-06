#if !XB1
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Profiler;
using VRage.Utils;

namespace VRage.Game.SessionComponents
{
    /// <summary>
    /// Communication between game and editor.
    /// </summary>
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class MySessionComponentExtDebug : MySessionComponentBase
    {
        private class MyDebugClientInfo
        {
            public TcpClient TcpClient;
            public MyExternalDebugStructures.CommonMsgHeader LastHeader;
        }
        // ------------------------------------------------------------------------------------
        // public members

        public delegate void ReceivedMsgHandler(MyExternalDebugStructures.CommonMsgHeader messageHeader, IntPtr messageData);
        // Event - received message.
        public event ReceivedMsgHandler ReceivedMsg
        {
            add
            {
                m_receivedMsgHandlers.Add(value);
                m_receivedMsgHandlers.ApplyAdditions();
            }
            remove
            {
                if (m_receivedMsgHandlers.Contains(value))
                {
                    m_receivedMsgHandlers.Remove(value);
                    m_receivedMsgHandlers.ApplyRemovals();
                }
            }
        }

        public bool IsHandlerRegistered(ReceivedMsgHandler handler)
        {
            return m_receivedMsgHandlers.Contains(handler);
        }

        // Static reference to this session component.
        public static MySessionComponentExtDebug Static = null;
        public static bool ForceDisable = false;
        // Static reference to this session component.
        public const int GameDebugPort = 13000;
        // Is any client connected?
        public bool HasClients
        {
            get { return m_clients.Count > 0; }
        }

        // ------------------------------------------------------------------------------------
        // private members

        // Maximum message size. 10 KB limit... for now.
        private const int MsgSizeLimit = 1024 * 10;

        // Thread for TCP listener.
        private Thread m_listenerThread;
        // Tcp listener instance.
        private TcpListener m_listener;
        // Connected tcp clients.
        private ConcurrentCachingList<MyDebugClientInfo> m_clients = new ConcurrentCachingList<MyDebugClientInfo>(1);
        // Is component active (listening)?
        private bool m_active = false;
        // Buffer for receiving. 10 KB limit... for now.
        private byte[] m_arrayBuffer = new byte[MsgSizeLimit];
        // Temporary receive buffer. 10 KB limit... for now.
        private IntPtr m_tempBuffer;

        // Array of handlers (callback methods).
        private ConcurrentCachingList<ReceivedMsgHandler> m_receivedMsgHandlers = new ConcurrentCachingList<ReceivedMsgHandler>();

        // ------------------------------------------------------------------------------------
        
        public override void LoadData()
        {
            if (Static != null)
            {
                // take data from previous instance
                m_listenerThread = Static.m_listenerThread;
                m_listener = Static.m_listener;
                m_clients = Static.m_clients;
                m_active = Static.m_active;
                m_arrayBuffer = Static.m_arrayBuffer;
                m_tempBuffer = Static.m_tempBuffer;
                m_receivedMsgHandlers = Static.m_receivedMsgHandlers;
                MySessionComponentExtDebug.Static = this;
                base.LoadData();
                return;
            }

            MySessionComponentExtDebug.Static = this;
            if (m_tempBuffer == IntPtr.Zero)
                m_tempBuffer = Marshal.AllocHGlobal(MsgSizeLimit);

            if (!ForceDisable)
                StartServer();

            base.LoadData();
        }

        protected override void UnloadData()
        {
            m_receivedMsgHandlers.ClearImmediate();
            base.UnloadData();
        }

        public void Dispose()
        {
            m_receivedMsgHandlers.ClearList();
            if (m_active)
            {
                StopServer();   // do not stop server
            }
            if (m_tempBuffer != IntPtr.Zero)
                Marshal.FreeHGlobal(m_tempBuffer);
        }

        /// <summary>
        /// Start using this component as server (game side).
        /// </summary>
        private bool StartServer()
        {
            if (!m_active)
            {
                m_listenerThread = new Thread(ServerListenerProc) { IsBackground = true};
                m_listenerThread.Start();
                m_active = true;
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Stop the server on the game side. Called automatically.
        /// </summary>
        private void StopServer()
        {
            if (m_active && m_listenerThread != null)
            {
                m_listener.Stop();
                //m_listenerThread.Interrupt(); // will stop itself
                foreach (var client in m_clients)
                {
                    if (client.TcpClient != null)
                    {
                        client.TcpClient.Client.Disconnect(true);
                        client.TcpClient.Close();
                    }
                }
                m_clients.ClearImmediate();
                m_active = false;
            }
        }

        /// <summary>
        /// Parallel thread - listener.
        /// </summary>
        private void ServerListenerProc()
        {
            Thread.CurrentThread.Name = "External Debugging Listener";
            ProfilerShort.Autocommit = false;

            try
            {
#if OFFICIAL_BUILD == true
            m_listener = new TcpListener(IPAddress.Loopback, GameDebugPort) {ExclusiveAddressUse = false};
#else
            m_listener = new TcpListener(IPAddress.Any, GameDebugPort) { ExclusiveAddressUse = false };
#endif
                m_listener.Start();
            }
            catch (SocketException ex)
            {
                MyLog.Default.WriteLine("Cannot start debug listener.");
                MyLog.Default.WriteLine(ex);
                m_listener = null;
                m_active = false;
                return;
            }
            MyLog.Default.WriteLine("External debugger: listening...");
            while (true)
            {
                try
                {
                    var client = m_listener.AcceptTcpClient();
                    client.Client.Blocking = true;
                    MyLog.Default.WriteLine("External debugger: accepted client.");
                    m_clients.Add(new MyDebugClientInfo()
                    {
                        TcpClient = client,
                        LastHeader = MyExternalDebugStructures.CommonMsgHeader.Create("UNKNOWN")
                    });
                    m_clients.ApplyAdditions();
                }
                catch (SocketException e)
                {
                    if (e.SocketErrorCode == SocketError.Interrupted)
                    {
                        m_listener.Stop();
                        m_listener = null;
                        MyLog.Default.WriteLine("External debugger: interrupted.");
                        ProfilerShort.Commit();
                        ProfilerShort.DestroyThread();
                        return;
                    }
                    else
                    {
                        if (MyLog.Default != null && MyLog.Default.LogEnabled)
                            MyLog.Default.WriteLine(e);
                        break;
                    }
                }
            }
            m_listener.Stop();
            m_listener = null;
            ProfilerShort.Commit();
            ProfilerShort.DestroyThread();
        }

        // Read messages coming from clients.
        public override void UpdateBeforeSimulation()
        {
            ProfilerShort.Begin("External Debugging");
            foreach (var clientInfo in m_clients)
            {
                if (clientInfo == null || clientInfo.TcpClient == null || clientInfo.TcpClient.Client == null || !clientInfo.TcpClient.Connected)
                {
                    if (clientInfo != null && clientInfo.TcpClient != null && clientInfo.TcpClient.Client != null &&
                        clientInfo.TcpClient.Client.Connected)
                    {
                        clientInfo.TcpClient.Client.Disconnect(true);
                        clientInfo.TcpClient.Close();
                    }

                    m_clients.Remove(clientInfo);
                    continue;
                }
                if (clientInfo.TcpClient.Connected && clientInfo.TcpClient.Available > 0)
                    ReadMessagesFromClients(clientInfo);
            }
            m_clients.ApplyRemovals();
            ProfilerShort.End();
        }

        // Read messages coming from one client.
        private void ReadMessagesFromClients(MyDebugClientInfo clientInfo)
        {
            Socket socket = clientInfo.TcpClient.Client;

            while (socket.Available >= 0)
            {
                bool readAnything = false;
                // receive header
                if (!clientInfo.LastHeader.IsValid
                    && socket.Available >= MyExternalDebugStructures.MsgHeaderSize) // already checked ^
                {
                    socket.Receive(m_arrayBuffer, MyExternalDebugStructures.MsgHeaderSize, SocketFlags.None);
                    Marshal.Copy(m_arrayBuffer, 0, m_tempBuffer, MyExternalDebugStructures.MsgHeaderSize);
                    clientInfo.LastHeader =
                        (MyExternalDebugStructures.CommonMsgHeader)
                            Marshal.PtrToStructure(m_tempBuffer,
                                typeof (MyExternalDebugStructures.CommonMsgHeader));

                    readAnything = true;
                }

                // receive body (only if we received header!)
                if (clientInfo.LastHeader.IsValid &&
                    socket.Available >= clientInfo.LastHeader.MsgSize)
                {
                    socket.Receive(m_arrayBuffer, clientInfo.LastHeader.MsgSize, SocketFlags.None);
                    if (m_receivedMsgHandlers != null && m_receivedMsgHandlers.Count > 0)
                    {
                        Marshal.Copy(m_arrayBuffer, 0, m_tempBuffer, clientInfo.LastHeader.MsgSize);
                        // callback
                        foreach (var handler in m_receivedMsgHandlers)
                            if (handler != null)
                                handler(clientInfo.LastHeader, m_tempBuffer);
                    }

                    // erase header
                    clientInfo.LastHeader = default(MyExternalDebugStructures.CommonMsgHeader);
                    readAnything = true;
                }

                if (!readAnything)
                    break;
            }
        }

        // Send messages to all clients.
        public bool SendMessageToClients<TMessage>(TMessage msg) where TMessage : struct, MyExternalDebugStructures.IExternalDebugMsg
        {
            int messageDataSize = Marshal.SizeOf(typeof (TMessage));
            MyExternalDebugStructures.CommonMsgHeader msgHeader = MyExternalDebugStructures.CommonMsgHeader.Create(msg.GetTypeStr(), messageDataSize);
            Marshal.StructureToPtr(msgHeader, m_tempBuffer, true);
            Marshal.Copy(m_tempBuffer, m_arrayBuffer, 0, MyExternalDebugStructures.MsgHeaderSize);
            Marshal.StructureToPtr(msg, m_tempBuffer, true);
            Marshal.Copy(m_tempBuffer, m_arrayBuffer, MyExternalDebugStructures.MsgHeaderSize, messageDataSize);

            foreach (var clientInfo in m_clients)
            {
                try
                {
                    if (clientInfo.TcpClient.Client != null)
                        clientInfo.TcpClient.Client.Send(m_arrayBuffer, 0,
                            MyExternalDebugStructures.MsgHeaderSize + messageDataSize, SocketFlags.None);
                }
                catch (SocketException)
                {
                    clientInfo.TcpClient.Close();
                }
            }
            return true;
        }
    }
}
#endif // !XB1
