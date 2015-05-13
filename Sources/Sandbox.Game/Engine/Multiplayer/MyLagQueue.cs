using Sandbox.Engine.Networking;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Engine.Multiplayer
{
    class MyLagQueue
    {
        struct Message
        {
            public DateTime Timestamp;
            public long Timestamp2;
            public byte[] Data;
            public int Length;

            public P2PMessageEnum SendType;
            public ulong UserId;
            public int Channel;
        }

        Stack<Message> m_messagePool = new Stack<Message>();
        Queue<Message> m_sendQueue = new Queue<Message>();

        Action<ulong, byte[], int, P2PMessageEnum, int> m_sendHandler;

        public MyLagQueue(Action<ulong, byte[], int, P2PMessageEnum, int> sendHandler)
        {
            m_sendHandler = sendHandler;
        }

        Message GetMessage()
        {
            if (m_messagePool.Count > 0)
            {
                return m_messagePool.Pop();
            }
            else
            {
                return new Message() { Data = new byte[128] };
            }
        }

        void ReturnMessage(Message msg)
        {
            m_messagePool.Push(msg);
        }

        public void Process(TimeSpan lag)
        {
            ProcessSend(lag);
        }

        public void EnqueueSend(ulong sendTo, byte[] data, int length, P2PMessageEnum sendType, int channel)
        {
            var msg = GetMessage();
            msg.Timestamp = DateTime.UtcNow;
            if (msg.Data.Length < length)
            {
                Array.Resize(ref msg.Data, length);
            }
            Array.Copy(data, 0, msg.Data, 0, length);
            msg.Length = length;
            msg.SendType = sendType;
            msg.UserId = sendTo;
            msg.Channel = channel;
            m_sendQueue.Enqueue(msg);
        }

        void ProcessSend(TimeSpan lag)
        {
            var now = DateTime.UtcNow;

            while (m_sendQueue.Count > 0 && (now - m_sendQueue.Peek().Timestamp) >= lag)
            {
                var msg = m_sendQueue.Dequeue();
                m_sendHandler(msg.UserId, msg.Data, (int)msg.Length, msg.SendType, msg.Channel);
                m_messagePool.Push(msg);
            }
        }
    }
}
