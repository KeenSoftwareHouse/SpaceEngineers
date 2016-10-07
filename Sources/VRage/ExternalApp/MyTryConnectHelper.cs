#if !XB1
using System;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;

namespace VRage
{
    public class MyTryConnectHelper
    {
        [DllImport("ws2_32.dll", SetLastError = true)]
        internal static extern int WSAConnect(
        [In] IntPtr socketHandle,
        [In] byte[] socketAddress,
        [In] int socketAddressSize,
        [In] IntPtr inBuffer,
        [In] IntPtr outBuffer,
        [In] IntPtr sQOS,
        [In] IntPtr gQOS
        );
        [DllImport("ws2_32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern IntPtr WSASocket(
        [In] AddressFamily addressFamily,
        [In] SocketType socketType,
        [In] ProtocolType protocolType,
        [In] IntPtr protocolInfo,
        [In] uint group,
        [In] int flags
        );
        [DllImport("ws2_32.dll", ExactSpelling = true, SetLastError = true)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal static extern int closesocket(
        [In] IntPtr socketHandle
        );
        [DllImport("ws2_32.dll", CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, SetLastError = true)]
        internal static extern int WSAStartup(
        [In] short wVersionRequested,
        [Out] out WSAData lpWSAData
        );
        [StructLayout(LayoutKind.Sequential)]
        internal struct WSAData
        {
            internal short wVersion;
            internal short wHighVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 257)]
            internal string szDescription;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 129)]
            internal string szSystemStatus;
            internal short iMaxSockets;
            internal short iMaxUdpDg;
            internal IntPtr lpVendorInfo;
        }

        
        static bool Initialized;
        static FieldInfo m_Buffer;
        public static bool TryConnect(string ipString, int port)
        {
            if (!Initialized)
            {
                var wsaData = new WSAData();
                if (WSAStartup(0x0202, out wsaData) != 0) return false;
                m_Buffer = typeof(SocketAddress).GetField("m_Buffer", (BindingFlags.Instance | BindingFlags.NonPublic));
                Initialized = true;
            }
            IPAddress address;
            if (!IPAddress.TryParse(ipString, out address)) return false;
            if (!((port >= 0) && (port <= 0xffff))) return false;
            var remoteEP = new IPEndPoint(address, port);
            SocketAddress socketAddress = remoteEP.Serialize();
            IntPtr m_Handle = WSASocket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp, IntPtr.Zero, 0, 1 /*overlapped*/);
            if (m_Handle == new IntPtr(-1)) return false;
            new SocketPermission(NetworkAccess.Connect, TransportType.Tcp, remoteEP.Address.ToString(), remoteEP.Port).Demand();
            var buf = (byte[])m_Buffer.GetValue(socketAddress);
            bool result = (WSAConnect(m_Handle, buf, socketAddress.Size, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero) == 0);
            closesocket(m_Handle);
            return result;
        }
    }
}
#endif // !XB1
