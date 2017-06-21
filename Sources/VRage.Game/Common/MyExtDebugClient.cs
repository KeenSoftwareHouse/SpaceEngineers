#if !XB1
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using VRage.Collections;
using VRage.Game.SessionComponents;
using VRage.Utils;

namespace VRage.Game.Common
{
    /// <summary>
    /// Auto-debug client.
    /// </summary>
    public class MyExtDebugClient : IDisposable
    {
        // ------------------------------------------------------------------------------------
        // public members

        public const int GameDebugPort = MySessionComponentExtDebug.GameDebugPort;

        public delegate void ReceivedMsgHandler(MyExternalDebugStructures.CommonMsgHeader messageHeader, IntPtr messageData);

        // Event - received message. Take care - called from background thread!
        public event ReceivedMsgHandler ReceivedMsg
        {
            add
            {
                if (!m_receivedMsgHandlers.Contains(value))
                {
                    m_receivedMsgHandlers.Add(value);
                    m_receivedMsgHandlers.ApplyAdditions();
                }
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

        public bool ConnectedToGame
        {
            get { return m_client != null && m_client.Connected; }
        }

        // ------------------------------------------------------------------------------------
        // private members

        // Maximum message size. 10 KB limit... for now.
        private const int MsgSizeLimit = 1024 * 10;

        // Instance of TCP client.
        private TcpClient m_client;
        // Buffer for receiving. 10 KB limit... for now.
        private readonly byte[] m_arrayBuffer = new byte[MsgSizeLimit];
        // Temporary receive buffer. 10 KB limit... for now.
        private IntPtr m_tempBuffer;
        // TCP client thread (for receiving messages).
        private Thread m_clientThread;
        private bool m_finished;
        // Array of handlers (callback methods).
        private readonly ConcurrentCachingList<ReceivedMsgHandler> m_receivedMsgHandlers = new ConcurrentCachingList<ReceivedMsgHandler>();

        // ------------------------------------------------------------------------------------

        public MyExtDebugClient()
        {
            m_tempBuffer = Marshal.AllocHGlobal(MsgSizeLimit);
            m_finished = false;
            m_clientThread = new Thread(ClientThreadProc) { IsBackground = true };
            m_clientThread.Start();
        }

        public void Dispose()
        {
            m_finished = true;
            if (m_client != null)
            {
                m_client.Client.Disconnect(false);
                m_client.Close();
            }
            Marshal.FreeHGlobal(m_tempBuffer);
        }

        private void ClientThreadProc()
        {
            while (!m_finished)
            {
                if (m_client == null || m_client.Client == null || !m_client.Connected)
                {
                    var result = MyTryConnectHelper.TryConnect(IPAddress.Loopback.ToString(), GameDebugPort);
                    if (!result)
                    {
                        Thread.Sleep(2500);
                        continue;
                    }

                    try
                    {
                        m_client = new TcpClient();
                        m_client.Connect(IPAddress.Loopback, GameDebugPort);
                    }
                    catch (Exception)
                    {
                        // just try to connect all the time
                    }

                    if (m_client == null || m_client.Client == null || !m_client.Connected)
                    {
                        Thread.Sleep(2500);
                        continue;
                    }
                }

                try
                {
                    if (m_client.Client == null)
                        continue;

                    if (m_client.Client.Receive(m_arrayBuffer, 0, MyExternalDebugStructures.MsgHeaderSize,
                        SocketFlags.None) == 0)
                    {
                        m_client.Client.Close();
                        m_client.Client = null;
                        m_client = null;
                    }
                    else
                    {
                        Marshal.Copy(m_arrayBuffer, 0, m_tempBuffer, MyExternalDebugStructures.MsgHeaderSize);
                        MyExternalDebugStructures.CommonMsgHeader header = (MyExternalDebugStructures.CommonMsgHeader)
                            Marshal.PtrToStructure(m_tempBuffer, typeof(MyExternalDebugStructures.CommonMsgHeader));
                        if (header.IsValid)
                        {
                            m_client.Client.Receive(m_arrayBuffer, header.MsgSize, SocketFlags.None);
                            if (m_receivedMsgHandlers != null)
                            {
                                Marshal.Copy(m_arrayBuffer, 0, m_tempBuffer, header.MsgSize);
                                // callback
                                foreach (var handler in m_receivedMsgHandlers)
                                    if (handler != null)
                                        handler(header, m_tempBuffer);
                            }
                        }
                    }
                }
                catch (SocketException)
                {
                    if (m_client.Client != null)
                    {
                        m_client.Client.Close();
                        m_client.Client = null;
                        m_client = null;
                    }
                }
                catch (ObjectDisposedException)
                {
                    if (m_client.Client != null)
                    {
                        m_client.Client.Close();
                        m_client.Client = null;
                        m_client = null;
                    }
                }
                catch (Exception)
                {
                    // ignore invalid message - TODO: do something smarter
                }
            }
        }

        public bool SendMessageToGame<TMessage>(TMessage msg) where TMessage : MyExternalDebugStructures.IExternalDebugMsg
        {
            if (m_client == null || m_client.Client == null || !m_client.Connected)
                return false;

            int messageDataSize = Marshal.SizeOf(typeof(TMessage));
            MyExternalDebugStructures.CommonMsgHeader msgHeader = MyExternalDebugStructures.CommonMsgHeader.Create(msg.GetTypeStr(), messageDataSize);
            Marshal.StructureToPtr(msgHeader, m_tempBuffer, true);
            Marshal.Copy(m_tempBuffer, m_arrayBuffer, 0, MyExternalDebugStructures.MsgHeaderSize);
            Marshal.StructureToPtr(msg, m_tempBuffer, true);
            Marshal.Copy(m_tempBuffer, m_arrayBuffer, MyExternalDebugStructures.MsgHeaderSize, messageDataSize);
            try
            {
                m_client.Client.Send(m_arrayBuffer, 0, MyExternalDebugStructures.MsgHeaderSize + messageDataSize, SocketFlags.None);
            }
            catch (SocketException)
            {
                return false;
            }
            return true;
        }
    }
}
#endif // !XB1
