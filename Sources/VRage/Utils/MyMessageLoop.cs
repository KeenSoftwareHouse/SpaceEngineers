using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using VRage.Utils;
using VRage.Win32;
#if !XB1
using System.Windows.Forms;
using MSG = VRage.Win32.WinApi.MSG;
#endif

namespace VRage.Utils
{
    public delegate void ActionRef<T>(ref T item);
#if !XB1
    public static class MyMessageLoop
    {

        [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
        public static extern void CopyMemory(IntPtr dest, IntPtr src, uint count);

        private static Dictionary<uint, ActionRef<Message>> m_messageDictionary;
        private static Queue<Message> m_messageQueue;
        private static Queue<WinApi.MyCopyData> m_messageCopyDataQueue;
        private static List<Message> m_tmpMessages;
        private static List<WinApi.MyCopyData> m_tmpCopyData;

        static MyMessageLoop()
        {
            const int initialQueueCapacity = 64;
            m_messageDictionary = new Dictionary<uint, ActionRef<Message>>();
            m_tmpMessages = new List<Message>(initialQueueCapacity);
            m_tmpCopyData = new List<WinApi.MyCopyData>(initialQueueCapacity);
            m_messageQueue = new Queue<Message>(initialQueueCapacity);
            m_messageCopyDataQueue = new Queue<WinApi.MyCopyData>(initialQueueCapacity);
        }

        public static void Process()
        {
            lock (m_messageQueue)
            {
                m_tmpMessages.AddRange(m_messageQueue);
                m_tmpCopyData.AddRange(m_messageCopyDataQueue);
                m_messageQueue.Clear();
                m_messageCopyDataQueue.Clear();
            }

            int tmpCopyDataIndex = 0;
            for (int i = 0; i < m_tmpMessages.Count; i++)
            {
                var msg = m_tmpMessages[i];
                if (msg.Msg != MyWMCodes.COPYDATA)
                {
                    ProcessMessage(ref msg);    // normal message, no local copies
                }
                else if (tmpCopyDataIndex < m_tmpCopyData.Count)  // copy data message, special handling
                {
                    var copyDataStruct = m_tmpCopyData[tmpCopyDataIndex++];
                    unsafe
                    {
                        void* ptr = &copyDataStruct;
                        msg.LParam = (IntPtr) ptr;
                        ProcessMessage(ref msg);
                    }
                    Marshal.FreeHGlobal(copyDataStruct.DataPointer); // free our local copy of data
                }
            }

            m_tmpMessages.Clear();
            m_tmpCopyData.Clear();
        }

        public static void AddMessageHandler(uint wmCode, ActionRef<Message> messageHandler)
        {
            if (m_messageDictionary.ContainsKey(wmCode))
                m_messageDictionary[wmCode] += messageHandler;
            else
                m_messageDictionary.Add(wmCode, messageHandler);
        }

        public static void AddMessageHandler(WinApi.WM wmCode, ActionRef<Message> messageHandler)
        {
            AddMessageHandler((uint)wmCode, messageHandler);
        }

        public static void RemoveMessageHandler(uint wmCode, ActionRef<Message> messageHandler)
        {
            if (m_messageDictionary.ContainsKey(wmCode))
                m_messageDictionary[wmCode] -= messageHandler;
        }

        public static void RemoveMessageHandler(WinApi.WM wmCode, ActionRef<Message> messageHandler)
        {
            RemoveMessageHandler((uint)wmCode, messageHandler);
        }

        public static void AddMessage(ref Message message)
        {
            lock (m_messageQueue)
            {
                if (message.Msg == MyWMCodes.COPYDATA)
                {
                    WinApi.MyCopyData copyData = (WinApi.MyCopyData)message.GetLParam(typeof(WinApi.MyCopyData));
                    IntPtr dataPointerCopy = Marshal.AllocHGlobal(copyData.DataSize);
                    CopyMemory(dataPointerCopy, copyData.DataPointer, (uint)copyData.DataSize);
                    copyData.DataPointer = dataPointerCopy;
                    m_messageCopyDataQueue.Enqueue(copyData);
                }
                m_messageQueue.Enqueue(message);
            }
        }

        public static void ClearMessageQueue()
        {
            lock (m_messageQueue)
                m_messageQueue.Clear();
        }

        private static void ProcessMessage(ref Message message)
        {
            ActionRef<Message> output = null;
            m_messageDictionary.TryGetValue((uint)message.Msg, out output);
            if (output != null)
                output(ref message);
        }
    }

#endif
}
