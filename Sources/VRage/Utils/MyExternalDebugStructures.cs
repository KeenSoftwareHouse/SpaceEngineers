using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using VRage.Win32;

namespace VRage.Utils
{
    public static class MyExternalDebugStructures
    {
        public interface IExternalDebugMsg
        {
            // Getter of message type name.
            string GetTypeStr();
        }

        public static readonly int MsgHeaderSize = Marshal.SizeOf(typeof(CommonMsgHeader));

        /// <summary>
        /// Convert from raw data to message.
        /// Message must be struct with sequential layout having first field "Header" of type "CommonMsg".
        /// </summary>
        public static bool ReadMessageFromPtr<TMessage>(ref CommonMsgHeader header, IntPtr data, out TMessage outMsg) where TMessage : IExternalDebugMsg
        {
#if XB1
            System.Diagnostics.Debug.Assert(false);
            //TODO:
            var tm = new TMessage[1]; outMsg = tm[0];//THIS IS HERE JUST TO RETURN SOMETHING FOR NOW!
            return true;
#else // !XB1
            outMsg = default(TMessage);
            if (data == IntPtr.Zero ||
                header.MsgSize != Marshal.SizeOf(typeof(TMessage)) ||
                header.MsgType != outMsg.GetTypeStr())
            {
                return false;
            }
            outMsg = (TMessage)Marshal.PtrToStructure(data, typeof(TMessage));
            return true;
#endif // !XB1
        }

        // Basic message, server as message header in derived messages.
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct CommonMsgHeader
        {
            // Message header
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 8)]
            public string MsgHeader;
            // Message type
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 8)]
            public string MsgType;
            [MarshalAs(UnmanagedType.I4)]
            public int MsgSize;

            public static CommonMsgHeader Create(string msgType, int msgSize = 0)
            {
                System.Diagnostics.Debug.Assert(msgType != null && msgType.Length <= 8);
                CommonMsgHeader rtnCommonMsgHeader = new CommonMsgHeader
                {
                    MsgHeader = "VRAGEMS",
                    MsgType = msgType,
                    MsgSize = msgSize
                };
                return rtnCommonMsgHeader;
            }

            public bool IsValid
            {
                get { return MsgHeader == "VRAGEMS" && MsgSize > 0; }
            }
        }

        // -----------------------------------------------------------------------------
        // DERIVED MESSAGES
        // -----------------------------------------------------------------------------

        // game -> editor, selected tree name
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct SelectedTreeMsg : IExternalDebugMsg
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 40)]
            public string BehaviorTreeName;

            string IExternalDebugMsg.GetTypeStr() { return "SELTREE"; }
        }

        // game -> editor, AC: name of selected animation controller
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct ACConnectToEditorMsg : IExternalDebugMsg
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 40)]
            public string ACName;

            string IExternalDebugMsg.GetTypeStr() { return "AC_CON"; }
        }

        // game -> editor, AC: address of current state node
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct ACSendStateToEditorMsg : IExternalDebugMsg
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 240)]
            public string CurrentNodeAddress;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
            public int[] VisitedTreeNodesPath;

            public static ACSendStateToEditorMsg Create(string currentNodeAddress, int[] visitedTreeNodesPath)
            {
                ACSendStateToEditorMsg rtnMsg = new ACSendStateToEditorMsg()
                {
                    CurrentNodeAddress = currentNodeAddress,
                    VisitedTreeNodesPath = new int[64]
                };
                if (visitedTreeNodesPath != null)
                    Array.Copy(visitedTreeNodesPath, rtnMsg.VisitedTreeNodesPath, Math.Min(visitedTreeNodesPath.Length, 64));
                return rtnMsg;
            }

            string IExternalDebugMsg.GetTypeStr() { return "AC_STA"; }
        }

        // editor -> game, AC: name of selected animation controller
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct ACReloadInGameMsg : IExternalDebugMsg
        {
            // subtype name
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 40)]
            public string ACName;
            // file path
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1024)]
            public string ACAddress;
            // file path
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1024)]
            public string ACContentAddress;

            string IExternalDebugMsg.GetTypeStr() { return "AC_LOAD"; }
        }
    }
}
