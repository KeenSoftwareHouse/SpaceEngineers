using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using VRage.Utils;
using VRage.Win32;
using MSG = VRage.Win32.WinApi.MSG;

namespace VRage.Utils
{
    public delegate void ActionRef<T>(ref T item);

    public static class MyMessageLoop
    {
        private static Dictionary<uint, ActionRef<Message>> m_messageDictionary;
        private static Queue<Message> m_messageQueue;
        private static List<Message> m_tmpMessages;

        static MyMessageLoop()
        {
            m_messageDictionary = new Dictionary<uint, ActionRef<Message>>();
            m_tmpMessages = new List<Message>();
            m_messageQueue = new Queue<Message>();
        }

        public static void Process()
        {
            lock (m_messageQueue)
            {
                m_tmpMessages.AddRange(m_messageQueue);
                m_messageQueue.Clear();
            }

            for (int i = 0; i < m_tmpMessages.Count; i++)
            {
                var msg = m_tmpMessages[i];
                ProcessMessage(ref msg);
            }

            m_tmpMessages.Clear();
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
                m_messageQueue.Enqueue(message);
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
}
